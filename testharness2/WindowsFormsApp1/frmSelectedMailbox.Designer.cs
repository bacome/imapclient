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
            this.cmdExpunge = new System.Windows.Forms.Button();
            this.cmdClose = new System.Windows.Forms.Button();
            this.rtx = new System.Windows.Forms.RichTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.cmdOverrideSortSet = new System.Windows.Forms.Button();
            this.cmdFilter = new System.Windows.Forms.Button();
            this.dgvMessages = new System.Windows.Forms.DataGridView();
            this.lblOverrideSort = new System.Windows.Forms.Label();
            this.gbxOverrideSort = new System.Windows.Forms.GroupBox();
            this.cmdOverrideSortClear = new System.Windows.Forms.Button();
            this.cmdFilterClear = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMessages)).BeginInit();
            this.gbxOverrideSort.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdExpunge
            // 
            this.cmdExpunge.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdExpunge.Location = new System.Drawing.Point(15, 544);
            this.cmdExpunge.Name = "cmdExpunge";
            this.cmdExpunge.Size = new System.Drawing.Size(100, 25);
            this.cmdExpunge.TabIndex = 4;
            this.cmdExpunge.Text = "Expunge";
            this.cmdExpunge.UseVisualStyleBackColor = true;
            this.cmdExpunge.Click += new System.EventHandler(this.cmdExpunge_Click);
            // 
            // cmdClose
            // 
            this.cmdClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdClose.Location = new System.Drawing.Point(121, 544);
            this.cmdClose.Name = "cmdClose";
            this.cmdClose.Size = new System.Drawing.Size(100, 25);
            this.cmdClose.TabIndex = 5;
            this.cmdClose.Text = "Expunge && Close";
            this.cmdClose.UseVisualStyleBackColor = true;
            this.cmdClose.Click += new System.EventHandler(this.cmdClose_Click);
            // 
            // rtx
            // 
            this.rtx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtx.Location = new System.Drawing.Point(3, 3);
            this.rtx.Name = "rtx";
            this.rtx.Size = new System.Drawing.Size(314, 422);
            this.rtx.TabIndex = 0;
            this.rtx.Text = "";
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
            this.splitContainer1.Panel1.Controls.Add(this.cmdFilterClear);
            this.splitContainer1.Panel1.Controls.Add(this.gbxOverrideSort);
            this.splitContainer1.Panel1.Controls.Add(this.cmdFilter);
            this.splitContainer1.Panel1.Controls.Add(this.rtx);
            this.splitContainer1.Panel1.Controls.Add(this.cmdExpunge);
            this.splitContainer1.Panel1.Controls.Add(this.cmdClose);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvMessages);
            this.splitContainer1.Size = new System.Drawing.Size(792, 572);
            this.splitContainer1.SplitterDistance = 320;
            this.splitContainer1.TabIndex = 0;
            // 
            // cmdOverrideSortSet
            // 
            this.cmdOverrideSortSet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdOverrideSortSet.Location = new System.Drawing.Point(12, 40);
            this.cmdOverrideSortSet.Name = "cmdOverrideSortSet";
            this.cmdOverrideSortSet.Size = new System.Drawing.Size(100, 25);
            this.cmdOverrideSortSet.TabIndex = 1;
            this.cmdOverrideSortSet.Text = "Set";
            this.cmdOverrideSortSet.UseVisualStyleBackColor = true;
            this.cmdOverrideSortSet.Click += new System.EventHandler(this.cmdOverrideSort_Click);
            // 
            // cmdFilter
            // 
            this.cmdFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdFilter.Location = new System.Drawing.Point(15, 431);
            this.cmdFilter.Name = "cmdFilter";
            this.cmdFilter.Size = new System.Drawing.Size(100, 25);
            this.cmdFilter.TabIndex = 1;
            this.cmdFilter.Text = "Filter";
            this.cmdFilter.UseVisualStyleBackColor = true;
            this.cmdFilter.Click += new System.EventHandler(this.cmdFilter_Click);
            // 
            // dgvMessages
            // 
            this.dgvMessages.AllowUserToAddRows = false;
            this.dgvMessages.AllowUserToDeleteRows = false;
            this.dgvMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvMessages.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvMessages.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvMessages.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMessages.Location = new System.Drawing.Point(3, 3);
            this.dgvMessages.Name = "dgvMessages";
            this.dgvMessages.ReadOnly = true;
            this.dgvMessages.Size = new System.Drawing.Size(462, 566);
            this.dgvMessages.TabIndex = 0;
            this.dgvMessages.RowHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_RowHeaderMouseDoubleClick);
            // 
            // lblOverrideSort
            // 
            this.lblOverrideSort.AutoSize = true;
            this.lblOverrideSort.Location = new System.Drawing.Point(9, 18);
            this.lblOverrideSort.Name = "lblOverrideSort";
            this.lblOverrideSort.Size = new System.Drawing.Size(79, 13);
            this.lblOverrideSort.TabIndex = 0;
            this.lblOverrideSort.Text = "<using default>";
            // 
            // gbxOverrideSort
            // 
            this.gbxOverrideSort.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbxOverrideSort.Controls.Add(this.cmdOverrideSortClear);
            this.gbxOverrideSort.Controls.Add(this.lblOverrideSort);
            this.gbxOverrideSort.Controls.Add(this.cmdOverrideSortSet);
            this.gbxOverrideSort.Location = new System.Drawing.Point(3, 462);
            this.gbxOverrideSort.Name = "gbxOverrideSort";
            this.gbxOverrideSort.Size = new System.Drawing.Size(314, 76);
            this.gbxOverrideSort.TabIndex = 3;
            this.gbxOverrideSort.TabStop = false;
            this.gbxOverrideSort.Text = "Override Sort";
            // 
            // cmdOverrideSortClear
            // 
            this.cmdOverrideSortClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdOverrideSortClear.Location = new System.Drawing.Point(118, 40);
            this.cmdOverrideSortClear.Name = "cmdOverrideSortClear";
            this.cmdOverrideSortClear.Size = new System.Drawing.Size(100, 25);
            this.cmdOverrideSortClear.TabIndex = 2;
            this.cmdOverrideSortClear.Text = "Clear";
            this.cmdOverrideSortClear.UseVisualStyleBackColor = true;
            this.cmdOverrideSortClear.Click += new System.EventHandler(this.cmdOverrideSortClear_Click);
            // 
            // cmdFilterClear
            // 
            this.cmdFilterClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdFilterClear.Location = new System.Drawing.Point(121, 431);
            this.cmdFilterClear.Name = "cmdFilterClear";
            this.cmdFilterClear.Size = new System.Drawing.Size(100, 25);
            this.cmdFilterClear.TabIndex = 2;
            this.cmdFilterClear.Text = "Clear";
            this.cmdFilterClear.UseVisualStyleBackColor = true;
            this.cmdFilterClear.Click += new System.EventHandler(this.cmdFilterClear_Click);
            // 
            // frmSelectedMailbox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 573);
            this.Controls.Add(this.splitContainer1);
            this.Name = "frmSelectedMailbox";
            this.Text = "frmSelectedMailbox";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmSelectedMailbox_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmSelectedMailbox_FormClosed);
            this.Load += new System.EventHandler(this.frmSelectedMailbox_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvMessages)).EndInit();
            this.gbxOverrideSort.ResumeLayout(false);
            this.gbxOverrideSort.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button cmdExpunge;
        private System.Windows.Forms.Button cmdClose;
        private System.Windows.Forms.RichTextBox rtx;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvMessages;
        private System.Windows.Forms.Button cmdFilter;
        private System.Windows.Forms.Button cmdOverrideSortSet;
        private System.Windows.Forms.Label lblOverrideSort;
        private System.Windows.Forms.GroupBox gbxOverrideSort;
        private System.Windows.Forms.Button cmdOverrideSortClear;
        private System.Windows.Forms.Button cmdFilterClear;
    }
}