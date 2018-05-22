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
            this.label2 = new System.Windows.Forms.Label();
            this.txtInstanceName = new System.Windows.Forms.TextBox();
            this.tab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.lblSectionCache = new System.Windows.Forms.Label();
            this.cmdGlobalSectionCache = new System.Windows.Forms.Button();
            this.tab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(-1, 130);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Proof of async:";
            // 
            // lblProofOfASync
            // 
            this.lblProofOfASync.AutoSize = true;
            this.lblProofOfASync.Location = new System.Drawing.Point(83, 130);
            this.lblProofOfASync.Name = "lblProofOfASync";
            this.lblProofOfASync.Size = new System.Drawing.Size(55, 13);
            this.lblProofOfASync.TabIndex = 2;
            this.lblProofOfASync.Text = "<counter>";
            // 
            // cmdTests
            // 
            this.cmdTests.Location = new System.Drawing.Point(6, 6);
            this.cmdTests.Name = "cmdTests";
            this.cmdTests.Size = new System.Drawing.Size(100, 25);
            this.cmdTests.TabIndex = 0;
            this.cmdTests.Text = "All Tests";
            this.cmdTests.UseVisualStyleBackColor = true;
            this.cmdTests.Click += new System.EventHandler(this.cmdTests_Click);
            // 
            // cmdQuickTests
            // 
            this.cmdQuickTests.Location = new System.Drawing.Point(6, 37);
            this.cmdQuickTests.Name = "cmdQuickTests";
            this.cmdQuickTests.Size = new System.Drawing.Size(100, 25);
            this.cmdQuickTests.TabIndex = 1;
            this.cmdQuickTests.Text = "Quick Tests";
            this.cmdQuickTests.UseVisualStyleBackColor = true;
            this.cmdQuickTests.Click += new System.EventHandler(this.cmdQuickTests_Click);
            // 
            // cmdCurrentTest
            // 
            this.cmdCurrentTest.Location = new System.Drawing.Point(6, 68);
            this.cmdCurrentTest.Name = "cmdCurrentTest";
            this.cmdCurrentTest.Size = new System.Drawing.Size(100, 25);
            this.cmdCurrentTest.TabIndex = 2;
            this.cmdCurrentTest.Text = "Current Test";
            this.cmdCurrentTest.UseVisualStyleBackColor = true;
            this.cmdCurrentTest.Click += new System.EventHandler(this.cmdCurrentTest_Click);
            // 
            // cmdCreate
            // 
            this.cmdCreate.Location = new System.Drawing.Point(88, 32);
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
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Instance Name";
            // 
            // txtInstanceName
            // 
            this.txtInstanceName.Location = new System.Drawing.Point(88, 6);
            this.txtInstanceName.Name = "txtInstanceName";
            this.txtInstanceName.Size = new System.Drawing.Size(206, 20);
            this.txtInstanceName.TabIndex = 1;
            // 
            // tab
            // 
            this.tab.Controls.Add(this.tabPage1);
            this.tab.Controls.Add(this.tabPage2);
            this.tab.Controls.Add(this.tabPage3);
            this.tab.Location = new System.Drawing.Point(2, 2);
            this.tab.Name = "tab";
            this.tab.SelectedIndex = 0;
            this.tab.Size = new System.Drawing.Size(311, 125);
            this.tab.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.cmdCreate);
            this.tabPage1.Controls.Add(this.txtInstanceName);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(303, 99);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "IMAP Client";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.cmdCurrentTest);
            this.tabPage3.Controls.Add(this.cmdTests);
            this.tabPage3.Controls.Add(this.cmdQuickTests);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(303, 99);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Library Tests";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.cmdGlobalSectionCache);
            this.tabPage2.Controls.Add(this.lblSectionCache);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(303, 99);
            this.tabPage2.TabIndex = 3;
            this.tabPage2.Text = "Global Section Cache";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // lblSectionCache
            // 
            this.lblSectionCache.AutoSize = true;
            this.lblSectionCache.Location = new System.Drawing.Point(3, 3);
            this.lblSectionCache.Name = "lblSectionCache";
            this.lblSectionCache.Size = new System.Drawing.Size(43, 13);
            this.lblSectionCache.TabIndex = 0;
            this.lblSectionCache.Text = "<none>";
            // 
            // cmdGlobalSectionCache
            // 
            this.cmdGlobalSectionCache.Location = new System.Drawing.Point(6, 68);
            this.cmdGlobalSectionCache.Name = "cmdGlobalSectionCache";
            this.cmdGlobalSectionCache.Size = new System.Drawing.Size(100, 25);
            this.cmdGlobalSectionCache.TabIndex = 1;
            this.cmdGlobalSectionCache.Text = "Choose ...";
            this.cmdGlobalSectionCache.UseVisualStyleBackColor = true;
            // 
            // frmStart
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(316, 146);
            this.Controls.Add(this.tab);
            this.Controls.Add(this.lblProofOfASync);
            this.Controls.Add(this.label1);
            this.Name = "frmStart";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "imapclient testharness - start";
            this.tab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
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
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtInstanceName;
        private System.Windows.Forms.TabControl tab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button cmdGlobalSectionCache;
        private System.Windows.Forms.Label lblSectionCache;
    }
}

