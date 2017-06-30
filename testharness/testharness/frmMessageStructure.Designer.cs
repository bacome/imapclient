namespace testharness
{
    partial class frmMessageStructure
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
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.tvwBodyStructure = new System.Windows.Forms.TreeView();
            this.cmdDownloadRaw = new System.Windows.Forms.Button();
            this.cmdDownload = new System.Windows.Forms.Button();
            this.cmdInspectRaw = new System.Windows.Forms.Button();
            this.cmdInspect = new System.Windows.Forms.Button();
            this.rtxPartDetail = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer3
            // 
            this.splitContainer3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.tvwBodyStructure);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.cmdDownloadRaw);
            this.splitContainer3.Panel2.Controls.Add(this.cmdDownload);
            this.splitContainer3.Panel2.Controls.Add(this.cmdInspectRaw);
            this.splitContainer3.Panel2.Controls.Add(this.cmdInspect);
            this.splitContainer3.Panel2.Controls.Add(this.rtxPartDetail);
            this.splitContainer3.Size = new System.Drawing.Size(516, 367);
            this.splitContainer3.SplitterDistance = 171;
            this.splitContainer3.TabIndex = 4;
            // 
            // tvwBodyStructure
            // 
            this.tvwBodyStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvwBodyStructure.Location = new System.Drawing.Point(3, 3);
            this.tvwBodyStructure.Name = "tvwBodyStructure";
            this.tvwBodyStructure.Size = new System.Drawing.Size(165, 361);
            this.tvwBodyStructure.TabIndex = 2;
            this.tvwBodyStructure.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvwBodyStructure_AfterSelect);
            // 
            // cmdDownloadRaw
            // 
            this.cmdDownloadRaw.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdDownloadRaw.Location = new System.Drawing.Point(144, 312);
            this.cmdDownloadRaw.Name = "cmdDownloadRaw";
            this.cmdDownloadRaw.Size = new System.Drawing.Size(94, 23);
            this.cmdDownloadRaw.TabIndex = 4;
            this.cmdDownloadRaw.Text = "Download Raw";
            this.cmdDownloadRaw.UseVisualStyleBackColor = true;
            this.cmdDownloadRaw.Click += new System.EventHandler(this.cmdDownloadRaw_Click);
            // 
            // cmdDownload
            // 
            this.cmdDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdDownload.Location = new System.Drawing.Point(244, 312);
            this.cmdDownload.Name = "cmdDownload";
            this.cmdDownload.Size = new System.Drawing.Size(94, 23);
            this.cmdDownload.TabIndex = 3;
            this.cmdDownload.Text = "Download";
            this.cmdDownload.UseVisualStyleBackColor = true;
            this.cmdDownload.Click += new System.EventHandler(this.cmdDownload_Click);
            // 
            // cmdInspectRaw
            // 
            this.cmdInspectRaw.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdInspectRaw.Location = new System.Drawing.Point(144, 341);
            this.cmdInspectRaw.Name = "cmdInspectRaw";
            this.cmdInspectRaw.Size = new System.Drawing.Size(94, 23);
            this.cmdInspectRaw.TabIndex = 2;
            this.cmdInspectRaw.Text = "Inspect Raw";
            this.cmdInspectRaw.UseVisualStyleBackColor = true;
            this.cmdInspectRaw.Click += new System.EventHandler(this.cmdInspectRaw_Click);
            // 
            // cmdInspect
            // 
            this.cmdInspect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdInspect.Location = new System.Drawing.Point(244, 341);
            this.cmdInspect.Name = "cmdInspect";
            this.cmdInspect.Size = new System.Drawing.Size(94, 23);
            this.cmdInspect.TabIndex = 1;
            this.cmdInspect.Text = "Inspect";
            this.cmdInspect.UseVisualStyleBackColor = true;
            this.cmdInspect.Click += new System.EventHandler(this.cmdInspect_Click);
            // 
            // rtxPartDetail
            // 
            this.rtxPartDetail.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxPartDetail.Location = new System.Drawing.Point(3, 3);
            this.rtxPartDetail.Name = "rtxPartDetail";
            this.rtxPartDetail.Size = new System.Drawing.Size(335, 303);
            this.rtxPartDetail.TabIndex = 0;
            this.rtxPartDetail.Text = "";
            // 
            // frmMessageStructure
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(516, 367);
            this.Controls.Add(this.splitContainer3);
            this.Name = "frmMessageStructure";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmMessageStructure";
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer3;
        public System.Windows.Forms.TreeView tvwBodyStructure;
        private System.Windows.Forms.RichTextBox rtxPartDetail;
        private System.Windows.Forms.Button cmdDownloadRaw;
        private System.Windows.Forms.Button cmdDownload;
        private System.Windows.Forms.Button cmdInspectRaw;
        private System.Windows.Forms.Button cmdInspect;
    }
}