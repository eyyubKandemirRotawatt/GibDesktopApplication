using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using System.Xml;
using GibDesktopApplication.Managers;
using tr.gov.tubitak.uekae.esya.api.asn.x509;
using tr.gov.tubitak.uekae.esya.api.common.crypto;
using tr.gov.tubitak.uekae.esya.api.common.util;
using tr.gov.tubitak.uekae.esya.api.smartcard.util;
using tr.gov.tubitak.uekae.esya.api.xmlsignature;
using tr.gov.tubitak.uekae.esya.api.xmlsignature.config;
using tr.gov.tubitak.uekae.esya.api.xmlsignature.document;

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
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Sadece XML dosyalarını göstersin
            openFileDialog.Filter = "XML Dosyaları (*.xml)|*.xml";
            openFileDialog.Title = "XML Dosyası Seç";
            openFileDialog.Multiselect = false; // Tek dosya seçilsin

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Seçilen dosyanın yolunu al
                loadedXmlFilePath = openFileDialog.FileName;

                // İstersen dosya yolunu ekrana göster
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
                // 1) Lisans kontrol
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string licensePath = Path.Combine(baseDir, "lisans", "lisans.xml");
                if (!File.Exists(licensePath))
                {
                    MessageBox.Show($"Lisans dosyası bulunamadı: {licensePath}");
                    return;
                }
                using (FileStream fs = new FileStream(licensePath, FileMode.Open, FileAccess.Read))
                    LicenseUtil.setLicenseXml(fs);

                // 2) Smartcard & sertifika
                SmartCardManager scm = SmartCardManager.getInstance();
                ECertificate cert = scm.getSignatureCertificate(false, false);

                if (cert == null)
                {
                    MessageBox.Show("Geçerli bir sertifika bulunamadı!");
                    return;
                }

                string pin = "060606"; // test için sabit
                BaseSigner signer = scm.getSigner(pin, cert);
               
                if (signer == null)
                {
                    MessageBox.Show("Signer oluşturulamadı!");
                    return;
                }

                // 3) XML yükleme
                XmlDocument sysXml = new XmlDocument();
                sysXml.PreserveWhitespace = true;
                sysXml.Load(loadedXmlFilePath);
                sysXml.Normalize();

                if (sysXml.DocumentElement == null)
                {
                    MessageBox.Show("XML geçersiz!");
                    return;
                }

                // 4) DOMDocument oluştur
                Uri xmlUri = new Uri(Path.GetFullPath(loadedXmlFilePath));
                byte[] xmlBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    sysXml.Save(ms);
                    xmlBytes = ms.ToArray();
                }

                // 5) InMemoryDocument oluştur
                Document xmlDoc = new InMemoryDocument(
                    xmlBytes,
                    loadedXmlFilePath,
                    "application/xml",
                    "UTF-8"
                );
                // 5) Context yükle
                string configPath = Path.Combine(baseDir, "config", "xmlsignature-config.xml");
                if (!File.Exists(configPath))
                {
                    MessageBox.Show($"Config dosyası bulunamadı: {configPath}");
                    return;
                }
                Context context = new Context();
                context.Config = new Config(configPath);
                

                // 6) SignedDocument oluştur, dokümanı ekle
                SignedDocument signedDoc = new SignedDocument(context);
                signedDoc.addDocument(xmlDoc);

                // 7) İmza oluştur ve uygula
                XMLSignature signature = signedDoc.createSignature();
                signature.addKeyInfo(cert);
                signature.sign(signer);
                
                // 8) Çıktıyı kaydet
                string outputPath = Path.Combine(
                   Path.GetDirectoryName(loadedXmlFilePath),
                   Path.GetFileNameWithoutExtension(loadedXmlFilePath) + "-imzali.xml"
                );

                using (FileStream fsOut = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    signedDoc.write(fsOut);

                MessageBox.Show("İmzalama başarıyla tamamlandı: " + outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}