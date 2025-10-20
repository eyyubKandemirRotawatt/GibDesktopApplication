using System.Drawing;
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
        private ModernCard grpService;
        private Label lblCompanyCode, lblUsername, lblPassword;
        private ModernTextBox txtUsername, txtPassword;

        private ModernCard grpFile;
        private Label lblSelectedFile;
        private ModernButton btnBrowse;

        private ModernCard grpSign;
        private ModernButton btnSign;

        private ModernCard grpPreview;
        private TextBox txtSignedXml;

        private ModernCard grpZipAndSend;
        private ModernButton btnZip, btnReSign, btnSendWsdl;
        private Label lblWsdlUrl;
        private TextBox txtWsdlUrl;

        private ModernCard grpLog;
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
            // Modern color palette
            var primaryColor = Color.FromArgb(41, 128, 185);      // Modern blue
            var secondaryColor = Color.FromArgb(52, 152, 219);    // Light blue
            var successColor = Color.FromArgb(46, 204, 113);      // Green
            var warningColor = Color.FromArgb(241, 196, 15);      // Yellow
            var dangerColor = Color.FromArgb(231, 76, 60);        // Red
            var darkText = Color.FromArgb(44, 62, 80);            // Dark gray
            var lightBg = Color.FromArgb(236, 240, 241);          // Light gray bg
            var white = Color.White;

            // Form settings
            this.Text = "E-ŞÜ Aylık Rapor İmzalama ve Gönderim Aracı";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 1200;
            this.Height = 850;
            this.BackColor = lightBg;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // === Service Credentials ===
            grpService = new ModernCard
            {
                Left = 25,
                Top = 20,
                Width = this.ClientSize.Width - 50,
                Height = 110,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = white
            };

            var headerService = new SectionHeader {
                Text = "🔑 Servis Kimlik Bilgileri",
                Left = 10,
                Top = 10
            };

            lblUsername = new Label {
                Text = "Kullanıcı Adı:",
                Left = 20,
                Top = 50,
                Width = 100,
                Font = new Font("Segoe UI", 9F),
                ForeColor = darkText
            };
            txtUsername = new ModernTextBox {
                Left = 125,
                Top = 48,
                Width = 220,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblPassword = new Label {
                Text = "Şifre:",
                Left = 380,
                Top = 50,
                Width = 80,
                Font = new Font("Segoe UI", 9F),
                ForeColor = darkText
            };
            txtPassword = new ModernTextBox {
                Left = 455,
                Top = 48,
                Width = 220,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Font = new Font("Segoe UI", 9.5F),
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            grpService.Controls.AddRange(new Control[] {
                headerService, lblUsername, txtUsername, lblPassword, txtPassword
            });

            // === File Selection ===
            grpFile = new ModernCard
            {
                Left = 25,
                Top = grpService.Bottom + 20,
                Width = this.ClientSize.Width - 50,
                Height = 105,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = white
            };

            var headerFile = new SectionHeader {
                Text = "📄 İmzalanacak Belge",
                Left = 10,
                Top = 10
            };

            lblSelectedFile = new Label
            {
                Text = "Dosya: (seçilmedi)",
                Left = 20,
                Top = 50,
                Width = grpFile.Width - 270,
                AutoEllipsis = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(127, 140, 141)
            };

            btnBrowse = new ModernButton
            {
                Text = "📁 Gözat",
                Left = grpFile.Width - 160,
                Top = 45,
                Width = 130,
                Height = 38,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ForeColor = white,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnBrowse.SetColors(secondaryColor, Color.FromArgb(44, 139, 196));
            btnBrowse.Click += BtnBrowse_Click;

            grpFile.Controls.AddRange(new Control[] { headerFile, lblSelectedFile, btnBrowse });

            // === Sign ===
            grpSign = new ModernCard
            {
                Left = 25,
                Top = grpFile.Bottom + 20,
                Width = this.ClientSize.Width - 50,
                Height = 95,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = white
            };

            var headerSign = new SectionHeader {
                Text = "✍ İmzalama",
                Left = 10,
                Top = 10
            };

            btnSign = new ModernButton
            {
                Text = "🔐 İMZALA",
                Left = 20,
                Top = 45,
                Width = 180,
                Height = 40,
                ForeColor = white,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSign.SetColors(successColor, Color.FromArgb(39, 174, 96));
            btnSign.Click += BtnSign_Click;

            grpSign.Controls.AddRange(new Control[] { headerSign, btnSign });

            // === Preview Signed XML ===
            grpPreview = new ModernCard
            {
                Left = 25,
                Top = grpSign.Bottom + 20,
                Width = this.ClientSize.Width - 50,
                Height = 250,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = white
            };

            var headerPreview = new SectionHeader {
                Text = "👁 İmzalı Belge Önizleme",
                Left = 10,
                Top = 10
            };

            txtSignedXml = new TextBox
            {
                Left = 15,
                Top = 45,
                Width = grpPreview.Width - 45,
                Height = grpPreview.Height - 70,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Consolas", 8.5F),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.None
            };

            grpPreview.Controls.AddRange(new Control[] { headerPreview, txtSignedXml });

            // === Zip + ReSign + Send ===
            grpZipAndSend = new ModernCard
            {
                Left = 25,
                Top = grpPreview.Bottom + 20,
                Width = this.ClientSize.Width - 50,
                Height = 125,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = white
            };

            var headerZip = new SectionHeader {
                Text = "📦 Zip ve Gönderim",
                Left = 10,
                Top = 10
            };

            btnZip = new ModernButton
            {
                Text = "🗜 ZIPLE",
                Left = 20,
                Top = 45,
                Width = 130,
                Height = 38,
                ForeColor = white,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnZip.SetColors(primaryColor, Color.FromArgb(36, 113, 163));
            btnZip.Click += BtnZip_Click;

            btnReSign = new ModernButton
            {
                Text = "🔄 TEKRAR İMZALA",
                Left = btnZip.Right + 15,
                Top = 45,
                Width = 170,
                Height = 38,
                ForeColor = white,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnReSign.SetColors(warningColor, Color.FromArgb(243, 156, 18));
            btnReSign.Click += BtnReSign_Click;

            lblWsdlUrl = new Label {
                Text = "Endpoint:",
                Left = 20,
                Top = 93,
                Width = 80,
                Font = new Font("Segoe UI", 9F),
                ForeColor = darkText
            };
            txtWsdlUrl = new TextBox
            {
                Left = 100,
                Top = 90,
                Width = 480,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "https://okctest.gib.gov.tr/okcesu/services/EArsivWsPort/earsiv.wsdl",
                Enabled = false,
                Font = new Font("Segoe UI", 8.5F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            btnSendWsdl = new ModernButton
            {
                Text = "📤 WSDL İLE GÖNDER",
                Left = grpZipAndSend.Width - 205,
                Top = 45,
                Width = 180,
                Height = 38,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ForeColor = white,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnSendWsdl.SetColors(successColor, Color.FromArgb(39, 174, 96));
            btnSendWsdl.Click += BtnSendWsdl_Click;

            grpZipAndSend.Controls.AddRange(new Control[] { headerZip, btnZip, btnReSign, lblWsdlUrl, txtWsdlUrl, btnSendWsdl });

            // === Standalone Status Check ===
            grpStatus = new ModernCard
            {
                Left = 25,
                Top = grpZipAndSend.Bottom + 20,
                Width = this.ClientSize.Width - 50,
                Height = 105,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = white
            };

            var headerStatus = new SectionHeader {
                Text = "🔍 Durumu Sorgula",
                Left = 10,
                Top = 10
            };

            var lblStatus = new Label {
                Text = "Paket Adı (UUID.zip):",
                Left = 20,
                Top = 52,
                Width = 150,
                Font = new Font("Segoe UI", 9F),
                ForeColor = darkText
            };
            txtStatusFileName = new ModernTextBox
            {
                Left = lblStatus.Right + 10,
                Top = 50,
                Width = 350,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle
            };

            btnGetBatchStatus = new ModernButton
            {
                Text = "🔍 Sorgula",
                Width = 140,
                Height = 38,
                Left = txtStatusFileName.Right + 15,
                Top = 47,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                ForeColor = white,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnGetBatchStatus.SetColors(primaryColor, Color.FromArgb(36, 113, 163));
            btnGetBatchStatus.Click += btnGetBatchStatus_Click;

            grpStatus.Controls.AddRange(new Control[] { headerStatus, lblStatus, txtStatusFileName, btnGetBatchStatus });

            // forma ekle
            this.Controls.Add(grpStatus);

            // === Log & Progress ===
            grpLog = new ModernCard
            {
                Left = 25,
                Top = grpStatus.Bottom + 20,
                Width = this.ClientSize.Width - 50,
                Height = 180,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = white
            };

            var headerLog = new SectionHeader {
                Text = "📋 İşlem Günlüğü",
                Left = 10,
                Top = 10
            };

            txtLog = new TextBox
            {
                Left = 15,
                Top = 45,
                Width = grpLog.Width - 45,
                Height = 100,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Consolas", 8.5F),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.None
            };

            progress = new ProgressBar
            {
                Left = 15,
                Top = txtLog.Bottom + 12,
                Width = grpLog.Width - 45,
                Height = 6,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Style = ProgressBarStyle.Continuous
            };

            grpLog.Controls.AddRange(new Control[] { headerLog, txtLog, progress });

            // === OpenFileDialog ===
            ofd = new OpenFileDialog
            {
                Filter = "XML Dosyaları (*.xml)|*.xml",
                Title = "İmzalanacak XML Dosyası Seç"
            };

            // Add to form
            this.Controls.AddRange(new Control[]
            {
                grpService, grpFile, grpSign, grpPreview, grpZipAndSend, grpStatus, grpLog
            });

            // Make layout responsive
            this.Resize += (s, e) =>
            {
                lblSelectedFile.Width = grpFile.Width - 270;
            };

            // Step gating
            btnSign.Enabled = false;
            btnZip.Enabled = false;
            btnReSign.Enabled = false;
            btnSendWsdl.Enabled = false;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(0, grpLog.Bottom + 30);
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
        private ModernCard grpStatus;
        private TextBox txtStatusFileName;
        private ModernButton btnGetBatchStatus;

    }
}

