using System.Drawing;
using System.Windows.Forms;

namespace GibDesktopApplication
{
    public partial class PinPromptForm : Form
    {
        private ModernTextBox txtPin;
        private ModernButton btnOk, btnCancel;
        private Label lbl;

        public string Pin => txtPin.Text;

        public PinPromptForm()
        {
            // Modern color palette
            var primaryColor = Color.FromArgb(41, 128, 185);      // Modern blue
            var successColor = Color.FromArgb(46, 204, 113);      // Green
            var darkText = Color.FromArgb(44, 62, 80);            // Dark gray
            var lightBg = Color.FromArgb(236, 240, 241);          // Light gray bg
            var white = Color.White;

            Text = "Mali Mühür PIN Girişi";
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Width = 480;
            Height = 280;
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.5F);

            // Main card container
            var mainCard = new ModernCard
            {
                Left = 0,
                Top = 0,
                Width = this.Width,
                Height = this.Height,
                BackColor = white,
                Dock = DockStyle.Fill
            };

            // Header panel with gradient
            var headerPanel = new Panel
            {
                Left = 0,
                Top = 0,
                Width = mainCard.Width,
                Height = 70,
                BackColor = primaryColor
            };

            var iconLabel = new Label
            {
                Text = "🔐",
                AutoSize = false,
                Width = 60,
                Height = 60,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 24F),
                Left = 20,
                Top = 5
            };
            headerPanel.Controls.Add(iconLabel);

            var headerLabel = new Label
            {
                Text = "Mali Mühür PIN Doğrulama",
                AutoSize = false,
                Width = headerPanel.Width - 90,
                Height = 40,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = white,
                Left = 85,
                Top = 15
            };
            headerPanel.Controls.Add(headerLabel);

            var subtitleLabel = new Label
            {
                Text = "Güvenli kimlik doğrulama",
                AutoSize = false,
                Width = headerPanel.Width - 90,
                Height = 20,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(200, 230, 245),
                Left = 85,
                Top = 45
            };
            headerPanel.Controls.Add(subtitleLabel);

            lbl = new Label
            {
                Text = "Lütfen mali mühür PIN kodunuzu girin:",
                AutoSize = true,
                Left = 35,
                Top = 100,
                Font = new Font("Segoe UI", 10F),
                ForeColor = darkText
            };

            txtPin = new ModernTextBox
            {
                Left = 35,
                Top = 135,
                Width = 410,
                Height = 40,
                UseSystemPasswordChar = true,
                TabIndex = 0,
                Font = new Font("Segoe UI", 12F)
            };

            // Button container
            var btnContainer = new Panel
            {
                Left = 0,
                Top = 195,
                Width = mainCard.Width,
                Height = 60,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            btnOk = new ModernButton
            {
                Text = "✓ Tamam",
                Left = 235,
                Top = 12,
                Width = 110,
                Height = 42,
                DialogResult = DialogResult.OK,
                TabIndex = 1,
                ForeColor = white,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnOk.SetColors(successColor, Color.FromArgb(39, 174, 96));
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPin.Text))
                {
                    MessageBox.Show("PIN boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                }
            };

            btnCancel = new ModernButton
            {
                Text = "✗ İptal",
                Left = 355,
                Top = 12,
                Width = 95,
                Height = 42,
                DialogResult = DialogResult.Cancel,
                TabIndex = 2,
                ForeColor = darkText,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancel.SetColors(Color.FromArgb(189, 195, 199), Color.FromArgb(149, 165, 166));

            btnContainer.Controls.AddRange(new Control[] { btnOk, btnCancel });

            mainCard.Controls.AddRange(new Control[] { headerPanel, lbl, txtPin, btnContainer });
            this.Controls.Add(mainCard);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Shown += (s, e) => txtPin.Focus();
        }
    }
}