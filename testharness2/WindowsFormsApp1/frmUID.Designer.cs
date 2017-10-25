namespace testharness2
{
    partial class frmUID
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
            this.tab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmdSaveAs = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rdoBase64 = new System.Windows.Forms.RadioButton();
            this.rdoQuotedPrintable = new System.Windows.Forms.RadioButton();
            this.rdoNone = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rdoMime = new System.Windows.Forms.RadioButton();
            this.rdoAll = new System.Windows.Forms.RadioButton();
            this.rdoHeader = new System.Windows.Forms.RadioButton();
            this.rdoText = new System.Windows.Forms.RadioButton();
            this.rdoFields = new System.Windows.Forms.RadioButton();
            this.rdoFieldsNot = new System.Windows.Forms.RadioButton();
            this.txtFieldNames = new System.Windows.Forms.TextBox();
            this.txtPart = new System.Windows.Forms.TextBox();
            this.txtUID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtUIDValidity = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.lblSelectedMailbox = new System.Windows.Forms.Label();
            this.tab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.SuspendLayout();
            // 
            // tab
            // 
            this.tab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tab.Controls.Add(this.tabPage1);
            this.tab.Controls.Add(this.tabPage2);
            this.tab.Location = new System.Drawing.Point(2, 25);
            this.tab.Name = "tab";
            this.tab.SelectedIndex = 0;
            this.tab.Size = new System.Drawing.Size(540, 362);
            this.tab.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.cmdSaveAs);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.txtFieldNames);
            this.tabPage1.Controls.Add(this.txtPart);
            this.tabPage1.Controls.Add(this.txtUID);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.txtUIDValidity);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(532, 336);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Fetch";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 32);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(26, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "UID";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 132);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Field Names";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Part";
            // 
            // cmdSaveAs
            // 
            this.cmdSaveAs.Location = new System.Drawing.Point(74, 204);
            this.cmdSaveAs.Name = "cmdSaveAs";
            this.cmdSaveAs.Size = new System.Drawing.Size(100, 25);
            this.cmdSaveAs.TabIndex = 10;
            this.cmdSaveAs.Text = "Save As ...";
            this.cmdSaveAs.UseVisualStyleBackColor = true;
            this.cmdSaveAs.Click += new System.EventHandler(this.cmdSaveAs_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rdoBase64);
            this.groupBox2.Controls.Add(this.rdoQuotedPrintable);
            this.groupBox2.Controls.Add(this.rdoNone);
            this.groupBox2.Location = new System.Drawing.Point(74, 155);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(437, 43);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Decoding Required";
            // 
            // rdoBase64
            // 
            this.rdoBase64.AutoSize = true;
            this.rdoBase64.Location = new System.Drawing.Point(176, 19);
            this.rdoBase64.Name = "rdoBase64";
            this.rdoBase64.Size = new System.Drawing.Size(64, 17);
            this.rdoBase64.TabIndex = 2;
            this.rdoBase64.Text = "Base 64";
            this.rdoBase64.UseVisualStyleBackColor = true;
            // 
            // rdoQuotedPrintable
            // 
            this.rdoQuotedPrintable.AutoSize = true;
            this.rdoQuotedPrintable.Location = new System.Drawing.Point(66, 19);
            this.rdoQuotedPrintable.Name = "rdoQuotedPrintable";
            this.rdoQuotedPrintable.Size = new System.Drawing.Size(104, 17);
            this.rdoQuotedPrintable.TabIndex = 1;
            this.rdoQuotedPrintable.Text = "Quoted-Printable";
            this.rdoQuotedPrintable.UseVisualStyleBackColor = true;
            // 
            // rdoNone
            // 
            this.rdoNone.AutoSize = true;
            this.rdoNone.Checked = true;
            this.rdoNone.Location = new System.Drawing.Point(9, 19);
            this.rdoNone.Name = "rdoNone";
            this.rdoNone.Size = new System.Drawing.Size(51, 17);
            this.rdoNone.TabIndex = 0;
            this.rdoNone.TabStop = true;
            this.rdoNone.Text = "None";
            this.rdoNone.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdoMime);
            this.groupBox1.Controls.Add(this.rdoAll);
            this.groupBox1.Controls.Add(this.rdoHeader);
            this.groupBox1.Controls.Add(this.rdoText);
            this.groupBox1.Controls.Add(this.rdoFields);
            this.groupBox1.Controls.Add(this.rdoFieldsNot);
            this.groupBox1.Location = new System.Drawing.Point(74, 77);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(437, 42);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Text Part";
            // 
            // rdoMime
            // 
            this.rdoMime.AutoSize = true;
            this.rdoMime.Location = new System.Drawing.Point(381, 19);
            this.rdoMime.Name = "rdoMime";
            this.rdoMime.Size = new System.Drawing.Size(50, 17);
            this.rdoMime.TabIndex = 5;
            this.rdoMime.Text = "Mime";
            this.rdoMime.UseVisualStyleBackColor = true;
            // 
            // rdoAll
            // 
            this.rdoAll.AutoSize = true;
            this.rdoAll.Checked = true;
            this.rdoAll.Location = new System.Drawing.Point(9, 19);
            this.rdoAll.Name = "rdoAll";
            this.rdoAll.Size = new System.Drawing.Size(36, 17);
            this.rdoAll.TabIndex = 0;
            this.rdoAll.TabStop = true;
            this.rdoAll.Text = "All";
            this.rdoAll.UseVisualStyleBackColor = true;
            // 
            // rdoHeader
            // 
            this.rdoHeader.AutoSize = true;
            this.rdoHeader.Location = new System.Drawing.Point(51, 19);
            this.rdoHeader.Name = "rdoHeader";
            this.rdoHeader.Size = new System.Drawing.Size(60, 17);
            this.rdoHeader.TabIndex = 1;
            this.rdoHeader.Text = "Header";
            this.rdoHeader.UseVisualStyleBackColor = true;
            // 
            // rdoText
            // 
            this.rdoText.AutoSize = true;
            this.rdoText.Location = new System.Drawing.Point(329, 19);
            this.rdoText.Name = "rdoText";
            this.rdoText.Size = new System.Drawing.Size(46, 17);
            this.rdoText.TabIndex = 4;
            this.rdoText.Text = "Text";
            this.rdoText.UseVisualStyleBackColor = true;
            // 
            // rdoFields
            // 
            this.rdoFields.AutoSize = true;
            this.rdoFields.Location = new System.Drawing.Point(117, 19);
            this.rdoFields.Name = "rdoFields";
            this.rdoFields.Size = new System.Drawing.Size(90, 17);
            this.rdoFields.TabIndex = 2;
            this.rdoFields.Text = "Header Fields";
            this.rdoFields.UseVisualStyleBackColor = true;
            this.rdoFields.CheckedChanged += new System.EventHandler(this.rdoFields_CheckedChanged);
            // 
            // rdoFieldsNot
            // 
            this.rdoFieldsNot.AutoSize = true;
            this.rdoFieldsNot.Location = new System.Drawing.Point(213, 19);
            this.rdoFieldsNot.Name = "rdoFieldsNot";
            this.rdoFieldsNot.Size = new System.Drawing.Size(110, 17);
            this.rdoFieldsNot.TabIndex = 3;
            this.rdoFieldsNot.Text = "Not Header Feilds";
            this.rdoFieldsNot.UseVisualStyleBackColor = true;
            this.rdoFieldsNot.CheckedChanged += new System.EventHandler(this.rdoFieldsNot_CheckedChanged);
            // 
            // txtFieldNames
            // 
            this.txtFieldNames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFieldNames.Location = new System.Drawing.Point(74, 129);
            this.txtFieldNames.Name = "txtFieldNames";
            this.txtFieldNames.Size = new System.Drawing.Size(437, 20);
            this.txtFieldNames.TabIndex = 8;
            this.txtFieldNames.Validating += new System.ComponentModel.CancelEventHandler(this.ZValHeaderFieldNames);
            this.txtFieldNames.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtPart
            // 
            this.txtPart.Location = new System.Drawing.Point(74, 51);
            this.txtPart.Name = "txtPart";
            this.txtPart.Size = new System.Drawing.Size(100, 20);
            this.txtPart.TabIndex = 5;
            this.txtPart.TextChanged += new System.EventHandler(this.txtPart_TextChanged);
            this.txtPart.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsPart);
            this.txtPart.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtUID
            // 
            this.txtUID.Location = new System.Drawing.Point(74, 29);
            this.txtUID.Name = "txtUID";
            this.txtUID.Size = new System.Drawing.Size(100, 20);
            this.txtUID.TabIndex = 3;
            this.txtUID.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsID);
            this.txtUID.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "UID Validity";
            // 
            // txtUIDValidity
            // 
            this.txtUIDValidity.Location = new System.Drawing.Point(74, 6);
            this.txtUIDValidity.Name = "txtUIDValidity";
            this.txtUIDValidity.Size = new System.Drawing.Size(100, 20);
            this.txtUIDValidity.TabIndex = 1;
            this.txtUIDValidity.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsID);
            this.txtUIDValidity.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(532, 234);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Copy, Store, Expunge";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // lblSelectedMailbox
            // 
            this.lblSelectedMailbox.AutoSize = true;
            this.lblSelectedMailbox.Location = new System.Drawing.Point(3, 9);
            this.lblSelectedMailbox.Name = "lblSelectedMailbox";
            this.lblSelectedMailbox.Size = new System.Drawing.Size(102, 13);
            this.lblSelectedMailbox.TabIndex = 0;
            this.lblSelectedMailbox.Text = "No selected mailbox";
            // 
            // frmUID
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(543, 388);
            this.Controls.Add(this.lblSelectedMailbox);
            this.Controls.Add(this.tab);
            this.Name = "frmUID";
            this.Text = "frmUID";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmUID_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmUID_FormClosed);
            this.Load += new System.EventHandler(this.frmUID_Load);
            this.tab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rdoAll;
        private System.Windows.Forms.RadioButton rdoHeader;
        private System.Windows.Forms.RadioButton rdoText;
        private System.Windows.Forms.RadioButton rdoFields;
        private System.Windows.Forms.RadioButton rdoFieldsNot;
        private System.Windows.Forms.TextBox txtFieldNames;
        private System.Windows.Forms.TextBox txtPart;
        private System.Windows.Forms.TextBox txtUID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUIDValidity;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button cmdSaveAs;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rdoBase64;
        private System.Windows.Forms.RadioButton rdoQuotedPrintable;
        private System.Windows.Forms.RadioButton rdoNone;
        private System.Windows.Forms.RadioButton rdoMime;
        private System.Windows.Forms.ErrorProvider erp;
        private System.Windows.Forms.Label lblSelectedMailbox;
    }
}