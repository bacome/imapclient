namespace testharness2
{
    partial class frmFilter
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
            this.chkInvert = new System.Windows.Forms.CheckBox();
            this.cmdOr = new System.Windows.Forms.Button();
            this.cmdApply = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkAnswered = new System.Windows.Forms.CheckBox();
            this.chkDeleted = new System.Windows.Forms.CheckBox();
            this.chkDraft = new System.Windows.Forms.CheckBox();
            this.chkFred = new System.Windows.Forms.CheckBox();
            this.chkFlagged = new System.Windows.Forms.CheckBox();
            this.chkRecent = new System.Windows.Forms.CheckBox();
            this.chkSeen = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkUnseen = new System.Windows.Forms.CheckBox();
            this.chkUnrecent = new System.Windows.Forms.CheckBox();
            this.chkUnflagged = new System.Windows.Forms.CheckBox();
            this.chkUnfred = new System.Windows.Forms.CheckBox();
            this.chkUndraft = new System.Windows.Forms.CheckBox();
            this.chkUndeleted = new System.Windows.Forms.CheckBox();
            this.chkUnanswered = new System.Windows.Forms.CheckBox();
            this.dgvContains = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.dtpRAfter = new System.Windows.Forms.DateTimePicker();
            this.dtpROn = new System.Windows.Forms.DateTimePicker();
            this.dtpRBefore = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.dateTimePicker3 = new System.Windows.Forms.DateTimePicker();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvContains)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkInvert
            // 
            this.chkInvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkInvert.AutoSize = true;
            this.chkInvert.Location = new System.Drawing.Point(5, 233);
            this.chkInvert.Name = "chkInvert";
            this.chkInvert.Size = new System.Drawing.Size(53, 17);
            this.chkInvert.TabIndex = 1;
            this.chkInvert.Text = "Invert";
            this.chkInvert.UseVisualStyleBackColor = true;
            // 
            // cmdOr
            // 
            this.cmdOr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdOr.Location = new System.Drawing.Point(64, 228);
            this.cmdOr.Name = "cmdOr";
            this.cmdOr.Size = new System.Drawing.Size(100, 25);
            this.cmdOr.TabIndex = 2;
            this.cmdOr.Text = "Or";
            this.cmdOr.UseVisualStyleBackColor = true;
            // 
            // cmdApply
            // 
            this.cmdApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdApply.Location = new System.Drawing.Point(375, 228);
            this.cmdApply.Name = "cmdApply";
            this.cmdApply.Size = new System.Drawing.Size(100, 25);
            this.cmdApply.TabIndex = 3;
            this.cmdApply.Text = "Apply";
            this.cmdApply.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Location = new System.Drawing.Point(5, 5);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(470, 217);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(462, 191);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Flags";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dgvContains);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(462, 191);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Text Contains";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkSeen);
            this.groupBox1.Controls.Add(this.chkRecent);
            this.groupBox1.Controls.Add(this.chkFlagged);
            this.groupBox1.Controls.Add(this.chkFred);
            this.groupBox1.Controls.Add(this.chkDraft);
            this.groupBox1.Controls.Add(this.chkDeleted);
            this.groupBox1.Controls.Add(this.chkAnswered);
            this.groupBox1.Location = new System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(302, 72);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Set";
            // 
            // chkAnswered
            // 
            this.chkAnswered.AutoSize = true;
            this.chkAnswered.Location = new System.Drawing.Point(12, 20);
            this.chkAnswered.Name = "chkAnswered";
            this.chkAnswered.Size = new System.Drawing.Size(73, 17);
            this.chkAnswered.TabIndex = 0;
            this.chkAnswered.Text = "Answered";
            this.chkAnswered.UseVisualStyleBackColor = true;
            // 
            // chkDeleted
            // 
            this.chkDeleted.AutoSize = true;
            this.chkDeleted.Location = new System.Drawing.Point(91, 20);
            this.chkDeleted.Name = "chkDeleted";
            this.chkDeleted.Size = new System.Drawing.Size(63, 17);
            this.chkDeleted.TabIndex = 1;
            this.chkDeleted.Text = "Deleted";
            this.chkDeleted.UseVisualStyleBackColor = true;
            // 
            // chkDraft
            // 
            this.chkDraft.AutoSize = true;
            this.chkDraft.Location = new System.Drawing.Point(170, 20);
            this.chkDraft.Name = "chkDraft";
            this.chkDraft.Size = new System.Drawing.Size(49, 17);
            this.chkDraft.TabIndex = 2;
            this.chkDraft.Text = "Draft";
            this.chkDraft.UseVisualStyleBackColor = true;
            // 
            // chkFred
            // 
            this.chkFred.AutoSize = true;
            this.chkFred.Location = new System.Drawing.Point(236, 43);
            this.chkFred.Name = "chkFred";
            this.chkFred.Size = new System.Drawing.Size(47, 17);
            this.chkFred.TabIndex = 6;
            this.chkFred.Text = "Fred";
            this.chkFred.UseVisualStyleBackColor = true;
            // 
            // chkFlagged
            // 
            this.chkFlagged.AutoSize = true;
            this.chkFlagged.Location = new System.Drawing.Point(12, 43);
            this.chkFlagged.Name = "chkFlagged";
            this.chkFlagged.Size = new System.Drawing.Size(64, 17);
            this.chkFlagged.TabIndex = 3;
            this.chkFlagged.Text = "Flagged";
            this.chkFlagged.UseVisualStyleBackColor = true;
            // 
            // chkRecent
            // 
            this.chkRecent.AutoSize = true;
            this.chkRecent.Location = new System.Drawing.Point(91, 43);
            this.chkRecent.Name = "chkRecent";
            this.chkRecent.Size = new System.Drawing.Size(61, 17);
            this.chkRecent.TabIndex = 4;
            this.chkRecent.Text = "Recent";
            this.chkRecent.UseVisualStyleBackColor = true;
            // 
            // chkSeen
            // 
            this.chkSeen.AutoSize = true;
            this.chkSeen.Location = new System.Drawing.Point(170, 43);
            this.chkSeen.Name = "chkSeen";
            this.chkSeen.Size = new System.Drawing.Size(51, 17);
            this.chkSeen.TabIndex = 5;
            this.chkSeen.Text = "Seen";
            this.chkSeen.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkUnseen);
            this.groupBox2.Controls.Add(this.chkUnrecent);
            this.groupBox2.Controls.Add(this.chkUnflagged);
            this.groupBox2.Controls.Add(this.chkUnfred);
            this.groupBox2.Controls.Add(this.chkUndraft);
            this.groupBox2.Controls.Add(this.chkUndeleted);
            this.groupBox2.Controls.Add(this.chkUnanswered);
            this.groupBox2.Location = new System.Drawing.Point(6, 84);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(302, 72);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Not Set";
            // 
            // chkUnseen
            // 
            this.chkUnseen.AutoSize = true;
            this.chkUnseen.Location = new System.Drawing.Point(170, 43);
            this.chkUnseen.Name = "chkUnseen";
            this.chkUnseen.Size = new System.Drawing.Size(51, 17);
            this.chkUnseen.TabIndex = 5;
            this.chkUnseen.Text = "Seen";
            this.chkUnseen.UseVisualStyleBackColor = true;
            // 
            // chkUnrecent
            // 
            this.chkUnrecent.AutoSize = true;
            this.chkUnrecent.Location = new System.Drawing.Point(91, 43);
            this.chkUnrecent.Name = "chkUnrecent";
            this.chkUnrecent.Size = new System.Drawing.Size(61, 17);
            this.chkUnrecent.TabIndex = 4;
            this.chkUnrecent.Text = "Recent";
            this.chkUnrecent.UseVisualStyleBackColor = true;
            // 
            // chkUnflagged
            // 
            this.chkUnflagged.AutoSize = true;
            this.chkUnflagged.Location = new System.Drawing.Point(12, 43);
            this.chkUnflagged.Name = "chkUnflagged";
            this.chkUnflagged.Size = new System.Drawing.Size(64, 17);
            this.chkUnflagged.TabIndex = 3;
            this.chkUnflagged.Text = "Flagged";
            this.chkUnflagged.UseVisualStyleBackColor = true;
            // 
            // chkUnfred
            // 
            this.chkUnfred.AutoSize = true;
            this.chkUnfred.Location = new System.Drawing.Point(236, 43);
            this.chkUnfred.Name = "chkUnfred";
            this.chkUnfred.Size = new System.Drawing.Size(47, 17);
            this.chkUnfred.TabIndex = 6;
            this.chkUnfred.Text = "Fred";
            this.chkUnfred.UseVisualStyleBackColor = true;
            // 
            // chkUndraft
            // 
            this.chkUndraft.AutoSize = true;
            this.chkUndraft.Location = new System.Drawing.Point(170, 20);
            this.chkUndraft.Name = "chkUndraft";
            this.chkUndraft.Size = new System.Drawing.Size(49, 17);
            this.chkUndraft.TabIndex = 2;
            this.chkUndraft.Text = "Draft";
            this.chkUndraft.UseVisualStyleBackColor = true;
            // 
            // chkUndeleted
            // 
            this.chkUndeleted.AutoSize = true;
            this.chkUndeleted.Location = new System.Drawing.Point(91, 20);
            this.chkUndeleted.Name = "chkUndeleted";
            this.chkUndeleted.Size = new System.Drawing.Size(63, 17);
            this.chkUndeleted.TabIndex = 1;
            this.chkUndeleted.Text = "Deleted";
            this.chkUndeleted.UseVisualStyleBackColor = true;
            // 
            // chkUnanswered
            // 
            this.chkUnanswered.AutoSize = true;
            this.chkUnanswered.Location = new System.Drawing.Point(12, 20);
            this.chkUnanswered.Name = "chkUnanswered";
            this.chkUnanswered.Size = new System.Drawing.Size(73, 17);
            this.chkUnanswered.TabIndex = 0;
            this.chkUnanswered.Text = "Answered";
            this.chkUnanswered.UseVisualStyleBackColor = true;
            // 
            // dgvContains
            // 
            this.dgvContains.AllowUserToAddRows = false;
            this.dgvContains.AllowUserToDeleteRows = false;
            this.dgvContains.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvContains.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvContains.Location = new System.Drawing.Point(3, 6);
            this.dgvContains.Name = "dgvContains";
            this.dgvContains.Size = new System.Drawing.Size(456, 182);
            this.dgvContains.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox4);
            this.tabPage3.Controls.Add(this.groupBox3);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(462, 191);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Dates";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(462, 191);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Headers";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(462, 191);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Other";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.dtpRBefore);
            this.groupBox3.Controls.Add(this.dtpROn);
            this.groupBox3.Controls.Add(this.dtpRAfter);
            this.groupBox3.Location = new System.Drawing.Point(6, 6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(316, 68);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Received";
            // 
            // dtpRAfter
            // 
            this.dtpRAfter.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpRAfter.Location = new System.Drawing.Point(18, 40);
            this.dtpRAfter.Name = "dtpRAfter";
            this.dtpRAfter.Size = new System.Drawing.Size(93, 20);
            this.dtpRAfter.TabIndex = 0;
            // 
            // dtpROn
            // 
            this.dtpROn.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpROn.Location = new System.Drawing.Point(117, 40);
            this.dtpROn.Name = "dtpROn";
            this.dtpROn.Size = new System.Drawing.Size(93, 20);
            this.dtpROn.TabIndex = 1;
            // 
            // dtpRBefore
            // 
            this.dtpRBefore.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpRBefore.Location = new System.Drawing.Point(216, 40);
            this.dtpRBefore.Name = "dtpRBefore";
            this.dtpRBefore.Size = new System.Drawing.Size(93, 20);
            this.dtpRBefore.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "After";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(114, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(21, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "On";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(213, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Before";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label4);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.dateTimePicker1);
            this.groupBox4.Controls.Add(this.dateTimePicker2);
            this.groupBox4.Controls.Add(this.dateTimePicker3);
            this.groupBox4.Location = new System.Drawing.Point(6, 80);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(316, 68);
            this.groupBox4.TabIndex = 1;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Sent";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(213, 20);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Before";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(114, 20);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(21, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "On";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 20);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(29, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "After";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker1.Location = new System.Drawing.Point(216, 40);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(93, 20);
            this.dateTimePicker1.TabIndex = 2;
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker2.Location = new System.Drawing.Point(117, 40);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(93, 20);
            this.dateTimePicker2.TabIndex = 1;
            // 
            // dateTimePicker3
            // 
            this.dateTimePicker3.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker3.Location = new System.Drawing.Point(18, 40);
            this.dateTimePicker3.Name = "dateTimePicker3";
            this.dateTimePicker3.Size = new System.Drawing.Size(93, 20);
            this.dateTimePicker3.TabIndex = 0;
            // 
            // frmFilter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 257);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.cmdApply);
            this.Controls.Add(this.cmdOr);
            this.Controls.Add(this.chkInvert);
            this.Name = "frmFilter";
            this.Text = "frmFilter";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvContains)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkInvert;
        private System.Windows.Forms.Button cmdOr;
        private System.Windows.Forms.Button cmdApply;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkUnseen;
        private System.Windows.Forms.CheckBox chkUnrecent;
        private System.Windows.Forms.CheckBox chkUnflagged;
        private System.Windows.Forms.CheckBox chkUnfred;
        private System.Windows.Forms.CheckBox chkUndraft;
        private System.Windows.Forms.CheckBox chkUndeleted;
        private System.Windows.Forms.CheckBox chkUnanswered;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkSeen;
        private System.Windows.Forms.CheckBox chkRecent;
        private System.Windows.Forms.CheckBox chkFlagged;
        private System.Windows.Forms.CheckBox chkFred;
        private System.Windows.Forms.CheckBox chkDraft;
        private System.Windows.Forms.CheckBox chkDeleted;
        private System.Windows.Forms.CheckBox chkAnswered;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dgvContains;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.DateTimePicker dateTimePicker2;
        private System.Windows.Forms.DateTimePicker dateTimePicker3;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpRBefore;
        private System.Windows.Forms.DateTimePicker dtpROn;
        private System.Windows.Forms.DateTimePicker dtpRAfter;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage5;
    }
}