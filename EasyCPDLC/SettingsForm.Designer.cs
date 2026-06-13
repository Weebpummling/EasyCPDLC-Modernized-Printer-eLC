namespace EasyCPDLC
{
    partial class SettingsForm
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
            settingsFrame = new DcduAssetPanel();
            settingsCard = new System.Windows.Forms.Panel();
            titleLabel = new System.Windows.Forms.Label();
            subtitleLabel = new System.Windows.Forms.Label();
            exitButton = new DcduHotspotButton();
            settingsFormatPanel = new System.Windows.Forms.FlowLayoutPanel();
            cancelButton = new DcduHotspotButton();
            okButton = new DcduHotspotButton();
            settingsFrame.SuspendLayout();
            settingsCard.SuspendLayout();
            SuspendLayout();
            // 
            // settingsFrame
            // 
            settingsFrame.AssetFileName = "SettingsWindowFrame.png";
            settingsFrame.ShowHotspotHighlight = false;
            settingsFrame.Controls.Add(settingsCard);
            settingsFrame.Location = new System.Drawing.Point(0, 0);
            settingsFrame.Name = "settingsFrame";
            settingsFrame.Size = new System.Drawing.Size(670, 235);
            settingsFrame.TabIndex = 0;
            settingsFrame.MouseMove += AssetFrame_MouseMove;
            settingsFrame.MouseLeave += AssetFrame_MouseLeave;
            settingsFrame.MouseDown += AssetFrame_MouseDown;
            settingsFrame.MouseUp += AssetFrame_MouseUp;
            settingsFrame.MouseClick += AssetFrame_MouseClick;
            // 
            // settingsCard
            // 
            settingsCard.BackColor = System.Drawing.Color.Transparent;
            settingsCard.Controls.Add(titleLabel);
            settingsCard.Controls.Add(subtitleLabel);
            settingsCard.Controls.Add(settingsFormatPanel);
            settingsCard.Location = new System.Drawing.Point(90, 32);
            settingsCard.Name = "settingsCard";
            settingsCard.Size = new System.Drawing.Size(434, 167);
            settingsCard.TabIndex = 1;
            settingsCard.MouseDown += WindowDrag;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Location = new System.Drawing.Point(3, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(0, 15);
            titleLabel.TabIndex = 0;
            titleLabel.Visible = false;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.Location = new System.Drawing.Point(3, 0);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new System.Drawing.Size(0, 15);
            subtitleLabel.TabIndex = 1;
            subtitleLabel.Visible = false;
            // 
            // exitButton
            // 
            exitButton.AccessibleName = "Close";
            exitButton.BackColor = System.Drawing.Color.Transparent;
            exitButton.Location = new System.Drawing.Point(555, 43);
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(59, 31);
            exitButton.TabIndex = 2;
            exitButton.Click += ExitButton_Click;
            // 
            // settingsFormatPanel
            // 
            settingsFormatPanel.AutoScroll = false;
            settingsFormatPanel.BackColor = System.Drawing.Color.Transparent;
            settingsFormatPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            settingsFormatPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            settingsFormatPanel.Location = new System.Drawing.Point(22, 12);
            settingsFormatPanel.Name = "settingsFormatPanel";
            settingsFormatPanel.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            settingsFormatPanel.Size = new System.Drawing.Size(390, 148);
            settingsFormatPanel.TabIndex = 2;
            settingsFormatPanel.WrapContents = false;
            settingsFormatPanel.MouseDown += WindowDrag;
            // 
            // cancelButton
            // 
            cancelButton.AccessibleName = "Cancel";
            cancelButton.BackColor = System.Drawing.Color.Transparent;
            cancelButton.Location = new System.Drawing.Point(555, 83);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(59, 31);
            cancelButton.TabIndex = 3;
            cancelButton.Click += CancelButton_Click;
            // 
            // okButton
            // 
            okButton.AccessibleName = "Save";
            okButton.BackColor = System.Drawing.Color.Transparent;
            okButton.Location = new System.Drawing.Point(555, 123);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(59, 31);
            okButton.TabIndex = 4;
            okButton.Click += OkButton_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            BackColor = System.Drawing.Color.FromArgb(8, 10, 12);
            ClientSize = new System.Drawing.Size(670, 235);
            Controls.Add(settingsFrame);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            MaximumSize = new System.Drawing.Size(670, 235);
            MinimumSize = new System.Drawing.Size(670, 235);
            Name = "SettingsForm";
            Text = "Settings";
            settingsFrame.ResumeLayout(false);
            settingsCard.ResumeLayout(false);
            settingsCard.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private DcduAssetPanel settingsFrame;
        private System.Windows.Forms.Panel settingsCard;
        private DcduHotspotButton exitButton;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.FlowLayoutPanel settingsFormatPanel;
        private DcduHotspotButton cancelButton;
        private DcduHotspotButton okButton;
    }
}
