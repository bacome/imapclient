namespace testharness2
{
    partial class frmStorableFlagsDialog
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
            this.label2 = new System.Windows.Forms.Label();
            this.txtFlags = new System.Windows.Forms.TextBox();
            this.chkSubmitted = new System.Windows.Forms.CheckBox();
            this.chkSubmitPending = new System.Windows.Forms.CheckBox();
            this.chkForwarded = new System.Windows.Forms.CheckBox();
            this.chkDraft = new System.Windows.Forms.CheckBox();
            this.chkSeen = new System.Windows.Forms.CheckBox();
            this.chkDeleted = new System.Windows.Forms.CheckBox();
            this.chkFlagged = new System.Windows.Forms.CheckBox();
            this.chkAnswered = new System.Windows.Forms.CheckBox();
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 13);
            this.label2.TabIndex = 36;
            this.label2.Text = "Flags (space separated)";
            // 
            // txtFlags
            // 
            this.txtFlags.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFlags.Location = new System.Drawing.Point(222, 58);
            this.txtFlags.Name = "txtFlags";
            this.txtFlags.Size = new System.Drawing.Size(313, 20);
            this.txtFlags.TabIndex = 34;
            this.txtFlags.Validating += new System.ComponentModel.CancelEventHandler(this.ZValFlagNames);
            this.txtFlags.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // chkSubmitted
            // 
            this.chkSubmitted.AutoSize = true;
            this.chkSubmitted.Location = new System.Drawing.Point(222, 35);
            this.chkSubmitted.Name = "chkSubmitted";
            this.chkSubmitted.Size = new System.Drawing.Size(73, 17);
            this.chkSubmitted.TabIndex = 33;
            this.chkSubmitted.Text = "Submitted";
            this.chkSubmitted.UseVisualStyleBackColor = true;
            // 
            // chkSubmitPending
            // 
            this.chkSubmitPending.AutoSize = true;
            this.chkSubmitPending.Location = new System.Drawing.Point(116, 35);
            this.chkSubmitPending.Name = "chkSubmitPending";
            this.chkSubmitPending.Size = new System.Drawing.Size(100, 17);
            this.chkSubmitPending.TabIndex = 32;
            this.chkSubmitPending.Text = "Submit Pending";
            this.chkSubmitPending.UseVisualStyleBackColor = true;
            // 
            // chkForwarded
            // 
            this.chkForwarded.AutoSize = true;
            this.chkForwarded.Location = new System.Drawing.Point(10, 35);
            this.chkForwarded.Name = "chkForwarded";
            this.chkForwarded.Size = new System.Drawing.Size(76, 17);
            this.chkForwarded.TabIndex = 31;
            this.chkForwarded.Text = "Forwarded";
            this.chkForwarded.UseVisualStyleBackColor = true;
            // 
            // chkDraft
            // 
            this.chkDraft.AutoSize = true;
            this.chkDraft.Checked = true;
            this.chkDraft.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDraft.Location = new System.Drawing.Point(222, 12);
            this.chkDraft.Name = "chkDraft";
            this.chkDraft.Size = new System.Drawing.Size(49, 17);
            this.chkDraft.TabIndex = 28;
            this.chkDraft.Text = "Draft";
            this.chkDraft.UseVisualStyleBackColor = true;
            // 
            // chkSeen
            // 
            this.chkSeen.AutoSize = true;
            this.chkSeen.Location = new System.Drawing.Point(434, 12);
            this.chkSeen.Name = "chkSeen";
            this.chkSeen.Size = new System.Drawing.Size(51, 17);
            this.chkSeen.TabIndex = 30;
            this.chkSeen.Text = "Seen";
            this.chkSeen.UseVisualStyleBackColor = true;
            // 
            // chkDeleted
            // 
            this.chkDeleted.AutoSize = true;
            this.chkDeleted.Location = new System.Drawing.Point(116, 12);
            this.chkDeleted.Name = "chkDeleted";
            this.chkDeleted.Size = new System.Drawing.Size(63, 17);
            this.chkDeleted.TabIndex = 27;
            this.chkDeleted.Text = "Deleted";
            this.chkDeleted.UseVisualStyleBackColor = true;
            // 
            // chkFlagged
            // 
            this.chkFlagged.AutoSize = true;
            this.chkFlagged.Location = new System.Drawing.Point(328, 12);
            this.chkFlagged.Name = "chkFlagged";
            this.chkFlagged.Size = new System.Drawing.Size(64, 17);
            this.chkFlagged.TabIndex = 29;
            this.chkFlagged.Text = "Flagged";
            this.chkFlagged.UseVisualStyleBackColor = true;
            // 
            // chkAnswered
            // 
            this.chkAnswered.AutoSize = true;
            this.chkAnswered.Location = new System.Drawing.Point(10, 12);
            this.chkAnswered.Name = "chkAnswered";
            this.chkAnswered.Size = new System.Drawing.Size(73, 17);
            this.chkAnswered.TabIndex = 26;
            this.chkAnswered.Text = "Answered";
            this.chkAnswered.UseVisualStyleBackColor = true;
            // 
            // cmdOK
            // 
            this.cmdOK.Location = new System.Drawing.Point(222, 84);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(100, 25);
            this.cmdOK.TabIndex = 38;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(328, 84);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(100, 25);
            this.cmdCancel.TabIndex = 39;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // frmStorableFlagsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(559, 114);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtFlags);
            this.Controls.Add(this.chkSubmitted);
            this.Controls.Add(this.chkSubmitPending);
            this.Controls.Add(this.chkForwarded);
            this.Controls.Add(this.chkDraft);
            this.Controls.Add(this.chkSeen);
            this.Controls.Add(this.chkDeleted);
            this.Controls.Add(this.chkFlagged);
            this.Controls.Add(this.chkAnswered);
            this.Name = "frmStorableFlagsDialog";
            this.Text = "frmStorableFlagsDialog";
            this.Load += new System.EventHandler(this.frmStorableFlagsDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtFlags;
        private System.Windows.Forms.CheckBox chkSubmitted;
        private System.Windows.Forms.CheckBox chkSubmitPending;
        private System.Windows.Forms.CheckBox chkForwarded;
        private System.Windows.Forms.CheckBox chkDraft;
        private System.Windows.Forms.CheckBox chkSeen;
        private System.Windows.Forms.CheckBox chkDeleted;
        private System.Windows.Forms.CheckBox chkFlagged;
        private System.Windows.Forms.CheckBox chkAnswered;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.ErrorProvider erp;
    }
}