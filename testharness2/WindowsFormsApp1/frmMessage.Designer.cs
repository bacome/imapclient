namespace testharness2
{
    partial class frmMessage
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
            this.cmdPrevious = new System.Windows.Forms.Button();
            this.cmdNext = new System.Windows.Forms.Button();
            this.chkAutoRefresh = new System.Windows.Forms.CheckBox();
            this.tab = new System.Windows.Forms.TabControl();
            this.tpgEnvelope = new System.Windows.Forms.TabPage();
            this.rtxEnvelope = new System.Windows.Forms.RichTextBox();
            this.tpgText = new System.Windows.Forms.TabPage();
            this.rtxText = new System.Windows.Forms.RichTextBox();
            this.tpgAttachments = new System.Windows.Forms.TabPage();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.tpgFlags = new System.Windows.Forms.TabPage();
            this.chkFred = new System.Windows.Forms.CheckBox();
            this.chkDeleted = new System.Windows.Forms.CheckBox();
            this.chkSeen = new System.Windows.Forms.CheckBox();
            this.rtxFlags = new System.Windows.Forms.RichTextBox();
            this.tpgBodyStructure = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tvwBodyStructure = new System.Windows.Forms.TreeView();
            this.tabBodyStructure = new System.Windows.Forms.TabControl();
            this.tpgSummary = new System.Windows.Forms.TabPage();
            this.rtxSummary = new System.Windows.Forms.RichTextBox();
            this.tpgRaw = new System.Windows.Forms.TabPage();
            this.cmdDownloadRaw = new System.Windows.Forms.Button();
            this.rtxRaw = new System.Windows.Forms.RichTextBox();
            this.tpgDecoded = new System.Windows.Forms.TabPage();
            this.pbx = new System.Windows.Forms.PictureBox();
            this.cmdDownloadDecoded = new System.Windows.Forms.Button();
            this.rtxDecoded = new System.Windows.Forms.RichTextBox();
            this.tpgOther = new System.Windows.Forms.TabPage();
            this.rtxOther = new System.Windows.Forms.RichTextBox();
            this.lblQueryError = new System.Windows.Forms.Label();
            this.tab.SuspendLayout();
            this.tpgEnvelope.SuspendLayout();
            this.tpgText.SuspendLayout();
            this.tpgAttachments.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.tpgFlags.SuspendLayout();
            this.tpgBodyStructure.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabBodyStructure.SuspendLayout();
            this.tpgSummary.SuspendLayout();
            this.tpgRaw.SuspendLayout();
            this.tpgDecoded.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx)).BeginInit();
            this.tpgOther.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdPrevious
            // 
            this.cmdPrevious.Location = new System.Drawing.Point(5, 5);
            this.cmdPrevious.Name = "cmdPrevious";
            this.cmdPrevious.Size = new System.Drawing.Size(100, 25);
            this.cmdPrevious.TabIndex = 0;
            this.cmdPrevious.Text = "Previous";
            this.cmdPrevious.UseVisualStyleBackColor = true;
            this.cmdPrevious.Click += new System.EventHandler(this.cmdPrevious_Click);
            // 
            // cmdNext
            // 
            this.cmdNext.Location = new System.Drawing.Point(111, 5);
            this.cmdNext.Name = "cmdNext";
            this.cmdNext.Size = new System.Drawing.Size(100, 25);
            this.cmdNext.TabIndex = 1;
            this.cmdNext.Text = "Next";
            this.cmdNext.UseVisualStyleBackColor = true;
            this.cmdNext.Click += new System.EventHandler(this.cmdNext_Click);
            // 
            // chkAutoRefresh
            // 
            this.chkAutoRefresh.AutoSize = true;
            this.chkAutoRefresh.Checked = true;
            this.chkAutoRefresh.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoRefresh.Location = new System.Drawing.Point(217, 10);
            this.chkAutoRefresh.Name = "chkAutoRefresh";
            this.chkAutoRefresh.Size = new System.Drawing.Size(88, 17);
            this.chkAutoRefresh.TabIndex = 2;
            this.chkAutoRefresh.Text = "Auto Refresh";
            this.chkAutoRefresh.UseVisualStyleBackColor = true;
            this.chkAutoRefresh.CheckedChanged += new System.EventHandler(this.chkAutoRefresh_CheckedChanged);
            // 
            // tab
            // 
            this.tab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tab.Controls.Add(this.tpgEnvelope);
            this.tab.Controls.Add(this.tpgText);
            this.tab.Controls.Add(this.tpgAttachments);
            this.tab.Controls.Add(this.tpgFlags);
            this.tab.Controls.Add(this.tpgBodyStructure);
            this.tab.Controls.Add(this.tpgOther);
            this.tab.Location = new System.Drawing.Point(5, 36);
            this.tab.Name = "tab";
            this.tab.SelectedIndex = 0;
            this.tab.Size = new System.Drawing.Size(781, 532);
            this.tab.TabIndex = 4;
            this.tab.Selected += new System.Windows.Forms.TabControlEventHandler(this.tab_Selected);
            // 
            // tpgEnvelope
            // 
            this.tpgEnvelope.Controls.Add(this.rtxEnvelope);
            this.tpgEnvelope.Location = new System.Drawing.Point(4, 22);
            this.tpgEnvelope.Name = "tpgEnvelope";
            this.tpgEnvelope.Padding = new System.Windows.Forms.Padding(3);
            this.tpgEnvelope.Size = new System.Drawing.Size(773, 506);
            this.tpgEnvelope.TabIndex = 4;
            this.tpgEnvelope.Text = "Envelope";
            this.tpgEnvelope.UseVisualStyleBackColor = true;
            // 
            // rtxEnvelope
            // 
            this.rtxEnvelope.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxEnvelope.Location = new System.Drawing.Point(6, 6);
            this.rtxEnvelope.Name = "rtxEnvelope";
            this.rtxEnvelope.Size = new System.Drawing.Size(761, 494);
            this.rtxEnvelope.TabIndex = 0;
            this.rtxEnvelope.Text = "";
            // 
            // tpgText
            // 
            this.tpgText.Controls.Add(this.rtxText);
            this.tpgText.Location = new System.Drawing.Point(4, 22);
            this.tpgText.Name = "tpgText";
            this.tpgText.Padding = new System.Windows.Forms.Padding(3);
            this.tpgText.Size = new System.Drawing.Size(773, 506);
            this.tpgText.TabIndex = 0;
            this.tpgText.Text = "Text";
            this.tpgText.UseVisualStyleBackColor = true;
            // 
            // rtxText
            // 
            this.rtxText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxText.Location = new System.Drawing.Point(6, 6);
            this.rtxText.Name = "rtxText";
            this.rtxText.Size = new System.Drawing.Size(761, 494);
            this.rtxText.TabIndex = 1;
            this.rtxText.Text = "";
            // 
            // tpgAttachments
            // 
            this.tpgAttachments.Controls.Add(this.dgv);
            this.tpgAttachments.Location = new System.Drawing.Point(4, 22);
            this.tpgAttachments.Name = "tpgAttachments";
            this.tpgAttachments.Padding = new System.Windows.Forms.Padding(3);
            this.tpgAttachments.Size = new System.Drawing.Size(773, 506);
            this.tpgAttachments.TabIndex = 1;
            this.tpgAttachments.Text = "Attachments";
            this.tpgAttachments.UseVisualStyleBackColor = true;
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgv.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Location = new System.Drawing.Point(3, 3);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.Size = new System.Drawing.Size(767, 500);
            this.dgv.TabIndex = 0;
            this.dgv.RowHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_RowHeaderMouseDoubleClick);
            // 
            // tpgFlags
            // 
            this.tpgFlags.Controls.Add(this.chkFred);
            this.tpgFlags.Controls.Add(this.chkDeleted);
            this.tpgFlags.Controls.Add(this.chkSeen);
            this.tpgFlags.Controls.Add(this.rtxFlags);
            this.tpgFlags.Location = new System.Drawing.Point(4, 22);
            this.tpgFlags.Name = "tpgFlags";
            this.tpgFlags.Padding = new System.Windows.Forms.Padding(3);
            this.tpgFlags.Size = new System.Drawing.Size(773, 506);
            this.tpgFlags.TabIndex = 2;
            this.tpgFlags.Text = "Flags";
            this.tpgFlags.UseVisualStyleBackColor = true;
            // 
            // chkFred
            // 
            this.chkFred.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkFred.AutoSize = true;
            this.chkFred.Location = new System.Drawing.Point(132, 489);
            this.chkFred.Name = "chkFred";
            this.chkFred.Size = new System.Drawing.Size(47, 17);
            this.chkFred.TabIndex = 3;
            this.chkFred.Text = "Fred";
            this.chkFred.UseVisualStyleBackColor = true;
            // 
            // chkDeleted
            // 
            this.chkDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkDeleted.AutoSize = true;
            this.chkDeleted.Location = new System.Drawing.Point(63, 489);
            this.chkDeleted.Name = "chkDeleted";
            this.chkDeleted.Size = new System.Drawing.Size(63, 17);
            this.chkDeleted.TabIndex = 2;
            this.chkDeleted.Text = "Deleted";
            this.chkDeleted.UseVisualStyleBackColor = true;
            // 
            // chkSeen
            // 
            this.chkSeen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkSeen.AutoSize = true;
            this.chkSeen.Location = new System.Drawing.Point(6, 489);
            this.chkSeen.Name = "chkSeen";
            this.chkSeen.Size = new System.Drawing.Size(51, 17);
            this.chkSeen.TabIndex = 1;
            this.chkSeen.Text = "Seen";
            this.chkSeen.UseVisualStyleBackColor = true;
            // 
            // rtxFlags
            // 
            this.rtxFlags.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxFlags.Location = new System.Drawing.Point(6, 6);
            this.rtxFlags.Name = "rtxFlags";
            this.rtxFlags.Size = new System.Drawing.Size(761, 477);
            this.rtxFlags.TabIndex = 0;
            this.rtxFlags.Text = "";
            // 
            // tpgBodyStructure
            // 
            this.tpgBodyStructure.Controls.Add(this.splitContainer1);
            this.tpgBodyStructure.Location = new System.Drawing.Point(4, 22);
            this.tpgBodyStructure.Name = "tpgBodyStructure";
            this.tpgBodyStructure.Padding = new System.Windows.Forms.Padding(3);
            this.tpgBodyStructure.Size = new System.Drawing.Size(773, 506);
            this.tpgBodyStructure.TabIndex = 3;
            this.tpgBodyStructure.Text = "Body Structure";
            this.tpgBodyStructure.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tvwBodyStructure);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabBodyStructure);
            this.splitContainer1.Size = new System.Drawing.Size(767, 500);
            this.splitContainer1.SplitterDistance = 255;
            this.splitContainer1.TabIndex = 0;
            // 
            // tvwBodyStructure
            // 
            this.tvwBodyStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvwBodyStructure.Location = new System.Drawing.Point(0, 0);
            this.tvwBodyStructure.Name = "tvwBodyStructure";
            this.tvwBodyStructure.Size = new System.Drawing.Size(252, 500);
            this.tvwBodyStructure.TabIndex = 0;
            this.tvwBodyStructure.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvwBodyStructure_AfterSelect);
            // 
            // tabBodyStructure
            // 
            this.tabBodyStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabBodyStructure.Controls.Add(this.tpgSummary);
            this.tabBodyStructure.Controls.Add(this.tpgRaw);
            this.tabBodyStructure.Controls.Add(this.tpgDecoded);
            this.tabBodyStructure.Location = new System.Drawing.Point(3, 0);
            this.tabBodyStructure.Name = "tabBodyStructure";
            this.tabBodyStructure.SelectedIndex = 0;
            this.tabBodyStructure.Size = new System.Drawing.Size(505, 500);
            this.tabBodyStructure.TabIndex = 1;
            this.tabBodyStructure.Selected += new System.Windows.Forms.TabControlEventHandler(this.tabBodyStructure_Selected);
            // 
            // tpgSummary
            // 
            this.tpgSummary.Controls.Add(this.rtxSummary);
            this.tpgSummary.Location = new System.Drawing.Point(4, 22);
            this.tpgSummary.Name = "tpgSummary";
            this.tpgSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tpgSummary.Size = new System.Drawing.Size(497, 474);
            this.tpgSummary.TabIndex = 0;
            this.tpgSummary.Text = "Summary";
            this.tpgSummary.UseVisualStyleBackColor = true;
            // 
            // rtxSummary
            // 
            this.rtxSummary.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxSummary.Location = new System.Drawing.Point(3, 3);
            this.rtxSummary.Name = "rtxSummary";
            this.rtxSummary.Size = new System.Drawing.Size(491, 468);
            this.rtxSummary.TabIndex = 0;
            this.rtxSummary.Text = "";
            // 
            // tpgRaw
            // 
            this.tpgRaw.Controls.Add(this.cmdDownloadRaw);
            this.tpgRaw.Controls.Add(this.rtxRaw);
            this.tpgRaw.Location = new System.Drawing.Point(4, 22);
            this.tpgRaw.Name = "tpgRaw";
            this.tpgRaw.Padding = new System.Windows.Forms.Padding(3);
            this.tpgRaw.Size = new System.Drawing.Size(497, 474);
            this.tpgRaw.TabIndex = 1;
            this.tpgRaw.Text = "Raw";
            this.tpgRaw.UseVisualStyleBackColor = true;
            // 
            // cmdDownloadRaw
            // 
            this.cmdDownloadRaw.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdDownloadRaw.Location = new System.Drawing.Point(394, 446);
            this.cmdDownloadRaw.Name = "cmdDownloadRaw";
            this.cmdDownloadRaw.Size = new System.Drawing.Size(100, 25);
            this.cmdDownloadRaw.TabIndex = 2;
            this.cmdDownloadRaw.Text = "Download";
            this.cmdDownloadRaw.UseVisualStyleBackColor = true;
            this.cmdDownloadRaw.Click += new System.EventHandler(this.cmdDownloadRaw_Click);
            // 
            // rtxRaw
            // 
            this.rtxRaw.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxRaw.Location = new System.Drawing.Point(3, 3);
            this.rtxRaw.Name = "rtxRaw";
            this.rtxRaw.Size = new System.Drawing.Size(491, 437);
            this.rtxRaw.TabIndex = 1;
            this.rtxRaw.Text = "";
            // 
            // tpgDecoded
            // 
            this.tpgDecoded.Controls.Add(this.pbx);
            this.tpgDecoded.Controls.Add(this.cmdDownloadDecoded);
            this.tpgDecoded.Controls.Add(this.rtxDecoded);
            this.tpgDecoded.Location = new System.Drawing.Point(4, 22);
            this.tpgDecoded.Name = "tpgDecoded";
            this.tpgDecoded.Padding = new System.Windows.Forms.Padding(3);
            this.tpgDecoded.Size = new System.Drawing.Size(497, 474);
            this.tpgDecoded.TabIndex = 2;
            this.tpgDecoded.Text = "Decoded";
            this.tpgDecoded.UseVisualStyleBackColor = true;
            // 
            // pbx
            // 
            this.pbx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbx.Location = new System.Drawing.Point(105, 53);
            this.pbx.Name = "pbx";
            this.pbx.Size = new System.Drawing.Size(165, 316);
            this.pbx.TabIndex = 4;
            this.pbx.TabStop = false;
            // 
            // cmdDownloadDecoded
            // 
            this.cmdDownloadDecoded.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdDownloadDecoded.Location = new System.Drawing.Point(394, 446);
            this.cmdDownloadDecoded.Name = "cmdDownloadDecoded";
            this.cmdDownloadDecoded.Size = new System.Drawing.Size(100, 25);
            this.cmdDownloadDecoded.TabIndex = 3;
            this.cmdDownloadDecoded.Text = "Download";
            this.cmdDownloadDecoded.UseVisualStyleBackColor = true;
            this.cmdDownloadDecoded.Click += new System.EventHandler(this.cmdDownloadDecoded_Click);
            // 
            // rtxDecoded
            // 
            this.rtxDecoded.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxDecoded.Location = new System.Drawing.Point(3, 3);
            this.rtxDecoded.Name = "rtxDecoded";
            this.rtxDecoded.Size = new System.Drawing.Size(491, 437);
            this.rtxDecoded.TabIndex = 1;
            this.rtxDecoded.Text = "";
            // 
            // tpgOther
            // 
            this.tpgOther.Controls.Add(this.rtxOther);
            this.tpgOther.Location = new System.Drawing.Point(4, 22);
            this.tpgOther.Name = "tpgOther";
            this.tpgOther.Padding = new System.Windows.Forms.Padding(3);
            this.tpgOther.Size = new System.Drawing.Size(773, 506);
            this.tpgOther.TabIndex = 5;
            this.tpgOther.Text = "Other";
            this.tpgOther.UseVisualStyleBackColor = true;
            // 
            // rtxOther
            // 
            this.rtxOther.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxOther.Location = new System.Drawing.Point(3, 6);
            this.rtxOther.Name = "rtxOther";
            this.rtxOther.Size = new System.Drawing.Size(764, 494);
            this.rtxOther.TabIndex = 1;
            this.rtxOther.Text = "";
            // 
            // lblQueryError
            // 
            this.lblQueryError.AutoSize = true;
            this.lblQueryError.Location = new System.Drawing.Point(311, 11);
            this.lblQueryError.Name = "lblQueryError";
            this.lblQueryError.Size = new System.Drawing.Size(60, 13);
            this.lblQueryError.TabIndex = 3;
            this.lblQueryError.Text = "Query Error";
            // 
            // frmMessage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 573);
            this.Controls.Add(this.lblQueryError);
            this.Controls.Add(this.tab);
            this.Controls.Add(this.chkAutoRefresh);
            this.Controls.Add(this.cmdNext);
            this.Controls.Add(this.cmdPrevious);
            this.Name = "frmMessage";
            this.Text = "frmMessage";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMessage_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMessage_FormClosed);
            this.Load += new System.EventHandler(this.frmMessage_Load);
            this.tab.ResumeLayout(false);
            this.tpgEnvelope.ResumeLayout(false);
            this.tpgText.ResumeLayout(false);
            this.tpgAttachments.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.tpgFlags.ResumeLayout(false);
            this.tpgFlags.PerformLayout();
            this.tpgBodyStructure.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabBodyStructure.ResumeLayout(false);
            this.tpgSummary.ResumeLayout(false);
            this.tpgRaw.ResumeLayout(false);
            this.tpgDecoded.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbx)).EndInit();
            this.tpgOther.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdPrevious;
        private System.Windows.Forms.Button cmdNext;
        private System.Windows.Forms.CheckBox chkAutoRefresh;
        private System.Windows.Forms.TabControl tab;
        private System.Windows.Forms.TabPage tpgText;
        private System.Windows.Forms.TabPage tpgAttachments;
        private System.Windows.Forms.TabPage tpgFlags;
        private System.Windows.Forms.TabPage tpgBodyStructure;
        private System.Windows.Forms.TabPage tpgEnvelope;
        private System.Windows.Forms.RichTextBox rtxEnvelope;
        private System.Windows.Forms.RichTextBox rtxText;
        private System.Windows.Forms.TabPage tpgOther;
        private System.Windows.Forms.RichTextBox rtxOther;
        private System.Windows.Forms.CheckBox chkFred;
        private System.Windows.Forms.CheckBox chkDeleted;
        private System.Windows.Forms.CheckBox chkSeen;
        private System.Windows.Forms.RichTextBox rtxFlags;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView tvwBodyStructure;
        private System.Windows.Forms.RichTextBox rtxSummary;
        private System.Windows.Forms.Label lblQueryError;
        private System.Windows.Forms.TabControl tabBodyStructure;
        private System.Windows.Forms.TabPage tpgSummary;
        private System.Windows.Forms.TabPage tpgRaw;
        private System.Windows.Forms.TabPage tpgDecoded;
        private System.Windows.Forms.RichTextBox rtxRaw;
        private System.Windows.Forms.RichTextBox rtxDecoded;
        private System.Windows.Forms.Button cmdDownloadRaw;
        private System.Windows.Forms.Button cmdDownloadDecoded;
        private System.Windows.Forms.PictureBox pbx;
        private System.Windows.Forms.DataGridView dgv;
    }
}