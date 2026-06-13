namespace EasyCPDLC
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            dcduFrame = new DcduAssetPanel();
            retrieveButton = new DcduHotspotButton();
            telexButton = new DcduHotspotButton();
            atcButton = new DcduHotspotButton();
            settingsButton = new DcduHotspotButton();
            helpButton = new DcduHotspotButton();
            refreshButtonVisual = new DcduHotspotButton();
            deleteButtonVisual = new DcduHotspotButton();
            pageUpButtonVisual = new DcduHotspotButton();
            exitButton = new DcduHotspotButton();
            screenPanel = new DcduScreenPanel();
            titleLabel = new System.Windows.Forms.Label();
            clockLabel = new System.Windows.Forms.Label();
            atcUnitLabel = new System.Windows.Forms.Label();
            atcUnitDisplay = new System.Windows.Forms.Label();
            statusCaptionLabel = new System.Windows.Forms.Label();
            statusValueLabel = new System.Windows.Forms.Label();
            messageHeaderLabel = new System.Windows.Forms.Label();
            outputTable = new System.Windows.Forms.TableLayoutPanel();
            messageFormatPanel = new System.Windows.Forms.FlowLayoutPanel();
            SendingProgress = new System.Windows.Forms.ProgressBar();
            outputScrollBar = new DcduScrollOverlay();
            iconList = new System.Windows.Forms.ImageList(components);
            dcduFrame.SuspendLayout();
            screenPanel.SuspendLayout();
            SuspendLayout();
            // 
            // dcduFrame
            // 
            dcduFrame.AssetFileName = "DCDU_Main_V15.png";
            dcduFrame.BackColor = System.Drawing.Color.Transparent;
            dcduFrame.Controls.Add(screenPanel);
            dcduFrame.Location = new System.Drawing.Point(0, 0);
            dcduFrame.Name = "dcduFrame";
            dcduFrame.Size = new System.Drawing.Size(700, 311);
            dcduFrame.TabIndex = 0;
            dcduFrame.MouseClick += DcduFrame_MouseClick;
            dcduFrame.MouseMove += DcduFrame_MouseMove;
            dcduFrame.MouseLeave += DcduFrame_MouseLeave;
            dcduFrame.MouseDown += DcduFrame_MouseDown;
            dcduFrame.MouseUp += DcduFrame_MouseUp;
            // 
            // retrieveButton
            // 
            retrieveButton.AccessibleName = "Connect";
            retrieveButton.Location = new System.Drawing.Point(26, 57);
            retrieveButton.Name = "retrieveButton";
            retrieveButton.Size = new System.Drawing.Size(47, 33);
            retrieveButton.TabIndex = 0;
            retrieveButton.Click += RetrieveButton_Click;
            // 
            // telexButton
            // 
            telexButton.AccessibleName = "Telex";
            telexButton.Enabled = false;
            telexButton.Location = new System.Drawing.Point(26, 101);
            telexButton.Name = "telexButton";
            telexButton.Size = new System.Drawing.Size(48, 31);
            telexButton.TabIndex = 1;
            telexButton.Click += TelexButton_Click;
            // 
            // atcButton
            // 
            atcButton.AccessibleName = "ATC";
            atcButton.Enabled = false;
            atcButton.Location = new System.Drawing.Point(25, 143);
            atcButton.Name = "atcButton";
            atcButton.Size = new System.Drawing.Size(48, 32);
            atcButton.TabIndex = 2;
            atcButton.Click += RequestButton_Click;
            // 
            // settingsButton
            // 
            settingsButton.AccessibleName = "Settings";
            settingsButton.Location = new System.Drawing.Point(26, 185);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new System.Drawing.Size(47, 32);
            settingsButton.TabIndex = 3;
            settingsButton.Click += SettingsButton_Click;
            // 
            // helpButton
            // 
            helpButton.AccessibleName = "Help";
            helpButton.Location = new System.Drawing.Point(623, 74);
            helpButton.Name = "helpButton";
            helpButton.Size = new System.Drawing.Size(47, 31);
            helpButton.TabIndex = 4;
            helpButton.Click += HelpButton_Click;
            // 
            // refreshButtonVisual
            // 
            refreshButtonVisual.AccessibleName = "Print";
            refreshButtonVisual.Enabled = false;
            refreshButtonVisual.Location = new System.Drawing.Point(-50, -50);
            refreshButtonVisual.Name = "refreshButtonVisual";
            refreshButtonVisual.Size = new System.Drawing.Size(1, 1);
            refreshButtonVisual.TabIndex = 5;
            // 
            // deleteButtonVisual
            // 
            deleteButtonVisual.AccessibleName = "Page Down";
            deleteButtonVisual.Enabled = false;
            deleteButtonVisual.Location = new System.Drawing.Point(-50, -50);
            deleteButtonVisual.Name = "deleteButtonVisual";
            deleteButtonVisual.Size = new System.Drawing.Size(1, 1);
            deleteButtonVisual.TabIndex = 6;
            // 
            // pageUpButtonVisual
            // 
            pageUpButtonVisual.AccessibleName = "Page Up";
            pageUpButtonVisual.Enabled = false;
            pageUpButtonVisual.Location = new System.Drawing.Point(-50, -50);
            pageUpButtonVisual.Name = "pageUpButtonVisual";
            pageUpButtonVisual.Size = new System.Drawing.Size(1, 1);
            pageUpButtonVisual.TabIndex = 7;
            // 
            // exitButton
            // 
            exitButton.AccessibleName = "Exit";
            exitButton.Location = new System.Drawing.Point(623, 116);
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(47, 31);
            exitButton.TabIndex = 8;
            exitButton.Click += ExitButton_Click;
            // 
            // screenPanel
            // 
            screenPanel.BackColor = System.Drawing.Color.Transparent;
            screenPanel.DrawScreenBackground = false;
            screenPanel.Controls.Add(titleLabel);
            screenPanel.Controls.Add(clockLabel);
            screenPanel.Controls.Add(atcUnitLabel);
            screenPanel.Controls.Add(atcUnitDisplay);
            screenPanel.Controls.Add(statusCaptionLabel);
            screenPanel.Controls.Add(statusValueLabel);
            screenPanel.Controls.Add(messageHeaderLabel);
            screenPanel.Controls.Add(outputTable);
            screenPanel.Controls.Add(messageFormatPanel);
            screenPanel.Controls.Add(SendingProgress);
            screenPanel.Controls.Add(outputScrollBar);
            screenPanel.ForeColor = System.Drawing.Color.FromArgb(224, 232, 238);
            screenPanel.Location = new System.Drawing.Point(103, 34);
            screenPanel.Name = "screenPanel";
            screenPanel.Padding = new System.Windows.Forms.Padding(18);
            screenPanel.Radius = 8;
            screenPanel.Size = new System.Drawing.Size(493, 232);
            screenPanel.TabIndex = 9;
            screenPanel.MouseDown += MoveWindow;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.BackColor = System.Drawing.Color.Transparent;
            titleLabel.Font = new System.Drawing.Font("Consolas", 16F, System.Drawing.FontStyle.Bold);
            titleLabel.ForeColor = System.Drawing.Color.FromArgb(224, 232, 238);
            titleLabel.Location = new System.Drawing.Point(8, 10);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(142, 19);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "EASYCPDLC";
            titleLabel.MouseDown += MoveWindow;
            // 
            // clockLabel
            // 
            clockLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            clockLabel.AutoSize = true;
            clockLabel.BackColor = System.Drawing.Color.Transparent;
            clockLabel.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            clockLabel.ForeColor = System.Drawing.Color.FromArgb(45, 231, 245);
            clockLabel.Location = new System.Drawing.Point(386, 8);
            clockLabel.Name = "clockLabel";
            clockLabel.Size = new System.Drawing.Size(70, 22);
            clockLabel.TabIndex = 13;
            clockLabel.Text = "OPEN";
            clockLabel.MouseDown += MoveWindow;
            // 
            // atcUnitLabel
            // 
            atcUnitLabel.AutoSize = true;
            atcUnitLabel.BackColor = System.Drawing.Color.Transparent;
            atcUnitLabel.Font = new System.Drawing.Font("Consolas", 10.5F, System.Drawing.FontStyle.Bold);
            atcUnitLabel.ForeColor = System.Drawing.Color.FromArgb(224, 232, 238);
            atcUnitLabel.Location = new System.Drawing.Point(238, 38);
            atcUnitLabel.Name = "atcUnitLabel";
            atcUnitLabel.Size = new System.Drawing.Size(150, 18);
            atcUnitLabel.TabIndex = 11;
            atcUnitLabel.Text = "CURRENT ATS UNIT:";
            // 
            // atcUnitDisplay
            // 
            atcUnitDisplay.AutoSize = true;
            atcUnitDisplay.BackColor = System.Drawing.Color.Transparent;
            atcUnitDisplay.Font = new System.Drawing.Font("Consolas", 10.5F, System.Drawing.FontStyle.Bold);
            atcUnitDisplay.ForeColor = System.Drawing.Color.FromArgb(45, 231, 245);
            atcUnitDisplay.Location = new System.Drawing.Point(397, 38);
            atcUnitDisplay.Name = "atcUnitDisplay";
            atcUnitDisplay.Size = new System.Drawing.Size(72, 18);
            atcUnitDisplay.TabIndex = 12;
            atcUnitDisplay.Text = "----";
            // 
            // statusCaptionLabel
            // 
            statusCaptionLabel.AutoSize = true;
            statusCaptionLabel.BackColor = System.Drawing.Color.Transparent;
            statusCaptionLabel.Font = new System.Drawing.Font("Consolas", 10.5F, System.Drawing.FontStyle.Bold);
            statusCaptionLabel.ForeColor = System.Drawing.Color.FromArgb(224, 232, 238);
            statusCaptionLabel.Location = new System.Drawing.Point(8, 38);
            statusCaptionLabel.Name = "statusCaptionLabel";
            statusCaptionLabel.Size = new System.Drawing.Size(74, 18);
            statusCaptionLabel.TabIndex = 9;
            statusCaptionLabel.Text = "STATUS:";
            // 
            // statusValueLabel
            // 
            statusValueLabel.AutoSize = true;
            statusValueLabel.BackColor = System.Drawing.Color.Transparent;
            statusValueLabel.Font = new System.Drawing.Font("Consolas", 10.5F, System.Drawing.FontStyle.Bold);
            statusValueLabel.ForeColor = System.Drawing.Color.FromArgb(255, 210, 76);
            statusValueLabel.Location = new System.Drawing.Point(84, 38);
            statusValueLabel.Name = "statusValueLabel";
            statusValueLabel.Size = new System.Drawing.Size(120, 18);
            statusValueLabel.TabIndex = 10;
            statusValueLabel.Text = "STANDBY";
            // 
            // messageHeaderLabel
            // 
            messageHeaderLabel.AutoSize = true;
            messageHeaderLabel.BackColor = System.Drawing.Color.Transparent;
            messageHeaderLabel.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            messageHeaderLabel.ForeColor = System.Drawing.Color.FromArgb(45, 231, 245);
            messageHeaderLabel.Location = new System.Drawing.Point(8, 66);
            messageHeaderLabel.Name = "messageHeaderLabel";
            messageHeaderLabel.Size = new System.Drawing.Size(220, 20);
            messageHeaderLabel.TabIndex = 14;
            messageHeaderLabel.Text = "MESSAGES / DATA LINK";
            // 
            // outputTable
            // 
            outputTable.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            outputTable.BackColor = System.Drawing.Color.Transparent;
            outputTable.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.None;
            outputTable.ColumnCount = 3;
            outputTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 68F));
            outputTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            outputTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            outputTable.ForeColor = System.Drawing.Color.FromArgb(224, 232, 238);
            outputTable.Location = new System.Drawing.Point(8, 94);
            outputTable.Margin = new System.Windows.Forms.Padding(0);
            outputTable.Name = "outputTable";
            outputTable.Padding = new System.Windows.Forms.Padding(0, 4, 12, 4);
            outputTable.RowCount = 1;
            outputTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            outputTable.Size = new System.Drawing.Size(466, 106);
            outputTable.TabIndex = 3;
            outputTable.TabStop = true;
            outputTable.Click += OutputTable_Click;
            // 
            // messageFormatPanel
            // 
            messageFormatPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            messageFormatPanel.AutoScroll = true;
            messageFormatPanel.BackColor = System.Drawing.Color.Transparent;
            messageFormatPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            messageFormatPanel.ForeColor = System.Drawing.Color.FromArgb(224, 232, 238);
            messageFormatPanel.Location = new System.Drawing.Point(25, 125);
            messageFormatPanel.Margin = new System.Windows.Forms.Padding(0);
            messageFormatPanel.Name = "messageFormatPanel";
            messageFormatPanel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 30);
            messageFormatPanel.Size = new System.Drawing.Size(475, 155);
            messageFormatPanel.TabIndex = 4;
            messageFormatPanel.TabStop = true;
            messageFormatPanel.Visible = false;
            // 
            // SendingProgress
            // 
            SendingProgress.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            SendingProgress.ForeColor = System.Drawing.Color.FromArgb(45, 231, 245);
            SendingProgress.Location = new System.Drawing.Point(8, 206);
            SendingProgress.MarqueeAnimationSpeed = 10;
            SendingProgress.Maximum = 30;
            SendingProgress.Name = "SendingProgress";
            SendingProgress.Size = new System.Drawing.Size(466, 8);
            SendingProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            SendingProgress.TabIndex = 8;
            SendingProgress.Visible = false;
            // 
            // outputScrollBar
            // 
            outputScrollBar.BackColor = System.Drawing.Color.Transparent;
            outputScrollBar.Location = new System.Drawing.Point(476, 94);
            outputScrollBar.Name = "outputScrollBar";
            outputScrollBar.Size = new System.Drawing.Size(8, 106);
            outputScrollBar.TabIndex = 15;
            outputScrollBar.TabStop = false;
            outputScrollBar.Target = outputTable;
            // 
            // 
            // iconList
            // 
            iconList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            iconList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            BackColor = System.Drawing.Color.FromArgb(1, 2, 3);
            ClientSize = new System.Drawing.Size(700, 311);
            Controls.Add(dcduFrame);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximumSize = new System.Drawing.Size(700, 311);
            MinimumSize = new System.Drawing.Size(700, 311);
            Name = "MainForm";
            Text = "EasyCPDLC";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            MouseDown += MoveWindow;
            dcduFrame.ResumeLayout(false);
            screenPanel.ResumeLayout(false);
            screenPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DcduAssetPanel dcduFrame;
        private DcduHotspotButton exitButton;
        private System.Windows.Forms.TableLayoutPanel outputTable;
        private DcduHotspotButton atcButton;
        private DcduHotspotButton telexButton;
        private DcduHotspotButton retrieveButton;
        private System.Windows.Forms.Label atcUnitLabel;
        private System.Windows.Forms.Label atcUnitDisplay;
        private DcduHotspotButton helpButton;
        private System.Windows.Forms.FlowLayoutPanel messageFormatPanel;
        private DcduHotspotButton settingsButton;
        private System.Windows.Forms.ImageList iconList;
        private System.Windows.Forms.ProgressBar SendingProgress;
        private DcduScrollOverlay outputScrollBar;
        private DcduScreenPanel screenPanel;
        private System.Windows.Forms.Label statusCaptionLabel;
        private System.Windows.Forms.Label statusValueLabel;
        private System.Windows.Forms.Label messageHeaderLabel;
        private System.Windows.Forms.Label clockLabel;
        private System.Windows.Forms.Label titleLabel;
        private DcduHotspotButton refreshButtonVisual;
        private DcduHotspotButton deleteButtonVisual;
        private DcduHotspotButton pageUpButtonVisual;
    }
}
