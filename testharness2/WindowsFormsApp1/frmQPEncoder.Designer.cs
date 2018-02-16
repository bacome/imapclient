namespace testharness2
{
    partial class frmQPEncoder
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
            this.gbxWrite = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtWInitial = new System.Windows.Forms.TextBox();
            this.txtWMaxTime = new System.Windows.Forms.TextBox();
            this.txtWMax = new System.Windows.Forms.TextBox();
            this.txtWMin = new System.Windows.Forms.TextBox();
            this.gbxRead = new System.Windows.Forms.GroupBox();
            this.label29 = new System.Windows.Forms.Label();
            this.label30 = new System.Windows.Forms.Label();
            this.label31 = new System.Windows.Forms.Label();
            this.label32 = new System.Windows.Forms.Label();
            this.txtRInitial = new System.Windows.Forms.TextBox();
            this.txtRMaxTime = new System.Windows.Forms.TextBox();
            this.txtRMax = new System.Windows.Forms.TextBox();
            this.txtRMin = new System.Windows.Forms.TextBox();
            this.gbxSourceType = new System.Windows.Forms.GroupBox();
            this.rdoLF = new System.Windows.Forms.RadioButton();
            this.rdoCRLF = new System.Windows.Forms.RadioButton();
            this.rdoBinary = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rdoEBCDIC = new System.Windows.Forms.RadioButton();
            this.rdoMinimal = new System.Windows.Forms.RadioButton();
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmdAsyncEncode = new System.Windows.Forms.Button();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.prg = new System.Windows.Forms.ProgressBar();
            this.gbxWrite.SuspendLayout();
            this.gbxRead.SuspendLayout();
            this.gbxSourceType.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.SuspendLayout();
            // 
            // gbxWrite
            // 
            this.gbxWrite.Controls.Add(this.label3);
            this.gbxWrite.Controls.Add(this.label4);
            this.gbxWrite.Controls.Add(this.label5);
            this.gbxWrite.Controls.Add(this.label6);
            this.gbxWrite.Controls.Add(this.txtWInitial);
            this.gbxWrite.Controls.Add(this.txtWMaxTime);
            this.gbxWrite.Controls.Add(this.txtWMax);
            this.gbxWrite.Controls.Add(this.txtWMin);
            this.gbxWrite.Location = new System.Drawing.Point(258, 147);
            this.gbxWrite.Name = "gbxWrite";
            this.gbxWrite.Size = new System.Drawing.Size(227, 111);
            this.gbxWrite.TabIndex = 5;
            this.gbxWrite.TabStop = false;
            this.gbxWrite.Text = "Write";
            this.gbxWrite.Validating += new System.ComponentModel.CancelEventHandler(this.gbxWrite_Validating);
            this.gbxWrite.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 85);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Initial";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 64);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Max Time";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 43);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(27, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Max";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 22);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(24, 13);
            this.label6.TabIndex = 0;
            this.label6.Text = "Min";
            // 
            // txtWInitial
            // 
            this.txtWInitial.Location = new System.Drawing.Point(133, 82);
            this.txtWInitial.Name = "txtWInitial";
            this.txtWInitial.Size = new System.Drawing.Size(70, 20);
            this.txtWInitial.TabIndex = 7;
            this.txtWInitial.Text = "1000";
            this.txtWInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtWInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtWMaxTime
            // 
            this.txtWMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtWMaxTime.Name = "txtWMaxTime";
            this.txtWMaxTime.Size = new System.Drawing.Size(70, 20);
            this.txtWMaxTime.TabIndex = 5;
            this.txtWMaxTime.Text = "1000";
            this.txtWMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtWMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtWMax
            // 
            this.txtWMax.Location = new System.Drawing.Point(133, 40);
            this.txtWMax.Name = "txtWMax";
            this.txtWMax.Size = new System.Drawing.Size(70, 20);
            this.txtWMax.TabIndex = 3;
            this.txtWMax.Text = "100000";
            this.txtWMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtWMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtWMin
            // 
            this.txtWMin.Location = new System.Drawing.Point(133, 19);
            this.txtWMin.Name = "txtWMin";
            this.txtWMin.Size = new System.Drawing.Size(70, 20);
            this.txtWMin.TabIndex = 1;
            this.txtWMin.Text = "1000";
            this.txtWMin.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtWMin.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxRead
            // 
            this.gbxRead.Controls.Add(this.label29);
            this.gbxRead.Controls.Add(this.label30);
            this.gbxRead.Controls.Add(this.label31);
            this.gbxRead.Controls.Add(this.label32);
            this.gbxRead.Controls.Add(this.txtRInitial);
            this.gbxRead.Controls.Add(this.txtRMaxTime);
            this.gbxRead.Controls.Add(this.txtRMax);
            this.gbxRead.Controls.Add(this.txtRMin);
            this.gbxRead.Location = new System.Drawing.Point(12, 147);
            this.gbxRead.Name = "gbxRead";
            this.gbxRead.Size = new System.Drawing.Size(227, 111);
            this.gbxRead.TabIndex = 4;
            this.gbxRead.TabStop = false;
            this.gbxRead.Text = "Read";
            this.gbxRead.Validating += new System.ComponentModel.CancelEventHandler(this.gbxRead_Validating);
            this.gbxRead.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(12, 85);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(31, 13);
            this.label29.TabIndex = 6;
            this.label29.Text = "Initial";
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(12, 64);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(53, 13);
            this.label30.TabIndex = 4;
            this.label30.Text = "Max Time";
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(12, 43);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(27, 13);
            this.label31.TabIndex = 2;
            this.label31.Text = "Max";
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(12, 22);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(24, 13);
            this.label32.TabIndex = 0;
            this.label32.Text = "Min";
            // 
            // txtRInitial
            // 
            this.txtRInitial.Location = new System.Drawing.Point(133, 82);
            this.txtRInitial.Name = "txtRInitial";
            this.txtRInitial.Size = new System.Drawing.Size(70, 20);
            this.txtRInitial.TabIndex = 7;
            this.txtRInitial.Text = "1000";
            this.txtRInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtRInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtRMaxTime
            // 
            this.txtRMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtRMaxTime.Name = "txtRMaxTime";
            this.txtRMaxTime.Size = new System.Drawing.Size(70, 20);
            this.txtRMaxTime.TabIndex = 5;
            this.txtRMaxTime.Text = "1000";
            this.txtRMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtRMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtRMax
            // 
            this.txtRMax.Location = new System.Drawing.Point(133, 40);
            this.txtRMax.Name = "txtRMax";
            this.txtRMax.Size = new System.Drawing.Size(70, 20);
            this.txtRMax.TabIndex = 3;
            this.txtRMax.Text = "100000";
            this.txtRMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtRMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtRMin
            // 
            this.txtRMin.Location = new System.Drawing.Point(133, 19);
            this.txtRMin.Name = "txtRMin";
            this.txtRMin.Size = new System.Drawing.Size(70, 20);
            this.txtRMin.TabIndex = 1;
            this.txtRMin.Text = "1000";
            this.txtRMin.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtRMin.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxSourceType
            // 
            this.gbxSourceType.Controls.Add(this.rdoLF);
            this.gbxSourceType.Controls.Add(this.rdoCRLF);
            this.gbxSourceType.Controls.Add(this.rdoBinary);
            this.gbxSourceType.Location = new System.Drawing.Point(12, 12);
            this.gbxSourceType.Name = "gbxSourceType";
            this.gbxSourceType.Size = new System.Drawing.Size(357, 47);
            this.gbxSourceType.TabIndex = 0;
            this.gbxSourceType.TabStop = false;
            this.gbxSourceType.Text = "Source Type";
            // 
            // rdoLF
            // 
            this.rdoLF.AutoSize = true;
            this.rdoLF.Location = new System.Drawing.Point(223, 19);
            this.rdoLF.Name = "rdoLF";
            this.rdoLF.Size = new System.Drawing.Size(121, 17);
            this.rdoLF.TabIndex = 2;
            this.rdoLF.Text = "LF Terminated Lines";
            this.rdoLF.UseVisualStyleBackColor = true;
            // 
            // rdoCRLF
            // 
            this.rdoCRLF.AutoSize = true;
            this.rdoCRLF.Checked = true;
            this.rdoCRLF.Location = new System.Drawing.Point(81, 19);
            this.rdoCRLF.Name = "rdoCRLF";
            this.rdoCRLF.Size = new System.Drawing.Size(136, 17);
            this.rdoCRLF.TabIndex = 1;
            this.rdoCRLF.TabStop = true;
            this.rdoCRLF.Text = "CRLF Terminated Lines";
            this.rdoCRLF.UseVisualStyleBackColor = true;
            // 
            // rdoBinary
            // 
            this.rdoBinary.AutoSize = true;
            this.rdoBinary.Location = new System.Drawing.Point(15, 19);
            this.rdoBinary.Name = "rdoBinary";
            this.rdoBinary.Size = new System.Drawing.Size(54, 17);
            this.rdoBinary.TabIndex = 0;
            this.rdoBinary.Text = "Binary";
            this.rdoBinary.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdoEBCDIC);
            this.groupBox1.Controls.Add(this.rdoMinimal);
            this.groupBox1.Location = new System.Drawing.Point(12, 65);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(357, 47);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Quoting Rule";
            // 
            // rdoEBCDIC
            // 
            this.rdoEBCDIC.AutoSize = true;
            this.rdoEBCDIC.Location = new System.Drawing.Point(81, 19);
            this.rdoEBCDIC.Name = "rdoEBCDIC";
            this.rdoEBCDIC.Size = new System.Drawing.Size(90, 17);
            this.rdoEBCDIC.TabIndex = 1;
            this.rdoEBCDIC.Text = "EBCDIC safer";
            this.rdoEBCDIC.UseVisualStyleBackColor = true;
            // 
            // rdoMinimal
            // 
            this.rdoMinimal.AutoSize = true;
            this.rdoMinimal.Checked = true;
            this.rdoMinimal.Location = new System.Drawing.Point(15, 19);
            this.rdoMinimal.Name = "rdoMinimal";
            this.rdoMinimal.Size = new System.Drawing.Size(60, 17);
            this.rdoMinimal.TabIndex = 0;
            this.rdoMinimal.TabStop = true;
            this.rdoMinimal.Text = "Minimal";
            this.rdoMinimal.UseVisualStyleBackColor = true;
            // 
            // txtTimeout
            // 
            this.txtTimeout.Location = new System.Drawing.Point(62, 121);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(50, 20);
            this.txtTimeout.TabIndex = 3;
            this.txtTimeout.Text = "60000";
            this.txtTimeout.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsTimeout);
            this.txtTimeout.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 124);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Timeout";
            // 
            // cmdAsyncEncode
            // 
            this.cmdAsyncEncode.Location = new System.Drawing.Point(12, 275);
            this.cmdAsyncEncode.Name = "cmdAsyncEncode";
            this.cmdAsyncEncode.Size = new System.Drawing.Size(100, 25);
            this.cmdAsyncEncode.TabIndex = 6;
            this.cmdAsyncEncode.Text = "Async Encode";
            this.cmdAsyncEncode.UseVisualStyleBackColor = true;
            this.cmdAsyncEncode.Click += new System.EventHandler(this.cmdAsyncEncode_Click);
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(139, 275);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 25);
            this.button1.TabIndex = 7;
            this.button1.Text = "Encode";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.cmdEncode_Click);
            // 
            // prg
            // 
            this.prg.Location = new System.Drawing.Point(258, 275);
            this.prg.Name = "prg";
            this.prg.Size = new System.Drawing.Size(226, 25);
            this.prg.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.prg.TabIndex = 8;
            this.prg.Visible = false;
            // 
            // frmQPEncoder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(509, 312);
            this.Controls.Add(this.prg);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.cmdAsyncEncode);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtTimeout);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.gbxSourceType);
            this.Controls.Add(this.gbxWrite);
            this.Controls.Add(this.gbxRead);
            this.Name = "frmQPEncoder";
            this.Text = "imapclient testharness - quoted printable encoder";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmQPEncoder_FormClosing);
            this.gbxWrite.ResumeLayout(false);
            this.gbxWrite.PerformLayout();
            this.gbxRead.ResumeLayout(false);
            this.gbxRead.PerformLayout();
            this.gbxSourceType.ResumeLayout(false);
            this.gbxSourceType.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbxWrite;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtWInitial;
        private System.Windows.Forms.TextBox txtWMaxTime;
        private System.Windows.Forms.TextBox txtWMax;
        private System.Windows.Forms.TextBox txtWMin;
        private System.Windows.Forms.GroupBox gbxRead;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.TextBox txtRInitial;
        private System.Windows.Forms.TextBox txtRMaxTime;
        private System.Windows.Forms.TextBox txtRMax;
        private System.Windows.Forms.TextBox txtRMin;
        private System.Windows.Forms.GroupBox gbxSourceType;
        private System.Windows.Forms.RadioButton rdoLF;
        private System.Windows.Forms.RadioButton rdoCRLF;
        private System.Windows.Forms.RadioButton rdoBinary;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rdoEBCDIC;
        private System.Windows.Forms.RadioButton rdoMinimal;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button cmdAsyncEncode;
        private System.Windows.Forms.ErrorProvider erp;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ProgressBar prg;
    }
}