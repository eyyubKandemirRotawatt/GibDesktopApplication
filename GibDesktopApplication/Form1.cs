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
using tr.gov.tubitak.uekae.esya.api.xmlsignature.document;     // InMemoryDocument, Document

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

                string pin = "060606"; // üretimde PIN'i kullanıcıdan alın
                BaseSigner signer = scm.getSigner(pin, cert);
                if (signer == null)
                {
                    MessageBox.Show("Signer oluşturulamadı!");
                    return;
                }

                // 3) Kaynak XML'i yükle
                var sysXml = new XmlDocument();
                sysXml.PreserveWhitespace = true;
                sysXml.Load(loadedXmlFilePath);
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

                // 4) Bellek belgesi (opsiyonel; MA3 için şart değil ama dursun)
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
                var signedDoc = new SignedDocument(context);

                // ÖNEMLİ: İmzalanacak düğüm olarak KÖK'ü veriyoruz (enveloped referans otomatik)
                signedDoc.addXMLNode(root);

                // İstersen memDoc'u da data object olarak ekleyebilirsin (zorunlu değil)
                 //signedDoc.addDocument(memDoc);

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
                    Path.GetDirectoryName(loadedXmlFilePath),
                    Path.GetFileNameWithoutExtension(loadedXmlFilePath) + "-imzali.xml"
                );
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),
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
