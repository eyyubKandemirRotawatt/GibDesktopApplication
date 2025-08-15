using System.Windows.Forms;

namespace GibDesktopApplication
{
    public partial class PinPromptForm : Form
    {

        private TextBox txtPin;
        private Button btnOk, btnCancel;
        private Label lbl;

        public string Pin => txtPin.Text;

        public PinPromptForm()
        {
            Text = "Mali Mühür PIN";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Width = 360;
            Height = 160;

            lbl = new Label
            {
                Text = "Lütfen mali mühür PIN'inizi girin:",
                AutoSize = true,
                Left = 16,
                Top = 16
            };

            txtPin = new TextBox
            {
                Left = 16,
                Top = 44,
                Width = 310,
                UseSystemPasswordChar = true,
                TabIndex = 0
            };

            btnOk = new Button
            {
                Text = "Tamam",
                Left = 160,
                Top = 80,
                Width = 80,
                DialogResult = DialogResult.OK,
                TabIndex = 1
            };
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPin.Text))
                {
                    MessageBox.Show("PIN boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None; // pencereyi kapatma
                }
            };

            btnCancel = new Button
            {
                Text = "İptal",
                Left = 246,
                Top = 80,
                Width = 80,
                DialogResult = DialogResult.Cancel,
                TabIndex = 2
            };

            Controls.AddRange(new Control[] { lbl, txtPin, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Shown += (s, e) => txtPin.Focus();
        }
    }
}