using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using System.Xml;

// MA3 / Mali Mühür
using GibDesktopApplication.Managers;                           // SmartCardManager
using tr.gov.tubitak.uekae.esya.api.asn.x509;                  // ECertificate
using tr.gov.tubitak.uekae.esya.api.common.crypto;             // BaseSigner
using tr.gov.tubitak.uekae.esya.api.common.util;               // LicenseUtil
using tr.gov.tubitak.uekae.esya.api.xmlsignature;              // XMLSignature, SignedDocument
using tr.gov.tubitak.uekae.esya.api.xmlsignature.config;       // Context, Config
using tr.gov.tubitak.uekae.esya.api.xmlsignature.document;     // InMemoryDocument, Document

using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GibDesktopApplication
{
    public partial class Form1 : Form
    {
        // State
        private string selectedFilePath = string.Empty;
        private string lastSignedXml = string.Empty;

        private string lastSignedXmlPath = string.Empty;   // İmzalanan XML'in diskteki yolu
        private string lastZipPath = string.Empty;         // Oluşturulan zip yolu
        private string lastZipBase64 = string.Empty;

        private string lastSoapXml = string.Empty;
        private ECertificate currentCertificate = null;    // Sertifikayı saklayalım
        private BaseSigner currentSigner = null;           // Signer'ı saklayalım

        public Form1()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                selectedFilePath = ofd.FileName;
                lblSelectedFile.Text = "Dosya: " + selectedFilePath;
                Log("Belge seçildi.");
                btnSign.Enabled = true; // belge seçilince imzala aç
            }
        }

        private void BtnSign_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedFilePath) || !File.Exists(selectedFilePath))
                {
                    MessageBox.Show("Lütfen imzalanacak belgeyi seçin.");
                    return;
                }

                var user = txtUsername.Text.Trim();
                var pass = txtPassword.Text;

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                {
                    MessageBox.Show("Servis kimlik bilgilerini doldurun.");
                    return;
                }

                progress.Style = ProgressBarStyle.Marquee;
                Log("İmzalama başlatıldı…");

                // === İMZALAMA KODU (MA3) ===
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 1) Lisans
                string licensePath = Path.Combine(baseDir, "lisans", "lisans.xml");
                if (!File.Exists(licensePath))
                {
                    MessageBox.Show($"Lisans dosyası bulunamadı: {licensePath}");
                    return;
                }
                using (FileStream lfs = new FileStream(licensePath, FileMode.Open, FileAccess.Read))
                    LicenseUtil.setLicenseXml(lfs);

                // 2) Kart & sertifika & signer (Mali Mühür)
                SmartCardManager scm = SmartCardManager.getInstance();
                ECertificate cert = scm.getSignatureCertificate(false, false);
                if (cert == null)
                {
                    MessageBox.Show("Kartta geçerli bir Mali Mühür sertifikası bulunamadı!");
                    return;
                }

                string pin = "060606"; // ÜRETİMDE: kullanıcıdan al
                BaseSigner signer = scm.getSigner(pin, cert);
                if (signer == null)
                {
                    MessageBox.Show("Signer oluşturulamadı!");
                    return;
                }

                // Sertifika ve signer'ı sakla (SOAP imzalama için)
                currentCertificate = cert;
                currentSigner = signer;

                // 3) Kaynak XML'i yükle
                var sysXml = new XmlDocument();
                sysXml.PreserveWhitespace = true;
                sysXml.Load(selectedFilePath);
                sysXml.Normalize();

                // 3.a) Kökte yanlış default ds namespace varsa temizle
                var defaultNsAttr = sysXml.DocumentElement?.GetAttributeNode("xmlns");
                if (defaultNsAttr != null && defaultNsAttr.Value == "http://www.w3.org/2000/09/xmldsig#")
                    sysXml.DocumentElement.Attributes.Remove(defaultNsAttr);

                // 3.b) Kök elemana Id ekle (yoksa)
                var root = sysXml.DocumentElement;
                if (root == null)
                {
                    MessageBox.Show("Geçersiz XML: kök düğüm bulunamadı.");
                    return;
                }

                // 4) (Opsiyonel) InMemoryDocument
                byte[] xmlBytes;
                using (var msSrc = new MemoryStream())
                {
                    sysXml.Save(msSrc);
                    xmlBytes = msSrc.ToArray();
                }
                Document memDoc = new InMemoryDocument(xmlBytes, selectedFilePath, "application/xml", "UTF-8");

                // 5) Context & Config (MA3)
                string configPath = Path.Combine(baseDir, "config", "xmlsignature-config.xml");
                if (!File.Exists(configPath))
                {
                    MessageBox.Show($"Config dosyası bulunamadı: {configPath}");
                    return;
                }
                Context context = new Context { Config = new Config(configPath) };

                // 6) İmzayı üret (rapor XML için)
                var signedDoc = new tr.gov.tubitak.uekae.esya.api.xmlsignature.SignedDocument(context);
                signedDoc.addXMLNode(root);
                var signature = signedDoc.createSignature();
                signature.addKeyInfo(cert);
                signature.sign(signer);

                // 7) İmzalı çıktıyı geçici DOM'a al
                var tmpSigned = new XmlDocument();
                tmpSigned.PreserveWhitespace = true;
                using (var msSigned = new MemoryStream())
                {
                    signedDoc.write(msSigned);
                    msSigned.Position = 0;
                    tmpSigned.Load(msSigned);
                }

                // 8) Signature düğümünü 'earsiv:baslik' içine taşı
                var nsmgr = new XmlNamespaceManager(sysXml.NameTable);
                nsmgr.AddNamespace("earsiv", "http://earsiv.efatura.gov.tr");

                var baslikNode = sysXml.SelectSingleNode("/*/earsiv:baslik", nsmgr) as XmlElement;
                if (baslikNode == null)
                {
                    MessageBox.Show("'earsiv:baslik' düğümü bulunamadı.");
                    return;
                }

                var sigNode = tmpSigned.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#")
                                       .Item(0) as XmlElement;
                if (sigNode == null)
                {
                    MessageBox.Show("İmza üretilemedi (Signature düğümü bulunamadı).");
                    return;
                }

                // Eski Signature varsa kaldır
                var oldSig = sysXml.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#").Item(0) as XmlElement;
                if (oldSig != null) oldSig.ParentNode.RemoveChild(oldSig);

                // İmzayı import edip baslik altına ekle
                XmlNode imported = sysXml.ImportNode(sigNode, true);
                baslikNode.AppendChild(imported);

                // 9) Kaydet
                string outputPath = Path.Combine(
                    Path.GetDirectoryName(selectedFilePath),
                    Path.GetFileNameWithoutExtension(selectedFilePath) + "-imzali.xml"
                );
                lastSignedXmlPath = outputPath;
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = false,
                    NewLineHandling = NewLineHandling.None
                };
                using (var writer = XmlWriter.Create(outputPath, settings))
                    sysXml.Save(writer);

                lastSignedXml = File.ReadAllText(outputPath, Encoding.UTF8);
                txtSignedXml.Text = lastSignedXml;

                // Adım düğmeleri
                btnZip.Enabled = true;
                btnReSign.Enabled = true;
                btnSendWsdl.Enabled = true;

                Log("İmzalama tamamlandı: " + outputPath);
                MessageBox.Show("Mali Mühür ile imzalama tamam (imza 'baslik' içinde).", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("Hata (İmzala): " + ex.Message);
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progress.Style = ProgressBarStyle.Blocks;
            }
        }

        private void BtnZip_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(lastSignedXml) || string.IsNullOrEmpty(lastSignedXmlPath) || !File.Exists(lastSignedXmlPath))
                {
                    MessageBox.Show("Önce belgeyi imzalayın.");
                    return;
                }

                // İmzalı dosya adı (örn: UUID.xml) -> ZIP = UUID.zip
                string signedFileName = Path.GetFileName(selectedFilePath);
                string zipFileName = Path.GetFileNameWithoutExtension(selectedFilePath) + ".zip";
                string zipPath = Path.Combine(Path.GetDirectoryName(selectedFilePath) ?? Environment.CurrentDirectory, zipFileName);

                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(lastSignedXmlPath, signedFileName, CompressionLevel.Optimal);
                }

                var zipBytes = File.ReadAllBytes(zipPath);
                lastZipBase64 = Convert.ToBase64String(zipBytes);
                lastZipPath = zipPath;

                Log($"Zip oluşturuldu: {zipPath} (boyut: {zipBytes.Length:N0} bayt)");
                MessageBox.Show("Zip oluşturuldu ve Base64 hazırlandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                btnReSign.Enabled = true;
                btnSendWsdl.Enabled = true;
            }
            catch (Exception ex)
            {
                Log("Hata (Zip): " + ex.Message);
                MessageBox.Show(ex.Message, "Zip Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReSign_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(lastZipBase64))
                {
                    MessageBox.Show("Önce ZIP dosyası oluşturun.");
                    return;
                }

                if (currentCertificate == null || currentSigner == null)
                {
                    MessageBox.Show("İmzalama sertifikası bulunamadı. Önce belgeyi imzalayın.");
                    return;
                }

                progress.Style = ProgressBarStyle.Marquee;
                Log("SOAP mesajı imzalanıyor...");

                // SOAP mesajını oluştur ve imzala (WSSE, .NET SignedXml)
                string soapMessage = CreateSignedSoapMessage();
                lastSoapXml = soapMessage;

                // Kaydet (debug için)
                string soapPath = Path.Combine(
                    Path.GetDirectoryName(lastZipPath) ?? Environment.CurrentDirectory,
                    "signed-soap-message.xml"
                );
                File.WriteAllText(soapPath, soapMessage, Encoding.UTF8);

                Log($"SOAP mesajı imzalandı ve kaydedildi: {soapPath}");
                MessageBox.Show("SOAP mesajı WSS ile imzalandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                btnSendWsdl.Enabled = true;
            }
            catch (Exception ex)
            {
                Log("Hata (SOAP İmzala): " + ex.Message);
                MessageBox.Show(ex.Message, "SOAP İmzalama Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progress.Style = ProgressBarStyle.Blocks;
            }
        }

        private string CreateSignedSoapMessage()
        {
            // Namespaces
            const string soap12Ns = "http://www.w3.org/2003/05/soap-envelope";
            const string wsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
            const string wsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
            const string dsNs = "http://www.w3.org/2000/09/xmldsig#";

            // IDs
            string tsId = "TS-" + Guid.NewGuid().ToString("N");
            string bodyId = "id-" + Guid.NewGuid().ToString("N");
            string bstId = "X509-" + Guid.NewGuid().ToString("N");
            string sigId = "SIG-" + Guid.NewGuid().ToString("N");

            // Time
            DateTime now = DateTime.UtcNow;
            string created = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            string expires = now.AddMinutes(50).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            if (string.IsNullOrEmpty(lastZipPath) || string.IsNullOrEmpty(lastZipBase64))
                throw new InvalidOperationException("Önce ZIP dosyasını oluşturun.");

            // Sertifikayı depodan, özel anahtarla alın (helper: ResolveX509FromStore)
            var x509 = ResolveX509FromStore(currentCertificate);
            if (x509 == null || !x509.HasPrivateKey)
                throw new Exception("Kart sertifikası özel anahtarı (X509) bulunamadı.");

            // BST için çıplak sertifika verisi
            string certBase64 = Convert.ToBase64String(x509.RawData);
            string zipFileName = Path.GetFileName(lastZipPath);

            // 1) İmzasız SOAP zarfı (SOAP 1.2)
            string unsignedSoap = $@"
<env:Envelope xmlns:env=""{soap12Ns}"">
  <env:Header>
    <wsse:Security xmlns:wsse=""{wsseNs}"" xmlns:wsu=""{wsuNs}"" env:mustUnderstand=""true"">
      <wsse:BinarySecurityToken
        wsu:Id=""{bstId}""
        EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary""
        ValueType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3"">{certBase64}</wsse:BinarySecurityToken>
      <wsu:Timestamp wsu:Id=""{tsId}"">
        <wsu:Created>{created}</wsu:Created>
        <wsu:Expires>{expires}</wsu:Expires>
      </wsu:Timestamp>
    </wsse:Security>
  </env:Header>
  <env:Body xmlns:wsu=""{wsuNs}"" wsu:Id=""{bodyId}"">
    <ns2:sendDocumentFile xmlns:ns2=""http://earsiv.vedop3.ggm.gov.org/"">
      <Attachment>
        <fileName>{zipFileName}</fileName>
        <binaryData>{lastZipBase64}</binaryData>
      </Attachment>
    </ns2:sendDocumentFile>
  </env:Body>
</env:Envelope>";

            var soapDoc = new XmlDocument { PreserveWhitespace = true };
            soapDoc.LoadXml(unsignedSoap);

            // NS Manager
            var nsmgr = new XmlNamespaceManager(soapDoc.NameTable);
            nsmgr.AddNamespace("env", soap12Ns);
            nsmgr.AddNamespace("wsse", wsseNs);
            nsmgr.AddNamespace("wsu", wsuNs);
            nsmgr.AddNamespace("ds", dsNs);

            // 2) İmzalanacak düğümler (yalnızca wsu:Id kullanıyoruz; düz Id EKLEMİYORUZ)
            var tsEl = soapDoc.SelectSingleNode($"//wsu:Timestamp[@wsu:Id='{tsId}']", nsmgr) as XmlElement
                         ?? throw new Exception("Timestamp bulunamadı.");
            var bodyEl = soapDoc.SelectSingleNode($"//env:Body[@wsu:Id='{bodyId}']", nsmgr) as XmlElement
                         ?? throw new Exception("Body bulunamadı.");

            // 3) .NET SignedXml ile WS-Security imzası (Exclusive C14N + RSA-SHA256; Digest SHA-1)
            var signedXml = new WsuSignedXml(soapDoc)
            {
                SigningKey = x509.GetRSAPrivateKey()
            };
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;                 // http://www.w3.org/2001/10/xml-exc-c14n#
            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"; // rsa-sha256

            // Referans 1: Timestamp
            var rTs = new Reference("#" + tsId) { DigestMethod = SignedXml.XmlDsigSHA1Url }; // örnek zarfa uyumlu (SHA-1)
            rTs.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(rTs);

            // Referans 2: Body
            var rBody = new Reference("#" + bodyId) { DigestMethod = SignedXml.XmlDsigSHA1Url };
            rBody.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(rBody);

            // KeyInfo = SecurityTokenReference (DirectReference → BST) — STR'ye wsu:Id VERMİYORUZ
            var ki = new KeyInfo();
            var strNode = soapDoc.CreateElement("wsse", "SecurityTokenReference", wsseNs);
            var refEl = soapDoc.CreateElement("wsse", "Reference", wsseNs);
            refEl.SetAttribute("URI", "#" + bstId);
            refEl.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            strNode.AppendChild(refEl);
            ki.AddClause(new KeyInfoNode(strNode));
            signedXml.KeyInfo = ki;

            // İmzayı üret
            signedXml.ComputeSignature();
            XmlElement sigXml = signedXml.GetXml();
            sigXml.SetAttribute("Id", sigId); // ds:Signature/@Id (isteğe bağlı ama sorun çıkarmaz)

            // 4) İmzayı wsse:Security altına, BST'den sonra (Timestamp'ten önce) ekle
            var securityEl = soapDoc.SelectSingleNode("//wsse:Security", nsmgr) as XmlElement
                             ?? throw new Exception("wsse:Security bulunamadı.");
            var bstNode = soapDoc.SelectSingleNode("//wsse:Security/wsse:BinarySecurityToken", nsmgr) as XmlElement;

            if (bstNode != null)
                securityEl.InsertAfter(sigXml, bstNode);
            else
                securityEl.AppendChild(sigXml);

            // 5) Hızlı kontrol/log
            var insertedSig = securityEl.SelectSingleNode("ds:Signature", nsmgr) as XmlElement;
            bool hasTs = false, hasBody = false; int refCount = 0;
            if (insertedSig != null)
            {
                var refs = insertedSig.SelectNodes("ds:SignedInfo/ds:Reference", nsmgr);
                refCount = refs?.Count ?? 0;
                if (refs != null)
                {
                    foreach (XmlElement r in refs)
                    {
                        var uri = r.GetAttribute("URI");
                        if (uri == "#" + tsId) hasTs = true;
                        if (uri == "#" + bodyId) hasBody = true;
                    }
                }
            }
            Log($"WSSE kontrol (NET): TS ref={(hasTs ? "VAR" : "YOK")}, Body ref={(hasBody ? "VAR" : "YOK")}, toplam ref={refCount}");

            // (Opsiyonel) Aynı wsu:Id’ye sahip birden çok öğe var mı? (hızlı teşhis)
            var bstList = soapDoc.SelectNodes("//wsse:BinarySecurityToken", nsmgr);
            Log($"BST sayısı: {bstList?.Count ?? 0}");
            var dup = soapDoc.SelectNodes($"//*[@wsu:Id='{bstId}']", nsmgr);
            Log($"'{bstId}' wsu:Id’sine sahip öğe sayısı: {dup?.Count ?? 0} (1 olmalı)");

            // 6) Son hali döndür
            return soapDoc.OuterXml;
        }


        private static X509Certificate2 ResolveX509FromStore(ECertificate eCert)
        {
            // ECertificate DER → temp X509 sadece THUMBPRINT almak için (private key yok)
            byte[] der = eCert.getEncoded();
            var temp = new X509Certificate2(der);
            string thumb = temp.Thumbprint?.Replace(" ", string.Empty)?.ToUpperInvariant();

            if (string.IsNullOrEmpty(thumb))
                throw new Exception("Sertifika parmak izi (Thumbprint) tespit edilemedi.");

            X509Certificate2 found = null;

            // Önce CurrentUser\My
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                var col = store.Certificates.Find(X509FindType.FindByThumbprint, thumb, validOnly: false);
                foreach (var c in col)
                {
                    if (c.HasPrivateKey) { found = c; break; }
                }
            }
            // Sonra LocalMachine\My
            if (found == null)
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                    var col = store.Certificates.Find(X509FindType.FindByThumbprint, thumb, validOnly: false);
                    foreach (var c in col)
                    {
                        if (c.HasPrivateKey) { found = c; break; }
                    }
                }
            }

            if (found == null)
                throw new Exception(
                    "Smart kart sertifikası Windows deposunda özel anahtarla bulunamadı. " +
                    "Kart sürücüsünün (CSP/KSP) kurulu olduğundan, kart takılı ve 'Smart Card' servisinin açık olduğundan emin olun. " +
                    "MMC → Certificates (Current User/Local Machine) → Personal → ilgili sertifikada 'You have a private key' ibaresi görünmeli.");

            return found;
        }

        private async void BtnSendWsdl_Click(object sender, EventArgs e)
        {
            try
            {
                var urlInput = txtWsdlUrl.Text.Trim();
                if (string.IsNullOrWhiteSpace(urlInput))
                {
                    MessageBox.Show("WSDL/Endpoint adresini girin.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(lastSoapXml))
                {
                    MessageBox.Show("Önce SOAP mesajını imzalayın (Tekrar İmzala).");
                    return;
                }

                // Kullanıcı/şifre (Basic Auth)
                var user = txtUsername.Text.Trim();
                var pass = txtPassword.Text;

                progress.Style = ProgressBarStyle.Marquee;

                var endpoint = NormalizeEndpointUrl(urlInput);
                Log($"Gönderim Endpoints: {endpoint}");

                Log("SOAP gönderimi başlıyor...");
                await SendSoapAsync(endpoint, lastSoapXml, user, pass);

                MessageBox.Show("Gönderim tamamlandı. Log’a ve yanıt dosyasına bakabilirsiniz.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (HttpRequestException hre)
            {
                Log("HTTP Hatası: " + hre.Message);
                MessageBox.Show("HTTP hatası: " + hre.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Log("Hata (WSDL Gönder): " + ex.Message);
                MessageBox.Show(ex.Message, "Gönderim Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progress.Style = ProgressBarStyle.Blocks;
            }
        }

        private void Log(string msg)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }

        private async Task SendSoapAsync(string endpointUrl, string soapXml, string username, string password)
        {
            using (var client = CreateSoapHttpClient(TimeSpan.FromMinutes(3)))
            {
                // SOAP 1.2 → action parametresi ile
                var contentType = "application/soap+xml; charset=utf-8; action=\"sendDocumentFile\"";

                using (var content = new StringContent(soapXml, Encoding.UTF8, "application/soap+xml"))
                {
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                    // Basic Auth
                    if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
                    {
                        client.DefaultRequestHeaders.Authorization =
                            AuthenticationHeaderValue.Parse(BuildBasicAuthHeader(username, password));
                    }

                    // POST
                    var resp = await client.PostAsync(endpointUrl, content);
                    var respText = await resp.Content.ReadAsStringAsync();

                    Log($"HTTP Status: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                    Log("Response Content (ilk 4 KB):");
                    Log(respText.Length > 4096 ? respText.Substring(0, 4096) + " ... (kısaltıldı)" : respText);

                    // Yanıtı dosyaya yaz
                    var folder = Path.GetDirectoryName(lastZipPath) ?? Environment.CurrentDirectory;
                    var respPath = Path.Combine(folder,
                        "sendDocumentFile-response-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xml");
                    File.WriteAllText(respPath, respText, Encoding.UTF8);
                    Log("Yanıt dosyaya kaydedildi: " + respPath);
                }
            }
        }

        private static string NormalizeEndpointUrl(string inputUrl)
        {
            if (string.IsNullOrWhiteSpace(inputUrl)) return inputUrl;

            // .../earsiv.wsdl → .../EArsivWsPort
            if (inputUrl.EndsWith("earsiv.wsdl", StringComparison.OrdinalIgnoreCase))
                return inputUrl.Substring(0, inputUrl.Length - "earsiv.wsdl".Length).TrimEnd('/');

            var u = new Uri(inputUrl, UriKind.Absolute);
            var builder = new UriBuilder(u) { Query = "" };
            var normalized = builder.Uri.ToString();

            if (normalized.EndsWith("/EArsivWsPort", StringComparison.OrdinalIgnoreCase))
                return normalized.TrimEnd('/');

            return normalized.TrimEnd('/');
        }

        private static string BuildBasicAuthHeader(string username, string password)
        {
            var raw = $"{username}:{password}";
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            return $"Basic {b64}";
        }

        private static HttpClient CreateSoapHttpClient(TimeSpan? timeout = null)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                // TEST ortamında aşağıdaki bypass kullanılabilir. ÜRETİMDE kaldırın ve KamuSM kök/ara sertifikaları OS'e ekleyin.
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            };

            var client = new HttpClient(handler)
            {
                Timeout = timeout ?? TimeSpan.FromMinutes(2)
            };

            client.DefaultRequestHeaders.ExpectContinue = false;
            return client;
        }
    }

    public class WsuSignedXml : SignedXml
    {
        private const string WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        public WsuSignedXml(XmlDocument document) : base(document) { }
        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            var e = base.GetIdElement(document, idValue);
            if (e != null) return e;
            var nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("wsu", WsuNs);
            return document.SelectSingleNode($"//*[@wsu:Id='{idValue}']", nsmgr) as XmlElement;
        }
    }
}
