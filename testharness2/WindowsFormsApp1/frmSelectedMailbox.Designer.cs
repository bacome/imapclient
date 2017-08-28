namespace testharness2
{
    partial class frmSelectedMailbox
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
            this.lblMailboxName = new System.Windows.Forms.Label();
            this.cmdExpunge = new System.Windows.Forms.Button();
            this.cmdClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblMailboxName
            // 
            this.lblMailboxName.AutoSize = true;
            this.lblMailboxName.Location = new System.Drawing.Point(9, 9);
            this.lblMailboxName.Name = "lblMailboxName";
            this.lblMailboxName.Size = new System.Drawing.Size(74, 13);
            this.lblMailboxName.TabIndex = 0;
            this.lblMailboxName.Text = "Mailbox Name";
            // 
            // cmdExpunge
            // 
            this.cmdExpunge.Location = new System.Drawing.Point(12, 25);
            this.cmdExpunge.Name = "cmdExpunge";
            this.cmdExpunge.Size = new System.Drawing.Size(100, 25);
            this.cmdExpunge.TabIndex = 1;
            this.cmdExpunge.Text = "Expunge";
            this.cmdExpunge.UseVisualStyleBackColor = true;
            this.cmdExpunge.Click += new System.EventHandler(this.cmdExpunge_Click);
            // 
            // cmdClose
            // 
            this.cmdClose.Location = new System.Drawing.Point(118, 25);
            this.cmdClose.Name = "cmdClose";
            this.cmdClose.Size = new System.Drawing.Size(100, 25);
            this.cmdClose.TabIndex = 2;
            this.cmdClose.Text = "Expunge && Close";
            this.cmdClose.UseVisualStyleBackColor = true;
            this.cmdClose.Click += new System.EventHandler(this.cmdClose_Click);
            // 
            // frmSelectedMailbox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Controls.Add(this.cmdClose);
            this.Controls.Add(this.cmdExpunge);
            this.Controls.Add(this.lblMailboxName);
            this.Name = "frmSelectedMailbox";
            this.Text = "frmSelectedMailbox";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmSelectedMailbox_FormClosed);
            this.Load += new System.EventHandler(this.frmSelectedMailbox_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblMailboxName;
        private System.Windows.Forms.Button cmdExpunge;
        private System.Windows.Forms.Button cmdClose;
    }
}