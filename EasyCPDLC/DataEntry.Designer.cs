namespace EasyCPDLC
{
    partial class DataEntry
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            loginFrame = new DcduAssetPanel();
            loginScreen = new DcduScreenPanel();
            titleLabel = new System.Windows.Forms.Label();
            subtitleLabel = new System.Windows.Forms.Label();
            exitButton = new System.Windows.Forms.Button();
            hoppieLogonLabel = new System.Windows.Forms.Label();
            roundPanel1 = new System.Windows.Forms.Panel();
            hoppieCodeTextBox = new System.Windows.Forms.TextBox();
            vatsimCIDLabel = new System.Windows.Forms.Label();
            roundPanel2 = new System.Windows.Forms.Panel();
            vatsimCIDTextBox = new System.Windows.Forms.TextBox();
            rememberCheckBox = new LoginBlueCheckBox();
            rememberLabel = new System.Windows.Forms.Label();
            connectButton = new LoginConnectHotspot();
            statusHintLabel = new System.Windows.Forms.Label();
            loginFrame.SuspendLayout();
            loginScreen.SuspendLayout();
            roundPanel1.SuspendLayout();
            roundPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // loginFrame
            // 
            loginFrame.AssetFileName = "DCDU_Login_V13.png";
            loginFrame.BackColor = System.Drawing.Color.Transparent;
            loginFrame.Controls.Add(loginScreen);
            loginFrame.Location = new System.Drawing.Point(0, 0);
            loginFrame.Name = "loginFrame";
            loginFrame.Size = new System.Drawing.Size(300, 533);
            loginFrame.TabIndex = 0;
            loginFrame.MouseClick += LoginFrame_MouseClick;
            loginFrame.MouseDown += LoginFrame_MouseDown;
            // 
            // loginScreen
            // 
            loginScreen.BackColor = System.Drawing.Color.Transparent;
            loginScreen.DrawScreenBackground = false;
            loginScreen.Controls.Add(titleLabel);
            loginScreen.Controls.Add(exitButton);
            loginScreen.Controls.Add(subtitleLabel);
            loginScreen.Controls.Add(hoppieLogonLabel);
            loginScreen.Controls.Add(roundPanel1);
            loginScreen.Controls.Add(vatsimCIDLabel);
            loginScreen.Controls.Add(roundPanel2);
            loginScreen.Controls.Add(rememberCheckBox);
            loginScreen.Controls.Add(rememberLabel);
            loginScreen.Controls.Add(connectButton);
            loginScreen.Controls.Add(statusHintLabel);
            loginScreen.Location = new System.Drawing.Point(0, 0);
            loginScreen.Name = "loginScreen";
            loginScreen.Padding = new System.Windows.Forms.Padding(0);
            loginScreen.Radius = 18;
            loginScreen.Size = new System.Drawing.Size(300, 533);
            loginScreen.TabIndex = 0;
            loginScreen.MouseClick += LoginScreen_MouseClick;
            loginScreen.MouseDown += LoginScreen_MouseDown;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.BackColor = System.Drawing.Color.Transparent;
            titleLabel.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            titleLabel.ForeColor = System.Drawing.Color.FromArgb(86, 255, 103);
            titleLabel.Location = new System.Drawing.Point(17, 18);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(94, 19);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "EASYCPDLC";
            titleLabel.Visible = false;
            titleLabel.MouseDown += DataEntry_MouseDown;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.BackColor = System.Drawing.Color.Transparent;
            subtitleLabel.Font = new System.Drawing.Font("Consolas", 7F, System.Drawing.FontStyle.Bold);
            subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(255, 210, 76);
            subtitleLabel.Location = new System.Drawing.Point(18, 45);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new System.Drawing.Size(94, 10);
            subtitleLabel.TabIndex = 1;
            subtitleLabel.Text = "ACARS LOGON / INIT";
            subtitleLabel.Visible = false;
            // 
            // exitButton
            // 
            exitButton.AccessibleName = "Close";
            exitButton.BackColor = System.Drawing.Color.Transparent;
            exitButton.Cursor = System.Windows.Forms.Cursors.Hand;
            exitButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(1, 2, 3);
            exitButton.FlatAppearance.BorderSize = 0;
            exitButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            exitButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            exitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            exitButton.Font = new System.Drawing.Font("Consolas", 7F, System.Drawing.FontStyle.Bold);
            exitButton.ForeColor = System.Drawing.Color.FromArgb(255, 210, 76);
            exitButton.Location = new System.Drawing.Point(258, 14);
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(24, 24);
            exitButton.TabIndex = 5;
            exitButton.TabStop = false;
            exitButton.Text = "";
            exitButton.Visible = false;
            exitButton.UseVisualStyleBackColor = false;
            exitButton.Click += ExitButton_Click;
            // 
            // hoppieLogonLabel
            // 
            hoppieLogonLabel.AutoSize = true;
            hoppieLogonLabel.BackColor = System.Drawing.Color.Transparent;
            hoppieLogonLabel.Font = new System.Drawing.Font("Consolas", 7F, System.Drawing.FontStyle.Bold);
            hoppieLogonLabel.ForeColor = System.Drawing.Color.FromArgb(45, 231, 245);
            hoppieLogonLabel.Location = new System.Drawing.Point(17, 80);
            hoppieLogonLabel.Name = "hoppieLogonLabel";
            hoppieLogonLabel.Size = new System.Drawing.Size(96, 10);
            hoppieLogonLabel.TabIndex = 2;
            hoppieLogonLabel.Text = "HOPPIE LOGON CODE";
            hoppieLogonLabel.Visible = false;
            // 
            // roundPanel1
            // 
            roundPanel1.BackColor = System.Drawing.Color.FromArgb(1, 12, 25);
            roundPanel1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            roundPanel1.Controls.Add(hoppieCodeTextBox);
            roundPanel1.Location = new System.Drawing.Point(76, 265);
            roundPanel1.Name = "roundPanel1";
            roundPanel1.Padding = new System.Windows.Forms.Padding(0);
            roundPanel1.Size = new System.Drawing.Size(182, 22);
            roundPanel1.TabIndex = 0;
            // 
            // hoppieCodeTextBox
            // 
            hoppieCodeTextBox.BackColor = System.Drawing.Color.FromArgb(1, 12, 25);
            hoppieCodeTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            hoppieCodeTextBox.Font = new System.Drawing.Font("Segoe UI", 9.2F, System.Drawing.FontStyle.Regular);
            hoppieCodeTextBox.ForeColor = System.Drawing.Color.FromArgb(225, 238, 255);
            hoppieCodeTextBox.Location = new System.Drawing.Point(-1, 1);
            hoppieCodeTextBox.MaxLength = 18;
            hoppieCodeTextBox.Name = "hoppieCodeTextBox";
            hoppieCodeTextBox.Size = new System.Drawing.Size(176, 18);
            hoppieCodeTextBox.TabIndex = 0;
            hoppieCodeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            hoppieCodeTextBox.TextChanged += HoppieCodeTextBox_TextChanged;
            // 
            // vatsimCIDLabel
            // 
            vatsimCIDLabel.AutoSize = true;
            vatsimCIDLabel.BackColor = System.Drawing.Color.Transparent;
            vatsimCIDLabel.Font = new System.Drawing.Font("Consolas", 7F, System.Drawing.FontStyle.Bold);
            vatsimCIDLabel.ForeColor = System.Drawing.Color.FromArgb(45, 231, 245);
            vatsimCIDLabel.Location = new System.Drawing.Point(17, 140);
            vatsimCIDLabel.Name = "vatsimCIDLabel";
            vatsimCIDLabel.Size = new System.Drawing.Size(53, 10);
            vatsimCIDLabel.TabIndex = 3;
            vatsimCIDLabel.Text = "VATSIM CID";
            vatsimCIDLabel.Visible = false;
            // 
            // roundPanel2
            // 
            roundPanel2.BackColor = System.Drawing.Color.FromArgb(1, 12, 25);
            roundPanel2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            roundPanel2.Controls.Add(vatsimCIDTextBox);
            roundPanel2.Location = new System.Drawing.Point(73, 334);
            roundPanel2.Name = "roundPanel2";
            roundPanel2.Padding = new System.Windows.Forms.Padding(0);
            roundPanel2.Size = new System.Drawing.Size(184, 22);
            roundPanel2.TabIndex = 1;
            // 
            // vatsimCIDTextBox
            // 
            vatsimCIDTextBox.BackColor = System.Drawing.Color.FromArgb(1, 12, 25);
            vatsimCIDTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            vatsimCIDTextBox.Font = new System.Drawing.Font("Segoe UI", 9.2F, System.Drawing.FontStyle.Regular);
            vatsimCIDTextBox.ForeColor = System.Drawing.Color.FromArgb(225, 238, 255);
            vatsimCIDTextBox.Location = new System.Drawing.Point(-1, 0);
            vatsimCIDTextBox.MaxLength = 7;
            vatsimCIDTextBox.Name = "vatsimCIDTextBox";
            vatsimCIDTextBox.Size = new System.Drawing.Size(176, 18);
            vatsimCIDTextBox.TabIndex = 1;
            vatsimCIDTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            vatsimCIDTextBox.TextChanged += VatsimCIDTextBox_TextChanged;
            vatsimCIDTextBox.KeyPress += NumsOnly;
            // 
            // rememberCheckBox
            // 
            rememberCheckBox.BackColor = System.Drawing.Color.Transparent;
            rememberCheckBox.Font = new System.Drawing.Font("Segoe UI", 8.6F, System.Drawing.FontStyle.Regular);
            rememberCheckBox.ForeColor = System.Drawing.Color.FromArgb(235, 244, 255);
            rememberCheckBox.Location = new System.Drawing.Point(31, 370);
            rememberCheckBox.Name = "rememberCheckBox";
            rememberCheckBox.Size = new System.Drawing.Size(22, 22);
            rememberCheckBox.TabIndex = 2;
            rememberCheckBox.Text = "";
            rememberCheckBox.CheckedChanged += RememberCheckBox_CheckedChanged;
            // 
            // rememberLabel
            // 
            rememberLabel.AutoSize = true;
            rememberLabel.BackColor = System.Drawing.Color.Transparent;
            rememberLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            rememberLabel.Font = new System.Drawing.Font("Segoe UI", 8.6F, System.Drawing.FontStyle.Regular);
            rememberLabel.ForeColor = System.Drawing.Color.FromArgb(235, 244, 255);
            rememberLabel.Location = new System.Drawing.Point(60, 373);
            rememberLabel.Name = "rememberLabel";
            rememberLabel.Size = new System.Drawing.Size(92, 16);
            rememberLabel.TabIndex = 9;
            rememberLabel.Text = "";
            rememberLabel.Visible = false;
            rememberLabel.Click += RememberLabel_Click;
            // 
            // connectButton
            // 
            connectButton.AccessibleName = "Connect";
            connectButton.Blue = true;
            connectButton.BackColor = System.Drawing.Color.Transparent;
            connectButton.Cursor = System.Windows.Forms.Cursors.Hand;
            connectButton.TabStop = false;
            connectButton.Enabled = true;
            connectButton.Font = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Bold);
            connectButton.ForeColor = System.Drawing.Color.FromArgb(255, 210, 76);
            connectButton.Location = new System.Drawing.Point(0, 0);
            connectButton.Name = "connectButton";
            connectButton.Size = new System.Drawing.Size(1, 1);
            connectButton.TabIndex = 3;
            connectButton.Text = "";
            connectButton.Visible = false;
            
            // 
            // statusHintLabel
            // 
            statusHintLabel.AutoSize = true;
            statusHintLabel.BackColor = System.Drawing.Color.Transparent;
            statusHintLabel.Font = new System.Drawing.Font("Consolas", 5.2F, System.Drawing.FontStyle.Bold);
            statusHintLabel.ForeColor = System.Drawing.Color.FromArgb(150, 170, 178);
            statusHintLabel.Location = new System.Drawing.Point(17, 270);
            statusHintLabel.Name = "statusHintLabel";
            statusHintLabel.Size = new System.Drawing.Size(134, 8);
            statusHintLabel.TabIndex = 4;
            statusHintLabel.Text = "SECURE LINK / VATSIM / HOPPIE NETWORK";
            statusHintLabel.Visible = false;
            // 
            // DataEntry
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            BackColor = System.Drawing.Color.FromArgb(1, 2, 3);
            ClientSize = new System.Drawing.Size(300, 533);
            Controls.Add(loginFrame);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            KeyPreview = true;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximumSize = new System.Drawing.Size(300, 533);
            MinimumSize = new System.Drawing.Size(300, 533);
            Name = "DataEntry";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "EasyCPDLC Login";
            MouseDown += DataEntry_MouseDown;
            loginFrame.ResumeLayout(false);
            loginScreen.ResumeLayout(false);
            loginScreen.PerformLayout();
            roundPanel1.ResumeLayout(false);
            roundPanel1.PerformLayout();
            roundPanel2.ResumeLayout(false);
            roundPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private DcduAssetPanel loginFrame;
        private DcduScreenPanel loginScreen;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Label hoppieLogonLabel;
        private System.Windows.Forms.Label vatsimCIDLabel;
        private LoginBlueCheckBox rememberCheckBox;
        private System.Windows.Forms.Label rememberLabel;
        private LoginConnectHotspot connectButton;
        private System.Windows.Forms.Label statusHintLabel;
        private System.Windows.Forms.Panel roundPanel1;
        private System.Windows.Forms.TextBox hoppieCodeTextBox;
        private System.Windows.Forms.Panel roundPanel2;
        private System.Windows.Forms.TextBox vatsimCIDTextBox;
    }
}
