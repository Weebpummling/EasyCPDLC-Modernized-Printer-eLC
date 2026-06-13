namespace EasyCPDLC
{
    partial class RequestForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RequestForm));
            requestFrame = new DcduAssetPanel();
            pdcButton = new DcduHotspotButton();
            logonButton = new DcduHotspotButton();
            requestButton = new DcduHotspotButton();
            reportButton = new DcduHotspotButton();
            clearButton = new DcduHotspotButton();
            sendButton = new DcduHotspotButton();
            exitButton = new DcduHotspotButton();
            requestScreen = new System.Windows.Forms.Panel();
            messageFormatPanel = new System.Windows.Forms.TableLayoutPanel();
            titleLabel = new System.Windows.Forms.Label();
            radioContainer = new System.Windows.Forms.Panel();
            ocnClxRadioButton = new System.Windows.Forms.RadioButton();
            reportRadioButton = new System.Windows.Forms.RadioButton();
            requestRadioButton = new System.Windows.Forms.RadioButton();
            logonRadioButton = new System.Windows.Forms.RadioButton();
            depClxRadioButton = new System.Windows.Forms.RadioButton();
            requestContainer = new System.Windows.Forms.Panel();
            wcwRadioButton = new System.Windows.Forms.RadioButton();
            directRadioButton = new System.Windows.Forms.RadioButton();
            speedRadioButton = new System.Windows.Forms.RadioButton();
            levelRadioButton = new System.Windows.Forms.RadioButton();
            requestFrame.SuspendLayout();
            requestScreen.SuspendLayout();
            radioContainer.SuspendLayout();
            requestContainer.SuspendLayout();
            SuspendLayout();
            // 
            // requestFrame
            // 
            requestFrame.AssetFileName = "RequestWindowFrame.png";
            requestFrame.ShowHotspotHighlight = false;
            requestFrame.Controls.Add(requestScreen);
            requestFrame.Controls.Add(radioContainer);
            requestFrame.Controls.Add(requestContainer);
            requestFrame.Location = new System.Drawing.Point(0, 0);
            requestFrame.Name = "requestFrame";
            requestFrame.Size = new System.Drawing.Size(750, 250);
            requestFrame.TabIndex = 0;
            requestFrame.MouseMove += AssetFrame_MouseMove;
            requestFrame.MouseLeave += AssetFrame_MouseLeave;
            requestFrame.MouseDown += AssetFrame_MouseDown;
            requestFrame.MouseUp += AssetFrame_MouseUp;
            requestFrame.MouseClick += AssetFrame_MouseClick;
            // 
            // pdcButton
            // 
            pdcButton.AccessibleName = "Request Clearance";
            pdcButton.BackColor = System.Drawing.Color.Transparent;
            pdcButton.Location = new System.Drawing.Point(17, 50);
            pdcButton.Name = "pdcButton";
            pdcButton.Size = new System.Drawing.Size(84, 32);
            pdcButton.TabIndex = 1;
            pdcButton.Click += PdcButton_Click;
            // 
            // logonButton
            // 
            logonButton.AccessibleName = "Logon";
            logonButton.BackColor = System.Drawing.Color.Transparent;
            logonButton.Location = new System.Drawing.Point(17, 88);
            logonButton.Name = "logonButton";
            logonButton.Size = new System.Drawing.Size(84, 32);
            logonButton.TabIndex = 2;
            logonButton.Click += LogonButton_Click;
            // 
            // requestButton
            // 
            requestButton.AccessibleName = "Request";
            requestButton.BackColor = System.Drawing.Color.Transparent;
            requestButton.Enabled = false;
            requestButton.Location = new System.Drawing.Point(17, 125);
            requestButton.Name = "requestButton";
            requestButton.Size = new System.Drawing.Size(84, 32);
            requestButton.TabIndex = 3;
            requestButton.Click += RequestButton_Click;
            // 
            // reportButton
            // 
            reportButton.AccessibleName = "Report";
            reportButton.BackColor = System.Drawing.Color.Transparent;
            reportButton.Enabled = false;
            reportButton.Location = new System.Drawing.Point(17, 162);
            reportButton.Name = "reportButton";
            reportButton.Size = new System.Drawing.Size(84, 32);
            reportButton.TabIndex = 4;
            reportButton.Click += ReportButton_Click;
            // 
            // clearButton
            // 
            clearButton.AccessibleName = "Clear";
            clearButton.BackColor = System.Drawing.Color.Transparent;
            clearButton.Location = new System.Drawing.Point(647, 127);
            clearButton.Name = "clearButton";
            clearButton.Size = new System.Drawing.Size(80, 32);
            clearButton.TabIndex = 5;
            clearButton.Click += ClearButton_Click;
            // 
            // sendButton
            // 
            sendButton.AccessibleName = "Send";
            sendButton.BackColor = System.Drawing.Color.Transparent;
            sendButton.Location = new System.Drawing.Point(646, 164);
            sendButton.Name = "sendButton";
            sendButton.Size = new System.Drawing.Size(81, 31);
            sendButton.TabIndex = 6;
            sendButton.Click += SendButton_Click;
            // 
            // exitButton
            // 
            exitButton.AccessibleName = "Close";
            exitButton.BackColor = System.Drawing.Color.Transparent;
            exitButton.Location = new System.Drawing.Point(646, 48);
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(81, 31);
            exitButton.TabIndex = 7;
            exitButton.Click += ExitButton_Click;
            // 
            // requestScreen
            // 
            requestScreen.BackColor = System.Drawing.Color.Transparent;
            requestScreen.Controls.Add(messageFormatPanel);
            requestScreen.Controls.Add(titleLabel);
            requestScreen.Location = new System.Drawing.Point(112, 29);
            requestScreen.Name = "requestScreen";
            requestScreen.Size = new System.Drawing.Size(532, 177);
            requestScreen.TabIndex = 8;
            requestScreen.MouseDown += WindowDrag;
            // 
            // messageFormatPanel
            // 
            messageFormatPanel.AutoScroll = false;
            messageFormatPanel.BackColor = System.Drawing.Color.Transparent;
            messageFormatPanel.ColumnCount = 6;
            messageFormatPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            messageFormatPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            messageFormatPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            messageFormatPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            messageFormatPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            messageFormatPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            messageFormatPanel.Location = new System.Drawing.Point(10, 10);
            messageFormatPanel.Name = "messageFormatPanel";
            messageFormatPanel.RowCount = 7;
            messageFormatPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            messageFormatPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            messageFormatPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            messageFormatPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            messageFormatPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            messageFormatPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            messageFormatPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            messageFormatPanel.Size = new System.Drawing.Size(512, 155);
            messageFormatPanel.TabIndex = 0;
            messageFormatPanel.Paint += messageFormatPanel_Paint;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.BackColor = System.Drawing.Color.Transparent;
            titleLabel.Location = new System.Drawing.Point(6, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(0, 15);
            titleLabel.TabIndex = 1;
            titleLabel.Visible = false;
            // 
            // radioContainer
            // 
            radioContainer.Controls.Add(ocnClxRadioButton);
            radioContainer.Controls.Add(reportRadioButton);
            radioContainer.Controls.Add(requestRadioButton);
            radioContainer.Controls.Add(logonRadioButton);
            radioContainer.Controls.Add(depClxRadioButton);
            radioContainer.Location = new System.Drawing.Point(60, 318);
            radioContainer.Name = "radioContainer";
            radioContainer.Size = new System.Drawing.Size(110, 20);
            radioContainer.TabIndex = 9;
            radioContainer.Visible = false;
            // 
            // ocnClxRadioButton
            // 
            ocnClxRadioButton.AutoSize = true;
            ocnClxRadioButton.Name = "ocnClxRadioButton";
            ocnClxRadioButton.TabStop = true;
            ocnClxRadioButton.Visible = false;
            // 
            // reportRadioButton
            // 
            reportRadioButton.AutoSize = true;
            reportRadioButton.Name = "reportRadioButton";
            reportRadioButton.TabStop = true;
            reportRadioButton.Visible = false;
            // 
            // requestRadioButton
            // 
            requestRadioButton.AutoSize = true;
            requestRadioButton.Name = "requestRadioButton";
            requestRadioButton.TabStop = true;
            requestRadioButton.Visible = false;
            // 
            // logonRadioButton
            // 
            logonRadioButton.AutoSize = true;
            logonRadioButton.Name = "logonRadioButton";
            logonRadioButton.TabStop = true;
            logonRadioButton.Visible = false;
            // 
            // depClxRadioButton
            // 
            depClxRadioButton.AutoSize = true;
            depClxRadioButton.Name = "depClxRadioButton";
            depClxRadioButton.TabStop = true;
            depClxRadioButton.Visible = false;
            // 
            // requestContainer
            // 
            requestContainer.Controls.Add(wcwRadioButton);
            requestContainer.Controls.Add(directRadioButton);
            requestContainer.Controls.Add(speedRadioButton);
            requestContainer.Controls.Add(levelRadioButton);
            requestContainer.Location = new System.Drawing.Point(950, 318);
            requestContainer.Name = "requestContainer";
            requestContainer.Size = new System.Drawing.Size(110, 20);
            requestContainer.TabIndex = 10;
            requestContainer.Visible = false;
            // 
            // wcwRadioButton
            // 
            wcwRadioButton.AutoSize = true;
            wcwRadioButton.Name = "wcwRadioButton";
            wcwRadioButton.TabStop = true;
            wcwRadioButton.Visible = false;
            // 
            // directRadioButton
            // 
            directRadioButton.AutoSize = true;
            directRadioButton.Name = "directRadioButton";
            directRadioButton.TabStop = true;
            directRadioButton.Visible = false;
            // 
            // speedRadioButton
            // 
            speedRadioButton.AutoSize = true;
            speedRadioButton.Name = "speedRadioButton";
            speedRadioButton.TabStop = true;
            speedRadioButton.Visible = false;
            // 
            // levelRadioButton
            // 
            levelRadioButton.AutoSize = true;
            levelRadioButton.Name = "levelRadioButton";
            levelRadioButton.TabStop = true;
            levelRadioButton.Visible = false;
            // 
            // RequestForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            BackColor = System.Drawing.Color.FromArgb(1, 2, 3);
            ClientSize = new System.Drawing.Size(750, 250);
            Controls.Add(requestFrame);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximumSize = new System.Drawing.Size(750, 250);
            MinimumSize = new System.Drawing.Size(750, 250);
            Name = "RequestForm";
            Text = "RequestForm";
            FormClosing += RequestForm_FormClosing;
            Load += RequestForm_Load;
            MouseDown += WindowDrag;
            requestFrame.ResumeLayout(false);
            requestScreen.ResumeLayout(false);
            requestScreen.PerformLayout();
            radioContainer.ResumeLayout(false);
            radioContainer.PerformLayout();
            requestContainer.ResumeLayout(false);
            requestContainer.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DcduAssetPanel requestFrame;
        private System.Windows.Forms.Panel requestScreen;
        private System.Windows.Forms.Label titleLabel;
        private DcduHotspotButton exitButton;
        private DcduHotspotButton sendButton;
        private DcduHotspotButton clearButton;
        private DcduHotspotButton pdcButton;
        private DcduHotspotButton logonButton;
        private DcduHotspotButton requestButton;
        private System.Windows.Forms.Panel radioContainer;
        private System.Windows.Forms.RadioButton logonRadioButton;
        private System.Windows.Forms.RadioButton depClxRadioButton;
        private System.Windows.Forms.RadioButton requestRadioButton;
        private System.Windows.Forms.RadioButton reportRadioButton;
        private DcduHotspotButton reportButton;
        private System.Windows.Forms.Panel requestContainer;
        private System.Windows.Forms.RadioButton wcwRadioButton;
        private System.Windows.Forms.RadioButton directRadioButton;
        private System.Windows.Forms.RadioButton speedRadioButton;
        private System.Windows.Forms.RadioButton levelRadioButton;
        private System.Windows.Forms.RadioButton ocnClxRadioButton;
        private System.Windows.Forms.TableLayoutPanel messageFormatPanel;
    }
}
