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
using System.Collections.Generic;

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

                // Kimlik bilgileri (ileride servis çağrısında kullanacaksın)
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

                string pin = "060606"; // üretimde PIN'i kullanıcıdan alın
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

                // 3.a) Kökte yanlışlıkla default ds namespace varsa temizleyin (ÖNEMLİ)
                // <earsiv:eArsivRaporu ... xmlns="http://www.w3.org/2000/09/xmldsig#"> gibi ise:
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
                if (!root.HasAttribute("Id"))
                {
                    var idAttr = sysXml.CreateAttribute("Id");
                    idAttr.Value = "RaporImzaId";
                    root.Attributes.Append(idAttr);
                }

                // 4) (Opsiyonel) InMemoryDocument hazırlığı
                byte[] xmlBytes;
                using (var msSrc = new MemoryStream())
                {
                    sysXml.Save(msSrc);
                    xmlBytes = msSrc.ToArray();
                }
                Document memDoc = new InMemoryDocument(xmlBytes, selectedFilePath, "application/xml", "UTF-8");

                // 5) Context & Config
                string configPath = Path.Combine(baseDir, "config", "xmlsignature-config.xml");
                if (!File.Exists(configPath))
                {
                    MessageBox.Show($"Config dosyası bulunamadı: {configPath}");
                    return;
                }
                Context context = new Context { Config = new Config(configPath) };

                // 6) İmzayı üret
                var signedDoc = new SignedDocument(context);

                // ÖNEMLİ: İmzalanacak düğüm olarak KÖK'ü veriyoruz (enveloped referans otomatik)
                signedDoc.addXMLNode(root);

                // İstersen memDoc'u da data object olarak ekleyebilirsin (zorunlu değil)
                // signedDoc.addDocument(memDoc);

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

                // Önizleme için oku
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

                // İmzalı dosya adı (örn: GUID.xml) -> ZIP = GUID.zip
                string signedFileName = Path.GetFileName(lastSignedXmlPath);
                string zipFileName = Path.GetFileNameWithoutExtension(lastSignedXmlPath) + ".zip";
                string zipPath = Path.Combine(Path.GetDirectoryName(lastSignedXmlPath) ?? Environment.CurrentDirectory, zipFileName);

                // Tek dosyalı ZIP (entry adı imzalı dosyayla aynı)
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(lastSignedXmlPath, signedFileName, CompressionLevel.Optimal);
                }

                // Base64 (SOAP <binaryData> için)
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

                // SOAP mesajını oluştur ve imzala
                string soapMessage = CreateSignedSoapMessage();
                lastSoapXml = soapMessage;

                // İmzalı SOAP mesajını kaydet (test/debug için)
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
            const string soapNs = "http://www.w3.org/2003/05/soap-envelope";
            const string wsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
            const string wsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
            const string dsNs = "http://www.w3.org/2000/09/xmldsig#";

            string tsId = "TS-" + Guid.NewGuid().ToString("N");
            string bodyId = "id-" + Guid.NewGuid().ToString("N");
            string bstId = "X509-" + Guid.NewGuid().ToString("N");
            string sigId = "SIG-" + Guid.NewGuid().ToString("N");
            string strId = "STR-" + Guid.NewGuid().ToString("N");

            DateTime now = DateTime.UtcNow;
            string created = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            string expires = now.AddMinutes(50).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            if (string.IsNullOrEmpty(lastZipPath) || string.IsNullOrEmpty(lastZipBase64))
                throw new InvalidOperationException("Önce ZIP dosyasını oluşturun.");

            // BST için sertifika baytları (özel anahtar gerekmez)
            byte[] certBytes;
            try { certBytes = currentCertificate.asX509Certificate2().RawData; }
            catch { certBytes = currentCertificate.getEncoded(); }
            string certBase64 = Convert.ToBase64String(certBytes);
            string zipFileName = Path.GetFileName(lastZipPath);

            // 1) İmzasız SOAP
            string xml = $@"
<env:Envelope xmlns:env=""{soapNs}"">
  <env:Header>
    <wsse:Security xmlns:wsse=""{wsseNs}"" xmlns:wsu=""{wsuNs}"" env:mustUnderstand=""true"">
      <wsse:BinarySecurityToken wsu:Id=""{bstId}""
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
            soapDoc.LoadXml(xml);

            var nsmgr = new XmlNamespaceManager(soapDoc.NameTable);
            nsmgr.AddNamespace("env", soapNs);
            nsmgr.AddNamespace("wsse", wsseNs);
            nsmgr.AddNamespace("wsu", wsuNs);
            nsmgr.AddNamespace("ds", dsNs);

            // 2) Timestamp ve Body’ye DÜZ 'Id' ekle (MA3’nin referans çözümü için kritik)
            var tsEl = soapDoc.SelectSingleNode($"//wsu:Timestamp[@wsu:Id='{tsId}']", nsmgr) as XmlElement
                         ?? throw new Exception("Timestamp bulunamadı.");
            var bodyEl = soapDoc.SelectSingleNode($"//env:Body[@wsu:Id='{bodyId}']", nsmgr) as XmlElement
                         ?? throw new Exception("Body bulunamadı.");

            tsEl.SetAttribute("Id", tsId);      // <... Id="TS-...">
            bodyEl.SetAttribute("Id", bodyId);  // <... Id="id-...">

            // 3) MA3 ile WS-Security imzası (kart üzerinde)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "config", "xmlsignature-config.xml");
            var context = new Context { Config = new Config(configPath) };
            var signedDoc = new SignedDocument(context);

            // İmzalanacak düğümler: Timestamp + Body (artık düz Id’leri de var)
            signedDoc.addXMLNode(tsEl);
            signedDoc.addXMLNode(bodyEl);

            var sig = signedDoc.createSignature();
            sig.addKeyInfo(currentCertificate);   // KeyInfo’yu az sonra STR/DirectReference’a çevireceğiz
            sig.sign(currentSigner);              // İmza kart üzerinde (RSA-SHA256; C14N config’ten gelir)

            // 4) ds:Signature’ı al
            var temp = new XmlDocument { PreserveWhitespace = true };
            using (var ms = new MemoryStream())
            {
                signedDoc.write(ms);
                ms.Position = 0;
                temp.Load(ms);
            }
            var signatureEl = temp.GetElementsByTagName("Signature", dsNs).Item(0) as XmlElement
                              ?? throw new Exception("Signature oluşturulamadı.");

            // 5) KeyInfo → STR (DirectReference → BST). XAdES/ds:Object bloklarını silmeyin.
            var oldKi = signatureEl.SelectSingleNode("ds:KeyInfo", nsmgr);
            if (oldKi != null) oldKi.ParentNode.RemoveChild(oldKi);

            var kiEl = temp.CreateElement("ds", "KeyInfo", dsNs);
            var strEl = temp.CreateElement("wsse", "SecurityTokenReference", wsseNs);
            strEl.SetAttribute("Id", wsuNs, strId);
            var refEl = temp.CreateElement("wsse", "Reference", wsseNs);
            refEl.SetAttribute("URI", "#" + bstId);
            refEl.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            strEl.AppendChild(refEl);
            kiEl.AppendChild(strEl);
            signatureEl.AppendChild(kiEl);

            signatureEl.SetAttribute("Id", sigId);

            // 6) Referansları LOG’la – #TS-… ve #id-… gerçekten geldi mi?
            var refs = signatureEl.SelectNodes("ds:SignedInfo/ds:Reference", nsmgr);
            bool hasTs = false, hasBody = false;
            if (refs != null)
            {
                foreach (XmlElement r in refs)
                {
                    var uri = r.GetAttribute("URI");
                    if (uri == "#" + tsId) hasTs = true;
                    if (uri == "#" + bodyId) hasBody = true;
                    var dm = r.SelectSingleNode("ds:DigestMethod", nsmgr) as XmlElement;
                    if (!string.Equals(dm?.GetAttribute("Algorithm"), "http://www.w3.org/2001/04/xmlenc#sha256", StringComparison.Ordinal))
                        Log("Uyarı: DigestMethod SHA-256 değil.");
                }
            }
            Log($"WSSE kontrol: TS ref={(hasTs ? "VAR" : "YOK")}, Body ref={(hasBody ? "VAR" : "YOK")}, toplam ref={refs?.Count}");

            // 7) İmzayı wsse:Security altına (BST sonrası) ekle
            var securityEl = soapDoc.SelectSingleNode("//wsse:Security", nsmgr) as XmlElement
                             ?? throw new Exception("wsse:Security bulunamadı.");
            var bstNode = soapDoc.SelectSingleNode("//wsse:Security/wsse:BinarySecurityToken", nsmgr) as XmlElement;

            var importedSig = soapDoc.ImportNode(signatureEl, true);
            if (bstNode != null) securityEl.InsertAfter(importedSig, bstNode);
            else securityEl.AppendChild(importedSig);

            return soapDoc.OuterXml;
        }

        private void BtnSendWsdl_Click(object sender, EventArgs e)
        {
            try
            {
                var url = txtWsdlUrl.Text.Trim();
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("WSDL/Endpoint adresini girin.");
                    return;
                }

                if (string.IsNullOrEmpty(lastSoapXml))
                {
                    MessageBox.Show("Önce SOAP mesajını imzalayın (Tekrar İmzala butonuna basın).");
                    return;
                }

                progress.Style = ProgressBarStyle.Marquee;
                Log("SOAP mesajı gönderiliyor...");

                // Gerçek SOAP gönderimi için HttpClient veya WebRequest kullanılabilir
                // Burada örnek implementasyon:

                /*
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("SOAPAction", "sendDocumentFile");
                    var content = new StringContent(lastSoapXml, Encoding.UTF8, "application/soap+xml");
                    var response = await client.PostAsync(url, content);
                    var responseText = await response.Content.ReadAsStringAsync();
                    
                    Log($"SOAP Response: {response.StatusCode}");
                    Log($"Response Content: {responseText}");
                    
                    // getBatchStatus ile durumu sorgulama kodu da eklenebilir
                }
                */

                Log($"SOAP mesajı hazır. Endpoint: {url}");
                Log("Not: Gerçek gönderim için HttpClient implementasyonu ekleyin.");

                MessageBox.Show("SOAP mesajı hazır. Gerçek gönderim için HttpClient kodu ekleyin.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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