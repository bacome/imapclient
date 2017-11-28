namespace testharness2
{
    partial class frmMailboxes
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
            this.tvw = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.cmdSubscriptions = new System.Windows.Forms.Button();
            this.gbxMailbox = new System.Windows.Forms.GroupBox();
            this.cmdUID = new System.Windows.Forms.Button();
            this.chkOpenOnSelect = new System.Windows.Forms.CheckBox();
            this.cmdExamine = new System.Windows.Forms.Button();
            this.gbxRename = new System.Windows.Forms.GroupBox();
            this.cmdRename = new System.Windows.Forms.Button();
            this.txtRename = new System.Windows.Forms.TextBox();
            this.cmdSelect = new System.Windows.Forms.Button();
            this.cmdSubscribe = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdUnsubscribe = new System.Windows.Forms.Button();
            this.gbxCreate = new System.Windows.Forms.GroupBox();
            this.cmdCreate = new System.Windows.Forms.Button();
            this.chkCreate = new System.Windows.Forms.CheckBox();
            this.txtCreate = new System.Windows.Forms.TextBox();
            this.rtx = new System.Windows.Forms.RichTextBox();
            this.cmdRenameTo = new System.Windows.Forms.Button();
            this.cmdRefresh = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.gbxMailbox.SuspendLayout();
            this.gbxRename.SuspendLayout();
            this.gbxCreate.SuspendLayout();
            this.SuspendLayout();
            // 
            // tvw
            // 
            this.tvw.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvw.Location = new System.Drawing.Point(0, 0);
            this.tvw.Name = "tvw";
            this.tvw.Size = new System.Drawing.Size(313, 571);
            this.tvw.TabIndex = 0;
            this.tvw.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tvw_AfterExpand);
            this.tvw.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvw_AfterSelect);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tvw);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.cmdSubscriptions);
            this.splitContainer1.Panel2.Controls.Add(this.gbxMailbox);
            this.splitContainer1.Panel2.Controls.Add(this.gbxCreate);
            this.splitContainer1.Panel2.Controls.Add(this.rtx);
            this.splitContainer1.Size = new System.Drawing.Size(792, 571);
            this.splitContainer1.SplitterDistance = 315;
            this.splitContainer1.TabIndex = 1;
            // 
            // cmdSubscriptions
            // 
            this.cmdSubscriptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdSubscriptions.Enabled = false;
            this.cmdSubscriptions.Location = new System.Drawing.Point(361, 273);
            this.cmdSubscriptions.Name = "cmdSubscriptions";
            this.cmdSubscriptions.Size = new System.Drawing.Size(100, 25);
            this.cmdSubscriptions.TabIndex = 1;
            this.cmdSubscriptions.Text = "Subscriptions";
            this.cmdSubscriptions.UseVisualStyleBackColor = true;
            this.cmdSubscriptions.Click += new System.EventHandler(this.cmdSubscriptions_Click);
            // 
            // gbxMailbox
            // 
            this.gbxMailbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbxMailbox.Controls.Add(this.cmdRefresh);
            this.gbxMailbox.Controls.Add(this.cmdUID);
            this.gbxMailbox.Controls.Add(this.chkOpenOnSelect);
            this.gbxMailbox.Controls.Add(this.cmdExamine);
            this.gbxMailbox.Controls.Add(this.gbxRename);
            this.gbxMailbox.Controls.Add(this.cmdSelect);
            this.gbxMailbox.Controls.Add(this.cmdSubscribe);
            this.gbxMailbox.Controls.Add(this.cmdDelete);
            this.gbxMailbox.Controls.Add(this.cmdUnsubscribe);
            this.gbxMailbox.Enabled = false;
            this.gbxMailbox.Location = new System.Drawing.Point(0, 304);
            this.gbxMailbox.Name = "gbxMailbox";
            this.gbxMailbox.Size = new System.Drawing.Size(473, 180);
            this.gbxMailbox.TabIndex = 2;
            this.gbxMailbox.TabStop = false;
            this.gbxMailbox.Text = "Mailbox";
            // 
            // cmdUID
            // 
            this.cmdUID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdUID.Location = new System.Drawing.Point(361, 20);
            this.cmdUID.Name = "cmdUID";
            this.cmdUID.Size = new System.Drawing.Size(100, 25);
            this.cmdUID.TabIndex = 3;
            this.cmdUID.Text = "UID ...";
            this.cmdUID.UseVisualStyleBackColor = true;
            this.cmdUID.Click += new System.EventHandler(this.cmdUID_Click);
            // 
            // chkOpenOnSelect
            // 
            this.chkOpenOnSelect.AutoSize = true;
            this.chkOpenOnSelect.Checked = true;
            this.chkOpenOnSelect.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOpenOnSelect.Location = new System.Drawing.Point(236, 25);
            this.chkOpenOnSelect.Name = "chkOpenOnSelect";
            this.chkOpenOnSelect.Size = new System.Drawing.Size(100, 17);
            this.chkOpenOnSelect.TabIndex = 2;
            this.chkOpenOnSelect.Text = "Open on Select";
            this.chkOpenOnSelect.UseVisualStyleBackColor = true;
            // 
            // cmdExamine
            // 
            this.cmdExamine.Location = new System.Drawing.Point(24, 20);
            this.cmdExamine.Name = "cmdExamine";
            this.cmdExamine.Size = new System.Drawing.Size(100, 25);
            this.cmdExamine.TabIndex = 0;
            this.cmdExamine.Text = "Select Read Only";
            this.cmdExamine.UseVisualStyleBackColor = true;
            this.cmdExamine.Click += new System.EventHandler(this.cmdExamine_Click);
            // 
            // gbxRename
            // 
            this.gbxRename.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbxRename.Controls.Add(this.cmdRenameTo);
            this.gbxRename.Controls.Add(this.cmdRename);
            this.gbxRename.Controls.Add(this.txtRename);
            this.gbxRename.Location = new System.Drawing.Point(12, 82);
            this.gbxRename.Name = "gbxRename";
            this.gbxRename.Size = new System.Drawing.Size(455, 83);
            this.gbxRename.TabIndex = 8;
            this.gbxRename.TabStop = false;
            this.gbxRename.Text = "Rename";
            // 
            // cmdRename
            // 
            this.cmdRename.Location = new System.Drawing.Point(12, 47);
            this.cmdRename.Name = "cmdRename";
            this.cmdRename.Size = new System.Drawing.Size(100, 25);
            this.cmdRename.TabIndex = 1;
            this.cmdRename.Text = "Rename";
            this.cmdRename.UseVisualStyleBackColor = true;
            this.cmdRename.Click += new System.EventHandler(this.cmdRename_Click);
            // 
            // txtRename
            // 
            this.txtRename.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRename.Location = new System.Drawing.Point(12, 21);
            this.txtRename.Name = "txtRename";
            this.txtRename.Size = new System.Drawing.Size(437, 20);
            this.txtRename.TabIndex = 0;
            // 
            // cmdSelect
            // 
            this.cmdSelect.Location = new System.Drawing.Point(130, 20);
            this.cmdSelect.Name = "cmdSelect";
            this.cmdSelect.Size = new System.Drawing.Size(100, 25);
            this.cmdSelect.TabIndex = 1;
            this.cmdSelect.Text = "Select for Update";
            this.cmdSelect.UseVisualStyleBackColor = true;
            this.cmdSelect.Click += new System.EventHandler(this.cmdSelect_Click);
            // 
            // cmdSubscribe
            // 
            this.cmdSubscribe.Location = new System.Drawing.Point(24, 51);
            this.cmdSubscribe.Name = "cmdSubscribe";
            this.cmdSubscribe.Size = new System.Drawing.Size(100, 25);
            this.cmdSubscribe.TabIndex = 4;
            this.cmdSubscribe.Text = "Subscribe";
            this.cmdSubscribe.UseVisualStyleBackColor = true;
            this.cmdSubscribe.Click += new System.EventHandler(this.cmdSubscribe_Click);
            // 
            // cmdDelete
            // 
            this.cmdDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdDelete.Location = new System.Drawing.Point(361, 51);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(100, 25);
            this.cmdDelete.TabIndex = 7;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            this.cmdDelete.Click += new System.EventHandler(this.cmdDelete_Click);
            // 
            // cmdUnsubscribe
            // 
            this.cmdUnsubscribe.Location = new System.Drawing.Point(130, 51);
            this.cmdUnsubscribe.Name = "cmdUnsubscribe";
            this.cmdUnsubscribe.Size = new System.Drawing.Size(100, 25);
            this.cmdUnsubscribe.TabIndex = 5;
            this.cmdUnsubscribe.Text = "Unsubscribe";
            this.cmdUnsubscribe.UseVisualStyleBackColor = true;
            this.cmdUnsubscribe.Click += new System.EventHandler(this.cmdUnsubscribe_Click);
            // 
            // gbxCreate
            // 
            this.gbxCreate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbxCreate.Controls.Add(this.cmdCreate);
            this.gbxCreate.Controls.Add(this.chkCreate);
            this.gbxCreate.Controls.Add(this.txtCreate);
            this.gbxCreate.Enabled = false;
            this.gbxCreate.Location = new System.Drawing.Point(0, 490);
            this.gbxCreate.Name = "gbxCreate";
            this.gbxCreate.Size = new System.Drawing.Size(473, 81);
            this.gbxCreate.TabIndex = 3;
            this.gbxCreate.TabStop = false;
            this.gbxCreate.Text = "Create Child Mailbox";
            // 
            // cmdCreate
            // 
            this.cmdCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCreate.Location = new System.Drawing.Point(361, 47);
            this.cmdCreate.Name = "cmdCreate";
            this.cmdCreate.Size = new System.Drawing.Size(100, 25);
            this.cmdCreate.TabIndex = 2;
            this.cmdCreate.Text = "Create";
            this.cmdCreate.UseVisualStyleBackColor = true;
            this.cmdCreate.Click += new System.EventHandler(this.cmdCreate_Click);
            // 
            // chkCreate
            // 
            this.chkCreate.AutoSize = true;
            this.chkCreate.Location = new System.Drawing.Point(24, 47);
            this.chkCreate.Name = "chkCreate";
            this.chkCreate.Size = new System.Drawing.Size(105, 17);
            this.chkCreate.TabIndex = 1;
            this.chkCreate.Text = "As Future Parent";
            this.chkCreate.UseVisualStyleBackColor = true;
            // 
            // txtCreate
            // 
            this.txtCreate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCreate.Location = new System.Drawing.Point(24, 21);
            this.txtCreate.Name = "txtCreate";
            this.txtCreate.Size = new System.Drawing.Size(437, 20);
            this.txtCreate.TabIndex = 0;
            // 
            // rtx
            // 
            this.rtx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtx.Location = new System.Drawing.Point(0, 0);
            this.rtx.Name = "rtx";
            this.rtx.Size = new System.Drawing.Size(473, 262);
            this.rtx.TabIndex = 0;
            this.rtx.Text = "";
            // 
            // cmdRenameTo
            // 
            this.cmdRenameTo.Location = new System.Drawing.Point(118, 47);
            this.cmdRenameTo.Name = "cmdRenameTo";
            this.cmdRenameTo.Size = new System.Drawing.Size(100, 25);
            this.cmdRenameTo.TabIndex = 2;
            this.cmdRenameTo.Text = "Rename ...";
            this.cmdRenameTo.UseVisualStyleBackColor = true;
            this.cmdRenameTo.Click += new System.EventHandler(this.cmdRenameTo_Click);
            // 
            // cmdRefresh
            // 
            this.cmdRefresh.Location = new System.Drawing.Point(236, 51);
            this.cmdRefresh.Name = "cmdRefresh";
            this.cmdRefresh.Size = new System.Drawing.Size(100, 25);
            this.cmdRefresh.TabIndex = 6;
            this.cmdRefresh.Text = "Refresh";
            this.cmdRefresh.UseVisualStyleBackColor = true;
            this.cmdRefresh.Click += new System.EventHandler(this.cmdRefresh_Click);
            // 
            // frmMailboxes
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 573);
            this.Controls.Add(this.splitContainer1);
            this.Name = "frmMailboxes";
            this.Text = "frmMailboxes";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMailboxes_FormClosed);
            this.Load += new System.EventHandler(this.frmMailboxes_Load);
            this.Shown += new System.EventHandler(this.frmMailboxes_Shown);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.gbxMailbox.ResumeLayout(false);
            this.gbxMailbox.PerformLayout();
            this.gbxRename.ResumeLayout(false);
            this.gbxRename.PerformLayout();
            this.gbxCreate.ResumeLayout(false);
            this.gbxCreate.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tvw;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox gbxRename;
        private System.Windows.Forms.Button cmdRename;
        private System.Windows.Forms.TextBox txtRename;
        private System.Windows.Forms.GroupBox gbxCreate;
        private System.Windows.Forms.Button cmdCreate;
        private System.Windows.Forms.CheckBox chkCreate;
        private System.Windows.Forms.TextBox txtCreate;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button cmdUnsubscribe;
        private System.Windows.Forms.Button cmdSubscribe;
        private System.Windows.Forms.Button cmdSelect;
        private System.Windows.Forms.Button cmdExamine;
        private System.Windows.Forms.RichTextBox rtx;
        private System.Windows.Forms.GroupBox gbxMailbox;
        private System.Windows.Forms.Button cmdSubscriptions;
        private System.Windows.Forms.Button cmdUID;
        private System.Windows.Forms.CheckBox chkOpenOnSelect;
        private System.Windows.Forms.Button cmdRenameTo;
        private System.Windows.Forms.Button cmdRefresh;
    }
}