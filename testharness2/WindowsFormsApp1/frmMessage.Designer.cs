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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.rtxEnvelope = new System.Windows.Forms.RichTextBox();
            this.tbpText = new System.Windows.Forms.TabPage();
            this.rtxText = new System.Windows.Forms.RichTextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.chkFred = new System.Windows.Forms.CheckBox();
            this.chkDeleted = new System.Windows.Forms.CheckBox();
            this.chkSeen = new System.Windows.Forms.CheckBox();
            this.rtxFlags = new System.Windows.Forms.RichTextBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.rtxOther = new System.Windows.Forms.RichTextBox();
            this.tabControl1.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tbpText.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage5.SuspendLayout();
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
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Controls.Add(this.tbpText);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Location = new System.Drawing.Point(5, 36);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(528, 299);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.rtxEnvelope);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(520, 273);
            this.tabPage4.TabIndex = 4;
            this.tabPage4.Text = "Envelope";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // rtxEnvelope
            // 
            this.rtxEnvelope.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxEnvelope.Location = new System.Drawing.Point(6, 6);
            this.rtxEnvelope.Name = "rtxEnvelope";
            this.rtxEnvelope.Size = new System.Drawing.Size(508, 261);
            this.rtxEnvelope.TabIndex = 0;
            this.rtxEnvelope.Text = "";
            // 
            // tbpText
            // 
            this.tbpText.Controls.Add(this.rtxText);
            this.tbpText.Location = new System.Drawing.Point(4, 22);
            this.tbpText.Name = "tbpText";
            this.tbpText.Padding = new System.Windows.Forms.Padding(3);
            this.tbpText.Size = new System.Drawing.Size(520, 273);
            this.tbpText.TabIndex = 0;
            this.tbpText.Text = "Text";
            this.tbpText.UseVisualStyleBackColor = true;
            // 
            // rtxText
            // 
            this.rtxText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxText.Location = new System.Drawing.Point(6, 6);
            this.rtxText.Name = "rtxText";
            this.rtxText.Size = new System.Drawing.Size(508, 261);
            this.rtxText.TabIndex = 1;
            this.rtxText.Text = "";
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(520, 273);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Attachments";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.chkFred);
            this.tabPage1.Controls.Add(this.chkDeleted);
            this.tabPage1.Controls.Add(this.chkSeen);
            this.tabPage1.Controls.Add(this.rtxFlags);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(520, 273);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "Flags";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // chkFred
            // 
            this.chkFred.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkFred.AutoSize = true;
            this.chkFred.Location = new System.Drawing.Point(126, 256);
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
            this.chkDeleted.Location = new System.Drawing.Point(57, 256);
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
            this.chkSeen.Location = new System.Drawing.Point(0, 256);
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
            this.rtxFlags.Size = new System.Drawing.Size(508, 244);
            this.rtxFlags.TabIndex = 0;
            this.rtxFlags.Text = "";
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(520, 273);
            this.tabPage3.TabIndex = 3;
            this.tabPage3.Text = "Body Structure";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.rtxOther);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(520, 273);
            this.tabPage5.TabIndex = 5;
            this.tabPage5.Text = "Other";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // rtxOther
            // 
            this.rtxOther.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxOther.Location = new System.Drawing.Point(3, 6);
            this.rtxOther.Name = "rtxOther";
            this.rtxOther.Size = new System.Drawing.Size(511, 261);
            this.rtxOther.TabIndex = 1;
            this.rtxOther.Text = "";
            // 
            // frmMessage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(539, 340);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.chkAutoRefresh);
            this.Controls.Add(this.cmdNext);
            this.Controls.Add(this.cmdPrevious);
            this.Name = "frmMessage";
            this.Text = "frmMessage";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMessage_FormClosed);
            this.Load += new System.EventHandler(this.frmMessage_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tbpText.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage5.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdPrevious;
        private System.Windows.Forms.Button cmdNext;
        private System.Windows.Forms.CheckBox chkAutoRefresh;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tbpText;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.RichTextBox rtxEnvelope;
        private System.Windows.Forms.RichTextBox rtxText;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.RichTextBox rtxOther;
        private System.Windows.Forms.CheckBox chkFred;
        private System.Windows.Forms.CheckBox chkDeleted;
        private System.Windows.Forms.CheckBox chkSeen;
        private System.Windows.Forms.RichTextBox rtxFlags;
    }
}