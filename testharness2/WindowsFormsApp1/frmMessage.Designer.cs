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
            this.components = new System.ComponentModel.Container();
            this.cmdPrevious = new System.Windows.Forms.Button();
            this.cmdNext = new System.Windows.Forms.Button();
            this.tab = new System.Windows.Forms.TabControl();
            this.tpgEnvelope = new System.Windows.Forms.TabPage();
            this.rtxEnvelope = new System.Windows.Forms.RichTextBox();
            this.tpgText = new System.Windows.Forms.TabPage();
            this.rtxText = new System.Windows.Forms.RichTextBox();
            this.tpgAttachments = new System.Windows.Forms.TabPage();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.tpgFlags = new System.Windows.Forms.TabPage();
            this.cmdStore = new System.Windows.Forms.Button();
            this.gbxFlags = new System.Windows.Forms.GroupBox();
            this.chkSubmitPending = new System.Windows.Forms.CheckBox();
            this.chkForwarded = new System.Windows.Forms.CheckBox();
            this.chkDraft = new System.Windows.Forms.CheckBox();
            this.chkSeen = new System.Windows.Forms.CheckBox();
            this.chkDeleted = new System.Windows.Forms.CheckBox();
            this.chkFlagged = new System.Windows.Forms.CheckBox();
            this.chkAnswered = new System.Windows.Forms.CheckBox();
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
            this.tpgMessageStream = new System.Windows.Forms.TabPage();
            this.pnlMessageStream = new System.Windows.Forms.Panel();
            this.gbxMSType = new System.Windows.Forms.GroupBox();
            this.rdoMSTMessage = new System.Windows.Forms.RadioButton();
            this.rdoMSTUID = new System.Windows.Forms.RadioButton();
            this.cmdDownloadStream = new System.Windows.Forms.Button();
            this.gbxDecoding = new System.Windows.Forms.GroupBox();
            this.rdoMSDOther = new System.Windows.Forms.RadioButton();
            this.rdoMSDBase64 = new System.Windows.Forms.RadioButton();
            this.rdoMSDQuotedPrintable = new System.Windows.Forms.RadioButton();
            this.rdoMSDNone = new System.Windows.Forms.RadioButton();
            this.txtMSTargetBufferSize = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tpgOther = new System.Windows.Forms.TabPage();
            this.rtxOther = new System.Windows.Forms.RichTextBox();
            this.lblQueryError = new System.Windows.Forms.Label();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.cmdCopyTo = new System.Windows.Forms.Button();
            this.cmdCopyForAppend = new System.Windows.Forms.Button();
            this.cmdCopyPartForAppend = new System.Windows.Forms.Button();
            this.tab.SuspendLayout();
            this.tpgEnvelope.SuspendLayout();
            this.tpgText.SuspendLayout();
            this.tpgAttachments.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.tpgFlags.SuspendLayout();
            this.gbxFlags.SuspendLayout();
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
            this.tpgMessageStream.SuspendLayout();
            this.pnlMessageStream.SuspendLayout();
            this.gbxMSType.SuspendLayout();
            this.gbxDecoding.SuspendLayout();
            this.tpgOther.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
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
            this.tab.TabIndex = 5;
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
            this.tpgFlags.Controls.Add(this.cmdStore);
            this.tpgFlags.Controls.Add(this.gbxFlags);
            this.tpgFlags.Controls.Add(this.rtxFlags);
            this.tpgFlags.Location = new System.Drawing.Point(4, 22);
            this.tpgFlags.Name = "tpgFlags";
            this.tpgFlags.Padding = new System.Windows.Forms.Padding(3);
            this.tpgFlags.Size = new System.Drawing.Size(773, 506);
            this.tpgFlags.TabIndex = 2;
            this.tpgFlags.Text = "Flags";
            this.tpgFlags.UseVisualStyleBackColor = true;
            // 
            // cmdStore
            // 
            this.cmdStore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdStore.Location = new System.Drawing.Point(6, 475);
            this.cmdStore.Name = "cmdStore";
            this.cmdStore.Size = new System.Drawing.Size(100, 25);
            this.cmdStore.TabIndex = 2;
            this.cmdStore.Text = "Store ...";
            this.cmdStore.UseVisualStyleBackColor = true;
            this.cmdStore.Click += new System.EventHandler(this.cmdStore_Click);
            // 
            // gbxFlags
            // 
            this.gbxFlags.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbxFlags.Controls.Add(this.chkSubmitPending);
            this.gbxFlags.Controls.Add(this.chkForwarded);
            this.gbxFlags.Controls.Add(this.chkDraft);
            this.gbxFlags.Controls.Add(this.chkSeen);
            this.gbxFlags.Controls.Add(this.chkDeleted);
            this.gbxFlags.Controls.Add(this.chkFlagged);
            this.gbxFlags.Controls.Add(this.chkAnswered);
            this.gbxFlags.Location = new System.Drawing.Point(6, 401);
            this.gbxFlags.Name = "gbxFlags";
            this.gbxFlags.Size = new System.Drawing.Size(761, 68);
            this.gbxFlags.TabIndex = 1;
            this.gbxFlags.TabStop = false;
            this.gbxFlags.Text = "Flags";
            // 
            // chkSubmitPending
            // 
            this.chkSubmitPending.AutoSize = true;
            this.chkSubmitPending.Location = new System.Drawing.Point(227, 42);
            this.chkSubmitPending.Name = "chkSubmitPending";
            this.chkSubmitPending.Size = new System.Drawing.Size(100, 17);
            this.chkSubmitPending.TabIndex = 6;
            this.chkSubmitPending.Text = "Submit Pending";
            this.chkSubmitPending.UseVisualStyleBackColor = true;
            this.chkSubmitPending.CheckedChanged += new System.EventHandler(this.chkSubmitPending_CheckedChanged);
            // 
            // chkForwarded
            // 
            this.chkForwarded.AutoSize = true;
            this.chkForwarded.Location = new System.Drawing.Point(15, 42);
            this.chkForwarded.Name = "chkForwarded";
            this.chkForwarded.Size = new System.Drawing.Size(76, 17);
            this.chkForwarded.TabIndex = 5;
            this.chkForwarded.Text = "Forwarded";
            this.chkForwarded.UseVisualStyleBackColor = true;
            this.chkForwarded.CheckedChanged += new System.EventHandler(this.chkForwarded_CheckedChanged);
            // 
            // chkDraft
            // 
            this.chkDraft.AutoSize = true;
            this.chkDraft.Location = new System.Drawing.Point(227, 19);
            this.chkDraft.Name = "chkDraft";
            this.chkDraft.Size = new System.Drawing.Size(49, 17);
            this.chkDraft.TabIndex = 2;
            this.chkDraft.Text = "Draft";
            this.chkDraft.UseVisualStyleBackColor = true;
            this.chkDraft.CheckedChanged += new System.EventHandler(this.chkDraft_CheckedChanged);
            // 
            // chkSeen
            // 
            this.chkSeen.AutoSize = true;
            this.chkSeen.Location = new System.Drawing.Point(439, 19);
            this.chkSeen.Name = "chkSeen";
            this.chkSeen.Size = new System.Drawing.Size(51, 17);
            this.chkSeen.TabIndex = 4;
            this.chkSeen.Text = "Seen";
            this.chkSeen.UseVisualStyleBackColor = true;
            this.chkSeen.CheckedChanged += new System.EventHandler(this.chkSeen_CheckedChanged);
            // 
            // chkDeleted
            // 
            this.chkDeleted.AutoSize = true;
            this.chkDeleted.Location = new System.Drawing.Point(121, 19);
            this.chkDeleted.Name = "chkDeleted";
            this.chkDeleted.Size = new System.Drawing.Size(63, 17);
            this.chkDeleted.TabIndex = 1;
            this.chkDeleted.Text = "Deleted";
            this.chkDeleted.UseVisualStyleBackColor = true;
            this.chkDeleted.CheckedChanged += new System.EventHandler(this.chkDeleted_CheckedChanged);
            // 
            // chkFlagged
            // 
            this.chkFlagged.AutoSize = true;
            this.chkFlagged.Location = new System.Drawing.Point(333, 19);
            this.chkFlagged.Name = "chkFlagged";
            this.chkFlagged.Size = new System.Drawing.Size(64, 17);
            this.chkFlagged.TabIndex = 3;
            this.chkFlagged.Text = "Flagged";
            this.chkFlagged.UseVisualStyleBackColor = true;
            this.chkFlagged.CheckedChanged += new System.EventHandler(this.chkFlagged_CheckedChanged);
            // 
            // chkAnswered
            // 
            this.chkAnswered.AutoSize = true;
            this.chkAnswered.Location = new System.Drawing.Point(15, 19);
            this.chkAnswered.Name = "chkAnswered";
            this.chkAnswered.Size = new System.Drawing.Size(73, 17);
            this.chkAnswered.TabIndex = 0;
            this.chkAnswered.Text = "Answered";
            this.chkAnswered.UseVisualStyleBackColor = true;
            this.chkAnswered.CheckedChanged += new System.EventHandler(this.chkAnswered_CheckedChanged);
            // 
            // rtxFlags
            // 
            this.rtxFlags.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxFlags.Location = new System.Drawing.Point(6, 6);
            this.rtxFlags.Name = "rtxFlags";
            this.rtxFlags.Size = new System.Drawing.Size(761, 389);
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
            this.tabBodyStructure.Controls.Add(this.tpgMessageStream);
            this.tabBodyStructure.Location = new System.Drawing.Point(3, 0);
            this.tabBodyStructure.Name = "tabBodyStructure";
            this.tabBodyStructure.SelectedIndex = 0;
            this.tabBodyStructure.Size = new System.Drawing.Size(505, 500);
            this.tabBodyStructure.TabIndex = 1;
            this.tabBodyStructure.Selected += new System.Windows.Forms.TabControlEventHandler(this.tabBodyStructure_Selected);
            // 
            // tpgSummary
            // 
            this.tpgSummary.Controls.Add(this.cmdCopyPartForAppend);
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
            this.rtxSummary.Size = new System.Drawing.Size(491, 437);
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
            // tpgMessageStream
            // 
            this.tpgMessageStream.Controls.Add(this.pnlMessageStream);
            this.tpgMessageStream.Location = new System.Drawing.Point(4, 22);
            this.tpgMessageStream.Name = "tpgMessageStream";
            this.tpgMessageStream.Size = new System.Drawing.Size(497, 474);
            this.tpgMessageStream.TabIndex = 3;
            this.tpgMessageStream.Text = "Message Stream";
            this.tpgMessageStream.UseVisualStyleBackColor = true;
            // 
            // pnlMessageStream
            // 
            this.pnlMessageStream.Controls.Add(this.gbxMSType);
            this.pnlMessageStream.Controls.Add(this.cmdDownloadStream);
            this.pnlMessageStream.Controls.Add(this.gbxDecoding);
            this.pnlMessageStream.Controls.Add(this.txtMSTargetBufferSize);
            this.pnlMessageStream.Controls.Add(this.label1);
            this.pnlMessageStream.Location = new System.Drawing.Point(3, 3);
            this.pnlMessageStream.Name = "pnlMessageStream";
            this.pnlMessageStream.Size = new System.Drawing.Size(481, 193);
            this.pnlMessageStream.TabIndex = 5;
            // 
            // gbxMSType
            // 
            this.gbxMSType.Controls.Add(this.rdoMSTMessage);
            this.gbxMSType.Controls.Add(this.rdoMSTUID);
            this.gbxMSType.Location = new System.Drawing.Point(3, 3);
            this.gbxMSType.Name = "gbxMSType";
            this.gbxMSType.Size = new System.Drawing.Size(389, 47);
            this.gbxMSType.TabIndex = 0;
            this.gbxMSType.TabStop = false;
            this.gbxMSType.Text = "Stream Type";
            // 
            // rdoMSTMessage
            // 
            this.rdoMSTMessage.AutoSize = true;
            this.rdoMSTMessage.Checked = true;
            this.rdoMSTMessage.Location = new System.Drawing.Point(98, 19);
            this.rdoMSTMessage.Name = "rdoMSTMessage";
            this.rdoMSTMessage.Size = new System.Drawing.Size(68, 17);
            this.rdoMSTMessage.TabIndex = 1;
            this.rdoMSTMessage.TabStop = true;
            this.rdoMSTMessage.Text = "Message";
            this.rdoMSTMessage.UseVisualStyleBackColor = true;
            // 
            // rdoMSTUID
            // 
            this.rdoMSTUID.AutoSize = true;
            this.rdoMSTUID.Location = new System.Drawing.Point(18, 19);
            this.rdoMSTUID.Name = "rdoMSTUID";
            this.rdoMSTUID.Size = new System.Drawing.Size(44, 17);
            this.rdoMSTUID.TabIndex = 0;
            this.rdoMSTUID.Text = "UID";
            this.rdoMSTUID.UseVisualStyleBackColor = true;
            // 
            // cmdDownloadStream
            // 
            this.cmdDownloadStream.Location = new System.Drawing.Point(292, 111);
            this.cmdDownloadStream.Name = "cmdDownloadStream";
            this.cmdDownloadStream.Size = new System.Drawing.Size(100, 25);
            this.cmdDownloadStream.TabIndex = 4;
            this.cmdDownloadStream.Text = "Download";
            this.cmdDownloadStream.UseVisualStyleBackColor = true;
            this.cmdDownloadStream.Click += new System.EventHandler(this.cmdDownloadStream_Click);
            // 
            // gbxDecoding
            // 
            this.gbxDecoding.Controls.Add(this.rdoMSDOther);
            this.gbxDecoding.Controls.Add(this.rdoMSDBase64);
            this.gbxDecoding.Controls.Add(this.rdoMSDQuotedPrintable);
            this.gbxDecoding.Controls.Add(this.rdoMSDNone);
            this.gbxDecoding.Location = new System.Drawing.Point(3, 56);
            this.gbxDecoding.Name = "gbxDecoding";
            this.gbxDecoding.Size = new System.Drawing.Size(389, 47);
            this.gbxDecoding.TabIndex = 1;
            this.gbxDecoding.TabStop = false;
            this.gbxDecoding.Text = "Decoding";
            // 
            // rdoMSDOther
            // 
            this.rdoMSDOther.AutoSize = true;
            this.rdoMSDOther.Location = new System.Drawing.Point(18, 19);
            this.rdoMSDOther.Name = "rdoMSDOther";
            this.rdoMSDOther.Size = new System.Drawing.Size(51, 17);
            this.rdoMSDOther.TabIndex = 3;
            this.rdoMSDOther.Text = "Other";
            this.rdoMSDOther.UseVisualStyleBackColor = true;
            // 
            // rdoMSDBase64
            // 
            this.rdoMSDBase64.AutoSize = true;
            this.rdoMSDBase64.Location = new System.Drawing.Point(178, 19);
            this.rdoMSDBase64.Name = "rdoMSDBase64";
            this.rdoMSDBase64.Size = new System.Drawing.Size(61, 17);
            this.rdoMSDBase64.TabIndex = 1;
            this.rdoMSDBase64.Text = "Base64";
            this.rdoMSDBase64.UseVisualStyleBackColor = true;
            // 
            // rdoMSDQuotedPrintable
            // 
            this.rdoMSDQuotedPrintable.AutoSize = true;
            this.rdoMSDQuotedPrintable.Location = new System.Drawing.Point(258, 19);
            this.rdoMSDQuotedPrintable.Name = "rdoMSDQuotedPrintable";
            this.rdoMSDQuotedPrintable.Size = new System.Drawing.Size(104, 17);
            this.rdoMSDQuotedPrintable.TabIndex = 2;
            this.rdoMSDQuotedPrintable.Text = "Quoted-Printable";
            this.rdoMSDQuotedPrintable.UseVisualStyleBackColor = true;
            // 
            // rdoMSDNone
            // 
            this.rdoMSDNone.AutoSize = true;
            this.rdoMSDNone.Checked = true;
            this.rdoMSDNone.Location = new System.Drawing.Point(98, 19);
            this.rdoMSDNone.Name = "rdoMSDNone";
            this.rdoMSDNone.Size = new System.Drawing.Size(51, 17);
            this.rdoMSDNone.TabIndex = 0;
            this.rdoMSDNone.TabStop = true;
            this.rdoMSDNone.Text = "None";
            this.rdoMSDNone.UseVisualStyleBackColor = true;
            // 
            // txtMSTargetBufferSize
            // 
            this.txtMSTargetBufferSize.Location = new System.Drawing.Point(101, 114);
            this.txtMSTargetBufferSize.Name = "txtMSTargetBufferSize";
            this.txtMSTargetBufferSize.Size = new System.Drawing.Size(68, 20);
            this.txtMSTargetBufferSize.TabIndex = 3;
            this.txtMSTargetBufferSize.Text = "1000000";
            this.txtMSTargetBufferSize.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtMSTargetBufferSize.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 117);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Target Buffer Size";
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
            this.lblQueryError.Location = new System.Drawing.Point(217, 11);
            this.lblQueryError.Name = "lblQueryError";
            this.lblQueryError.Size = new System.Drawing.Size(60, 13);
            this.lblQueryError.TabIndex = 2;
            this.lblQueryError.Text = "Query Error";
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // cmdCopyTo
            // 
            this.cmdCopyTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCopyTo.Location = new System.Drawing.Point(686, 5);
            this.cmdCopyTo.Name = "cmdCopyTo";
            this.cmdCopyTo.Size = new System.Drawing.Size(100, 25);
            this.cmdCopyTo.TabIndex = 4;
            this.cmdCopyTo.Text = "Copy To ...";
            this.cmdCopyTo.UseVisualStyleBackColor = true;
            this.cmdCopyTo.Click += new System.EventHandler(this.cmdCopyTo_Click);
            // 
            // cmdCopyForAppend
            // 
            this.cmdCopyForAppend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCopyForAppend.Location = new System.Drawing.Point(580, 5);
            this.cmdCopyForAppend.Name = "cmdCopyForAppend";
            this.cmdCopyForAppend.Size = new System.Drawing.Size(100, 25);
            this.cmdCopyForAppend.TabIndex = 3;
            this.cmdCopyForAppend.Text = "Copy for Append";
            this.cmdCopyForAppend.UseVisualStyleBackColor = true;
            this.cmdCopyForAppend.Click += new System.EventHandler(this.cmdCopyForAppend_Click);
            // 
            // cmdCopyPartForAppend
            // 
            this.cmdCopyPartForAppend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCopyPartForAppend.Location = new System.Drawing.Point(394, 446);
            this.cmdCopyPartForAppend.Name = "cmdCopyPartForAppend";
            this.cmdCopyPartForAppend.Size = new System.Drawing.Size(100, 25);
            this.cmdCopyPartForAppend.TabIndex = 4;
            this.cmdCopyPartForAppend.Text = "Copy for Append";
            this.cmdCopyPartForAppend.UseVisualStyleBackColor = true;
            this.cmdCopyPartForAppend.Click += new System.EventHandler(this.cmdCopyPartForAppend_Click);
            // 
            // frmMessage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(792, 573);
            this.Controls.Add(this.cmdCopyForAppend);
            this.Controls.Add(this.cmdCopyTo);
            this.Controls.Add(this.lblQueryError);
            this.Controls.Add(this.tab);
            this.Controls.Add(this.cmdNext);
            this.Controls.Add(this.cmdPrevious);
            this.Name = "frmMessage";
            this.Text = "frmMessage";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMessage_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMessage_FormClosed);
            this.Load += new System.EventHandler(this.frmMessage_Load);
            this.Shown += new System.EventHandler(this.frmMessage_Shown);
            this.tab.ResumeLayout(false);
            this.tpgEnvelope.ResumeLayout(false);
            this.tpgText.ResumeLayout(false);
            this.tpgAttachments.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.tpgFlags.ResumeLayout(false);
            this.gbxFlags.ResumeLayout(false);
            this.gbxFlags.PerformLayout();
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
            this.tpgMessageStream.ResumeLayout(false);
            this.pnlMessageStream.ResumeLayout(false);
            this.pnlMessageStream.PerformLayout();
            this.gbxMSType.ResumeLayout(false);
            this.gbxMSType.PerformLayout();
            this.gbxDecoding.ResumeLayout(false);
            this.gbxDecoding.PerformLayout();
            this.tpgOther.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdPrevious;
        private System.Windows.Forms.Button cmdNext;
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
        private System.Windows.Forms.ErrorProvider erp;
        private System.Windows.Forms.GroupBox gbxFlags;
        private System.Windows.Forms.CheckBox chkSubmitPending;
        private System.Windows.Forms.CheckBox chkForwarded;
        private System.Windows.Forms.CheckBox chkDraft;
        private System.Windows.Forms.CheckBox chkSeen;
        private System.Windows.Forms.CheckBox chkDeleted;
        private System.Windows.Forms.CheckBox chkFlagged;
        private System.Windows.Forms.CheckBox chkAnswered;
        private System.Windows.Forms.Button cmdStore;
        private System.Windows.Forms.Button cmdCopyTo;
        private System.Windows.Forms.TabPage tpgMessageStream;
        private System.Windows.Forms.Button cmdDownloadStream;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtMSTargetBufferSize;
        private System.Windows.Forms.GroupBox gbxDecoding;
        private System.Windows.Forms.RadioButton rdoMSDBase64;
        private System.Windows.Forms.RadioButton rdoMSDQuotedPrintable;
        private System.Windows.Forms.RadioButton rdoMSDNone;
        private System.Windows.Forms.GroupBox gbxMSType;
        private System.Windows.Forms.RadioButton rdoMSTMessage;
        private System.Windows.Forms.RadioButton rdoMSTUID;
        private System.Windows.Forms.Panel pnlMessageStream;
        private System.Windows.Forms.RadioButton rdoMSDOther;
        private System.Windows.Forms.Button cmdCopyForAppend;
        private System.Windows.Forms.Button cmdCopyPartForAppend;
    }
}