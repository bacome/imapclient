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
            this.cmdNewClient = new System.Windows.Forms.Button();
            this.tmrProofOfASync = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Proof of async:";
            // 
            // lblProofOfASync
            // 
            this.lblProofOfASync.AutoSize = true;
            this.lblProofOfASync.Location = new System.Drawing.Point(96, 9);
            this.lblProofOfASync.Name = "lblProofOfASync";
            this.lblProofOfASync.Size = new System.Drawing.Size(55, 13);
            this.lblProofOfASync.TabIndex = 1;
            this.lblProofOfASync.Text = "<counter>";
            // 
            // cmdTests
            // 
            this.cmdTests.Location = new System.Drawing.Point(15, 36);
            this.cmdTests.Name = "cmdTests";
            this.cmdTests.Size = new System.Drawing.Size(136, 30);
            this.cmdTests.TabIndex = 2;
            this.cmdTests.Text = "Tests";
            this.cmdTests.UseVisualStyleBackColor = true;
            // 
            // cmdQuickTests
            // 
            this.cmdQuickTests.Location = new System.Drawing.Point(15, 72);
            this.cmdQuickTests.Name = "cmdQuickTests";
            this.cmdQuickTests.Size = new System.Drawing.Size(135, 30);
            this.cmdQuickTests.TabIndex = 3;
            this.cmdQuickTests.Text = "Quick Tests";
            this.cmdQuickTests.UseVisualStyleBackColor = true;
            // 
            // cmdCurrentTest
            // 
            this.cmdCurrentTest.Location = new System.Drawing.Point(15, 111);
            this.cmdCurrentTest.Name = "cmdCurrentTest";
            this.cmdCurrentTest.Size = new System.Drawing.Size(134, 31);
            this.cmdCurrentTest.TabIndex = 4;
            this.cmdCurrentTest.Text = "Current Test";
            this.cmdCurrentTest.UseVisualStyleBackColor = true;
            // 
            // cmdNewClient
            // 
            this.cmdNewClient.Location = new System.Drawing.Point(16, 149);
            this.cmdNewClient.Name = "cmdNewClient";
            this.cmdNewClient.Size = new System.Drawing.Size(132, 30);
            this.cmdNewClient.TabIndex = 5;
            this.cmdNewClient.Text = "New Client";
            this.cmdNewClient.UseVisualStyleBackColor = true;
            // 
            // tmrProofOfASync
            // 
            this.tmrProofOfASync.Enabled = true;
            this.tmrProofOfASync.Tick += new System.EventHandler(this.tmrProofOfASync_Tick);
            // 
            // start
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(261, 190);
            this.Controls.Add(this.cmdNewClient);
            this.Controls.Add(this.cmdCurrentTest);
            this.Controls.Add(this.cmdQuickTests);
            this.Controls.Add(this.cmdTests);
            this.Controls.Add(this.lblProofOfASync);
            this.Controls.Add(this.label1);
            this.Name = "start";
            this.Text = "imapclient testharness - start";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblProofOfASync;
        private System.Windows.Forms.Button cmdTests;
        private System.Windows.Forms.Button cmdQuickTests;
        private System.Windows.Forms.Button cmdCurrentTest;
        private System.Windows.Forms.Button cmdNewClient;
        private System.Windows.Forms.Timer tmrProofOfASync;
    }
}

