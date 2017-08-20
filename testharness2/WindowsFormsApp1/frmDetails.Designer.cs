namespace testharness2
{
    partial class frmDetails
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
            this.rtx = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // rtx
            // 
            this.rtx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtx.Location = new System.Drawing.Point(0, 0);
            this.rtx.Name = "rtx";
            this.rtx.Size = new System.Drawing.Size(104, 116);
            this.rtx.TabIndex = 0;
            this.rtx.Text = "";
            // 
            // frmDetails
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(104, 117);
            this.Controls.Add(this.rtx);
            this.Name = "frmDetails";
            this.Text = "frmDetails";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmDetails_FormClosed);
            this.Load += new System.EventHandler(this.frmDetails_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtx;
    }
}