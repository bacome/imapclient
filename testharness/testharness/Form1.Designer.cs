﻿namespace testharness
{
    partial class Form1
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
            this.txtHost = new System.Windows.Forms.TextBox();
            this.cmdConnect = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.chkSSL = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.pnlCredentials = new System.Windows.Forms.Panel();
            this.label11 = new System.Windows.Forms.Label();
            this.txtTrace = new System.Windows.Forms.TextBox();
            this.rdoCredNone = new System.Windows.Forms.RadioButton();
            this.rdoCredBasic = new System.Windows.Forms.RadioButton();
            this.rdoCredAnon = new System.Windows.Forms.RadioButton();
            this.pnlConnection = new System.Windows.Forms.Panel();
            this.rtxResponseText = new System.Windows.Forms.RichTextBox();
            this.rtxState = new System.Windows.Forms.RichTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cmdConnectAsync = new System.Windows.Forms.Button();
            this.txtTimeouts = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.cmdDisconnect = new System.Windows.Forms.Button();
            this.cmdDisconnectAsync = new System.Windows.Forms.Button();
            this.cmdTests = new System.Windows.Forms.Button();
            this.pnlIdle = new System.Windows.Forms.Panel();
            this.txtIdleRestartInterval = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtPollInterval = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtStartDelay = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chkAutoIdle = new System.Windows.Forms.CheckBox();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.tmr = new System.Windows.Forms.Timer(this.components);
            this.lblTimer = new System.Windows.Forms.Label();
            this.cmdTestsQuick = new System.Windows.Forms.Button();
            this.cmdApply = new System.Windows.Forms.Button();
            this.pnlIgnoreCapabilities = new System.Windows.Forms.Panel();
            this.chkIgnoreQResync = new System.Windows.Forms.CheckBox();
            this.chkIgnoreBinary = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.chkIgnoreNamespace = new System.Windows.Forms.CheckBox();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tvwMailboxes = new System.Windows.Forms.TreeView();
            this.cmdView = new System.Windows.Forms.Button();
            this.cmdStructure = new System.Windows.Forms.Button();
            this.dgvMessageHeaders = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.cmdTestsCurrent = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.chkFetchNobble = new System.Windows.Forms.CheckBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.chkMailboxUID = new System.Windows.Forms.CheckBox();
            this.chkMailboxBodyStructure = new System.Windows.Forms.CheckBox();
            this.chkMailboxSize = new System.Windows.Forms.CheckBox();
            this.chkMailboxReceived = new System.Windows.Forms.CheckBox();
            this.chkMailboxEnvelope = new System.Windows.Forms.CheckBox();
            this.chkMailboxFlags = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.chkMessageUID = new System.Windows.Forms.CheckBox();
            this.chkMessageBodyStructure = new System.Windows.Forms.CheckBox();
            this.chkMessageSize = new System.Windows.Forms.CheckBox();
            this.chkMessageReceived = new System.Windows.Forms.CheckBox();
            this.chkMessageEnvelope = new System.Windows.Forms.CheckBox();
            this.chkMessageFlags = new System.Windows.Forms.CheckBox();
            this.label13 = new System.Windows.Forms.Label();
            this.pnlCredentials.SuspendLayout();
            this.pnlConnection.SuspendLayout();
            this.pnlIdle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.pnlIgnoreCapabilities.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMessageHeaders)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(53, 8);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(154, 20);
            this.txtHost.TabIndex = 0;
            this.txtHost.Text = "192.168.56.101";
            this.txtHost.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxNotBlank);
            this.txtHost.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // cmdConnect
            // 
            this.cmdConnect.Location = new System.Drawing.Point(12, 177);
            this.cmdConnect.Name = "cmdConnect";
            this.cmdConnect.Size = new System.Drawing.Size(106, 25);
            this.cmdConnect.TabIndex = 15;
            this.cmdConnect.Text = "Connect";
            this.cmdConnect.UseVisualStyleBackColor = true;
            this.cmdConnect.Click += new System.EventHandler(this.cmdConnect_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Host";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(213, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Port";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(245, 8);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(32, 20);
            this.txtPort.TabIndex = 1;
            this.txtPort.Text = "143";
            this.txtPort.Validating += new System.ComponentModel.CancelEventHandler(this.txtPort_Validating);
            this.txtPort.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // chkSSL
            // 
            this.chkSSL.AutoSize = true;
            this.chkSSL.Location = new System.Drawing.Point(283, 10);
            this.chkSSL.Name = "chkSSL";
            this.chkSSL.Size = new System.Drawing.Size(46, 17);
            this.chkSSL.TabIndex = 2;
            this.chkSSL.Text = "SSL";
            this.chkSSL.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(98, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "UserId";
            // 
            // txtUserId
            // 
            this.txtUserId.Location = new System.Drawing.Point(157, 57);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(164, 20);
            this.txtUserId.TabIndex = 6;
            this.txtUserId.Text = "imaptest1";
            this.txtUserId.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxNotBlank);
            this.txtUserId.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(98, 82);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Password";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(157, 79);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(164, 20);
            this.txtPassword.TabIndex = 7;
            this.txtPassword.Text = "imaptest1";
            this.txtPassword.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxNotBlank);
            this.txtPassword.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // pnlCredentials
            // 
            this.pnlCredentials.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlCredentials.Controls.Add(this.label11);
            this.pnlCredentials.Controls.Add(this.txtTrace);
            this.pnlCredentials.Controls.Add(this.rdoCredNone);
            this.pnlCredentials.Controls.Add(this.txtPassword);
            this.pnlCredentials.Controls.Add(this.rdoCredBasic);
            this.pnlCredentials.Controls.Add(this.label4);
            this.pnlCredentials.Controls.Add(this.rdoCredAnon);
            this.pnlCredentials.Controls.Add(this.label3);
            this.pnlCredentials.Controls.Add(this.txtUserId);
            this.pnlCredentials.Location = new System.Drawing.Point(12, 56);
            this.pnlCredentials.Name = "pnlCredentials";
            this.pnlCredentials.Size = new System.Drawing.Size(336, 115);
            this.pnlCredentials.TabIndex = 13;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(98, 38);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(35, 13);
            this.label11.TabIndex = 10;
            this.label11.Text = "Trace";
            // 
            // txtTrace
            // 
            this.txtTrace.Location = new System.Drawing.Point(157, 35);
            this.txtTrace.Name = "txtTrace";
            this.txtTrace.Size = new System.Drawing.Size(164, 20);
            this.txtTrace.TabIndex = 9;
            this.txtTrace.TextChanged += new System.EventHandler(this.txtTrace_TextChanged);
            this.txtTrace.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxNotBlank);
            this.txtTrace.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // rdoCredNone
            // 
            this.rdoCredNone.AutoSize = true;
            this.rdoCredNone.Location = new System.Drawing.Point(5, 12);
            this.rdoCredNone.Name = "rdoCredNone";
            this.rdoCredNone.Size = new System.Drawing.Size(51, 17);
            this.rdoCredNone.TabIndex = 3;
            this.rdoCredNone.Text = "None";
            this.rdoCredNone.UseVisualStyleBackColor = true;
            this.rdoCredNone.CheckedChanged += new System.EventHandler(this.rdoCredNone_CheckedChanged);
            // 
            // rdoCredBasic
            // 
            this.rdoCredBasic.AutoSize = true;
            this.rdoCredBasic.Checked = true;
            this.rdoCredBasic.Location = new System.Drawing.Point(5, 58);
            this.rdoCredBasic.Name = "rdoCredBasic";
            this.rdoCredBasic.Size = new System.Drawing.Size(51, 17);
            this.rdoCredBasic.TabIndex = 5;
            this.rdoCredBasic.TabStop = true;
            this.rdoCredBasic.Text = "Basic";
            this.rdoCredBasic.UseVisualStyleBackColor = true;
            this.rdoCredBasic.CheckedChanged += new System.EventHandler(this.rdoCredBasic_CheckedChanged);
            // 
            // rdoCredAnon
            // 
            this.rdoCredAnon.AutoSize = true;
            this.rdoCredAnon.Location = new System.Drawing.Point(5, 35);
            this.rdoCredAnon.Name = "rdoCredAnon";
            this.rdoCredAnon.Size = new System.Drawing.Size(80, 17);
            this.rdoCredAnon.TabIndex = 4;
            this.rdoCredAnon.Text = "Anonymous";
            this.rdoCredAnon.UseVisualStyleBackColor = true;
            this.rdoCredAnon.CheckedChanged += new System.EventHandler(this.rdoCredAnon_CheckedChanged);
            // 
            // pnlConnection
            // 
            this.pnlConnection.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlConnection.Controls.Add(this.label1);
            this.pnlConnection.Controls.Add(this.txtHost);
            this.pnlConnection.Controls.Add(this.chkSSL);
            this.pnlConnection.Controls.Add(this.label2);
            this.pnlConnection.Controls.Add(this.txtPort);
            this.pnlConnection.Location = new System.Drawing.Point(12, 12);
            this.pnlConnection.Name = "pnlConnection";
            this.pnlConnection.Size = new System.Drawing.Size(336, 38);
            this.pnlConnection.TabIndex = 14;
            // 
            // rtxResponseText
            // 
            this.rtxResponseText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxResponseText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtxResponseText.Location = new System.Drawing.Point(6, 6);
            this.rtxResponseText.Name = "rtxResponseText";
            this.rtxResponseText.ReadOnly = true;
            this.rtxResponseText.Size = new System.Drawing.Size(311, 332);
            this.rtxResponseText.TabIndex = 21;
            this.rtxResponseText.Text = "";
            // 
            // rtxState
            // 
            this.rtxState.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtxState.Location = new System.Drawing.Point(364, 30);
            this.rtxState.Name = "rtxState";
            this.rtxState.ReadOnly = true;
            this.rtxState.Size = new System.Drawing.Size(602, 99);
            this.rtxState.TabIndex = 20;
            this.rtxState.Text = "";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(361, 14);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(32, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "State";
            // 
            // cmdConnectAsync
            // 
            this.cmdConnectAsync.Location = new System.Drawing.Point(124, 177);
            this.cmdConnectAsync.Name = "cmdConnectAsync";
            this.cmdConnectAsync.Size = new System.Drawing.Size(106, 25);
            this.cmdConnectAsync.TabIndex = 16;
            this.cmdConnectAsync.Text = "Connect Async";
            this.cmdConnectAsync.UseVisualStyleBackColor = true;
            this.cmdConnectAsync.Click += new System.EventHandler(this.cmdConnectAsync_Click);
            // 
            // txtTimeouts
            // 
            this.txtTimeouts.Location = new System.Drawing.Point(64, 9);
            this.txtTimeouts.Name = "txtTimeouts";
            this.txtTimeouts.Size = new System.Drawing.Size(42, 20);
            this.txtTimeouts.TabIndex = 8;
            this.txtTimeouts.Text = "60000";
            this.txtTimeouts.TextChanged += new System.EventHandler(this.txtTimeouts_TextChanged);
            this.txtTimeouts.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxIsMilliseconds);
            this.txtTimeouts.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 12);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(50, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "Timeouts";
            // 
            // cmdCancel
            // 
            this.cmdCancel.Location = new System.Drawing.Point(236, 177);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(106, 24);
            this.cmdCancel.TabIndex = 19;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // cmdDisconnect
            // 
            this.cmdDisconnect.Location = new System.Drawing.Point(12, 208);
            this.cmdDisconnect.Name = "cmdDisconnect";
            this.cmdDisconnect.Size = new System.Drawing.Size(106, 24);
            this.cmdDisconnect.TabIndex = 17;
            this.cmdDisconnect.Text = "Disconnect";
            this.cmdDisconnect.UseVisualStyleBackColor = true;
            this.cmdDisconnect.Click += new System.EventHandler(this.cmdDisconnect_Click);
            // 
            // cmdDisconnectAsync
            // 
            this.cmdDisconnectAsync.Location = new System.Drawing.Point(124, 208);
            this.cmdDisconnectAsync.Name = "cmdDisconnectAsync";
            this.cmdDisconnectAsync.Size = new System.Drawing.Size(106, 24);
            this.cmdDisconnectAsync.TabIndex = 18;
            this.cmdDisconnectAsync.Text = "Disconnect Async";
            this.cmdDisconnectAsync.UseVisualStyleBackColor = true;
            this.cmdDisconnectAsync.Click += new System.EventHandler(this.cmdDisconnectAsync_Click);
            // 
            // cmdTests
            // 
            this.cmdTests.Location = new System.Drawing.Point(12, 632);
            this.cmdTests.Name = "cmdTests";
            this.cmdTests.Size = new System.Drawing.Size(106, 24);
            this.cmdTests.TabIndex = 14;
            this.cmdTests.Text = "Tests";
            this.cmdTests.UseVisualStyleBackColor = true;
            this.cmdTests.Click += new System.EventHandler(this.cmdTests_Click);
            // 
            // pnlIdle
            // 
            this.pnlIdle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlIdle.Controls.Add(this.txtIdleRestartInterval);
            this.pnlIdle.Controls.Add(this.label10);
            this.pnlIdle.Controls.Add(this.txtPollInterval);
            this.pnlIdle.Controls.Add(this.label9);
            this.pnlIdle.Controls.Add(this.txtStartDelay);
            this.pnlIdle.Controls.Add(this.label8);
            this.pnlIdle.Controls.Add(this.chkAutoIdle);
            this.pnlIdle.Location = new System.Drawing.Point(153, 35);
            this.pnlIdle.Name = "pnlIdle";
            this.pnlIdle.Size = new System.Drawing.Size(178, 106);
            this.pnlIdle.TabIndex = 26;
            // 
            // txtIdleRestartInterval
            // 
            this.txtIdleRestartInterval.Location = new System.Drawing.Point(123, 53);
            this.txtIdleRestartInterval.Name = "txtIdleRestartInterval";
            this.txtIdleRestartInterval.Size = new System.Drawing.Size(40, 20);
            this.txtIdleRestartInterval.TabIndex = 12;
            this.txtIdleRestartInterval.Text = "60000";
            this.txtIdleRestartInterval.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxIsMilliseconds);
            this.txtIdleRestartInterval.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 56);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(78, 13);
            this.label10.TabIndex = 5;
            this.label10.Text = "Idle restart time";
            // 
            // txtPollInterval
            // 
            this.txtPollInterval.Location = new System.Drawing.Point(123, 74);
            this.txtPollInterval.Name = "txtPollInterval";
            this.txtPollInterval.Size = new System.Drawing.Size(40, 20);
            this.txtPollInterval.TabIndex = 13;
            this.txtPollInterval.Text = "60000";
            this.txtPollInterval.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxIsMilliseconds);
            this.txtPollInterval.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 77);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(99, 13);
            this.label9.TabIndex = 3;
            this.label9.Text = "Poll Interval (NoOp)";
            // 
            // txtStartDelay
            // 
            this.txtStartDelay.Location = new System.Drawing.Point(123, 31);
            this.txtStartDelay.Name = "txtStartDelay";
            this.txtStartDelay.Size = new System.Drawing.Size(40, 20);
            this.txtStartDelay.TabIndex = 11;
            this.txtStartDelay.Text = "1000";
            this.txtStartDelay.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxIsMilliseconds);
            this.txtStartDelay.Validated += new System.EventHandler(this.ControlValidated);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 34);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 13);
            this.label8.TabIndex = 1;
            this.label8.Text = "Start Delay";
            // 
            // chkAutoIdle
            // 
            this.chkAutoIdle.AutoSize = true;
            this.chkAutoIdle.Checked = true;
            this.chkAutoIdle.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoIdle.Location = new System.Drawing.Point(6, 10);
            this.chkAutoIdle.Name = "chkAutoIdle";
            this.chkAutoIdle.Size = new System.Drawing.Size(68, 17);
            this.chkAutoIdle.TabIndex = 10;
            this.chkAutoIdle.Text = "Auto Idle";
            this.chkAutoIdle.UseVisualStyleBackColor = true;
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // tmr
            // 
            this.tmr.Enabled = true;
            this.tmr.Tick += new System.EventHandler(this.tmr_Tick);
            // 
            // lblTimer
            // 
            this.lblTimer.AutoSize = true;
            this.lblTimer.Location = new System.Drawing.Point(249, 214);
            this.lblTimer.Name = "lblTimer";
            this.lblTimer.Size = new System.Drawing.Size(41, 13);
            this.lblTimer.TabIndex = 27;
            this.lblTimer.Text = "<timer>";
            // 
            // cmdTestsQuick
            // 
            this.cmdTestsQuick.Location = new System.Drawing.Point(124, 632);
            this.cmdTestsQuick.Name = "cmdTestsQuick";
            this.cmdTestsQuick.Size = new System.Drawing.Size(106, 24);
            this.cmdTestsQuick.TabIndex = 28;
            this.cmdTestsQuick.Text = "Quick Tests";
            this.cmdTestsQuick.UseVisualStyleBackColor = true;
            this.cmdTestsQuick.Click += new System.EventHandler(this.cmdTestsQuick_Click);
            // 
            // cmdApply
            // 
            this.cmdApply.Location = new System.Drawing.Point(223, 166);
            this.cmdApply.Name = "cmdApply";
            this.cmdApply.Size = new System.Drawing.Size(106, 22);
            this.cmdApply.TabIndex = 28;
            this.cmdApply.Text = "Apply";
            this.cmdApply.UseVisualStyleBackColor = true;
            this.cmdApply.Click += new System.EventHandler(this.cmdApply_Click);
            // 
            // pnlIgnoreCapabilities
            // 
            this.pnlIgnoreCapabilities.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlIgnoreCapabilities.Controls.Add(this.chkIgnoreQResync);
            this.pnlIgnoreCapabilities.Controls.Add(this.chkIgnoreBinary);
            this.pnlIgnoreCapabilities.Controls.Add(this.label12);
            this.pnlIgnoreCapabilities.Controls.Add(this.chkIgnoreNamespace);
            this.pnlIgnoreCapabilities.Location = new System.Drawing.Point(12, 35);
            this.pnlIgnoreCapabilities.Name = "pnlIgnoreCapabilities";
            this.pnlIgnoreCapabilities.Size = new System.Drawing.Size(124, 108);
            this.pnlIgnoreCapabilities.TabIndex = 27;
            // 
            // chkIgnoreQResync
            // 
            this.chkIgnoreQResync.AutoSize = true;
            this.chkIgnoreQResync.Location = new System.Drawing.Point(13, 73);
            this.chkIgnoreQResync.Name = "chkIgnoreQResync";
            this.chkIgnoreQResync.Size = new System.Drawing.Size(70, 17);
            this.chkIgnoreQResync.TabIndex = 16;
            this.chkIgnoreQResync.Text = "QResync";
            this.chkIgnoreQResync.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreBinary
            // 
            this.chkIgnoreBinary.AutoSize = true;
            this.chkIgnoreBinary.Location = new System.Drawing.Point(13, 50);
            this.chkIgnoreBinary.Name = "chkIgnoreBinary";
            this.chkIgnoreBinary.Size = new System.Drawing.Size(55, 17);
            this.chkIgnoreBinary.TabIndex = 15;
            this.chkIgnoreBinary.Text = "Binary";
            this.chkIgnoreBinary.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 4);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(93, 13);
            this.label12.TabIndex = 1;
            this.label12.Text = "Ignore Capabilities";
            // 
            // chkIgnoreNamespace
            // 
            this.chkIgnoreNamespace.AutoSize = true;
            this.chkIgnoreNamespace.Location = new System.Drawing.Point(13, 29);
            this.chkIgnoreNamespace.Name = "chkIgnoreNamespace";
            this.chkIgnoreNamespace.Size = new System.Drawing.Size(83, 17);
            this.chkIgnoreNamespace.TabIndex = 14;
            this.chkIgnoreNamespace.Text = "Namespace";
            this.chkIgnoreNamespace.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            this.tabControl2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl2.Controls.Add(this.tabPage4);
            this.tabControl2.Controls.Add(this.tabPage3);
            this.tabControl2.Location = new System.Drawing.Point(364, 139);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(602, 517);
            this.tabControl2.TabIndex = 30;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.splitContainer1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(594, 491);
            this.tabPage4.TabIndex = 1;
            this.tabPage4.Text = "Mailboxes";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(6, 6);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tvwMailboxes);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.cmdView);
            this.splitContainer1.Panel2.Controls.Add(this.cmdStructure);
            this.splitContainer1.Panel2.Controls.Add(this.dgvMessageHeaders);
            this.splitContainer1.Size = new System.Drawing.Size(585, 478);
            this.splitContainer1.SplitterDistance = 169;
            this.splitContainer1.TabIndex = 32;
            // 
            // tvwMailboxes
            // 
            this.tvwMailboxes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvwMailboxes.Location = new System.Drawing.Point(3, 3);
            this.tvwMailboxes.Name = "tvwMailboxes";
            this.tvwMailboxes.Size = new System.Drawing.Size(163, 472);
            this.tvwMailboxes.TabIndex = 0;
            this.tvwMailboxes.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tvwMailboxes_AfterExpand);
            this.tvwMailboxes.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvwMailboxes_AfterSelect);
            // 
            // cmdView
            // 
            this.cmdView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdView.Location = new System.Drawing.Point(199, 453);
            this.cmdView.Name = "cmdView";
            this.cmdView.Size = new System.Drawing.Size(102, 22);
            this.cmdView.TabIndex = 3;
            this.cmdView.Text = "View";
            this.cmdView.UseVisualStyleBackColor = true;
            this.cmdView.Click += new System.EventHandler(this.cmdView_Click);
            // 
            // cmdStructure
            // 
            this.cmdStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdStructure.Location = new System.Drawing.Point(307, 453);
            this.cmdStructure.Name = "cmdStructure";
            this.cmdStructure.Size = new System.Drawing.Size(102, 22);
            this.cmdStructure.TabIndex = 2;
            this.cmdStructure.Text = "Structure";
            this.cmdStructure.UseVisualStyleBackColor = true;
            this.cmdStructure.Click += new System.EventHandler(this.cmdStructure_Click);
            // 
            // dgvMessageHeaders
            // 
            this.dgvMessageHeaders.AllowUserToAddRows = false;
            this.dgvMessageHeaders.AllowUserToDeleteRows = false;
            this.dgvMessageHeaders.AllowUserToResizeColumns = false;
            this.dgvMessageHeaders.AllowUserToResizeRows = false;
            this.dgvMessageHeaders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvMessageHeaders.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvMessageHeaders.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvMessageHeaders.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvMessageHeaders.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvMessageHeaders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMessageHeaders.Location = new System.Drawing.Point(3, 3);
            this.dgvMessageHeaders.Name = "dgvMessageHeaders";
            this.dgvMessageHeaders.ReadOnly = true;
            this.dgvMessageHeaders.Size = new System.Drawing.Size(406, 444);
            this.dgvMessageHeaders.TabIndex = 1;
            this.dgvMessageHeaders.CurrentCellChanged += new System.EventHandler(this.dgvMessageHeaders_CurrentCellChanged);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.rtxResponseText);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(594, 491);
            this.tabPage3.TabIndex = 0;
            this.tabPage3.Text = "Response Text";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // cmdTestsCurrent
            // 
            this.cmdTestsCurrent.Location = new System.Drawing.Point(236, 632);
            this.cmdTestsCurrent.Name = "cmdTestsCurrent";
            this.cmdTestsCurrent.Size = new System.Drawing.Size(106, 24);
            this.cmdTestsCurrent.TabIndex = 31;
            this.cmdTestsCurrent.Text = "Current";
            this.cmdTestsCurrent.UseVisualStyleBackColor = true;
            this.cmdTestsCurrent.Click += new System.EventHandler(this.cmdTestsCurrent_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.chkFetchNobble);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.txtTimeouts);
            this.panel1.Controls.Add(this.cmdApply);
            this.panel1.Controls.Add(this.pnlIgnoreCapabilities);
            this.panel1.Controls.Add(this.pnlIdle);
            this.panel1.Location = new System.Drawing.Point(12, 238);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(336, 198);
            this.panel1.TabIndex = 32;
            // 
            // chkFetchNobble
            // 
            this.chkFetchNobble.AutoSize = true;
            this.chkFetchNobble.Location = new System.Drawing.Point(11, 166);
            this.chkFetchNobble.Name = "chkFetchNobble";
            this.chkFetchNobble.Size = new System.Drawing.Size(90, 17);
            this.chkFetchNobble.TabIndex = 29;
            this.chkFetchNobble.Text = "Fetch Nobble";
            this.chkFetchNobble.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.chkMailboxUID);
            this.panel2.Controls.Add(this.chkMailboxBodyStructure);
            this.panel2.Controls.Add(this.chkMailboxSize);
            this.panel2.Controls.Add(this.chkMailboxReceived);
            this.panel2.Controls.Add(this.chkMailboxEnvelope);
            this.panel2.Controls.Add(this.chkMailboxFlags);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Location = new System.Drawing.Point(12, 442);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(336, 89);
            this.panel2.TabIndex = 33;
            // 
            // chkMailboxUID
            // 
            this.chkMailboxUID.AutoSize = true;
            this.chkMailboxUID.Location = new System.Drawing.Point(239, 59);
            this.chkMailboxUID.Name = "chkMailboxUID";
            this.chkMailboxUID.Size = new System.Drawing.Size(45, 17);
            this.chkMailboxUID.TabIndex = 6;
            this.chkMailboxUID.Text = "UID";
            this.chkMailboxUID.UseVisualStyleBackColor = true;
            // 
            // chkMailboxBodyStructure
            // 
            this.chkMailboxBodyStructure.AutoSize = true;
            this.chkMailboxBodyStructure.Location = new System.Drawing.Point(126, 59);
            this.chkMailboxBodyStructure.Name = "chkMailboxBodyStructure";
            this.chkMailboxBodyStructure.Size = new System.Drawing.Size(96, 17);
            this.chkMailboxBodyStructure.TabIndex = 5;
            this.chkMailboxBodyStructure.Text = "Body Structure";
            this.chkMailboxBodyStructure.UseVisualStyleBackColor = true;
            // 
            // chkMailboxSize
            // 
            this.chkMailboxSize.AutoSize = true;
            this.chkMailboxSize.Location = new System.Drawing.Point(19, 59);
            this.chkMailboxSize.Name = "chkMailboxSize";
            this.chkMailboxSize.Size = new System.Drawing.Size(46, 17);
            this.chkMailboxSize.TabIndex = 4;
            this.chkMailboxSize.Text = "Size";
            this.chkMailboxSize.UseVisualStyleBackColor = true;
            // 
            // chkMailboxReceived
            // 
            this.chkMailboxReceived.AutoSize = true;
            this.chkMailboxReceived.Checked = true;
            this.chkMailboxReceived.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMailboxReceived.Location = new System.Drawing.Point(239, 36);
            this.chkMailboxReceived.Name = "chkMailboxReceived";
            this.chkMailboxReceived.Size = new System.Drawing.Size(72, 17);
            this.chkMailboxReceived.TabIndex = 3;
            this.chkMailboxReceived.Text = "Received";
            this.chkMailboxReceived.UseVisualStyleBackColor = true;
            // 
            // chkMailboxEnvelope
            // 
            this.chkMailboxEnvelope.AutoSize = true;
            this.chkMailboxEnvelope.Checked = true;
            this.chkMailboxEnvelope.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMailboxEnvelope.Location = new System.Drawing.Point(126, 36);
            this.chkMailboxEnvelope.Name = "chkMailboxEnvelope";
            this.chkMailboxEnvelope.Size = new System.Drawing.Size(71, 17);
            this.chkMailboxEnvelope.TabIndex = 2;
            this.chkMailboxEnvelope.Text = "Envelope";
            this.chkMailboxEnvelope.UseVisualStyleBackColor = true;
            // 
            // chkMailboxFlags
            // 
            this.chkMailboxFlags.AutoSize = true;
            this.chkMailboxFlags.Checked = true;
            this.chkMailboxFlags.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMailboxFlags.Location = new System.Drawing.Point(21, 36);
            this.chkMailboxFlags.Name = "chkMailboxFlags";
            this.chkMailboxFlags.Size = new System.Drawing.Size(51, 17);
            this.chkMailboxFlags.TabIndex = 1;
            this.chkMailboxFlags.Text = "Flags";
            this.chkMailboxFlags.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(4, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(212, 13);
            this.label6.TabIndex = 0;
            this.label6.Text = "When selecting mailbox get these attributes";
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.chkMessageUID);
            this.panel3.Controls.Add(this.chkMessageBodyStructure);
            this.panel3.Controls.Add(this.chkMessageSize);
            this.panel3.Controls.Add(this.chkMessageReceived);
            this.panel3.Controls.Add(this.chkMessageEnvelope);
            this.panel3.Controls.Add(this.chkMessageFlags);
            this.panel3.Controls.Add(this.label13);
            this.panel3.Location = new System.Drawing.Point(12, 537);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(336, 89);
            this.panel3.TabIndex = 34;
            // 
            // chkMessageUID
            // 
            this.chkMessageUID.AutoSize = true;
            this.chkMessageUID.Location = new System.Drawing.Point(239, 59);
            this.chkMessageUID.Name = "chkMessageUID";
            this.chkMessageUID.Size = new System.Drawing.Size(45, 17);
            this.chkMessageUID.TabIndex = 6;
            this.chkMessageUID.Text = "UID";
            this.chkMessageUID.UseVisualStyleBackColor = true;
            // 
            // chkMessageBodyStructure
            // 
            this.chkMessageBodyStructure.AutoSize = true;
            this.chkMessageBodyStructure.Checked = true;
            this.chkMessageBodyStructure.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMessageBodyStructure.Location = new System.Drawing.Point(126, 59);
            this.chkMessageBodyStructure.Name = "chkMessageBodyStructure";
            this.chkMessageBodyStructure.Size = new System.Drawing.Size(96, 17);
            this.chkMessageBodyStructure.TabIndex = 5;
            this.chkMessageBodyStructure.Text = "Body Structure";
            this.chkMessageBodyStructure.UseVisualStyleBackColor = true;
            // 
            // chkMessageSize
            // 
            this.chkMessageSize.AutoSize = true;
            this.chkMessageSize.Checked = true;
            this.chkMessageSize.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMessageSize.Location = new System.Drawing.Point(19, 59);
            this.chkMessageSize.Name = "chkMessageSize";
            this.chkMessageSize.Size = new System.Drawing.Size(46, 17);
            this.chkMessageSize.TabIndex = 4;
            this.chkMessageSize.Text = "Size";
            this.chkMessageSize.UseVisualStyleBackColor = true;
            // 
            // chkMessageReceived
            // 
            this.chkMessageReceived.AutoSize = true;
            this.chkMessageReceived.Location = new System.Drawing.Point(239, 36);
            this.chkMessageReceived.Name = "chkMessageReceived";
            this.chkMessageReceived.Size = new System.Drawing.Size(72, 17);
            this.chkMessageReceived.TabIndex = 3;
            this.chkMessageReceived.Text = "Received";
            this.chkMessageReceived.UseVisualStyleBackColor = true;
            // 
            // chkMessageEnvelope
            // 
            this.chkMessageEnvelope.AutoSize = true;
            this.chkMessageEnvelope.Location = new System.Drawing.Point(126, 36);
            this.chkMessageEnvelope.Name = "chkMessageEnvelope";
            this.chkMessageEnvelope.Size = new System.Drawing.Size(71, 17);
            this.chkMessageEnvelope.TabIndex = 2;
            this.chkMessageEnvelope.Text = "Envelope";
            this.chkMessageEnvelope.UseVisualStyleBackColor = true;
            // 
            // chkMessageFlags
            // 
            this.chkMessageFlags.AutoSize = true;
            this.chkMessageFlags.Location = new System.Drawing.Point(21, 36);
            this.chkMessageFlags.Name = "chkMessageFlags";
            this.chkMessageFlags.Size = new System.Drawing.Size(51, 17);
            this.chkMessageFlags.TabIndex = 1;
            this.chkMessageFlags.Text = "Flags";
            this.chkMessageFlags.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(4, 9);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(219, 13);
            this.label13.TabIndex = 0;
            this.label13.Text = "When selecting message get these attributes";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(978, 662);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdTestsCurrent);
            this.Controls.Add(this.cmdDisconnectAsync);
            this.Controls.Add(this.cmdDisconnect);
            this.Controls.Add(this.tabControl2);
            this.Controls.Add(this.cmdConnect);
            this.Controls.Add(this.cmdConnectAsync);
            this.Controls.Add(this.cmdTestsQuick);
            this.Controls.Add(this.lblTimer);
            this.Controls.Add(this.cmdTests);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.rtxState);
            this.Controls.Add(this.pnlConnection);
            this.Controls.Add(this.pnlCredentials);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlCredentials.ResumeLayout(false);
            this.pnlCredentials.PerformLayout();
            this.pnlConnection.ResumeLayout(false);
            this.pnlConnection.PerformLayout();
            this.pnlIdle.ResumeLayout(false);
            this.pnlIdle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.pnlIgnoreCapabilities.ResumeLayout(false);
            this.pnlIgnoreCapabilities.PerformLayout();
            this.tabControl2.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvMessageHeaders)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Button cmdConnect;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.CheckBox chkSSL;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUserId;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Panel pnlCredentials;
        private System.Windows.Forms.RadioButton rdoCredNone;
        private System.Windows.Forms.RadioButton rdoCredBasic;
        private System.Windows.Forms.RadioButton rdoCredAnon;
        private System.Windows.Forms.Panel pnlConnection;
        private System.Windows.Forms.RichTextBox rtxResponseText;
        private System.Windows.Forms.RichTextBox rtxState;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button cmdConnectAsync;
        private System.Windows.Forms.TextBox txtTimeouts;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button cmdDisconnect;
        private System.Windows.Forms.Button cmdDisconnectAsync;
        private System.Windows.Forms.Button cmdTests;
        private System.Windows.Forms.Panel pnlIdle;
        private System.Windows.Forms.TextBox txtIdleRestartInterval;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtPollInterval;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtStartDelay;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkAutoIdle;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtTrace;
        private System.Windows.Forms.ErrorProvider erp;
        private System.Windows.Forms.Label lblTimer;
        private System.Windows.Forms.Timer tmr;
        private System.Windows.Forms.Button cmdTestsQuick;
        private System.Windows.Forms.Panel pnlIgnoreCapabilities;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.CheckBox chkIgnoreNamespace;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TreeView tvwMailboxes;
        private System.Windows.Forms.Button cmdApply;
        private System.Windows.Forms.Button cmdTestsCurrent;
        private System.Windows.Forms.DataGridView dgvMessageHeaders;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckBox chkIgnoreBinary;
        private System.Windows.Forms.Button cmdView;
        private System.Windows.Forms.Button cmdStructure;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox chkMailboxUID;
        private System.Windows.Forms.CheckBox chkMailboxBodyStructure;
        private System.Windows.Forms.CheckBox chkMailboxSize;
        private System.Windows.Forms.CheckBox chkMailboxReceived;
        private System.Windows.Forms.CheckBox chkMailboxEnvelope;
        private System.Windows.Forms.CheckBox chkMailboxFlags;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkFetchNobble;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.CheckBox chkMessageUID;
        private System.Windows.Forms.CheckBox chkMessageBodyStructure;
        private System.Windows.Forms.CheckBox chkMessageSize;
        private System.Windows.Forms.CheckBox chkMessageReceived;
        private System.Windows.Forms.CheckBox chkMessageEnvelope;
        private System.Windows.Forms.CheckBox chkMessageFlags;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.CheckBox chkIgnoreQResync;
    }
}

