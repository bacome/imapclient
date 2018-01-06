namespace testharness2
{
    partial class frmAppend
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.cmdAppend = new System.Windows.Forms.Button();
            this.cmdMultiPart = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Location = new System.Drawing.Point(3, 3);
            this.dgv.Name = "dgv";
            this.dgv.Size = new System.Drawing.Size(586, 337);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            this.dgv.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_RowValidated);
            this.dgv.RowValidating += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dgv_RowValidating);
            // 
            // cmdAppend
            // 
            this.cmdAppend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdAppend.Location = new System.Drawing.Point(489, 346);
            this.cmdAppend.Name = "cmdAppend";
            this.cmdAppend.Size = new System.Drawing.Size(100, 25);
            this.cmdAppend.TabIndex = 2;
            this.cmdAppend.Text = "Append ...";
            this.cmdAppend.UseVisualStyleBackColor = true;
            this.cmdAppend.Click += new System.EventHandler(this.cmdAppend_Click);
            // 
            // cmdMultiPart
            // 
            this.cmdMultiPart.Location = new System.Drawing.Point(3, 346);
            this.cmdMultiPart.Name = "cmdMultiPart";
            this.cmdMultiPart.Size = new System.Drawing.Size(100, 25);
            this.cmdMultiPart.TabIndex = 1;
            this.cmdMultiPart.Text = "Multi Part ...";
            this.cmdMultiPart.UseVisualStyleBackColor = true;
            this.cmdMultiPart.Click += new System.EventHandler(this.cmdMultiPart_Click);
            // 
            // frmAppend
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(592, 373);
            this.Controls.Add(this.cmdMultiPart);
            this.Controls.Add(this.cmdAppend);
            this.Controls.Add(this.dgv);
            this.Name = "frmAppend";
            this.Text = "frmAppend";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmAppend_FormClosed);
            this.Load += new System.EventHandler(this.frmAppend_Load);
            this.Shown += new System.EventHandler(this.frmAppend_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Button cmdAppend;
        private System.Windows.Forms.Button cmdMultiPart;
    }
}