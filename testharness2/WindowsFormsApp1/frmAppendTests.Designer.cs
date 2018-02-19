namespace testharness2
{
    partial class frmAppendTests
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
            this.gbxSMTPDetails = new System.Windows.Forms.GroupBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.label41 = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.chkSSL = new System.Windows.Forms.CheckBox();
            this.label42 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.label43 = new System.Windows.Forms.Label();
            this.txtSendTo = new System.Windows.Forms.TextBox();
            this.cmdCurrentTest = new System.Windows.Forms.Button();
            this.cmdTests = new System.Windows.Forms.Button();
            this.rtx = new System.Windows.Forms.RichTextBox();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.gbxSMTPDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.SuspendLayout();
            // 
            // gbxSMTPDetails
            // 
            this.gbxSMTPDetails.Controls.Add(this.txtPassword);
            this.gbxSMTPDetails.Controls.Add(this.label4);
            this.gbxSMTPDetails.Controls.Add(this.label3);
            this.gbxSMTPDetails.Controls.Add(this.txtUserId);
            this.gbxSMTPDetails.Controls.Add(this.label41);
            this.gbxSMTPDetails.Controls.Add(this.txtHost);
            this.gbxSMTPDetails.Controls.Add(this.chkSSL);
            this.gbxSMTPDetails.Controls.Add(this.label42);
            this.gbxSMTPDetails.Controls.Add(this.txtPort);
            this.gbxSMTPDetails.Location = new System.Drawing.Point(12, 12);
            this.gbxSMTPDetails.Name = "gbxSMTPDetails";
            this.gbxSMTPDetails.Size = new System.Drawing.Size(372, 91);
            this.gbxSMTPDetails.TabIndex = 0;
            this.gbxSMTPDetails.TabStop = false;
            this.gbxSMTPDetails.Text = "SMTP Details";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(71, 62);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(150, 20);
            this.txtPassword.TabIndex = 8;
            this.txtPassword.Text = "imaptest1";
            this.txtPassword.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtPassword.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 65);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Password";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 43);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "UserId";
            // 
            // txtUserId
            // 
            this.txtUserId.Location = new System.Drawing.Point(71, 40);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(150, 20);
            this.txtUserId.TabIndex = 6;
            this.txtUserId.Text = "imaptest1";
            this.txtUserId.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtUserId.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label41
            // 
            this.label41.AutoSize = true;
            this.label41.Location = new System.Drawing.Point(12, 22);
            this.label41.Name = "label41";
            this.label41.Size = new System.Drawing.Size(29, 13);
            this.label41.TabIndex = 0;
            this.label41.Text = "Host";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(71, 17);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(150, 20);
            this.txtHost.TabIndex = 1;
            this.txtHost.Text = "192.168.56.101";
            this.txtHost.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtHost.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // chkSSL
            // 
            this.chkSSL.AutoSize = true;
            this.chkSSL.Location = new System.Drawing.Point(317, 20);
            this.chkSSL.Name = "chkSSL";
            this.chkSSL.Size = new System.Drawing.Size(46, 17);
            this.chkSSL.TabIndex = 4;
            this.chkSSL.Text = "SSL";
            this.chkSSL.UseVisualStyleBackColor = true;
            // 
            // label42
            // 
            this.label42.AutoSize = true;
            this.label42.Location = new System.Drawing.Point(233, 22);
            this.label42.Name = "label42";
            this.label42.Size = new System.Drawing.Size(26, 13);
            this.label42.TabIndex = 2;
            this.label42.Text = "Port";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(265, 19);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(32, 20);
            this.txtPort.TabIndex = 3;
            this.txtPort.Text = "25";
            this.txtPort.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsPortNumber);
            this.txtPort.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Enabled = false;
            this.cmdCancel.Location = new System.Drawing.Point(224, 151);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(100, 25);
            this.cmdCancel.TabIndex = 5;
            this.cmdCancel.Text = "Tests Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // label43
            // 
            this.label43.AutoSize = true;
            this.label43.Location = new System.Drawing.Point(12, 128);
            this.label43.Name = "label43";
            this.label43.Size = new System.Drawing.Size(48, 13);
            this.label43.TabIndex = 1;
            this.label43.Text = "Send To";
            // 
            // txtSendTo
            // 
            this.txtSendTo.Location = new System.Drawing.Point(72, 125);
            this.txtSendTo.Name = "txtSendTo";
            this.txtSendTo.Size = new System.Drawing.Size(312, 20);
            this.txtSendTo.TabIndex = 2;
            this.txtSendTo.Text = "imaptest1@dovecot.bacome.work";
            this.txtSendTo.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsEmailAddress);
            this.txtSendTo.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdCurrentTest
            // 
            this.cmdCurrentTest.Location = new System.Drawing.Point(118, 151);
            this.cmdCurrentTest.Name = "cmdCurrentTest";
            this.cmdCurrentTest.Size = new System.Drawing.Size(100, 25);
            this.cmdCurrentTest.TabIndex = 4;
            this.cmdCurrentTest.Text = "Current Test";
            this.cmdCurrentTest.UseVisualStyleBackColor = true;
            // 
            // cmdTests
            // 
            this.cmdTests.Location = new System.Drawing.Point(12, 151);
            this.cmdTests.Name = "cmdTests";
            this.cmdTests.Size = new System.Drawing.Size(100, 25);
            this.cmdTests.TabIndex = 3;
            this.cmdTests.Text = "All Tests";
            this.cmdTests.UseVisualStyleBackColor = true;
            this.cmdTests.Click += new System.EventHandler(this.cmdTests_Click);
            // 
            // rtx
            // 
            this.rtx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtx.Location = new System.Drawing.Point(12, 199);
            this.rtx.Name = "rtx";
            this.rtx.Size = new System.Drawing.Size(371, 109);
            this.rtx.TabIndex = 6;
            this.rtx.Text = "";
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // frmAppendTests
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(398, 321);
            this.Controls.Add(this.rtx);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.label43);
            this.Controls.Add(this.txtSendTo);
            this.Controls.Add(this.cmdCurrentTest);
            this.Controls.Add(this.cmdTests);
            this.Controls.Add(this.gbxSMTPDetails);
            this.Name = "frmAppendTests";
            this.Text = "frmAppendTests";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmAppendTests_FormClosing);
            this.Load += new System.EventHandler(this.frmAppendTests_Load);
            this.gbxSMTPDetails.ResumeLayout(false);
            this.gbxSMTPDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbxSMTPDetails;
        private System.Windows.Forms.Label label41;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.CheckBox chkSSL;
        private System.Windows.Forms.Label label42;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label label43;
        private System.Windows.Forms.TextBox txtSendTo;
        private System.Windows.Forms.Button cmdCurrentTest;
        private System.Windows.Forms.Button cmdTests;
        private System.Windows.Forms.RichTextBox rtx;
        private System.Windows.Forms.ErrorProvider erp;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUserId;
    }
}