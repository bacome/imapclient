namespace testharness2
{
    partial class frmMessageStream
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cmdCopy = new System.Windows.Forms.Button();
            this.txtBytesToCopy = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblCurrentBufferSize = new System.Windows.Forms.Label();
            this.cmdRefresh = new System.Windows.Forms.Button();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.lblCopied = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblCopied);
            this.groupBox1.Controls.Add(this.cmdCopy);
            this.groupBox1.Controls.Add(this.txtBytesToCopy);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtTimeout);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(3, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(251, 137);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Copy";
            // 
            // cmdCopy
            // 
            this.cmdCopy.Location = new System.Drawing.Point(124, 71);
            this.cmdCopy.Name = "cmdCopy";
            this.cmdCopy.Size = new System.Drawing.Size(100, 25);
            this.cmdCopy.TabIndex = 4;
            this.cmdCopy.Text = "Copy";
            this.cmdCopy.UseVisualStyleBackColor = true;
            this.cmdCopy.Click += new System.EventHandler(this.cmdCopy_Click);
            // 
            // txtBytesToCopy
            // 
            this.txtBytesToCopy.Location = new System.Drawing.Point(124, 45);
            this.txtBytesToCopy.Name = "txtBytesToCopy";
            this.txtBytesToCopy.Size = new System.Drawing.Size(100, 20);
            this.txtBytesToCopy.TabIndex = 3;
            this.txtBytesToCopy.Text = "1000";
            this.txtBytesToCopy.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtBytesToCopy.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Bytes to Copy";
            // 
            // txtTimeout
            // 
            this.txtTimeout.Location = new System.Drawing.Point(125, 19);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(99, 20);
            this.txtTimeout.TabIndex = 1;
            this.txtTimeout.Text = "100";
            this.txtTimeout.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtTimeout.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Timeout (ms)";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lblCurrentBufferSize);
            this.groupBox2.Controls.Add(this.cmdRefresh);
            this.groupBox2.Location = new System.Drawing.Point(260, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(143, 137);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Current Buffer Size";
            // 
            // lblCurrentBufferSize
            // 
            this.lblCurrentBufferSize.AutoSize = true;
            this.lblCurrentBufferSize.Location = new System.Drawing.Point(15, 22);
            this.lblCurrentBufferSize.Name = "lblCurrentBufferSize";
            this.lblCurrentBufferSize.Size = new System.Drawing.Size(37, 13);
            this.lblCurrentBufferSize.TabIndex = 1;
            this.lblCurrentBufferSize.Text = "<size>";
            // 
            // cmdRefresh
            // 
            this.cmdRefresh.Location = new System.Drawing.Point(18, 71);
            this.cmdRefresh.Name = "cmdRefresh";
            this.cmdRefresh.Size = new System.Drawing.Size(100, 25);
            this.cmdRefresh.TabIndex = 0;
            this.cmdRefresh.Text = "Refresh";
            this.cmdRefresh.UseVisualStyleBackColor = true;
            this.cmdRefresh.Click += new System.EventHandler(this.cmdRefresh_Click);
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // lblCopied
            // 
            this.lblCopied.AutoSize = true;
            this.lblCopied.Location = new System.Drawing.Point(121, 109);
            this.lblCopied.Name = "lblCopied";
            this.lblCopied.Size = new System.Drawing.Size(51, 13);
            this.lblCopied.TabIndex = 5;
            this.lblCopied.Text = "<copied>";
            // 
            // frmMessageStream
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(406, 147);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "frmMessageStream";
            this.Text = "frmMessageStream";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMessageStream_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMessageStream_FormClosed);
            this.Load += new System.EventHandler(this.frmMessageStream_Load);
            this.Shown += new System.EventHandler(this.frmMessageStream_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button cmdCopy;
        private System.Windows.Forms.TextBox txtBytesToCopy;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblCurrentBufferSize;
        private System.Windows.Forms.Button cmdRefresh;
        private System.Windows.Forms.ErrorProvider erp;
        private System.Windows.Forms.Label lblCopied;
    }
}