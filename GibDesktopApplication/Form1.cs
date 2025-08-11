using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GibDesktopApplication
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string loadedXmlFilePath = "";

        private void loadXmlButton_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "XML Dosyaları (*.xml)|*.xml",
                Title = "XML Dosyası Seç",
                Multiselect = false
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                loadedXmlFilePath = ofd.FileName;
                MessageBox.Show("Seçilen dosya: " + loadedXmlFilePath);
            }
        }

        private void getCertificateInfosBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(loadedXmlFilePath))
            {
                MessageBox.Show("Lütfen önce bir XML dosyası seçin.");
                return;
            }

            try
            {
                // 1) XML yükle
                var xml = new XmlDocument { PreserveWhitespace = true };
                xml.Load(loadedXmlFilePath);
                xml.Normalize();

                // 1.1) placeholder Signature varsa kaldır
                var nsmgr = new XmlNamespaceManager(xml.NameTable);
                nsmgr.AddNamespace("earsiv", "http://earsiv.efatura.gov.tr");
                var placeholder = xml.SelectSingleNode("//earsiv:baslik/Signature[@Id='ID001']", nsmgr);
                placeholder?.ParentNode?.RemoveChild(placeholder);

                // 1.2) imzanın konacağı düğüm
                var baslik = xml.SelectSingleNode("/*/earsiv:baslik", nsmgr) as XmlElement;
                if (baslik == null)
                {
                    MessageBox.Show("XML içinde 'earsiv:baslik' bulunamadı.");
                    return;
                }

                // 2) Sertifikayı mağazadan otomatik bul (RSA + private key)
                X509Certificate2 cert = FindSigningCertificate();
                if (cert == null)
                {
                    MessageBox.Show("Uygun (RSA, özel anahtarlı) bir sertifika bulunamadı.");
                    return;
                }

                using (RSA rsa = cert.GetRSAPrivateKey())
                {
                    if (rsa == null)
                    {
                        MessageBox.Show("Sertifikada kullanılabilir RSA özel anahtarı yok.");
                        return;
                    }

                    // 3) SignedXml kur
                    var signedXml = new System.Security.Cryptography.Xml.SignedXml(xml);
                    signedXml.SigningKey = rsa;
                    signedXml.SignedInfo.SignatureMethod = System.Security.Cryptography.Xml.SignedXml.XmlDsigRSASHA256Url;

                    // 3.1) Belgenin tamamına referans (URI="") + Enveloped + C14N
                    var reference = new System.Security.Cryptography.Xml.Reference("");
                    reference.DigestMethod = System.Security.Cryptography.Xml.SignedXml.XmlDsigSHA256Url;
                    reference.AddTransform(new System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform());
                    reference.AddTransform(new System.Security.Cryptography.Xml.XmlDsigC14NTransform());

                    signedXml.AddReference(reference);

                    // 3.2) KeyInfo (X509)
                    var keyInfo = new System.Security.Cryptography.Xml.KeyInfo();
                    var x509Data = new System.Security.Cryptography.Xml.KeyInfoX509Data(cert);
                    x509Data.AddIssuerSerial(cert.Issuer, cert.GetSerialNumberString());
                    keyInfo.AddClause(x509Data);
                    signedXml.KeyInfo = keyInfo;

                    // 4) İmzayı üret ve baslik altına ekle
                    signedXml.ComputeSignature();
                    XmlElement dsSignature = signedXml.GetXml();
                    baslik.AppendChild(xml.ImportNode(dsSignature, true));
                }

                // 5) Kaydet
                string outputPath = Path.Combine(
                    Path.GetDirectoryName(loadedXmlFilePath),
                    Path.GetFileNameWithoutExtension(loadedXmlFilePath) + "-imzali.xml"
                );

                var settings = new XmlWriterSettings
                {
                    Encoding = new System.Text.UTF8Encoding(false),
                    Indent = false,
                    NewLineHandling = NewLineHandling.None
                };
                using (var writer = XmlWriter.Create(outputPath, settings))
                    xml.Save(writer);

                MessageBox.Show("İmzalama başarıyla tamamlandı: " + outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Kritik Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static X509Certificate2 FindSigningCertificate()
        {
            // CurrentUser\My → geçerli/özel anahtarlı/RSA olan ilk sertifika
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var nowValid = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                foreach (var c in nowValid.OfType<X509Certificate2>())
                {
                    try
                    {
                        if (!c.HasPrivateKey) continue;
                        using (var rsa = c.GetRSAPrivateKey())
                        {
                            if (rsa != null) return c;
                        }
                    }
                    catch { /* bazı tokenlar erişimde exception atabilir, geç */ }
                }
            }
            return null;
        }
    }
}
