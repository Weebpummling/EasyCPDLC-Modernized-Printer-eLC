namespace EasyCPDLC
{
    partial class TelexForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TelexForm));
            telexFrame = new DcduAssetPanel();
            freeTextButton = new DcduHotspotButton();
            metarButton = new DcduHotspotButton();
            atisButton = new DcduHotspotButton();
            clearButton = new DcduHotspotButton();
            sendButton = new DcduHotspotButton();
            exitButton = new DcduHotspotButton();
            telexScreen = new System.Windows.Forms.Panel();
            messageFormatPanel = new System.Windows.Forms.FlowLayoutPanel();
            titleLabel = new System.Windows.Forms.Label();
            radioContainer = new System.Windows.Forms.Panel();
            atisRadioButton = new System.Windows.Forms.RadioButton();
            metarRadioButton = new System.Windows.Forms.RadioButton();
            freeTextRadioButton = new System.Windows.Forms.RadioButton();
            telexFrame.SuspendLayout();
            telexScreen.SuspendLayout();
            radioContainer.SuspendLayout();
            SuspendLayout();
            // 
            // telexFrame
            // 
            telexFrame.AssetFileName = "TelexWindowFrame.png";
            telexFrame.ShowHotspotHighlight = false;
            telexFrame.Controls.Add(telexScreen);
            telexFrame.Controls.Add(radioContainer);
            telexFrame.Location = new System.Drawing.Point(0, 0);
            telexFrame.Name = "telexFrame";
            telexFrame.Size = new System.Drawing.Size(700, 233);
            telexFrame.TabIndex = 0;
            telexFrame.MouseMove += AssetFrame_MouseMove;
            telexFrame.MouseLeave += AssetFrame_MouseLeave;
            telexFrame.MouseDown += AssetFrame_MouseDown;
            telexFrame.MouseUp += AssetFrame_MouseUp;
            telexFrame.MouseClick += AssetFrame_MouseClick;
            // 
            // freeTextButton
            // 
            freeTextButton.AccessibleName = "Free Text";
            freeTextButton.BackColor = System.Drawing.Color.Transparent;
            freeTextButton.Location = new System.Drawing.Point(23, 52);
            freeTextButton.Name = "freeTextButton";
            freeTextButton.Size = new System.Drawing.Size(58, 28);
            freeTextButton.TabIndex = 1;
            freeTextButton.Click += FreeTextButton_Click;
            // 
            // metarButton
            // 
            metarButton.AccessibleName = "METAR";
            metarButton.BackColor = System.Drawing.Color.Transparent;
            metarButton.Location = new System.Drawing.Point(23, 90);
            metarButton.Name = "metarButton";
            metarButton.Size = new System.Drawing.Size(58, 30);
            metarButton.TabIndex = 2;
            metarButton.Click += MetarButton_Click;
            // 
            // atisButton
            // 
            atisButton.AccessibleName = "ATIS";
            atisButton.BackColor = System.Drawing.Color.Transparent;
            atisButton.Location = new System.Drawing.Point(23, 128);
            atisButton.Name = "atisButton";
            atisButton.Size = new System.Drawing.Size(58, 29);
            atisButton.TabIndex = 3;
            atisButton.Click += AtisButton_Click;
            // 
            // clearButton
            // 
            clearButton.AccessibleName = "Clear";
            clearButton.BackColor = System.Drawing.Color.Transparent;
            clearButton.Location = new System.Drawing.Point(619, 128);
            clearButton.Name = "clearButton";
            clearButton.Size = new System.Drawing.Size(54, 28);
            clearButton.TabIndex = 4;
            clearButton.Click += ResetPanel;
            // 
            // sendButton
            // 
            sendButton.AccessibleName = "Send";
            sendButton.BackColor = System.Drawing.Color.Transparent;
            sendButton.Location = new System.Drawing.Point(619, 165);
            sendButton.Name = "sendButton";
            sendButton.Size = new System.Drawing.Size(54, 29);
            sendButton.TabIndex = 5;
            sendButton.Click += SendButton_Click;
            // 
            // exitButton
            // 
            exitButton.AccessibleName = "Close";
            exitButton.BackColor = System.Drawing.Color.Transparent;
            exitButton.Location = new System.Drawing.Point(619, 51);
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(54, 28);
            exitButton.TabIndex = 6;
            exitButton.Click += ExitButton_Click;
            // 
            // telexScreen
            // 
            telexScreen.BackColor = System.Drawing.Color.Transparent;
            telexScreen.Controls.Add(messageFormatPanel);
            telexScreen.Controls.Add(titleLabel);
            telexScreen.Location = new System.Drawing.Point(104, 37);
            telexScreen.Name = "telexScreen";
            telexScreen.Size = new System.Drawing.Size(496, 157);
            telexScreen.TabIndex = 7;
            telexScreen.MouseDown += WindowDrag;
            // 
            // messageFormatPanel
            // 
            messageFormatPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            messageFormatPanel.AutoScroll = true;
            messageFormatPanel.BackColor = System.Drawing.Color.Transparent;
            messageFormatPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            messageFormatPanel.Location = new System.Drawing.Point(12, 12);
            messageFormatPanel.Margin = new System.Windows.Forms.Padding(5);
            messageFormatPanel.Name = "messageFormatPanel";
            messageFormatPanel.Padding = new System.Windows.Forms.Padding(8, 0, 0, 30);
            messageFormatPanel.Size = new System.Drawing.Size(470, 135);
            messageFormatPanel.TabIndex = 0;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.BackColor = System.Drawing.Color.Transparent;
            titleLabel.Location = new System.Drawing.Point(3, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(0, 15);
            titleLabel.TabIndex = 1;
            titleLabel.Visible = false;
            // 
            // radioContainer
            // 
            radioContainer.Controls.Add(atisRadioButton);
            radioContainer.Controls.Add(metarRadioButton);
            radioContainer.Controls.Add(freeTextRadioButton);
            radioContainer.Location = new System.Drawing.Point(46, 303);
            radioContainer.Name = "radioContainer";
            radioContainer.Size = new System.Drawing.Size(100, 20);
            radioContainer.TabIndex = 8;
            radioContainer.Visible = false;
            // 
            // atisRadioButton
            // 
            atisRadioButton.AutoSize = true;
            atisRadioButton.Name = "atisRadioButton";
            atisRadioButton.TabStop = true;
            atisRadioButton.Visible = false;
            // 
            // metarRadioButton
            // 
            metarRadioButton.AutoSize = true;
            metarRadioButton.Name = "metarRadioButton";
            metarRadioButton.TabStop = true;
            metarRadioButton.Visible = false;
            // 
            // freeTextRadioButton
            // 
            freeTextRadioButton.AutoSize = true;
            freeTextRadioButton.Name = "freeTextRadioButton";
            freeTextRadioButton.TabStop = true;
            freeTextRadioButton.Visible = false;
            // 
            // TelexForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            BackColor = System.Drawing.Color.FromArgb(1, 2, 3);
            ClientSize = new System.Drawing.Size(700, 233);
            Controls.Add(telexFrame);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximumSize = new System.Drawing.Size(700, 233);
            MinimumSize = new System.Drawing.Size(700, 233);
            Name = "TelexForm";
            Text = "TelexForm";
            FormClosing += TelexForm_FormClosing;
            Load += ReloadPanel;
            MouseDown += WindowDrag;
            telexFrame.ResumeLayout(false);
            telexScreen.ResumeLayout(false);
            telexScreen.PerformLayout();
            radioContainer.ResumeLayout(false);
            radioContainer.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private DcduAssetPanel telexFrame;
        private System.Windows.Forms.Panel telexScreen;
        private System.Windows.Forms.FlowLayoutPanel messageFormatPanel;
        private DcduHotspotButton clearButton;
        private DcduHotspotButton sendButton;
        private DcduHotspotButton exitButton;
        private System.Windows.Forms.Label titleLabel;
        private DcduHotspotButton freeTextButton;
        private DcduHotspotButton metarButton;
        private DcduHotspotButton atisButton;
        private System.Windows.Forms.Panel radioContainer;
        private System.Windows.Forms.RadioButton atisRadioButton;
        private System.Windows.Forms.RadioButton metarRadioButton;
        private System.Windows.Forms.RadioButton freeTextRadioButton;
    }
}
