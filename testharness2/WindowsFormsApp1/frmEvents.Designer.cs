namespace testharness2
{
    partial class frmEvents
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
            this.rtx = new System.Windows.Forms.RichTextBox();
            this.tmr = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // rtx
            // 
            this.rtx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtx.Location = new System.Drawing.Point(0, 0);
            this.rtx.Name = "rtx";
            this.rtx.Size = new System.Drawing.Size(792, 772);
            this.rtx.TabIndex = 0;
            this.rtx.Text = "";
            // 
            // tmr
            // 
            this.tmr.Enabled = true;
            this.tmr.Tick += new System.EventHandler(this.tmr_Tick);
            // 
            // frmEvents
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 773);
            this.Controls.Add(this.rtx);
            this.Name = "frmEvents";
            this.Text = "frmEvents";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmEvents_FormClosed);
            this.Load += new System.EventHandler(this.frmEvents_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtx;
        private System.Windows.Forms.Timer tmr;
    }
}