namespace GibDesktopApplication
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.firmCodeLabel = new System.Windows.Forms.Label();
            this.firmPassword = new System.Windows.Forms.Label();
            this.firmCodeTextBox = new System.Windows.Forms.TextBox();
            this.firmPasswordTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.loadXmlButton = new System.Windows.Forms.Button();
            this.getCertificateInfosBtn = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // firmCodeLabel
            // 
            this.firmCodeLabel.AutoSize = true;
            this.firmCodeLabel.Location = new System.Drawing.Point(6, 27);
            this.firmCodeLabel.Name = "firmCodeLabel";
            this.firmCodeLabel.Size = new System.Drawing.Size(69, 13);
            this.firmCodeLabel.TabIndex = 0;
            this.firmCodeLabel.Text = "Firma Kodu : ";
            // 
            // firmPassword
            // 
            this.firmPassword.AutoSize = true;
            this.firmPassword.Location = new System.Drawing.Point(6, 51);
            this.firmPassword.Name = "firmPassword";
            this.firmPassword.Size = new System.Drawing.Size(72, 13);
            this.firmPassword.TabIndex = 1;
            this.firmPassword.Text = "Firma Şifresi : ";
            // 
            // firmCodeTextBox
            // 
            this.firmCodeTextBox.Location = new System.Drawing.Point(81, 24);
            this.firmCodeTextBox.Name = "firmCodeTextBox";
            this.firmCodeTextBox.Size = new System.Drawing.Size(100, 20);
            this.firmCodeTextBox.TabIndex = 2;
            // 
            // firmPasswordTextBox
            // 
            this.firmPasswordTextBox.Location = new System.Drawing.Point(81, 48);
            this.firmPasswordTextBox.Name = "firmPasswordTextBox";
            this.firmPasswordTextBox.Size = new System.Drawing.Size(100, 20);
            this.firmPasswordTextBox.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.firmCodeLabel);
            this.groupBox1.Controls.Add(this.firmPasswordTextBox);
            this.groupBox1.Controls.Add(this.firmPassword);
            this.groupBox1.Controls.Add(this.firmCodeTextBox);
            this.groupBox1.Location = new System.Drawing.Point(12, 22);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 100);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Firma Bilgileri";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // loadXmlButton
            // 
            this.loadXmlButton.Location = new System.Drawing.Point(60, 156);
            this.loadXmlButton.Name = "loadXmlButton";
            this.loadXmlButton.Size = new System.Drawing.Size(75, 23);
            this.loadXmlButton.TabIndex = 5;
            this.loadXmlButton.Text = "XML Yükle";
            this.loadXmlButton.UseVisualStyleBackColor = true;
            this.loadXmlButton.Click += new System.EventHandler(this.loadXmlButton_Click);
            // 
            // getCertificateInfosBtn
            // 
            this.getCertificateInfosBtn.Location = new System.Drawing.Point(310, 12);
            this.getCertificateInfosBtn.Name = "getCertificateInfosBtn";
            this.getCertificateInfosBtn.Size = new System.Drawing.Size(176, 23);
            this.getCertificateInfosBtn.TabIndex = 6;
            this.getCertificateInfosBtn.Text = "Sertifika Bilgileri Getir";
            this.getCertificateInfosBtn.UseVisualStyleBackColor = true;
            this.getCertificateInfosBtn.Click += new System.EventHandler(this.getCertificateInfosBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.getCertificateInfosBtn);
            this.Controls.Add(this.loadXmlButton);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

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

