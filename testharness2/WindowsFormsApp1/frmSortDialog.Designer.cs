namespace testharness2
{
    partial class frmSortDialog
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.cmdOK = new System.Windows.Forms.Button();
            this.rdoThreadReferences = new System.Windows.Forms.RadioButton();
            this.rdoThreadOrderedSubject = new System.Windows.Forms.RadioButton();
            this.rdoNone = new System.Windows.Forms.RadioButton();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.rdoOther = new System.Windows.Forms.RadioButton();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgv.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Location = new System.Drawing.Point(5, 97);
            this.dgv.Name = "dgv";
            this.dgv.Size = new System.Drawing.Size(206, 213);
            this.dgv.TabIndex = 4;
            this.dgv.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellClick);
            this.dgv.Validating += new System.ComponentModel.CancelEventHandler(this.dgv_Validating);
            this.dgv.Validated += new System.EventHandler(this.dgv_Validated);
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdOK.Location = new System.Drawing.Point(5, 325);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(100, 25);
            this.cmdOK.TabIndex = 5;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // rdoThreadReferences
            // 
            this.rdoThreadReferences.AutoSize = true;
            this.rdoThreadReferences.Location = new System.Drawing.Point(5, 51);
            this.rdoThreadReferences.Name = "rdoThreadReferences";
            this.rdoThreadReferences.Size = new System.Drawing.Size(117, 17);
            this.rdoThreadReferences.TabIndex = 2;
            this.rdoThreadReferences.TabStop = true;
            this.rdoThreadReferences.Text = "Thread References";
            this.rdoThreadReferences.UseVisualStyleBackColor = true;
            this.rdoThreadReferences.CheckedChanged += new System.EventHandler(this.ZCheckedChanged);
            // 
            // rdoThreadOrderedSubject
            // 
            this.rdoThreadOrderedSubject.AutoSize = true;
            this.rdoThreadOrderedSubject.Location = new System.Drawing.Point(5, 28);
            this.rdoThreadOrderedSubject.Name = "rdoThreadOrderedSubject";
            this.rdoThreadOrderedSubject.Size = new System.Drawing.Size(139, 17);
            this.rdoThreadOrderedSubject.TabIndex = 1;
            this.rdoThreadOrderedSubject.TabStop = true;
            this.rdoThreadOrderedSubject.Text = "Thread Ordered Subject";
            this.rdoThreadOrderedSubject.UseVisualStyleBackColor = true;
            this.rdoThreadOrderedSubject.CheckedChanged += new System.EventHandler(this.ZCheckedChanged);
            // 
            // rdoNone
            // 
            this.rdoNone.AutoSize = true;
            this.rdoNone.Location = new System.Drawing.Point(5, 5);
            this.rdoNone.Name = "rdoNone";
            this.rdoNone.Size = new System.Drawing.Size(51, 17);
            this.rdoNone.TabIndex = 0;
            this.rdoNone.TabStop = true;
            this.rdoNone.Text = "None";
            this.rdoNone.UseVisualStyleBackColor = true;
            this.rdoNone.CheckedChanged += new System.EventHandler(this.ZCheckedChanged);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(111, 325);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(100, 25);
            this.cmdCancel.TabIndex = 6;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            // 
            // rdoOther
            // 
            this.rdoOther.AutoSize = true;
            this.rdoOther.Location = new System.Drawing.Point(5, 74);
            this.rdoOther.Name = "rdoOther";
            this.rdoOther.Size = new System.Drawing.Size(51, 17);
            this.rdoOther.TabIndex = 3;
            this.rdoOther.TabStop = true;
            this.rdoOther.Text = "Other";
            this.rdoOther.UseVisualStyleBackColor = true;
            this.rdoOther.CheckedChanged += new System.EventHandler(this.ZCheckedChanged);
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // frmSortDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(243, 353);
            this.Controls.Add(this.rdoOther);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.rdoThreadReferences);
            this.Controls.Add(this.rdoThreadOrderedSubject);
            this.Controls.Add(this.rdoNone);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.dgv);
            this.Name = "frmSortDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmSort";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmSortDialog_FormClosing);
            this.Load += new System.EventHandler(this.frmSort_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.RadioButton rdoThreadReferences;
        private System.Windows.Forms.RadioButton rdoThreadOrderedSubject;
        private System.Windows.Forms.RadioButton rdoNone;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.RadioButton rdoOther;
        private System.Windows.Forms.ErrorProvider erp;
    }
}