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
            this.components = new System.ComponentModel.Container();
            this.chkInvert = new System.Windows.Forms.CheckBox();
            this.cmdOr = new System.Windows.Forms.Button();
            this.cmdApply = new System.Windows.Forms.Button();
            this.tab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkSeen = new System.Windows.Forms.CheckBox();
            this.chkRecent = new System.Windows.Forms.CheckBox();
            this.chkFlagged = new System.Windows.Forms.CheckBox();
            this.chkDraft = new System.Windows.Forms.CheckBox();
            this.chkDeleted = new System.Windows.Forms.CheckBox();
            this.chkAnswered = new System.Windows.Forms.CheckBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dgvParts = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.dtpSBefore = new System.Windows.Forms.DateTimePicker();
            this.dtpSOn = new System.Windows.Forms.DateTimePicker();
            this.dtpSAfter = new System.Windows.Forms.DateTimePicker();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dtpRBefore = new System.Windows.Forms.DateTimePicker();
            this.dtpROn = new System.Windows.Forms.DateTimePicker();
            this.dtpRAfter = new System.Windows.Forms.DateTimePicker();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.dgvHeaders = new System.Windows.Forms.DataGridView();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.txtSizeSmaller = new System.Windows.Forms.TextBox();
            this.txtSizeLarger = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.chkSubmitted = new System.Windows.Forms.CheckBox();
            this.chkSubmitPending = new System.Windows.Forms.CheckBox();
            this.chkForwarded = new System.Windows.Forms.CheckBox();
            this.chkMDNSent = new System.Windows.Forms.CheckBox();
            this.txtSet = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtNotSet = new System.Windows.Forms.TextBox();
            this.chkUnsubmitted = new System.Windows.Forms.CheckBox();
            this.chkUnsubmitPending = new System.Windows.Forms.CheckBox();
            this.chkUnforwarded = new System.Windows.Forms.CheckBox();
            this.chkUnMDNSent = new System.Windows.Forms.CheckBox();
            this.chkUnseen = new System.Windows.Forms.CheckBox();
            this.chkUnrecent = new System.Windows.Forms.CheckBox();
            this.chkUnflagged = new System.Windows.Forms.CheckBox();
            this.chkUnDraft = new System.Windows.Forms.CheckBox();
            this.chkUndeleted = new System.Windows.Forms.CheckBox();
            this.chkUnanswered = new System.Windows.Forms.CheckBox();
            this.tab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParts)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeaders)).BeginInit();
            this.tabPage5.SuspendLayout();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkInvert
            // 
            this.chkInvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkInvert.AutoSize = true;
            this.chkInvert.Location = new System.Drawing.Point(5, 297);
            this.chkInvert.Name = "chkInvert";
            this.chkInvert.Size = new System.Drawing.Size(53, 17);
            this.chkInvert.TabIndex = 1;
            this.chkInvert.Text = "Invert";
            this.chkInvert.UseVisualStyleBackColor = true;
            // 
            // cmdOr
            // 
            this.cmdOr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdOr.Location = new System.Drawing.Point(64, 292);
            this.cmdOr.Name = "cmdOr";
            this.cmdOr.Size = new System.Drawing.Size(100, 25);
            this.cmdOr.TabIndex = 2;
            this.cmdOr.Text = "Or";
            this.cmdOr.UseVisualStyleBackColor = true;
            this.cmdOr.Click += new System.EventHandler(this.cmdOr_Click);
            // 
            // cmdApply
            // 
            this.cmdApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdApply.Location = new System.Drawing.Point(528, 292);
            this.cmdApply.Name = "cmdApply";
            this.cmdApply.Size = new System.Drawing.Size(100, 25);
            this.cmdApply.TabIndex = 3;
            this.cmdApply.Text = "Apply";
            this.cmdApply.UseVisualStyleBackColor = true;
            this.cmdApply.Click += new System.EventHandler(this.cmdApply_Click);
            // 
            // tab
            // 
            this.tab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tab.Controls.Add(this.tabPage1);
            this.tab.Controls.Add(this.tabPage2);
            this.tab.Controls.Add(this.tabPage3);
            this.tab.Controls.Add(this.tabPage4);
            this.tab.Controls.Add(this.tabPage5);
            this.tab.Location = new System.Drawing.Point(5, 5);
            this.tab.Name = "tab";
            this.tab.SelectedIndex = 0;
            this.tab.Size = new System.Drawing.Size(623, 281);
            this.tab.TabIndex = 0;
            this.tab.Validating += new System.ComponentModel.CancelEventHandler(this.tab_Validating);
            this.tab.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(615, 255);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Flags";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.txtSet);
            this.groupBox1.Controls.Add(this.chkSubmitted);
            this.groupBox1.Controls.Add(this.chkSubmitPending);
            this.groupBox1.Controls.Add(this.chkForwarded);
            this.groupBox1.Controls.Add(this.chkMDNSent);
            this.groupBox1.Controls.Add(this.chkSeen);
            this.groupBox1.Controls.Add(this.chkRecent);
            this.groupBox1.Controls.Add(this.chkFlagged);
            this.groupBox1.Controls.Add(this.chkDraft);
            this.groupBox1.Controls.Add(this.chkDeleted);
            this.groupBox1.Controls.Add(this.chkAnswered);
            this.groupBox1.Location = new System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(603, 117);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Set";
            // 
            // chkSeen
            // 
            this.chkSeen.AutoSize = true;
            this.chkSeen.Location = new System.Drawing.Point(542, 20);
            this.chkSeen.Name = "chkSeen";
            this.chkSeen.Size = new System.Drawing.Size(51, 17);
            this.chkSeen.TabIndex = 5;
            this.chkSeen.Text = "Seen";
            this.chkSeen.UseVisualStyleBackColor = true;
            // 
            // chkRecent
            // 
            this.chkRecent.AutoSize = true;
            this.chkRecent.Location = new System.Drawing.Point(436, 20);
            this.chkRecent.Name = "chkRecent";
            this.chkRecent.Size = new System.Drawing.Size(61, 17);
            this.chkRecent.TabIndex = 4;
            this.chkRecent.Text = "Recent";
            this.chkRecent.UseVisualStyleBackColor = true;
            // 
            // chkFlagged
            // 
            this.chkFlagged.AutoSize = true;
            this.chkFlagged.Location = new System.Drawing.Point(330, 20);
            this.chkFlagged.Name = "chkFlagged";
            this.chkFlagged.Size = new System.Drawing.Size(64, 17);
            this.chkFlagged.TabIndex = 3;
            this.chkFlagged.Text = "Flagged";
            this.chkFlagged.UseVisualStyleBackColor = true;
            // 
            // chkDraft
            // 
            this.chkDraft.AutoSize = true;
            this.chkDraft.Location = new System.Drawing.Point(224, 20);
            this.chkDraft.Name = "chkDraft";
            this.chkDraft.Size = new System.Drawing.Size(49, 17);
            this.chkDraft.TabIndex = 2;
            this.chkDraft.Text = "Draft";
            this.chkDraft.UseVisualStyleBackColor = true;
            // 
            // chkDeleted
            // 
            this.chkDeleted.AutoSize = true;
            this.chkDeleted.Location = new System.Drawing.Point(118, 20);
            this.chkDeleted.Name = "chkDeleted";
            this.chkDeleted.Size = new System.Drawing.Size(63, 17);
            this.chkDeleted.TabIndex = 1;
            this.chkDeleted.Text = "Deleted";
            this.chkDeleted.UseVisualStyleBackColor = true;
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
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dgvParts);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(615, 255);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Parts Contain";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dgvParts
            // 
            this.dgvParts.AllowUserToAddRows = false;
            this.dgvParts.AllowUserToDeleteRows = false;
            this.dgvParts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvParts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParts.Location = new System.Drawing.Point(3, 6);
            this.dgvParts.Name = "dgvParts";
            this.dgvParts.Size = new System.Drawing.Size(609, 246);
            this.dgvParts.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox4);
            this.tabPage3.Controls.Add(this.groupBox3);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(641, 255);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Dates";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label4);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.dtpSBefore);
            this.groupBox4.Controls.Add(this.dtpSOn);
            this.groupBox4.Controls.Add(this.dtpSAfter);
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
            // dtpSBefore
            // 
            this.dtpSBefore.CustomFormat = " ";
            this.dtpSBefore.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpSBefore.Location = new System.Drawing.Point(216, 40);
            this.dtpSBefore.Name = "dtpSBefore";
            this.dtpSBefore.Size = new System.Drawing.Size(93, 20);
            this.dtpSBefore.TabIndex = 2;
            this.dtpSBefore.DropDown += new System.EventHandler(this.ZDateDropDown);
            this.dtpSBefore.Enter += new System.EventHandler(this.ZDateEnter);
            this.dtpSBefore.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ZDateKeyDown);
            // 
            // dtpSOn
            // 
            this.dtpSOn.CustomFormat = " ";
            this.dtpSOn.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpSOn.Location = new System.Drawing.Point(117, 40);
            this.dtpSOn.Name = "dtpSOn";
            this.dtpSOn.Size = new System.Drawing.Size(93, 20);
            this.dtpSOn.TabIndex = 1;
            this.dtpSOn.DropDown += new System.EventHandler(this.ZDateDropDown);
            this.dtpSOn.Enter += new System.EventHandler(this.ZDateEnter);
            this.dtpSOn.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ZDateKeyDown);
            // 
            // dtpSAfter
            // 
            this.dtpSAfter.CustomFormat = " ";
            this.dtpSAfter.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpSAfter.Location = new System.Drawing.Point(18, 40);
            this.dtpSAfter.Name = "dtpSAfter";
            this.dtpSAfter.Size = new System.Drawing.Size(93, 20);
            this.dtpSAfter.TabIndex = 0;
            this.dtpSAfter.DropDown += new System.EventHandler(this.ZDateDropDown);
            this.dtpSAfter.Enter += new System.EventHandler(this.ZDateEnter);
            this.dtpSAfter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ZDateKeyDown);
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
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(213, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Before";
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "After";
            // 
            // dtpRBefore
            // 
            this.dtpRBefore.CustomFormat = " ";
            this.dtpRBefore.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpRBefore.Location = new System.Drawing.Point(216, 40);
            this.dtpRBefore.Name = "dtpRBefore";
            this.dtpRBefore.Size = new System.Drawing.Size(93, 20);
            this.dtpRBefore.TabIndex = 2;
            this.dtpRBefore.DropDown += new System.EventHandler(this.ZDateDropDown);
            this.dtpRBefore.Enter += new System.EventHandler(this.ZDateEnter);
            this.dtpRBefore.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ZDateKeyDown);
            // 
            // dtpROn
            // 
            this.dtpROn.CustomFormat = " ";
            this.dtpROn.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpROn.Location = new System.Drawing.Point(117, 40);
            this.dtpROn.Name = "dtpROn";
            this.dtpROn.Size = new System.Drawing.Size(93, 20);
            this.dtpROn.TabIndex = 1;
            this.dtpROn.DropDown += new System.EventHandler(this.ZDateDropDown);
            this.dtpROn.Enter += new System.EventHandler(this.ZDateEnter);
            this.dtpROn.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ZDateKeyDown);
            // 
            // dtpRAfter
            // 
            this.dtpRAfter.CustomFormat = " ";
            this.dtpRAfter.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpRAfter.Location = new System.Drawing.Point(18, 40);
            this.dtpRAfter.Name = "dtpRAfter";
            this.dtpRAfter.Size = new System.Drawing.Size(93, 20);
            this.dtpRAfter.TabIndex = 0;
            this.dtpRAfter.DropDown += new System.EventHandler(this.ZDateDropDown);
            this.dtpRAfter.Enter += new System.EventHandler(this.ZDateEnter);
            this.dtpRAfter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ZDateKeyDown);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.dgvHeaders);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(641, 255);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Headers Contain";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // dgvHeaders
            // 
            this.dgvHeaders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvHeaders.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvHeaders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHeaders.Location = new System.Drawing.Point(3, 4);
            this.dgvHeaders.Name = "dgvHeaders";
            this.dgvHeaders.Size = new System.Drawing.Size(635, 246);
            this.dgvHeaders.TabIndex = 1;
            this.dgvHeaders.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvHeaders_RowValidated);
            this.dgvHeaders.RowValidating += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dgvHeaders_RowValidating);
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.groupBox5);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(641, 255);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Other";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.txtSizeSmaller);
            this.groupBox5.Controls.Add(this.txtSizeLarger);
            this.groupBox5.Controls.Add(this.label8);
            this.groupBox5.Controls.Add(this.label9);
            this.groupBox5.Location = new System.Drawing.Point(6, 6);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(264, 68);
            this.groupBox5.TabIndex = 1;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Size";
            // 
            // txtSizeSmaller
            // 
            this.txtSizeSmaller.Location = new System.Drawing.Point(140, 40);
            this.txtSizeSmaller.Name = "txtSizeSmaller";
            this.txtSizeSmaller.Size = new System.Drawing.Size(94, 20);
            this.txtSizeSmaller.TabIndex = 7;
            this.txtSizeSmaller.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtSizeSmaller.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtSizeLarger
            // 
            this.txtSizeLarger.Location = new System.Drawing.Point(17, 40);
            this.txtSizeLarger.Name = "txtSizeLarger";
            this.txtSizeLarger.Size = new System.Drawing.Size(94, 20);
            this.txtSizeLarger.TabIndex = 6;
            this.txtSizeLarger.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtSizeLarger.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(137, 20);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(41, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = "Smaller";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(15, 20);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(37, 13);
            this.label9.TabIndex = 3;
            this.label9.Text = "Larger";
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // chkSubmitted
            // 
            this.chkSubmitted.AutoSize = true;
            this.chkSubmitted.Location = new System.Drawing.Point(224, 43);
            this.chkSubmitted.Name = "chkSubmitted";
            this.chkSubmitted.Size = new System.Drawing.Size(73, 17);
            this.chkSubmitted.TabIndex = 11;
            this.chkSubmitted.Text = "Submitted";
            this.chkSubmitted.UseVisualStyleBackColor = true;
            // 
            // chkSubmitPending
            // 
            this.chkSubmitPending.AutoSize = true;
            this.chkSubmitPending.Location = new System.Drawing.Point(118, 43);
            this.chkSubmitPending.Name = "chkSubmitPending";
            this.chkSubmitPending.Size = new System.Drawing.Size(100, 17);
            this.chkSubmitPending.TabIndex = 10;
            this.chkSubmitPending.Text = "Submit Pending";
            this.chkSubmitPending.UseVisualStyleBackColor = true;
            // 
            // chkForwarded
            // 
            this.chkForwarded.AutoSize = true;
            this.chkForwarded.Location = new System.Drawing.Point(12, 43);
            this.chkForwarded.Name = "chkForwarded";
            this.chkForwarded.Size = new System.Drawing.Size(76, 17);
            this.chkForwarded.TabIndex = 9;
            this.chkForwarded.Text = "Forwarded";
            this.chkForwarded.UseVisualStyleBackColor = true;
            // 
            // chkMDNSent
            // 
            this.chkMDNSent.AutoSize = true;
            this.chkMDNSent.Location = new System.Drawing.Point(436, 43);
            this.chkMDNSent.Name = "chkMDNSent";
            this.chkMDNSent.Size = new System.Drawing.Size(73, 17);
            this.chkMDNSent.TabIndex = 12;
            this.chkMDNSent.Text = "MDNSent";
            this.chkMDNSent.UseVisualStyleBackColor = true;
            // 
            // txtSet
            // 
            this.txtSet.Location = new System.Drawing.Point(12, 85);
            this.txtSet.Name = "txtSet";
            this.txtSet.Size = new System.Drawing.Size(581, 20);
            this.txtSet.TabIndex = 13;
            this.txtSet.Validating += new System.ComponentModel.CancelEventHandler(this.ZValFlagNames);
            this.txtSet.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 69);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(120, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Flags (space separated)";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.txtNotSet);
            this.groupBox2.Controls.Add(this.chkUnsubmitted);
            this.groupBox2.Controls.Add(this.chkUnsubmitPending);
            this.groupBox2.Controls.Add(this.chkUnforwarded);
            this.groupBox2.Controls.Add(this.chkUnMDNSent);
            this.groupBox2.Controls.Add(this.chkUnseen);
            this.groupBox2.Controls.Add(this.chkUnrecent);
            this.groupBox2.Controls.Add(this.chkUnflagged);
            this.groupBox2.Controls.Add(this.chkUnDraft);
            this.groupBox2.Controls.Add(this.chkUndeleted);
            this.groupBox2.Controls.Add(this.chkUnanswered);
            this.groupBox2.Location = new System.Drawing.Point(6, 129);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(603, 117);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Not Set";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(9, 69);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(120, 13);
            this.label10.TabIndex = 14;
            this.label10.Text = "Flags (space separated)";
            // 
            // txtNotSet
            // 
            this.txtNotSet.Location = new System.Drawing.Point(12, 85);
            this.txtNotSet.Name = "txtNotSet";
            this.txtNotSet.Size = new System.Drawing.Size(581, 20);
            this.txtNotSet.TabIndex = 13;
            this.txtNotSet.Validating += new System.ComponentModel.CancelEventHandler(this.ZValFlagNames);
            this.txtNotSet.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // chkUnsubmitted
            // 
            this.chkUnsubmitted.AutoSize = true;
            this.chkUnsubmitted.Location = new System.Drawing.Point(224, 43);
            this.chkUnsubmitted.Name = "chkUnsubmitted";
            this.chkUnsubmitted.Size = new System.Drawing.Size(73, 17);
            this.chkUnsubmitted.TabIndex = 11;
            this.chkUnsubmitted.Text = "Submitted";
            this.chkUnsubmitted.UseVisualStyleBackColor = true;
            // 
            // chkUnsubmitPending
            // 
            this.chkUnsubmitPending.AutoSize = true;
            this.chkUnsubmitPending.Location = new System.Drawing.Point(118, 43);
            this.chkUnsubmitPending.Name = "chkUnsubmitPending";
            this.chkUnsubmitPending.Size = new System.Drawing.Size(100, 17);
            this.chkUnsubmitPending.TabIndex = 10;
            this.chkUnsubmitPending.Text = "Submit Pending";
            this.chkUnsubmitPending.UseVisualStyleBackColor = true;
            // 
            // chkUnforwarded
            // 
            this.chkUnforwarded.AutoSize = true;
            this.chkUnforwarded.Location = new System.Drawing.Point(12, 43);
            this.chkUnforwarded.Name = "chkUnforwarded";
            this.chkUnforwarded.Size = new System.Drawing.Size(76, 17);
            this.chkUnforwarded.TabIndex = 9;
            this.chkUnforwarded.Text = "Forwarded";
            this.chkUnforwarded.UseVisualStyleBackColor = true;
            // 
            // chkUnMDNSent
            // 
            this.chkUnMDNSent.AutoSize = true;
            this.chkUnMDNSent.Location = new System.Drawing.Point(436, 43);
            this.chkUnMDNSent.Name = "chkUnMDNSent";
            this.chkUnMDNSent.Size = new System.Drawing.Size(73, 17);
            this.chkUnMDNSent.TabIndex = 12;
            this.chkUnMDNSent.Text = "MDNSent";
            this.chkUnMDNSent.UseVisualStyleBackColor = true;
            // 
            // chkUnseen
            // 
            this.chkUnseen.AutoSize = true;
            this.chkUnseen.Location = new System.Drawing.Point(542, 20);
            this.chkUnseen.Name = "chkUnseen";
            this.chkUnseen.Size = new System.Drawing.Size(51, 17);
            this.chkUnseen.TabIndex = 5;
            this.chkUnseen.Text = "Seen";
            this.chkUnseen.UseVisualStyleBackColor = true;
            // 
            // chkUnrecent
            // 
            this.chkUnrecent.AutoSize = true;
            this.chkUnrecent.Location = new System.Drawing.Point(436, 20);
            this.chkUnrecent.Name = "chkUnrecent";
            this.chkUnrecent.Size = new System.Drawing.Size(61, 17);
            this.chkUnrecent.TabIndex = 4;
            this.chkUnrecent.Text = "Recent";
            this.chkUnrecent.UseVisualStyleBackColor = true;
            // 
            // chkUnflagged
            // 
            this.chkUnflagged.AutoSize = true;
            this.chkUnflagged.Location = new System.Drawing.Point(330, 20);
            this.chkUnflagged.Name = "chkUnflagged";
            this.chkUnflagged.Size = new System.Drawing.Size(64, 17);
            this.chkUnflagged.TabIndex = 3;
            this.chkUnflagged.Text = "Flagged";
            this.chkUnflagged.UseVisualStyleBackColor = true;
            // 
            // chkUnDraft
            // 
            this.chkUnDraft.AutoSize = true;
            this.chkUnDraft.Location = new System.Drawing.Point(224, 20);
            this.chkUnDraft.Name = "chkUnDraft";
            this.chkUnDraft.Size = new System.Drawing.Size(49, 17);
            this.chkUnDraft.TabIndex = 2;
            this.chkUnDraft.Text = "Draft";
            this.chkUnDraft.UseVisualStyleBackColor = true;
            // 
            // chkUndeleted
            // 
            this.chkUndeleted.AutoSize = true;
            this.chkUndeleted.Location = new System.Drawing.Point(118, 20);
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
            // frmFilter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(652, 321);
            this.Controls.Add(this.tab);
            this.Controls.Add(this.cmdApply);
            this.Controls.Add(this.cmdOr);
            this.Controls.Add(this.chkInvert);
            this.Name = "frmFilter";
            this.Text = "frmFilter";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmFilter_FormClosing);
            this.Load += new System.EventHandler(this.frmFilter_Load);
            this.tab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvParts)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeaders)).EndInit();
            this.tabPage5.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkInvert;
        private System.Windows.Forms.Button cmdOr;
        private System.Windows.Forms.Button cmdApply;
        private System.Windows.Forms.TabControl tab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkSeen;
        private System.Windows.Forms.CheckBox chkRecent;
        private System.Windows.Forms.CheckBox chkFlagged;
        private System.Windows.Forms.CheckBox chkDraft;
        private System.Windows.Forms.CheckBox chkDeleted;
        private System.Windows.Forms.CheckBox chkAnswered;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dgvParts;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DateTimePicker dtpSBefore;
        private System.Windows.Forms.DateTimePicker dtpSOn;
        private System.Windows.Forms.DateTimePicker dtpSAfter;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpRBefore;
        private System.Windows.Forms.DateTimePicker dtpROn;
        private System.Windows.Forms.DateTimePicker dtpRAfter;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.DataGridView dgvHeaders;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TextBox txtSizeSmaller;
        private System.Windows.Forms.TextBox txtSizeLarger;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ErrorProvider erp;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtNotSet;
        private System.Windows.Forms.CheckBox chkUnsubmitted;
        private System.Windows.Forms.CheckBox chkUnsubmitPending;
        private System.Windows.Forms.CheckBox chkUnforwarded;
        private System.Windows.Forms.CheckBox chkUnMDNSent;
        private System.Windows.Forms.CheckBox chkUnseen;
        private System.Windows.Forms.CheckBox chkUnrecent;
        private System.Windows.Forms.CheckBox chkUnflagged;
        private System.Windows.Forms.CheckBox chkUnDraft;
        private System.Windows.Forms.CheckBox chkUndeleted;
        private System.Windows.Forms.CheckBox chkUnanswered;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtSet;
        private System.Windows.Forms.CheckBox chkSubmitted;
        private System.Windows.Forms.CheckBox chkSubmitPending;
        private System.Windows.Forms.CheckBox chkForwarded;
        private System.Windows.Forms.CheckBox chkMDNSent;
    }
}