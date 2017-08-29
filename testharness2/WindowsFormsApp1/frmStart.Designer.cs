namespace testharness2
{
    partial class frmStart
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
            this.label1 = new System.Windows.Forms.Label();
            this.lblProofOfASync = new System.Windows.Forms.Label();
            this.cmdTests = new System.Windows.Forms.Button();
            this.cmdQuickTests = new System.Windows.Forms.Button();
            this.cmdCurrentTest = new System.Windows.Forms.Button();
            this.cmdCreate = new System.Windows.Forms.Button();
            this.tmrProofOfASync = new System.Windows.Forms.Timer(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtInstanceName = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Proof of async:";
            // 
            // lblProofOfASync
            // 
            this.lblProofOfASync.AutoSize = true;
            this.lblProofOfASync.Location = new System.Drawing.Point(86, 9);
            this.lblProofOfASync.Name = "lblProofOfASync";
            this.lblProofOfASync.Size = new System.Drawing.Size(55, 13);
            this.lblProofOfASync.TabIndex = 1;
            this.lblProofOfASync.Text = "<counter>";
            // 
            // cmdTests
            // 
            this.cmdTests.Location = new System.Drawing.Point(320, 39);
            this.cmdTests.Name = "cmdTests";
            this.cmdTests.Size = new System.Drawing.Size(100, 25);
            this.cmdTests.TabIndex = 3;
            this.cmdTests.Text = "Tests";
            this.cmdTests.UseVisualStyleBackColor = true;
            this.cmdTests.Click += new System.EventHandler(this.cmdTests_Click);
            // 
            // cmdQuickTests
            // 
            this.cmdQuickTests.Location = new System.Drawing.Point(320, 69);
            this.cmdQuickTests.Name = "cmdQuickTests";
            this.cmdQuickTests.Size = new System.Drawing.Size(100, 25);
            this.cmdQuickTests.TabIndex = 4;
            this.cmdQuickTests.Text = "Quick Tests";
            this.cmdQuickTests.UseVisualStyleBackColor = true;
            this.cmdQuickTests.Click += new System.EventHandler(this.cmdQuickTests_Click);
            // 
            // cmdCurrentTest
            // 
            this.cmdCurrentTest.Location = new System.Drawing.Point(320, 99);
            this.cmdCurrentTest.Name = "cmdCurrentTest";
            this.cmdCurrentTest.Size = new System.Drawing.Size(100, 25);
            this.cmdCurrentTest.TabIndex = 5;
            this.cmdCurrentTest.Text = "Current Test";
            this.cmdCurrentTest.UseVisualStyleBackColor = true;
            this.cmdCurrentTest.Click += new System.EventHandler(this.cmdCurrentTest_Click);
            // 
            // cmdCreate
            // 
            this.cmdCreate.Location = new System.Drawing.Point(100, 45);
            this.cmdCreate.Name = "cmdCreate";
            this.cmdCreate.Size = new System.Drawing.Size(100, 25);
            this.cmdCreate.TabIndex = 2;
            this.cmdCreate.Text = "Create";
            this.cmdCreate.UseVisualStyleBackColor = true;
            this.cmdCreate.Click += new System.EventHandler(this.cmdCreate_Click);
            // 
            // tmrProofOfASync
            // 
            this.tmrProofOfASync.Enabled = true;
            this.tmrProofOfASync.Tick += new System.EventHandler(this.tmrProofOfASync_Tick);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtInstanceName);
            this.groupBox1.Controls.Add(this.cmdCreate);
            this.groupBox1.Location = new System.Drawing.Point(5, 34);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(309, 90);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "New Client";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Instance Name";
            // 
            // txtInstanceName
            // 
            this.txtInstanceName.Location = new System.Drawing.Point(100, 19);
            this.txtInstanceName.Name = "txtInstanceName";
            this.txtInstanceName.Size = new System.Drawing.Size(198, 20);
            this.txtInstanceName.TabIndex = 1;
            // 
            // frmStart
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(425, 128);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cmdCurrentTest);
            this.Controls.Add(this.cmdQuickTests);
            this.Controls.Add(this.cmdTests);
            this.Controls.Add(this.lblProofOfASync);
            this.Controls.Add(this.label1);
            this.Name = "frmStart";
            this.Text = "imapclient testharness - start";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblProofOfASync;
        private System.Windows.Forms.Button cmdTests;
        private System.Windows.Forms.Button cmdQuickTests;
        private System.Windows.Forms.Button cmdCurrentTest;
        private System.Windows.Forms.Button cmdCreate;
        private System.Windows.Forms.Timer tmrProofOfASync;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtInstanceName;
    }
}

