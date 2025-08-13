using System.Windows.Forms;

namespace GibDesktopApplication
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Controls
        private GroupBox grpService;
        private Label lblCompanyCode, lblUsername, lblPassword;
        private TextBox txtUsername, txtPassword;

        private GroupBox grpFile;
        private Label lblSelectedFile;
        private Button btnBrowse;

        private GroupBox grpSign;
        private Button btnSign;

        private GroupBox grpPreview;
        private TextBox txtSignedXml;

        private GroupBox grpZipAndSend;
        private Button btnZip, btnReSign, btnSendWsdl;
        private Label lblWsdlUrl;
        private TextBox txtWsdlUrl;

        private GroupBox grpLog;
        private TextBox txtLog;
        private ProgressBar progress;

        private OpenFileDialog ofd;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "E-ŞÜ Aylık Rapor İmzalama ve Gönderim Aracı";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 1100;
            this.Height = 800;

            // === Service Credentials ===
            grpService = new GroupBox
            {
                Text = "1) Servis Kimlik Bilgileri",
                Left = 10,
                Top = 10,
                Width = this.ClientSize.Width - 20,
                Height = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblUsername = new Label { Text = "Kullanıcı Adı:", Left = 15, Top = 28, Width = 90 };
            txtUsername = new TextBox { Left = 110, Top = 24, Width = 200, Anchor = AnchorStyles.Top | AnchorStyles.Left };

            lblPassword = new Label { Text = "Şifre:", Left = 330, Top = 28, Width = 95 };
            txtPassword = new TextBox { Left = 430, Top = 24, Width = 200, Anchor = AnchorStyles.Top | AnchorStyles.Left };


            grpService.Controls.AddRange(new Control[] { 
                lblUsername, txtUsername, lblPassword, txtPassword
            });

            // === File Selection ===
            grpFile = new GroupBox
            {
                Text = "2) İmzalanacak Belge",
                Left = 10,
                Top = grpService.Bottom + 8,
                Width = this.ClientSize.Width - 20,
                Height = 90,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblSelectedFile = new Label
            {
                Text = "Dosya: (seçilmedi)",
                Left = 15,
                Top = 30,
                Width = grpFile.Width - 200,
                AutoEllipsis = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnBrowse = new Button
            {
                Text = "Gözat…",
                Left = grpFile.Width - 110,
                Top = 25,
                Width = 90,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowse.Click += BtnBrowse_Click;

            grpFile.Controls.AddRange(new Control[] { lblSelectedFile, btnBrowse });

            // === Sign ===
            grpSign = new GroupBox
            {
                Text = "3) İmzala",
                Left = 10,
                Top = grpFile.Bottom + 8,
                Width = this.ClientSize.Width - 20,
                Height = 70,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnSign = new Button
            {
                Text = "İMZALA",
                Left = 15,
                Top = 25,
                Width = 120
            };
            btnSign.Click += BtnSign_Click;

            grpSign.Controls.Add(btnSign);

            // === Preview Signed XML ===
            grpPreview = new GroupBox
            {
                Text = "4) İmzalı Belge Önizleme",
                Left = 10,
                Top = grpSign.Bottom + 8,
                Width = this.ClientSize.Width - 20,
                Height = 300,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txtSignedXml = new TextBox
            {
                Left = 15,
                Top = 25,
                Width = grpPreview.Width - 30,
                Height = grpPreview.Height - 35,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            grpPreview.Controls.Add(txtSignedXml);

            // === Zip + ReSign + Send ===
            grpZipAndSend = new GroupBox
            {
                Text = "5) Zip + Tekrar İmzala   /   6) WSDL ile Gönder",
                Left = 10,
                Top = grpPreview.Bottom + 8,
                Width = this.ClientSize.Width - 20,
                Height = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnZip = new Button
            {
                Text = "ZIPLE",
                Left = 15,
                Top = 25,
                Width = 120
            };
            btnZip.Click += BtnZip_Click;

            btnReSign = new Button
            {
                Text = "TEKRAR İMZALA",
                Left = btnZip.Right + 10,
                Top = 25,
                Width = 140
            };
            btnReSign.Click += BtnReSign_Click;

            lblWsdlUrl = new Label { Text = "WSDL/Endpoint:", Left = btnReSign.Right + 20, Top = 31, Width = 100 };
            txtWsdlUrl = new TextBox
            {
                Left = lblWsdlUrl.Right + 8,
                Top = 27,
                Width = 350,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "https://ornek.gov.tr/servis?wsdl" // taslak
            };

            btnSendWsdl = new Button
            {
                Text = "WSDL İLE GÖNDER",
                Left = grpZipAndSend.Width - 170,
                Top = 25,
                Width = 150,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSendWsdl.Click += BtnSendWsdl_Click;

            grpZipAndSend.Controls.AddRange(new Control[] { btnZip, btnReSign, lblWsdlUrl, txtWsdlUrl, btnSendWsdl });

            // === Log & Progress ===
            grpLog = new GroupBox
            {
                Text = "Log",
                Left = 10,
                Top = grpZipAndSend.Bottom + 8,
                Width = this.ClientSize.Width - 20,
                Height = 120,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            txtLog = new TextBox
            {
                Left = 15,
                Top = 25,
                Width = grpLog.Width - 30,
                Height = 60,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            progress = new ProgressBar
            {
                Left = 15,
                Top = txtLog.Bottom + 10,
                Width = grpLog.Width - 30,
                Height = 18,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            grpLog.Controls.AddRange(new Control[] { txtLog, progress });

            // === OpenFileDialog ===
            ofd = new OpenFileDialog
            {
                Filter = "XML Dosyaları (*.xml)|*.xml",
                Title = "İmzalanacak XML Dosyası Seç"
            };

            // Add to form
            this.Controls.AddRange(new Control[]
            {
                grpService, grpFile, grpSign, grpPreview, grpZipAndSend, grpLog
            });

            // Make layout responsive
            this.Resize += (s, e) =>
            {
                lblSelectedFile.Width = grpFile.Width - 200;
            };

            // Step gating
            btnSign.Enabled = false;
            btnZip.Enabled = false;
            btnReSign.Enabled = false;
            btnSendWsdl.Enabled = false;

        }

        #endregion

        private System.Windows.Forms.Label firmCodeLabel;
        private System.Windows.Forms.Label firmPassword;
        private System.Windows.Forms.TextBox firmCodeTextBox;
        private System.Windows.Forms.TextBox firmPasswordTextBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button loadXmlButton;
        private System.Windows.Forms.Button getCertificateInfosBtn;
    }
}

