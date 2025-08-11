using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using GibDesktopApplication.Managers;                           // SmartCardManager
using tr.gov.tubitak.uekae.esya.api.asn.x509;                  // ECertificate
using tr.gov.tubitak.uekae.esya.api.common.crypto;             // BaseSigner
using tr.gov.tubitak.uekae.esya.api.common.util;               // LicenseUtil
using tr.gov.tubitak.uekae.esya.api.xmlsignature;              // XMLSignature, SignedDocument
using tr.gov.tubitak.uekae.esya.api.xmlsignature.config;       // Context, Config
using tr.gov.tubitak.uekae.esya.api.xmlsignature.document;     // DOMDocument, InMemoryDocument, Document

namespace GibDesktopApplication
{
    public partial class Form1 : Form
    {
        public Form1() { InitializeComponent(); }

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
                // 1) Lisans
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
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

                string pin = "060606"; // üretimde kullanıcıdan alın
                BaseSigner signer = scm.getSigner(pin, cert);
                if (signer == null)
                {
                    MessageBox.Show("Signer oluşturulamadı!");
                    return;
                }

                // 3) Kaynak XML (imzasız) yükle
                var sysXml = new XmlDocument();
                sysXml.PreserveWhitespace = true;
                sysXml.Load(loadedXmlFilePath);
                sysXml.Normalize();

                // 4) İmzalanacak veri: InMemoryDocument (DOM ctor sıkıntılarını baypas eder)
                byte[] xmlBytes;
                using (var msSrc = new MemoryStream())
                {
                    sysXml.Save(msSrc);
                    xmlBytes = msSrc.ToArray();
                }
                Document memDoc = new InMemoryDocument(xmlBytes, loadedXmlFilePath, "application/xml", "UTF-8");

                // 5) Context & Config
                string configPath = Path.Combine(baseDir, "config", "xmlsignature-config.xml");
                if (!File.Exists(configPath))
                {
                    MessageBox.Show($"Config dosyası bulunamadı: {configPath}");
                    return;
                }
                Context context = new Context { Config = new Config(configPath) };

                // 6) İmzayı üret
                SignedDocument signedDoc = new SignedDocument(context);
                signedDoc.addDocument(memDoc);

                XMLSignature signature = signedDoc.createSignature();
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
                    sysXml.Save(writer);

                MessageBox.Show("Mali Mühür ile imzalama tamam (imza 'baslik' içinde): " + outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
