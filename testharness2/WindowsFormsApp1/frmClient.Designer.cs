namespace testharness2
{
    partial class frmClient
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
            this.gbxServer = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.chkSSL = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.gbxCredentials = new System.Windows.Forms.GroupBox();
            this.chkRequireTLS = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtTrace = new System.Windows.Forms.TextBox();
            this.rdoCredNone = new System.Windows.Forms.RadioButton();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.rdoCredBasic = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.rdoCredAnon = new System.Windows.Forms.RadioButton();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.gbxConnect = new System.Windows.Forms.GroupBox();
            this.cmdConnect = new System.Windows.Forms.Button();
            this.gbxMailboxCacheData = new System.Windows.Forms.GroupBox();
            this.chkCacheSubscribed = new System.Windows.Forms.CheckBox();
            this.chkCacheHighestModSeq = new System.Windows.Forms.CheckBox();
            this.chkCacheUnseenCount = new System.Windows.Forms.CheckBox();
            this.chkCacheUIDValidity = new System.Windows.Forms.CheckBox();
            this.chkCacheUIDNext = new System.Windows.Forms.CheckBox();
            this.chkCacheRecentCount = new System.Windows.Forms.CheckBox();
            this.chkCacheMessageCount = new System.Windows.Forms.CheckBox();
            this.chkCacheSpecialUse = new System.Windows.Forms.CheckBox();
            this.chkCacheChildren = new System.Windows.Forms.CheckBox();
            this.gbxOther = new System.Windows.Forms.GroupBox();
            this.chkMailboxReferrals = new System.Windows.Forms.CheckBox();
            this.gbxCapabilities = new System.Windows.Forms.GroupBox();
            this.chkIgnoreEnable = new System.Windows.Forms.CheckBox();
            this.chkIgnoreStartTLS = new System.Windows.Forms.CheckBox();
            this.chkIgnoreCondStore = new System.Windows.Forms.CheckBox();
            this.chkIgnoreESort = new System.Windows.Forms.CheckBox();
            this.chkIgnoreThreadReferences = new System.Windows.Forms.CheckBox();
            this.chkIgnoreThreadOrderedSubject = new System.Windows.Forms.CheckBox();
            this.chkIgnoreSortDisplay = new System.Windows.Forms.CheckBox();
            this.chkSort = new System.Windows.Forms.CheckBox();
            this.chkIgnoreESearch = new System.Windows.Forms.CheckBox();
            this.chkIgnoreMailboxReferrals = new System.Windows.Forms.CheckBox();
            this.chkIgnoreSpecialUse = new System.Windows.Forms.CheckBox();
            this.chkIgnoreListExtended = new System.Windows.Forms.CheckBox();
            this.chkIgnoreListStatus = new System.Windows.Forms.CheckBox();
            this.chkIgnoreSASLIR = new System.Windows.Forms.CheckBox();
            this.chkIgnoreId = new System.Windows.Forms.CheckBox();
            this.chkIgnoreIdle = new System.Windows.Forms.CheckBox();
            this.chkIgnoreLiteralPlus = new System.Windows.Forms.CheckBox();
            this.chkIgnoreUTF8 = new System.Windows.Forms.CheckBox();
            this.chkIgnoreQResync = new System.Windows.Forms.CheckBox();
            this.chkIgnoreBinary = new System.Windows.Forms.CheckBox();
            this.chkIgnoreNamespace = new System.Windows.Forms.CheckBox();
            this.gbxSettings = new System.Windows.Forms.GroupBox();
            this.gbxFetchBodyWrite = new System.Windows.Forms.GroupBox();
            this.cmdFWSet = new System.Windows.Forms.Button();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.txtFWInitial = new System.Windows.Forms.TextBox();
            this.txtFWMaxTime = new System.Windows.Forms.TextBox();
            this.txtFWMax = new System.Windows.Forms.TextBox();
            this.txtFWMin = new System.Windows.Forms.TextBox();
            this.gbxFetchBodyRead = new System.Windows.Forms.GroupBox();
            this.cmdFRSet = new System.Windows.Forms.Button();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.txtFRInitial = new System.Windows.Forms.TextBox();
            this.txtFRMaxTime = new System.Windows.Forms.TextBox();
            this.txtFRMax = new System.Windows.Forms.TextBox();
            this.txtFRMin = new System.Windows.Forms.TextBox();
            this.gbxFetchAttributes = new System.Windows.Forms.GroupBox();
            this.cmdFASet = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtFAInitial = new System.Windows.Forms.TextBox();
            this.txtFAMaxTime = new System.Windows.Forms.TextBox();
            this.txtFAMax = new System.Windows.Forms.TextBox();
            this.txtFAMin = new System.Windows.Forms.TextBox();
            this.gbxTimeout = new System.Windows.Forms.GroupBox();
            this.cmdTimeoutSet = new System.Windows.Forms.Button();
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.gbxIdle = new System.Windows.Forms.GroupBox();
            this.cmdIdleSet = new System.Windows.Forms.Button();
            this.txtIdleRestartInterval = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtIdlePollInterval = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtIdleStartDelay = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chkIdleAuto = new System.Windows.Forms.CheckBox();
            this.cmdResponseText = new System.Windows.Forms.Button();
            this.gbxWindows = new System.Windows.Forms.GroupBox();
            this.cmdMailboxes = new System.Windows.Forms.Button();
            this.cmdDetails = new System.Windows.Forms.Button();
            this.cmdSelectedMailbox = new System.Windows.Forms.Button();
            this.cmdNetworkActivity = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.lblState = new System.Windows.Forms.Label();
            this.cmdDisconnect = new System.Windows.Forms.Button();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.gbxServer.SuspendLayout();
            this.gbxCredentials.SuspendLayout();
            this.gbxConnect.SuspendLayout();
            this.gbxMailboxCacheData.SuspendLayout();
            this.gbxOther.SuspendLayout();
            this.gbxCapabilities.SuspendLayout();
            this.gbxSettings.SuspendLayout();
            this.gbxFetchBodyWrite.SuspendLayout();
            this.gbxFetchBodyRead.SuspendLayout();
            this.gbxFetchAttributes.SuspendLayout();
            this.gbxTimeout.SuspendLayout();
            this.gbxIdle.SuspendLayout();
            this.gbxWindows.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.SuspendLayout();
            // 
            // gbxServer
            // 
            this.gbxServer.Controls.Add(this.label1);
            this.gbxServer.Controls.Add(this.txtHost);
            this.gbxServer.Controls.Add(this.chkSSL);
            this.gbxServer.Controls.Add(this.label2);
            this.gbxServer.Controls.Add(this.txtPort);
            this.gbxServer.Location = new System.Drawing.Point(6, 18);
            this.gbxServer.Name = "gbxServer";
            this.gbxServer.Size = new System.Drawing.Size(342, 50);
            this.gbxServer.TabIndex = 29;
            this.gbxServer.TabStop = false;
            this.gbxServer.Text = "Server";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Host";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(62, 19);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(150, 20);
            this.txtHost.TabIndex = 4;
            this.txtHost.Text = "192.168.56.101";
            this.txtHost.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtHost.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // chkSSL
            // 
            this.chkSSL.AutoSize = true;
            this.chkSSL.Location = new System.Drawing.Point(292, 21);
            this.chkSSL.Name = "chkSSL";
            this.chkSSL.Size = new System.Drawing.Size(46, 17);
            this.chkSSL.TabIndex = 7;
            this.chkSSL.Text = "SSL";
            this.chkSSL.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(222, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Port";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(254, 19);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(32, 20);
            this.txtPort.TabIndex = 5;
            this.txtPort.Text = "143";
            this.txtPort.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsPortNumber);
            this.txtPort.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxCredentials
            // 
            this.gbxCredentials.Controls.Add(this.chkRequireTLS);
            this.gbxCredentials.Controls.Add(this.label11);
            this.gbxCredentials.Controls.Add(this.txtTrace);
            this.gbxCredentials.Controls.Add(this.rdoCredNone);
            this.gbxCredentials.Controls.Add(this.txtPassword);
            this.gbxCredentials.Controls.Add(this.rdoCredBasic);
            this.gbxCredentials.Controls.Add(this.label4);
            this.gbxCredentials.Controls.Add(this.rdoCredAnon);
            this.gbxCredentials.Controls.Add(this.label3);
            this.gbxCredentials.Controls.Add(this.txtUserId);
            this.gbxCredentials.Location = new System.Drawing.Point(6, 74);
            this.gbxCredentials.Name = "gbxCredentials";
            this.gbxCredentials.Size = new System.Drawing.Size(342, 144);
            this.gbxCredentials.TabIndex = 30;
            this.gbxCredentials.TabStop = false;
            this.gbxCredentials.Text = "Credentials";
            // 
            // chkRequireTLS
            // 
            this.chkRequireTLS.AutoSize = true;
            this.chkRequireTLS.Location = new System.Drawing.Point(15, 118);
            this.chkRequireTLS.Name = "chkRequireTLS";
            this.chkRequireTLS.Size = new System.Drawing.Size(86, 17);
            this.chkRequireTLS.TabIndex = 21;
            this.chkRequireTLS.Text = "Require TLS";
            this.chkRequireTLS.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(103, 43);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(35, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "Trace";
            // 
            // txtTrace
            // 
            this.txtTrace.Location = new System.Drawing.Point(159, 40);
            this.txtTrace.Name = "txtTrace";
            this.txtTrace.Size = new System.Drawing.Size(150, 20);
            this.txtTrace.TabIndex = 19;
            this.txtTrace.EnabledChanged += new System.EventHandler(this.ZValEnableChanged);
            this.txtTrace.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtTrace.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // rdoCredNone
            // 
            this.rdoCredNone.AutoSize = true;
            this.rdoCredNone.Location = new System.Drawing.Point(15, 19);
            this.rdoCredNone.Name = "rdoCredNone";
            this.rdoCredNone.Size = new System.Drawing.Size(51, 17);
            this.rdoCredNone.TabIndex = 12;
            this.rdoCredNone.Text = "None";
            this.rdoCredNone.UseVisualStyleBackColor = true;
            this.rdoCredNone.CheckedChanged += new System.EventHandler(this.ZCredCheckedChanged);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(159, 89);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(150, 20);
            this.txtPassword.TabIndex = 17;
            this.txtPassword.Text = "imaptest1";
            this.txtPassword.EnabledChanged += new System.EventHandler(this.ZValEnableChanged);
            this.txtPassword.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtPassword.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // rdoCredBasic
            // 
            this.rdoCredBasic.AutoSize = true;
            this.rdoCredBasic.Checked = true;
            this.rdoCredBasic.Location = new System.Drawing.Point(15, 67);
            this.rdoCredBasic.Name = "rdoCredBasic";
            this.rdoCredBasic.Size = new System.Drawing.Size(51, 17);
            this.rdoCredBasic.TabIndex = 14;
            this.rdoCredBasic.TabStop = true;
            this.rdoCredBasic.Text = "Basic";
            this.rdoCredBasic.UseVisualStyleBackColor = true;
            this.rdoCredBasic.CheckedChanged += new System.EventHandler(this.ZCredCheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(100, 92);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 18;
            this.label4.Text = "Password";
            // 
            // rdoCredAnon
            // 
            this.rdoCredAnon.AutoSize = true;
            this.rdoCredAnon.Location = new System.Drawing.Point(15, 41);
            this.rdoCredAnon.Name = "rdoCredAnon";
            this.rdoCredAnon.Size = new System.Drawing.Size(80, 17);
            this.rdoCredAnon.TabIndex = 13;
            this.rdoCredAnon.Text = "Anonymous";
            this.rdoCredAnon.UseVisualStyleBackColor = true;
            this.rdoCredAnon.CheckedChanged += new System.EventHandler(this.ZCredCheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(103, 69);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 15;
            this.label3.Text = "UserId";
            // 
            // txtUserId
            // 
            this.txtUserId.Location = new System.Drawing.Point(159, 66);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(150, 20);
            this.txtUserId.TabIndex = 16;
            this.txtUserId.Text = "imaptest1";
            this.txtUserId.EnabledChanged += new System.EventHandler(this.ZValEnableChanged);
            this.txtUserId.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtUserId.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxConnect
            // 
            this.gbxConnect.Controls.Add(this.cmdConnect);
            this.gbxConnect.Controls.Add(this.gbxMailboxCacheData);
            this.gbxConnect.Controls.Add(this.gbxOther);
            this.gbxConnect.Controls.Add(this.gbxCapabilities);
            this.gbxConnect.Controls.Add(this.gbxServer);
            this.gbxConnect.Controls.Add(this.gbxCredentials);
            this.gbxConnect.Location = new System.Drawing.Point(0, 0);
            this.gbxConnect.Name = "gbxConnect";
            this.gbxConnect.Size = new System.Drawing.Size(356, 799);
            this.gbxConnect.TabIndex = 31;
            this.gbxConnect.TabStop = false;
            this.gbxConnect.Text = "Connect";
            // 
            // cmdConnect
            // 
            this.cmdConnect.Location = new System.Drawing.Point(248, 766);
            this.cmdConnect.Name = "cmdConnect";
            this.cmdConnect.Size = new System.Drawing.Size(100, 25);
            this.cmdConnect.TabIndex = 34;
            this.cmdConnect.Text = "Connect";
            this.cmdConnect.UseVisualStyleBackColor = true;
            this.cmdConnect.Click += new System.EventHandler(this.cmdConnect_Click);
            // 
            // gbxMailboxCacheData
            // 
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheSubscribed);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheHighestModSeq);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheUnseenCount);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheUIDValidity);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheUIDNext);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheRecentCount);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheMessageCount);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheSpecialUse);
            this.gbxMailboxCacheData.Controls.Add(this.chkCacheChildren);
            this.gbxMailboxCacheData.Location = new System.Drawing.Point(6, 600);
            this.gbxMailboxCacheData.Name = "gbxMailboxCacheData";
            this.gbxMailboxCacheData.Size = new System.Drawing.Size(342, 160);
            this.gbxMailboxCacheData.TabIndex = 33;
            this.gbxMailboxCacheData.TabStop = false;
            this.gbxMailboxCacheData.Text = "Mailbox Cache Data";
            // 
            // chkCacheSubscribed
            // 
            this.chkCacheSubscribed.AutoSize = true;
            this.chkCacheSubscribed.Location = new System.Drawing.Point(15, 19);
            this.chkCacheSubscribed.Name = "chkCacheSubscribed";
            this.chkCacheSubscribed.Size = new System.Drawing.Size(79, 17);
            this.chkCacheSubscribed.TabIndex = 47;
            this.chkCacheSubscribed.Text = "Subscribed";
            this.chkCacheSubscribed.UseVisualStyleBackColor = true;
            // 
            // chkCacheHighestModSeq
            // 
            this.chkCacheHighestModSeq.AutoSize = true;
            this.chkCacheHighestModSeq.Location = new System.Drawing.Point(154, 134);
            this.chkCacheHighestModSeq.Name = "chkCacheHighestModSeq";
            this.chkCacheHighestModSeq.Size = new System.Drawing.Size(102, 17);
            this.chkCacheHighestModSeq.TabIndex = 46;
            this.chkCacheHighestModSeq.Text = "HighestModSeq";
            this.chkCacheHighestModSeq.UseVisualStyleBackColor = true;
            // 
            // chkCacheUnseenCount
            // 
            this.chkCacheUnseenCount.AutoSize = true;
            this.chkCacheUnseenCount.Location = new System.Drawing.Point(154, 111);
            this.chkCacheUnseenCount.Name = "chkCacheUnseenCount";
            this.chkCacheUnseenCount.Size = new System.Drawing.Size(94, 17);
            this.chkCacheUnseenCount.TabIndex = 45;
            this.chkCacheUnseenCount.Text = "Unseen Count";
            this.chkCacheUnseenCount.UseVisualStyleBackColor = true;
            // 
            // chkCacheUIDValidity
            // 
            this.chkCacheUIDValidity.AutoSize = true;
            this.chkCacheUIDValidity.Location = new System.Drawing.Point(154, 88);
            this.chkCacheUIDValidity.Name = "chkCacheUIDValidity";
            this.chkCacheUIDValidity.Size = new System.Drawing.Size(78, 17);
            this.chkCacheUIDValidity.TabIndex = 44;
            this.chkCacheUIDValidity.Text = "UIDValidity";
            this.chkCacheUIDValidity.UseVisualStyleBackColor = true;
            // 
            // chkCacheUIDNext
            // 
            this.chkCacheUIDNext.AutoSize = true;
            this.chkCacheUIDNext.Location = new System.Drawing.Point(154, 65);
            this.chkCacheUIDNext.Name = "chkCacheUIDNext";
            this.chkCacheUIDNext.Size = new System.Drawing.Size(67, 17);
            this.chkCacheUIDNext.TabIndex = 43;
            this.chkCacheUIDNext.Text = "UIDNext";
            this.chkCacheUIDNext.UseVisualStyleBackColor = true;
            // 
            // chkCacheRecentCount
            // 
            this.chkCacheRecentCount.AutoSize = true;
            this.chkCacheRecentCount.Location = new System.Drawing.Point(154, 42);
            this.chkCacheRecentCount.Name = "chkCacheRecentCount";
            this.chkCacheRecentCount.Size = new System.Drawing.Size(92, 17);
            this.chkCacheRecentCount.TabIndex = 42;
            this.chkCacheRecentCount.Text = "Recent Count";
            this.chkCacheRecentCount.UseVisualStyleBackColor = true;
            // 
            // chkCacheMessageCount
            // 
            this.chkCacheMessageCount.AutoSize = true;
            this.chkCacheMessageCount.Location = new System.Drawing.Point(154, 19);
            this.chkCacheMessageCount.Name = "chkCacheMessageCount";
            this.chkCacheMessageCount.Size = new System.Drawing.Size(100, 17);
            this.chkCacheMessageCount.TabIndex = 41;
            this.chkCacheMessageCount.Text = "Message Count";
            this.chkCacheMessageCount.UseVisualStyleBackColor = true;
            // 
            // chkCacheSpecialUse
            // 
            this.chkCacheSpecialUse.AutoSize = true;
            this.chkCacheSpecialUse.Location = new System.Drawing.Point(15, 65);
            this.chkCacheSpecialUse.Name = "chkCacheSpecialUse";
            this.chkCacheSpecialUse.Size = new System.Drawing.Size(83, 17);
            this.chkCacheSpecialUse.TabIndex = 40;
            this.chkCacheSpecialUse.Text = "Special Use";
            this.chkCacheSpecialUse.UseVisualStyleBackColor = true;
            // 
            // chkCacheChildren
            // 
            this.chkCacheChildren.AutoSize = true;
            this.chkCacheChildren.Location = new System.Drawing.Point(15, 42);
            this.chkCacheChildren.Name = "chkCacheChildren";
            this.chkCacheChildren.Size = new System.Drawing.Size(64, 17);
            this.chkCacheChildren.TabIndex = 39;
            this.chkCacheChildren.Text = "Children";
            this.chkCacheChildren.UseVisualStyleBackColor = true;
            // 
            // gbxOther
            // 
            this.gbxOther.Controls.Add(this.chkMailboxReferrals);
            this.gbxOther.Location = new System.Drawing.Point(6, 551);
            this.gbxOther.Name = "gbxOther";
            this.gbxOther.Size = new System.Drawing.Size(342, 43);
            this.gbxOther.TabIndex = 32;
            this.gbxOther.TabStop = false;
            this.gbxOther.Text = "Other";
            // 
            // chkMailboxReferrals
            // 
            this.chkMailboxReferrals.AutoSize = true;
            this.chkMailboxReferrals.Location = new System.Drawing.Point(15, 19);
            this.chkMailboxReferrals.Name = "chkMailboxReferrals";
            this.chkMailboxReferrals.Size = new System.Drawing.Size(107, 17);
            this.chkMailboxReferrals.TabIndex = 20;
            this.chkMailboxReferrals.Text = "Mailbox Referrals";
            this.chkMailboxReferrals.UseVisualStyleBackColor = true;
            // 
            // gbxCapabilities
            // 
            this.gbxCapabilities.Controls.Add(this.chkIgnoreEnable);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreStartTLS);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreCondStore);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreESort);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreThreadReferences);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreThreadOrderedSubject);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreSortDisplay);
            this.gbxCapabilities.Controls.Add(this.chkSort);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreESearch);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreMailboxReferrals);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreSpecialUse);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreListExtended);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreListStatus);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreSASLIR);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreId);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreIdle);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreLiteralPlus);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreUTF8);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreQResync);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreBinary);
            this.gbxCapabilities.Controls.Add(this.chkIgnoreNamespace);
            this.gbxCapabilities.Location = new System.Drawing.Point(6, 224);
            this.gbxCapabilities.Name = "gbxCapabilities";
            this.gbxCapabilities.Size = new System.Drawing.Size(342, 321);
            this.gbxCapabilities.TabIndex = 31;
            this.gbxCapabilities.TabStop = false;
            this.gbxCapabilities.Text = "Ignore Capabilities";
            // 
            // chkIgnoreEnable
            // 
            this.chkIgnoreEnable.AutoSize = true;
            this.chkIgnoreEnable.Location = new System.Drawing.Point(15, 42);
            this.chkIgnoreEnable.Name = "chkIgnoreEnable";
            this.chkIgnoreEnable.Size = new System.Drawing.Size(59, 17);
            this.chkIgnoreEnable.TabIndex = 39;
            this.chkIgnoreEnable.Text = "Enable";
            this.chkIgnoreEnable.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreStartTLS
            // 
            this.chkIgnoreStartTLS.AutoSize = true;
            this.chkIgnoreStartTLS.Location = new System.Drawing.Point(15, 19);
            this.chkIgnoreStartTLS.Name = "chkIgnoreStartTLS";
            this.chkIgnoreStartTLS.Size = new System.Drawing.Size(71, 17);
            this.chkIgnoreStartTLS.TabIndex = 38;
            this.chkIgnoreStartTLS.Text = "Start TLS";
            this.chkIgnoreStartTLS.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreCondStore
            // 
            this.chkIgnoreCondStore.AutoSize = true;
            this.chkIgnoreCondStore.Location = new System.Drawing.Point(15, 272);
            this.chkIgnoreCondStore.Name = "chkIgnoreCondStore";
            this.chkIgnoreCondStore.Size = new System.Drawing.Size(76, 17);
            this.chkIgnoreCondStore.TabIndex = 37;
            this.chkIgnoreCondStore.Text = "CondStore";
            this.chkIgnoreCondStore.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreESort
            // 
            this.chkIgnoreESort.AutoSize = true;
            this.chkIgnoreESort.Location = new System.Drawing.Point(159, 249);
            this.chkIgnoreESort.Name = "chkIgnoreESort";
            this.chkIgnoreESort.Size = new System.Drawing.Size(52, 17);
            this.chkIgnoreESort.TabIndex = 34;
            this.chkIgnoreESort.Text = "ESort";
            this.chkIgnoreESort.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreThreadReferences
            // 
            this.chkIgnoreThreadReferences.AutoSize = true;
            this.chkIgnoreThreadReferences.Location = new System.Drawing.Point(159, 226);
            this.chkIgnoreThreadReferences.Name = "chkIgnoreThreadReferences";
            this.chkIgnoreThreadReferences.Size = new System.Drawing.Size(121, 17);
            this.chkIgnoreThreadReferences.TabIndex = 33;
            this.chkIgnoreThreadReferences.Text = "Thread=References";
            this.chkIgnoreThreadReferences.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreThreadOrderedSubject
            // 
            this.chkIgnoreThreadOrderedSubject.AutoSize = true;
            this.chkIgnoreThreadOrderedSubject.Location = new System.Drawing.Point(159, 203);
            this.chkIgnoreThreadOrderedSubject.Name = "chkIgnoreThreadOrderedSubject";
            this.chkIgnoreThreadOrderedSubject.Size = new System.Drawing.Size(140, 17);
            this.chkIgnoreThreadOrderedSubject.TabIndex = 32;
            this.chkIgnoreThreadOrderedSubject.Text = "Thread=OrderedSubject";
            this.chkIgnoreThreadOrderedSubject.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreSortDisplay
            // 
            this.chkIgnoreSortDisplay.AutoSize = true;
            this.chkIgnoreSortDisplay.Location = new System.Drawing.Point(159, 180);
            this.chkIgnoreSortDisplay.Name = "chkIgnoreSortDisplay";
            this.chkIgnoreSortDisplay.Size = new System.Drawing.Size(85, 17);
            this.chkIgnoreSortDisplay.TabIndex = 31;
            this.chkIgnoreSortDisplay.Text = "Sort=Display";
            this.chkIgnoreSortDisplay.UseVisualStyleBackColor = true;
            // 
            // chkSort
            // 
            this.chkSort.AutoSize = true;
            this.chkSort.Location = new System.Drawing.Point(159, 157);
            this.chkSort.Name = "chkSort";
            this.chkSort.Size = new System.Drawing.Size(45, 17);
            this.chkSort.TabIndex = 30;
            this.chkSort.Text = "Sort";
            this.chkSort.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreESearch
            // 
            this.chkIgnoreESearch.AutoSize = true;
            this.chkIgnoreESearch.Location = new System.Drawing.Point(159, 134);
            this.chkIgnoreESearch.Name = "chkIgnoreESearch";
            this.chkIgnoreESearch.Size = new System.Drawing.Size(67, 17);
            this.chkIgnoreESearch.TabIndex = 29;
            this.chkIgnoreESearch.Text = "ESearch";
            this.chkIgnoreESearch.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreMailboxReferrals
            // 
            this.chkIgnoreMailboxReferrals.AutoSize = true;
            this.chkIgnoreMailboxReferrals.Location = new System.Drawing.Point(15, 157);
            this.chkIgnoreMailboxReferrals.Name = "chkIgnoreMailboxReferrals";
            this.chkIgnoreMailboxReferrals.Size = new System.Drawing.Size(107, 17);
            this.chkIgnoreMailboxReferrals.TabIndex = 28;
            this.chkIgnoreMailboxReferrals.Text = "Mailbox Referrals";
            this.chkIgnoreMailboxReferrals.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreSpecialUse
            // 
            this.chkIgnoreSpecialUse.AutoSize = true;
            this.chkIgnoreSpecialUse.Location = new System.Drawing.Point(15, 226);
            this.chkIgnoreSpecialUse.Name = "chkIgnoreSpecialUse";
            this.chkIgnoreSpecialUse.Size = new System.Drawing.Size(83, 17);
            this.chkIgnoreSpecialUse.TabIndex = 27;
            this.chkIgnoreSpecialUse.Text = "Special Use";
            this.chkIgnoreSpecialUse.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreListExtended
            // 
            this.chkIgnoreListExtended.AutoSize = true;
            this.chkIgnoreListExtended.Location = new System.Drawing.Point(15, 180);
            this.chkIgnoreListExtended.Name = "chkIgnoreListExtended";
            this.chkIgnoreListExtended.Size = new System.Drawing.Size(90, 17);
            this.chkIgnoreListExtended.TabIndex = 26;
            this.chkIgnoreListExtended.Text = "List Extended";
            this.chkIgnoreListExtended.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreListStatus
            // 
            this.chkIgnoreListStatus.AutoSize = true;
            this.chkIgnoreListStatus.Location = new System.Drawing.Point(15, 203);
            this.chkIgnoreListStatus.Name = "chkIgnoreListStatus";
            this.chkIgnoreListStatus.Size = new System.Drawing.Size(75, 17);
            this.chkIgnoreListStatus.TabIndex = 25;
            this.chkIgnoreListStatus.Text = "List Status";
            this.chkIgnoreListStatus.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreSASLIR
            // 
            this.chkIgnoreSASLIR.AutoSize = true;
            this.chkIgnoreSASLIR.Location = new System.Drawing.Point(159, 88);
            this.chkIgnoreSASLIR.Name = "chkIgnoreSASLIR";
            this.chkIgnoreSASLIR.Size = new System.Drawing.Size(67, 17);
            this.chkIgnoreSASLIR.TabIndex = 24;
            this.chkIgnoreSASLIR.Text = "SASL-IR";
            this.chkIgnoreSASLIR.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreId
            // 
            this.chkIgnoreId.AutoSize = true;
            this.chkIgnoreId.Location = new System.Drawing.Point(15, 88);
            this.chkIgnoreId.Name = "chkIgnoreId";
            this.chkIgnoreId.Size = new System.Drawing.Size(35, 17);
            this.chkIgnoreId.TabIndex = 23;
            this.chkIgnoreId.Text = "Id";
            this.chkIgnoreId.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreIdle
            // 
            this.chkIgnoreIdle.AutoSize = true;
            this.chkIgnoreIdle.Location = new System.Drawing.Point(159, 65);
            this.chkIgnoreIdle.Name = "chkIgnoreIdle";
            this.chkIgnoreIdle.Size = new System.Drawing.Size(43, 17);
            this.chkIgnoreIdle.TabIndex = 22;
            this.chkIgnoreIdle.Text = "Idle";
            this.chkIgnoreIdle.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreLiteralPlus
            // 
            this.chkIgnoreLiteralPlus.AutoSize = true;
            this.chkIgnoreLiteralPlus.Location = new System.Drawing.Point(159, 19);
            this.chkIgnoreLiteralPlus.Name = "chkIgnoreLiteralPlus";
            this.chkIgnoreLiteralPlus.Size = new System.Drawing.Size(68, 17);
            this.chkIgnoreLiteralPlus.TabIndex = 21;
            this.chkIgnoreLiteralPlus.Text = "Literal+/-";
            this.chkIgnoreLiteralPlus.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreUTF8
            // 
            this.chkIgnoreUTF8.AutoSize = true;
            this.chkIgnoreUTF8.Location = new System.Drawing.Point(15, 65);
            this.chkIgnoreUTF8.Name = "chkIgnoreUTF8";
            this.chkIgnoreUTF8.Size = new System.Drawing.Size(53, 17);
            this.chkIgnoreUTF8.TabIndex = 20;
            this.chkIgnoreUTF8.Text = "UTF8";
            this.chkIgnoreUTF8.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreQResync
            // 
            this.chkIgnoreQResync.AutoSize = true;
            this.chkIgnoreQResync.Location = new System.Drawing.Point(15, 295);
            this.chkIgnoreQResync.Name = "chkIgnoreQResync";
            this.chkIgnoreQResync.Size = new System.Drawing.Size(70, 17);
            this.chkIgnoreQResync.TabIndex = 19;
            this.chkIgnoreQResync.Text = "QResync";
            this.chkIgnoreQResync.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreBinary
            // 
            this.chkIgnoreBinary.AutoSize = true;
            this.chkIgnoreBinary.Location = new System.Drawing.Point(159, 42);
            this.chkIgnoreBinary.Name = "chkIgnoreBinary";
            this.chkIgnoreBinary.Size = new System.Drawing.Size(55, 17);
            this.chkIgnoreBinary.TabIndex = 18;
            this.chkIgnoreBinary.Text = "Binary";
            this.chkIgnoreBinary.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreNamespace
            // 
            this.chkIgnoreNamespace.AutoSize = true;
            this.chkIgnoreNamespace.Location = new System.Drawing.Point(15, 111);
            this.chkIgnoreNamespace.Name = "chkIgnoreNamespace";
            this.chkIgnoreNamespace.Size = new System.Drawing.Size(83, 17);
            this.chkIgnoreNamespace.TabIndex = 17;
            this.chkIgnoreNamespace.Text = "Namespace";
            this.chkIgnoreNamespace.UseVisualStyleBackColor = true;
            // 
            // gbxSettings
            // 
            this.gbxSettings.Controls.Add(this.gbxFetchBodyWrite);
            this.gbxSettings.Controls.Add(this.gbxFetchBodyRead);
            this.gbxSettings.Controls.Add(this.gbxFetchAttributes);
            this.gbxSettings.Controls.Add(this.gbxTimeout);
            this.gbxSettings.Controls.Add(this.gbxIdle);
            this.gbxSettings.Location = new System.Drawing.Point(362, 0);
            this.gbxSettings.Name = "gbxSettings";
            this.gbxSettings.Size = new System.Drawing.Size(230, 728);
            this.gbxSettings.TabIndex = 32;
            this.gbxSettings.TabStop = false;
            this.gbxSettings.Text = "Settings";
            // 
            // gbxFetchBodyWrite
            // 
            this.gbxFetchBodyWrite.Controls.Add(this.cmdFWSet);
            this.gbxFetchBodyWrite.Controls.Add(this.label18);
            this.gbxFetchBodyWrite.Controls.Add(this.label19);
            this.gbxFetchBodyWrite.Controls.Add(this.label20);
            this.gbxFetchBodyWrite.Controls.Add(this.label21);
            this.gbxFetchBodyWrite.Controls.Add(this.txtFWInitial);
            this.gbxFetchBodyWrite.Controls.Add(this.txtFWMaxTime);
            this.gbxFetchBodyWrite.Controls.Add(this.txtFWMax);
            this.gbxFetchBodyWrite.Controls.Add(this.txtFWMin);
            this.gbxFetchBodyWrite.Location = new System.Drawing.Point(6, 570);
            this.gbxFetchBodyWrite.Name = "gbxFetchBodyWrite";
            this.gbxFetchBodyWrite.Size = new System.Drawing.Size(194, 145);
            this.gbxFetchBodyWrite.TabIndex = 32;
            this.gbxFetchBodyWrite.TabStop = false;
            this.gbxFetchBodyWrite.Text = "Fetch Body Write";
            this.gbxFetchBodyWrite.Validating += new System.ComponentModel.CancelEventHandler(this.gbxFetchBodyWrite_Validating);
            this.gbxFetchBodyWrite.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdFWSet
            // 
            this.cmdFWSet.Location = new System.Drawing.Point(16, 109);
            this.cmdFWSet.Name = "cmdFWSet";
            this.cmdFWSet.Size = new System.Drawing.Size(100, 25);
            this.cmdFWSet.TabIndex = 27;
            this.cmdFWSet.Text = "Set";
            this.cmdFWSet.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(13, 85);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(31, 13);
            this.label18.TabIndex = 26;
            this.label18.Text = "Initial";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(13, 64);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(53, 13);
            this.label19.TabIndex = 25;
            this.label19.Text = "Max Time";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(13, 43);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(27, 13);
            this.label20.TabIndex = 24;
            this.label20.Text = "Max";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(13, 22);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(24, 13);
            this.label21.TabIndex = 23;
            this.label21.Text = "Min";
            // 
            // txtFWInitial
            // 
            this.txtFWInitial.Location = new System.Drawing.Point(133, 82);
            this.txtFWInitial.Name = "txtFWInitial";
            this.txtFWInitial.Size = new System.Drawing.Size(40, 20);
            this.txtFWInitial.TabIndex = 22;
            this.txtFWInitial.Text = "1000";
            this.txtFWInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFWInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFWMaxTime
            // 
            this.txtFWMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtFWMaxTime.Name = "txtFWMaxTime";
            this.txtFWMaxTime.Size = new System.Drawing.Size(40, 20);
            this.txtFWMaxTime.TabIndex = 21;
            this.txtFWMaxTime.Text = "1000";
            this.txtFWMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtFWMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFWMax
            // 
            this.txtFWMax.Location = new System.Drawing.Point(133, 40);
            this.txtFWMax.Name = "txtFWMax";
            this.txtFWMax.Size = new System.Drawing.Size(40, 20);
            this.txtFWMax.TabIndex = 20;
            this.txtFWMax.Text = "1000";
            this.txtFWMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFWMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFWMin
            // 
            this.txtFWMin.Location = new System.Drawing.Point(133, 19);
            this.txtFWMin.Name = "txtFWMin";
            this.txtFWMin.Size = new System.Drawing.Size(40, 20);
            this.txtFWMin.TabIndex = 19;
            this.txtFWMin.Text = "1000";
            this.txtFWMin.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFWMin.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxFetchBodyRead
            // 
            this.gbxFetchBodyRead.Controls.Add(this.cmdFRSet);
            this.gbxFetchBodyRead.Controls.Add(this.label14);
            this.gbxFetchBodyRead.Controls.Add(this.label15);
            this.gbxFetchBodyRead.Controls.Add(this.label16);
            this.gbxFetchBodyRead.Controls.Add(this.label17);
            this.gbxFetchBodyRead.Controls.Add(this.txtFRInitial);
            this.gbxFetchBodyRead.Controls.Add(this.txtFRMaxTime);
            this.gbxFetchBodyRead.Controls.Add(this.txtFRMax);
            this.gbxFetchBodyRead.Controls.Add(this.txtFRMin);
            this.gbxFetchBodyRead.Location = new System.Drawing.Point(6, 417);
            this.gbxFetchBodyRead.Name = "gbxFetchBodyRead";
            this.gbxFetchBodyRead.Size = new System.Drawing.Size(194, 145);
            this.gbxFetchBodyRead.TabIndex = 31;
            this.gbxFetchBodyRead.TabStop = false;
            this.gbxFetchBodyRead.Text = "Fetch Body Read";
            this.gbxFetchBodyRead.Validating += new System.ComponentModel.CancelEventHandler(this.gbxFetchBodyRead_Validating);
            this.gbxFetchBodyRead.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdFRSet
            // 
            this.cmdFRSet.Location = new System.Drawing.Point(16, 109);
            this.cmdFRSet.Name = "cmdFRSet";
            this.cmdFRSet.Size = new System.Drawing.Size(100, 25);
            this.cmdFRSet.TabIndex = 27;
            this.cmdFRSet.Text = "Set";
            this.cmdFRSet.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(13, 85);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(31, 13);
            this.label14.TabIndex = 26;
            this.label14.Text = "Initial";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(13, 64);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(53, 13);
            this.label15.TabIndex = 25;
            this.label15.Text = "Max Time";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(13, 43);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(27, 13);
            this.label16.TabIndex = 24;
            this.label16.Text = "Max";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(13, 22);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(24, 13);
            this.label17.TabIndex = 23;
            this.label17.Text = "Min";
            // 
            // txtFRInitial
            // 
            this.txtFRInitial.Location = new System.Drawing.Point(133, 82);
            this.txtFRInitial.Name = "txtFRInitial";
            this.txtFRInitial.Size = new System.Drawing.Size(40, 20);
            this.txtFRInitial.TabIndex = 22;
            this.txtFRInitial.Text = "1000";
            this.txtFRInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFRInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFRMaxTime
            // 
            this.txtFRMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtFRMaxTime.Name = "txtFRMaxTime";
            this.txtFRMaxTime.Size = new System.Drawing.Size(40, 20);
            this.txtFRMaxTime.TabIndex = 21;
            this.txtFRMaxTime.Text = "1000";
            this.txtFRMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtFRMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFRMax
            // 
            this.txtFRMax.Location = new System.Drawing.Point(133, 40);
            this.txtFRMax.Name = "txtFRMax";
            this.txtFRMax.Size = new System.Drawing.Size(40, 20);
            this.txtFRMax.TabIndex = 20;
            this.txtFRMax.Text = "1000";
            this.txtFRMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFRMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFRMin
            // 
            this.txtFRMin.Location = new System.Drawing.Point(133, 19);
            this.txtFRMin.Name = "txtFRMin";
            this.txtFRMin.Size = new System.Drawing.Size(40, 20);
            this.txtFRMin.TabIndex = 19;
            this.txtFRMin.Text = "1000";
            this.txtFRMin.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFRMin.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxFetchAttributes
            // 
            this.gbxFetchAttributes.Controls.Add(this.cmdFASet);
            this.gbxFetchAttributes.Controls.Add(this.label13);
            this.gbxFetchAttributes.Controls.Add(this.label12);
            this.gbxFetchAttributes.Controls.Add(this.label7);
            this.gbxFetchAttributes.Controls.Add(this.label6);
            this.gbxFetchAttributes.Controls.Add(this.txtFAInitial);
            this.gbxFetchAttributes.Controls.Add(this.txtFAMaxTime);
            this.gbxFetchAttributes.Controls.Add(this.txtFAMax);
            this.gbxFetchAttributes.Controls.Add(this.txtFAMin);
            this.gbxFetchAttributes.Location = new System.Drawing.Point(6, 266);
            this.gbxFetchAttributes.Name = "gbxFetchAttributes";
            this.gbxFetchAttributes.Size = new System.Drawing.Size(194, 145);
            this.gbxFetchAttributes.TabIndex = 30;
            this.gbxFetchAttributes.TabStop = false;
            this.gbxFetchAttributes.Text = "Fetch Attributes";
            this.gbxFetchAttributes.Validating += new System.ComponentModel.CancelEventHandler(this.gbxFetchAttributes_Validating);
            this.gbxFetchAttributes.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdFASet
            // 
            this.cmdFASet.Location = new System.Drawing.Point(16, 109);
            this.cmdFASet.Name = "cmdFASet";
            this.cmdFASet.Size = new System.Drawing.Size(100, 25);
            this.cmdFASet.TabIndex = 27;
            this.cmdFASet.Text = "Set";
            this.cmdFASet.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(13, 85);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(31, 13);
            this.label13.TabIndex = 26;
            this.label13.Text = "Initial";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(13, 64);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(53, 13);
            this.label12.TabIndex = 25;
            this.label12.Text = "Max Time";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 43);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(27, 13);
            this.label7.TabIndex = 24;
            this.label7.Text = "Max";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 22);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(24, 13);
            this.label6.TabIndex = 23;
            this.label6.Text = "Min";
            // 
            // txtFAInitial
            // 
            this.txtFAInitial.Location = new System.Drawing.Point(133, 82);
            this.txtFAInitial.Name = "txtFAInitial";
            this.txtFAInitial.Size = new System.Drawing.Size(40, 20);
            this.txtFAInitial.TabIndex = 22;
            this.txtFAInitial.Text = "1000";
            this.txtFAInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtFAInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFAMaxTime
            // 
            this.txtFAMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtFAMaxTime.Name = "txtFAMaxTime";
            this.txtFAMaxTime.Size = new System.Drawing.Size(40, 20);
            this.txtFAMaxTime.TabIndex = 21;
            this.txtFAMaxTime.Text = "1000";
            this.txtFAMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtFAMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFAMax
            // 
            this.txtFAMax.Location = new System.Drawing.Point(133, 40);
            this.txtFAMax.Name = "txtFAMax";
            this.txtFAMax.Size = new System.Drawing.Size(40, 20);
            this.txtFAMax.TabIndex = 20;
            this.txtFAMax.Text = "1000";
            this.txtFAMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtFAMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFAMin
            // 
            this.txtFAMin.Location = new System.Drawing.Point(133, 19);
            this.txtFAMin.Name = "txtFAMin";
            this.txtFAMin.Size = new System.Drawing.Size(40, 20);
            this.txtFAMin.TabIndex = 19;
            this.txtFAMin.Text = "1000";
            this.txtFAMin.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtFAMin.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxTimeout
            // 
            this.gbxTimeout.Controls.Add(this.cmdTimeoutSet);
            this.gbxTimeout.Controls.Add(this.txtTimeout);
            this.gbxTimeout.Location = new System.Drawing.Point(6, 174);
            this.gbxTimeout.Name = "gbxTimeout";
            this.gbxTimeout.Size = new System.Drawing.Size(194, 83);
            this.gbxTimeout.TabIndex = 29;
            this.gbxTimeout.TabStop = false;
            this.gbxTimeout.Text = "Timeout";
            // 
            // cmdTimeoutSet
            // 
            this.cmdTimeoutSet.Location = new System.Drawing.Point(16, 45);
            this.cmdTimeoutSet.Name = "cmdTimeoutSet";
            this.cmdTimeoutSet.Size = new System.Drawing.Size(100, 25);
            this.cmdTimeoutSet.TabIndex = 22;
            this.cmdTimeoutSet.Text = "Set";
            this.cmdTimeoutSet.UseVisualStyleBackColor = true;
            // 
            // txtTimeout
            // 
            this.txtTimeout.Location = new System.Drawing.Point(16, 19);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(40, 20);
            this.txtTimeout.TabIndex = 10;
            this.txtTimeout.Text = "60000";
            this.txtTimeout.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsTimeout);
            this.txtTimeout.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxIdle
            // 
            this.gbxIdle.Controls.Add(this.cmdIdleSet);
            this.gbxIdle.Controls.Add(this.txtIdleRestartInterval);
            this.gbxIdle.Controls.Add(this.label10);
            this.gbxIdle.Controls.Add(this.txtIdlePollInterval);
            this.gbxIdle.Controls.Add(this.label9);
            this.gbxIdle.Controls.Add(this.txtIdleStartDelay);
            this.gbxIdle.Controls.Add(this.label8);
            this.gbxIdle.Controls.Add(this.chkIdleAuto);
            this.gbxIdle.Location = new System.Drawing.Point(6, 18);
            this.gbxIdle.Name = "gbxIdle";
            this.gbxIdle.Size = new System.Drawing.Size(194, 150);
            this.gbxIdle.TabIndex = 28;
            this.gbxIdle.TabStop = false;
            this.gbxIdle.Text = "Idle";
            // 
            // cmdIdleSet
            // 
            this.cmdIdleSet.Location = new System.Drawing.Point(16, 111);
            this.cmdIdleSet.Name = "cmdIdleSet";
            this.cmdIdleSet.Size = new System.Drawing.Size(100, 25);
            this.cmdIdleSet.TabIndex = 21;
            this.cmdIdleSet.Text = "Set";
            this.cmdIdleSet.UseVisualStyleBackColor = true;
            this.cmdIdleSet.Click += new System.EventHandler(this.cmdIdleSet_Click);
            // 
            // txtIdleRestartInterval
            // 
            this.txtIdleRestartInterval.Location = new System.Drawing.Point(133, 61);
            this.txtIdleRestartInterval.Name = "txtIdleRestartInterval";
            this.txtIdleRestartInterval.Size = new System.Drawing.Size(40, 20);
            this.txtIdleRestartInterval.TabIndex = 19;
            this.txtIdleRestartInterval.Text = "60000";
            this.txtIdleRestartInterval.EnabledChanged += new System.EventHandler(this.ZValEnableChanged);
            this.txtIdleRestartInterval.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtIdleRestartInterval.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(13, 64);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(78, 13);
            this.label10.TabIndex = 16;
            this.label10.Text = "Idle restart time";
            // 
            // txtIdlePollInterval
            // 
            this.txtIdlePollInterval.Location = new System.Drawing.Point(133, 82);
            this.txtIdlePollInterval.Name = "txtIdlePollInterval";
            this.txtIdlePollInterval.Size = new System.Drawing.Size(40, 20);
            this.txtIdlePollInterval.TabIndex = 20;
            this.txtIdlePollInterval.Text = "60000";
            this.txtIdlePollInterval.EnabledChanged += new System.EventHandler(this.ZValEnableChanged);
            this.txtIdlePollInterval.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtIdlePollInterval.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(13, 85);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(99, 13);
            this.label9.TabIndex = 15;
            this.label9.Text = "Poll Interval (NoOp)";
            // 
            // txtIdleStartDelay
            // 
            this.txtIdleStartDelay.Location = new System.Drawing.Point(133, 40);
            this.txtIdleStartDelay.Name = "txtIdleStartDelay";
            this.txtIdleStartDelay.Size = new System.Drawing.Size(40, 20);
            this.txtIdleStartDelay.TabIndex = 18;
            this.txtIdleStartDelay.Text = "1000";
            this.txtIdleStartDelay.EnabledChanged += new System.EventHandler(this.ZValEnableChanged);
            this.txtIdleStartDelay.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtIdleStartDelay.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(13, 43);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 13);
            this.label8.TabIndex = 14;
            this.label8.Text = "Start Delay";
            // 
            // chkIdleAuto
            // 
            this.chkIdleAuto.AutoSize = true;
            this.chkIdleAuto.Checked = true;
            this.chkIdleAuto.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIdleAuto.Location = new System.Drawing.Point(16, 19);
            this.chkIdleAuto.Name = "chkIdleAuto";
            this.chkIdleAuto.Size = new System.Drawing.Size(68, 17);
            this.chkIdleAuto.TabIndex = 17;
            this.chkIdleAuto.Text = "Auto Idle";
            this.chkIdleAuto.UseVisualStyleBackColor = true;
            this.chkIdleAuto.CheckedChanged += new System.EventHandler(this.chkIdleAuto_CheckedChanged);
            // 
            // cmdResponseText
            // 
            this.cmdResponseText.Location = new System.Drawing.Point(15, 49);
            this.cmdResponseText.Name = "cmdResponseText";
            this.cmdResponseText.Size = new System.Drawing.Size(100, 25);
            this.cmdResponseText.TabIndex = 33;
            this.cmdResponseText.Text = "Response text";
            this.cmdResponseText.UseVisualStyleBackColor = true;
            // 
            // gbxWindows
            // 
            this.gbxWindows.Controls.Add(this.cmdMailboxes);
            this.gbxWindows.Controls.Add(this.cmdDetails);
            this.gbxWindows.Controls.Add(this.cmdSelectedMailbox);
            this.gbxWindows.Controls.Add(this.cmdNetworkActivity);
            this.gbxWindows.Controls.Add(this.cmdResponseText);
            this.gbxWindows.Location = new System.Drawing.Point(599, 0);
            this.gbxWindows.Name = "gbxWindows";
            this.gbxWindows.Size = new System.Drawing.Size(129, 177);
            this.gbxWindows.TabIndex = 34;
            this.gbxWindows.TabStop = false;
            this.gbxWindows.Text = "Windows";
            // 
            // cmdMailboxes
            // 
            this.cmdMailboxes.Location = new System.Drawing.Point(15, 139);
            this.cmdMailboxes.Name = "cmdMailboxes";
            this.cmdMailboxes.Size = new System.Drawing.Size(100, 25);
            this.cmdMailboxes.TabIndex = 37;
            this.cmdMailboxes.Text = "Mailboxes";
            this.cmdMailboxes.UseVisualStyleBackColor = true;
            // 
            // cmdDetails
            // 
            this.cmdDetails.Location = new System.Drawing.Point(15, 19);
            this.cmdDetails.Name = "cmdDetails";
            this.cmdDetails.Size = new System.Drawing.Size(100, 25);
            this.cmdDetails.TabIndex = 36;
            this.cmdDetails.Text = "Details";
            this.cmdDetails.UseVisualStyleBackColor = true;
            // 
            // cmdSelectedMailbox
            // 
            this.cmdSelectedMailbox.Location = new System.Drawing.Point(15, 109);
            this.cmdSelectedMailbox.Name = "cmdSelectedMailbox";
            this.cmdSelectedMailbox.Size = new System.Drawing.Size(100, 25);
            this.cmdSelectedMailbox.TabIndex = 35;
            this.cmdSelectedMailbox.Text = "Selected Mailbox";
            this.cmdSelectedMailbox.UseVisualStyleBackColor = true;
            // 
            // cmdNetworkActivity
            // 
            this.cmdNetworkActivity.Location = new System.Drawing.Point(15, 79);
            this.cmdNetworkActivity.Name = "cmdNetworkActivity";
            this.cmdNetworkActivity.Size = new System.Drawing.Size(100, 25);
            this.cmdNetworkActivity.TabIndex = 34;
            this.cmdNetworkActivity.Text = "Network Activity";
            this.cmdNetworkActivity.UseVisualStyleBackColor = true;
            // 
            // cmdCancel
            // 
            this.cmdCancel.Location = new System.Drawing.Point(362, 766);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(100, 25);
            this.cmdCancel.TabIndex = 2;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.Location = new System.Drawing.Point(362, 738);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(32, 13);
            this.lblState.TabIndex = 36;
            this.lblState.Text = "State";
            // 
            // cmdDisconnect
            // 
            this.cmdDisconnect.Location = new System.Drawing.Point(468, 766);
            this.cmdDisconnect.Name = "cmdDisconnect";
            this.cmdDisconnect.Size = new System.Drawing.Size(100, 25);
            this.cmdDisconnect.TabIndex = 36;
            this.cmdDisconnect.Text = "Disconnect";
            this.cmdDisconnect.UseVisualStyleBackColor = true;
            this.cmdDisconnect.Click += new System.EventHandler(this.cmdDisconnect_Click);
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // frmClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(732, 799);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdDisconnect);
            this.Controls.Add(this.gbxWindows);
            this.Controls.Add(this.gbxSettings);
            this.Controls.Add(this.gbxConnect);
            this.Name = "frmClient";
            this.Text = "frmClient";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmClient_FormClosing);
            this.Load += new System.EventHandler(this.frmClient_Load);
            this.gbxServer.ResumeLayout(false);
            this.gbxServer.PerformLayout();
            this.gbxCredentials.ResumeLayout(false);
            this.gbxCredentials.PerformLayout();
            this.gbxConnect.ResumeLayout(false);
            this.gbxMailboxCacheData.ResumeLayout(false);
            this.gbxMailboxCacheData.PerformLayout();
            this.gbxOther.ResumeLayout(false);
            this.gbxOther.PerformLayout();
            this.gbxCapabilities.ResumeLayout(false);
            this.gbxCapabilities.PerformLayout();
            this.gbxSettings.ResumeLayout(false);
            this.gbxFetchBodyWrite.ResumeLayout(false);
            this.gbxFetchBodyWrite.PerformLayout();
            this.gbxFetchBodyRead.ResumeLayout(false);
            this.gbxFetchBodyRead.PerformLayout();
            this.gbxFetchAttributes.ResumeLayout(false);
            this.gbxFetchAttributes.PerformLayout();
            this.gbxTimeout.ResumeLayout(false);
            this.gbxTimeout.PerformLayout();
            this.gbxIdle.ResumeLayout(false);
            this.gbxIdle.PerformLayout();
            this.gbxWindows.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbxServer;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.CheckBox chkSSL;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.GroupBox gbxCredentials;
        private System.Windows.Forms.CheckBox chkRequireTLS;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtTrace;
        private System.Windows.Forms.RadioButton rdoCredNone;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.RadioButton rdoCredBasic;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton rdoCredAnon;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUserId;
        private System.Windows.Forms.GroupBox gbxConnect;
        private System.Windows.Forms.GroupBox gbxCapabilities;
        private System.Windows.Forms.CheckBox chkIgnoreQResync;
        private System.Windows.Forms.CheckBox chkIgnoreBinary;
        private System.Windows.Forms.CheckBox chkIgnoreNamespace;
        private System.Windows.Forms.CheckBox chkIgnoreEnable;
        private System.Windows.Forms.CheckBox chkIgnoreStartTLS;
        private System.Windows.Forms.CheckBox chkIgnoreCondStore;
        private System.Windows.Forms.CheckBox chkIgnoreESort;
        private System.Windows.Forms.CheckBox chkIgnoreThreadReferences;
        private System.Windows.Forms.CheckBox chkIgnoreThreadOrderedSubject;
        private System.Windows.Forms.CheckBox chkIgnoreSortDisplay;
        private System.Windows.Forms.CheckBox chkSort;
        private System.Windows.Forms.CheckBox chkIgnoreESearch;
        private System.Windows.Forms.CheckBox chkIgnoreMailboxReferrals;
        private System.Windows.Forms.CheckBox chkIgnoreSpecialUse;
        private System.Windows.Forms.CheckBox chkIgnoreListExtended;
        private System.Windows.Forms.CheckBox chkIgnoreListStatus;
        private System.Windows.Forms.CheckBox chkIgnoreSASLIR;
        private System.Windows.Forms.CheckBox chkIgnoreId;
        private System.Windows.Forms.CheckBox chkIgnoreIdle;
        private System.Windows.Forms.CheckBox chkIgnoreLiteralPlus;
        private System.Windows.Forms.CheckBox chkIgnoreUTF8;
        private System.Windows.Forms.GroupBox gbxSettings;
        private System.Windows.Forms.GroupBox gbxTimeout;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.GroupBox gbxIdle;
        private System.Windows.Forms.TextBox txtIdleRestartInterval;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtIdlePollInterval;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtIdleStartDelay;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkIdleAuto;
        private System.Windows.Forms.Button cmdResponseText;
        private System.Windows.Forms.GroupBox gbxWindows;
        private System.Windows.Forms.Button cmdSelectedMailbox;
        private System.Windows.Forms.Button cmdNetworkActivity;
        private System.Windows.Forms.GroupBox gbxMailboxCacheData;
        private System.Windows.Forms.GroupBox gbxOther;
        private System.Windows.Forms.CheckBox chkMailboxReferrals;
        private System.Windows.Forms.Button cmdDetails;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.GroupBox gbxFetchAttributes;
        private System.Windows.Forms.Button cmdMailboxes;
        private System.Windows.Forms.Button cmdIdleSet;
        private System.Windows.Forms.Button cmdConnect;
        private System.Windows.Forms.CheckBox chkCacheSubscribed;
        private System.Windows.Forms.CheckBox chkCacheHighestModSeq;
        private System.Windows.Forms.CheckBox chkCacheUnseenCount;
        private System.Windows.Forms.CheckBox chkCacheUIDValidity;
        private System.Windows.Forms.CheckBox chkCacheUIDNext;
        private System.Windows.Forms.CheckBox chkCacheRecentCount;
        private System.Windows.Forms.CheckBox chkCacheMessageCount;
        private System.Windows.Forms.CheckBox chkCacheSpecialUse;
        private System.Windows.Forms.CheckBox chkCacheChildren;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtFAInitial;
        private System.Windows.Forms.TextBox txtFAMaxTime;
        private System.Windows.Forms.TextBox txtFAMax;
        private System.Windows.Forms.TextBox txtFAMin;
        private System.Windows.Forms.GroupBox gbxFetchBodyWrite;
        private System.Windows.Forms.Button cmdFWSet;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TextBox txtFWInitial;
        private System.Windows.Forms.TextBox txtFWMaxTime;
        private System.Windows.Forms.TextBox txtFWMax;
        private System.Windows.Forms.TextBox txtFWMin;
        private System.Windows.Forms.GroupBox gbxFetchBodyRead;
        private System.Windows.Forms.Button cmdFRSet;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox txtFRInitial;
        private System.Windows.Forms.TextBox txtFRMaxTime;
        private System.Windows.Forms.TextBox txtFRMax;
        private System.Windows.Forms.TextBox txtFRMin;
        private System.Windows.Forms.Button cmdFASet;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button cmdTimeoutSet;
        private System.Windows.Forms.Button cmdDisconnect;
        private System.Windows.Forms.ErrorProvider erp;
    }
}