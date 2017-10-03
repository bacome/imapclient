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
            this.gbxTLSRequirement = new System.Windows.Forms.GroupBox();
            this.rdoTLSRequired = new System.Windows.Forms.RadioButton();
            this.rdoTLSIndifferent = new System.Windows.Forms.RadioButton();
            this.rdoTLSDisallowed = new System.Windows.Forms.RadioButton();
            this.chkTryIfNotAdvertised = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtTrace = new System.Windows.Forms.TextBox();
            this.rdoCredNone = new System.Windows.Forms.RadioButton();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.rdoCredBasic = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.rdoCredAnon = new System.Windows.Forms.RadioButton();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
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
            this.chkIgnoreSort = new System.Windows.Forms.CheckBox();
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
            this.gbxFetchCacheItems = new System.Windows.Forms.GroupBox();
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
            this.cmdSubscriptions = new System.Windows.Forms.Button();
            this.cmdEvents = new System.Windows.Forms.Button();
            this.cmdMailboxes = new System.Windows.Forms.Button();
            this.cmdDetails = new System.Windows.Forms.Button();
            this.cmdSelectedMailbox = new System.Windows.Forms.Button();
            this.cmdNetworkActivity = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.lblState = new System.Windows.Forms.Label();
            this.cmdDisconnect = new System.Windows.Forms.Button();
            this.erp = new System.Windows.Forms.ErrorProvider(this.components);
            this.tabConnect = new System.Windows.Forms.TabControl();
            this.tbpDetails = new System.Windows.Forms.TabPage();
            this.tbpCapabilities = new System.Windows.Forms.TabPage();
            this.gbxConnect = new System.Windows.Forms.GroupBox();
            this.gbxSelectedMailbox = new System.Windows.Forms.GroupBox();
            this.label25 = new System.Windows.Forms.Label();
            this.txtSMTextBytes = new System.Windows.Forms.TextBox();
            this.chkProgressBar = new System.Windows.Forms.CheckBox();
            this.chkTrackUIDNext = new System.Windows.Forms.CheckBox();
            this.chkTrackUnseen = new System.Windows.Forms.CheckBox();
            this.txtSMMessages = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tabClient = new System.Windows.Forms.TabControl();
            this.tbpSettings = new System.Windows.Forms.TabPage();
            this.tbpDefaults = new System.Windows.Forms.TabPage();
            this.gbxDefaultSort = new System.Windows.Forms.GroupBox();
            this.cmdSort = new System.Windows.Forms.Button();
            this.lblSort = new System.Windows.Forms.Label();
            this.tbpWindows = new System.Windows.Forms.TabPage();
            this.gbxMailboxes = new System.Windows.Forms.GroupBox();
            this.chkMStatus = new System.Windows.Forms.CheckBox();
            this.chkMLSub = new System.Windows.Forms.CheckBox();
            this.chkMList = new System.Windows.Forms.CheckBox();
            this.gbxEvents = new System.Windows.Forms.GroupBox();
            this.txtEvents = new System.Windows.Forms.TextBox();
            this.label24 = new System.Windows.Forms.Label();
            this.gbxNetworkActivity = new System.Windows.Forms.GroupBox();
            this.txtNetworkActivity = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.tpgResponseText = new System.Windows.Forms.TabPage();
            this.gbxResponseTextCode = new System.Windows.Forms.GroupBox();
            this.chkRTCUnknownCTE = new System.Windows.Forms.CheckBox();
            this.chkRTCUseAttr = new System.Windows.Forms.CheckBox();
            this.chkRTCReferral = new System.Windows.Forms.CheckBox();
            this.chkRTCRFC5530 = new System.Windows.Forms.CheckBox();
            this.chkRTCTryCreate = new System.Windows.Forms.CheckBox();
            this.chkRTCParse = new System.Windows.Forms.CheckBox();
            this.chkRTCBadCharset = new System.Windows.Forms.CheckBox();
            this.chkRTCAlert = new System.Windows.Forms.CheckBox();
            this.chkRTCUnknown = new System.Windows.Forms.CheckBox();
            this.chkRTCNone = new System.Windows.Forms.CheckBox();
            this.gbxResponseTextType = new System.Windows.Forms.GroupBox();
            this.chkRTTContinue = new System.Windows.Forms.CheckBox();
            this.chkRTTProtocolError = new System.Windows.Forms.CheckBox();
            this.chkRTTAuthenticationCancelled = new System.Windows.Forms.CheckBox();
            this.chkRTTFailure = new System.Windows.Forms.CheckBox();
            this.chkRTTSuccess = new System.Windows.Forms.CheckBox();
            this.chkRTTError = new System.Windows.Forms.CheckBox();
            this.chkRTTWarning = new System.Windows.Forms.CheckBox();
            this.chkRTTInformation = new System.Windows.Forms.CheckBox();
            this.chkRTTBye = new System.Windows.Forms.CheckBox();
            this.chkRTTGreeting = new System.Windows.Forms.CheckBox();
            this.label23 = new System.Windows.Forms.Label();
            this.txtResponseText = new System.Windows.Forms.TextBox();
            this.cmdResponseText = new System.Windows.Forms.Button();
            this.cmdPoll = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.cmdAHSet = new System.Windows.Forms.Button();
            this.label26 = new System.Windows.Forms.Label();
            this.txtAHHeaderFieldNames = new System.Windows.Forms.TextBox();
            this.chkAHBodyStructure = new System.Windows.Forms.CheckBox();
            this.chkAHModSeq = new System.Windows.Forms.CheckBox();
            this.chkAHUID = new System.Windows.Forms.CheckBox();
            this.chkAHSize = new System.Windows.Forms.CheckBox();
            this.chkAHReceived = new System.Windows.Forms.CheckBox();
            this.chkAHFlags = new System.Windows.Forms.CheckBox();
            this.chkAHEnvelope = new System.Windows.Forms.CheckBox();
            this.chkPEnvelope = new System.Windows.Forms.CheckBox();
            this.chkPSent = new System.Windows.Forms.CheckBox();
            this.chkPSubject = new System.Windows.Forms.CheckBox();
            this.chkPMessageId = new System.Windows.Forms.CheckBox();
            this.chkPFlags = new System.Windows.Forms.CheckBox();
            this.chkPIsAnswered = new System.Windows.Forms.CheckBox();
            this.chkPIsFlagged = new System.Windows.Forms.CheckBox();
            this.chkPIsSubmitted = new System.Windows.Forms.CheckBox();
            this.chkPReceived = new System.Windows.Forms.CheckBox();
            this.chkPSize = new System.Windows.Forms.CheckBox();
            this.chkPUID = new System.Windows.Forms.CheckBox();
            this.chkPModSeq = new System.Windows.Forms.CheckBox();
            this.chkPBodyStructure = new System.Windows.Forms.CheckBox();
            this.chkPAttachments = new System.Windows.Forms.CheckBox();
            this.chkPImportance = new System.Windows.Forms.CheckBox();
            this.chkPPlainTextSize = new System.Windows.Forms.CheckBox();
            this.chkPReferences = new System.Windows.Forms.CheckBox();
            this.label27 = new System.Windows.Forms.Label();
            this.label28 = new System.Windows.Forms.Label();
            this.cmdPSet = new System.Windows.Forms.Button();
            this.gbxServer.SuspendLayout();
            this.gbxCredentials.SuspendLayout();
            this.gbxTLSRequirement.SuspendLayout();
            this.gbxMailboxCacheData.SuspendLayout();
            this.gbxOther.SuspendLayout();
            this.gbxCapabilities.SuspendLayout();
            this.gbxFetchBodyWrite.SuspendLayout();
            this.gbxFetchBodyRead.SuspendLayout();
            this.gbxFetchCacheItems.SuspendLayout();
            this.gbxTimeout.SuspendLayout();
            this.gbxIdle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).BeginInit();
            this.tabConnect.SuspendLayout();
            this.tbpDetails.SuspendLayout();
            this.tbpCapabilities.SuspendLayout();
            this.gbxConnect.SuspendLayout();
            this.gbxSelectedMailbox.SuspendLayout();
            this.tabClient.SuspendLayout();
            this.tbpSettings.SuspendLayout();
            this.tbpDefaults.SuspendLayout();
            this.gbxDefaultSort.SuspendLayout();
            this.tbpWindows.SuspendLayout();
            this.gbxMailboxes.SuspendLayout();
            this.gbxEvents.SuspendLayout();
            this.gbxNetworkActivity.SuspendLayout();
            this.tpgResponseText.SuspendLayout();
            this.gbxResponseTextCode.SuspendLayout();
            this.gbxResponseTextType.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbxServer
            // 
            this.gbxServer.Controls.Add(this.label1);
            this.gbxServer.Controls.Add(this.txtHost);
            this.gbxServer.Controls.Add(this.chkSSL);
            this.gbxServer.Controls.Add(this.label2);
            this.gbxServer.Controls.Add(this.txtPort);
            this.gbxServer.Location = new System.Drawing.Point(0, 0);
            this.gbxServer.Name = "gbxServer";
            this.gbxServer.Size = new System.Drawing.Size(342, 50);
            this.gbxServer.TabIndex = 0;
            this.gbxServer.TabStop = false;
            this.gbxServer.Text = "Server";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Host";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(62, 19);
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
            this.chkSSL.Location = new System.Drawing.Point(292, 21);
            this.chkSSL.Name = "chkSSL";
            this.chkSSL.Size = new System.Drawing.Size(46, 17);
            this.chkSSL.TabIndex = 4;
            this.chkSSL.Text = "SSL";
            this.chkSSL.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(222, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Port";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(254, 19);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(32, 20);
            this.txtPort.TabIndex = 3;
            this.txtPort.Text = "143";
            this.txtPort.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsPortNumber);
            this.txtPort.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxCredentials
            // 
            this.gbxCredentials.Controls.Add(this.gbxTLSRequirement);
            this.gbxCredentials.Controls.Add(this.chkTryIfNotAdvertised);
            this.gbxCredentials.Controls.Add(this.label11);
            this.gbxCredentials.Controls.Add(this.txtTrace);
            this.gbxCredentials.Controls.Add(this.rdoCredNone);
            this.gbxCredentials.Controls.Add(this.txtPassword);
            this.gbxCredentials.Controls.Add(this.rdoCredBasic);
            this.gbxCredentials.Controls.Add(this.label4);
            this.gbxCredentials.Controls.Add(this.rdoCredAnon);
            this.gbxCredentials.Controls.Add(this.label3);
            this.gbxCredentials.Controls.Add(this.txtUserId);
            this.gbxCredentials.Location = new System.Drawing.Point(0, 56);
            this.gbxCredentials.Name = "gbxCredentials";
            this.gbxCredentials.Size = new System.Drawing.Size(342, 191);
            this.gbxCredentials.TabIndex = 1;
            this.gbxCredentials.TabStop = false;
            this.gbxCredentials.Text = "Credentials";
            // 
            // gbxTLSRequirement
            // 
            this.gbxTLSRequirement.Controls.Add(this.rdoTLSRequired);
            this.gbxTLSRequirement.Controls.Add(this.rdoTLSIndifferent);
            this.gbxTLSRequirement.Controls.Add(this.rdoTLSDisallowed);
            this.gbxTLSRequirement.Location = new System.Drawing.Point(12, 106);
            this.gbxTLSRequirement.Name = "gbxTLSRequirement";
            this.gbxTLSRequirement.Size = new System.Drawing.Size(294, 49);
            this.gbxTLSRequirement.TabIndex = 9;
            this.gbxTLSRequirement.TabStop = false;
            this.gbxTLSRequirement.Text = "TLS Requirement";
            // 
            // rdoTLSRequired
            // 
            this.rdoTLSRequired.AutoSize = true;
            this.rdoTLSRequired.Location = new System.Drawing.Point(115, 19);
            this.rdoTLSRequired.Name = "rdoTLSRequired";
            this.rdoTLSRequired.Size = new System.Drawing.Size(68, 17);
            this.rdoTLSRequired.TabIndex = 1;
            this.rdoTLSRequired.Text = "Required";
            this.rdoTLSRequired.UseVisualStyleBackColor = true;
            // 
            // rdoTLSIndifferent
            // 
            this.rdoTLSIndifferent.AutoSize = true;
            this.rdoTLSIndifferent.Checked = true;
            this.rdoTLSIndifferent.Location = new System.Drawing.Point(19, 19);
            this.rdoTLSIndifferent.Name = "rdoTLSIndifferent";
            this.rdoTLSIndifferent.Size = new System.Drawing.Size(72, 17);
            this.rdoTLSIndifferent.TabIndex = 0;
            this.rdoTLSIndifferent.TabStop = true;
            this.rdoTLSIndifferent.Text = "Indifferent";
            this.rdoTLSIndifferent.UseVisualStyleBackColor = true;
            // 
            // rdoTLSDisallowed
            // 
            this.rdoTLSDisallowed.AutoSize = true;
            this.rdoTLSDisallowed.Location = new System.Drawing.Point(210, 19);
            this.rdoTLSDisallowed.Name = "rdoTLSDisallowed";
            this.rdoTLSDisallowed.Size = new System.Drawing.Size(76, 17);
            this.rdoTLSDisallowed.TabIndex = 2;
            this.rdoTLSDisallowed.Text = "Disallowed";
            this.rdoTLSDisallowed.UseVisualStyleBackColor = true;
            // 
            // chkTryIfNotAdvertised
            // 
            this.chkTryIfNotAdvertised.AutoSize = true;
            this.chkTryIfNotAdvertised.Location = new System.Drawing.Point(12, 166);
            this.chkTryIfNotAdvertised.Name = "chkTryIfNotAdvertised";
            this.chkTryIfNotAdvertised.Size = new System.Drawing.Size(119, 17);
            this.chkTryIfNotAdvertised.TabIndex = 10;
            this.chkTryIfNotAdvertised.Text = "Try if not advertised";
            this.chkTryIfNotAdvertised.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(103, 43);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(35, 13);
            this.label11.TabIndex = 2;
            this.label11.Text = "Trace";
            // 
            // txtTrace
            // 
            this.txtTrace.Location = new System.Drawing.Point(159, 40);
            this.txtTrace.Name = "txtTrace";
            this.txtTrace.Size = new System.Drawing.Size(150, 20);
            this.txtTrace.TabIndex = 3;
            this.txtTrace.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtTrace.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // rdoCredNone
            // 
            this.rdoCredNone.AutoSize = true;
            this.rdoCredNone.Location = new System.Drawing.Point(12, 19);
            this.rdoCredNone.Name = "rdoCredNone";
            this.rdoCredNone.Size = new System.Drawing.Size(51, 17);
            this.rdoCredNone.TabIndex = 0;
            this.rdoCredNone.Text = "None";
            this.rdoCredNone.UseVisualStyleBackColor = true;
            this.rdoCredNone.CheckedChanged += new System.EventHandler(this.ZCredCheckedChanged);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(159, 84);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(150, 20);
            this.txtPassword.TabIndex = 8;
            this.txtPassword.Text = "imaptest1";
            this.txtPassword.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtPassword.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // rdoCredBasic
            // 
            this.rdoCredBasic.AutoSize = true;
            this.rdoCredBasic.Checked = true;
            this.rdoCredBasic.Location = new System.Drawing.Point(12, 63);
            this.rdoCredBasic.Name = "rdoCredBasic";
            this.rdoCredBasic.Size = new System.Drawing.Size(51, 17);
            this.rdoCredBasic.TabIndex = 4;
            this.rdoCredBasic.TabStop = true;
            this.rdoCredBasic.Text = "Basic";
            this.rdoCredBasic.UseVisualStyleBackColor = true;
            this.rdoCredBasic.CheckedChanged += new System.EventHandler(this.ZCredCheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(103, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Password";
            // 
            // rdoCredAnon
            // 
            this.rdoCredAnon.AutoSize = true;
            this.rdoCredAnon.Location = new System.Drawing.Point(12, 41);
            this.rdoCredAnon.Name = "rdoCredAnon";
            this.rdoCredAnon.Size = new System.Drawing.Size(80, 17);
            this.rdoCredAnon.TabIndex = 1;
            this.rdoCredAnon.Text = "Anonymous";
            this.rdoCredAnon.UseVisualStyleBackColor = true;
            this.rdoCredAnon.CheckedChanged += new System.EventHandler(this.ZCredCheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(103, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "UserId";
            // 
            // txtUserId
            // 
            this.txtUserId.Location = new System.Drawing.Point(159, 62);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(150, 20);
            this.txtUserId.TabIndex = 6;
            this.txtUserId.Text = "imaptest1";
            this.txtUserId.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxNotBlank);
            this.txtUserId.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdConnect
            // 
            this.cmdConnect.Location = new System.Drawing.Point(261, 463);
            this.cmdConnect.Name = "cmdConnect";
            this.cmdConnect.Size = new System.Drawing.Size(100, 25);
            this.cmdConnect.TabIndex = 1;
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
            this.gbxMailboxCacheData.Location = new System.Drawing.Point(0, 253);
            this.gbxMailboxCacheData.Name = "gbxMailboxCacheData";
            this.gbxMailboxCacheData.Size = new System.Drawing.Size(342, 160);
            this.gbxMailboxCacheData.TabIndex = 2;
            this.gbxMailboxCacheData.TabStop = false;
            this.gbxMailboxCacheData.Text = "Mailbox Cache Data";
            // 
            // chkCacheSubscribed
            // 
            this.chkCacheSubscribed.AutoSize = true;
            this.chkCacheSubscribed.Location = new System.Drawing.Point(12, 19);
            this.chkCacheSubscribed.Name = "chkCacheSubscribed";
            this.chkCacheSubscribed.Size = new System.Drawing.Size(79, 17);
            this.chkCacheSubscribed.TabIndex = 0;
            this.chkCacheSubscribed.Text = "Subscribed";
            this.chkCacheSubscribed.UseVisualStyleBackColor = true;
            // 
            // chkCacheHighestModSeq
            // 
            this.chkCacheHighestModSeq.AutoSize = true;
            this.chkCacheHighestModSeq.Location = new System.Drawing.Point(154, 134);
            this.chkCacheHighestModSeq.Name = "chkCacheHighestModSeq";
            this.chkCacheHighestModSeq.Size = new System.Drawing.Size(102, 17);
            this.chkCacheHighestModSeq.TabIndex = 8;
            this.chkCacheHighestModSeq.Text = "HighestModSeq";
            this.chkCacheHighestModSeq.UseVisualStyleBackColor = true;
            // 
            // chkCacheUnseenCount
            // 
            this.chkCacheUnseenCount.AutoSize = true;
            this.chkCacheUnseenCount.Location = new System.Drawing.Point(154, 111);
            this.chkCacheUnseenCount.Name = "chkCacheUnseenCount";
            this.chkCacheUnseenCount.Size = new System.Drawing.Size(94, 17);
            this.chkCacheUnseenCount.TabIndex = 7;
            this.chkCacheUnseenCount.Text = "Unseen Count";
            this.chkCacheUnseenCount.UseVisualStyleBackColor = true;
            // 
            // chkCacheUIDValidity
            // 
            this.chkCacheUIDValidity.AutoSize = true;
            this.chkCacheUIDValidity.Location = new System.Drawing.Point(154, 88);
            this.chkCacheUIDValidity.Name = "chkCacheUIDValidity";
            this.chkCacheUIDValidity.Size = new System.Drawing.Size(78, 17);
            this.chkCacheUIDValidity.TabIndex = 6;
            this.chkCacheUIDValidity.Text = "UIDValidity";
            this.chkCacheUIDValidity.UseVisualStyleBackColor = true;
            // 
            // chkCacheUIDNext
            // 
            this.chkCacheUIDNext.AutoSize = true;
            this.chkCacheUIDNext.Location = new System.Drawing.Point(154, 65);
            this.chkCacheUIDNext.Name = "chkCacheUIDNext";
            this.chkCacheUIDNext.Size = new System.Drawing.Size(67, 17);
            this.chkCacheUIDNext.TabIndex = 5;
            this.chkCacheUIDNext.Text = "UIDNext";
            this.chkCacheUIDNext.UseVisualStyleBackColor = true;
            // 
            // chkCacheRecentCount
            // 
            this.chkCacheRecentCount.AutoSize = true;
            this.chkCacheRecentCount.Location = new System.Drawing.Point(154, 42);
            this.chkCacheRecentCount.Name = "chkCacheRecentCount";
            this.chkCacheRecentCount.Size = new System.Drawing.Size(92, 17);
            this.chkCacheRecentCount.TabIndex = 4;
            this.chkCacheRecentCount.Text = "Recent Count";
            this.chkCacheRecentCount.UseVisualStyleBackColor = true;
            // 
            // chkCacheMessageCount
            // 
            this.chkCacheMessageCount.AutoSize = true;
            this.chkCacheMessageCount.Location = new System.Drawing.Point(154, 19);
            this.chkCacheMessageCount.Name = "chkCacheMessageCount";
            this.chkCacheMessageCount.Size = new System.Drawing.Size(100, 17);
            this.chkCacheMessageCount.TabIndex = 3;
            this.chkCacheMessageCount.Text = "Message Count";
            this.chkCacheMessageCount.UseVisualStyleBackColor = true;
            // 
            // chkCacheSpecialUse
            // 
            this.chkCacheSpecialUse.AutoSize = true;
            this.chkCacheSpecialUse.Location = new System.Drawing.Point(12, 65);
            this.chkCacheSpecialUse.Name = "chkCacheSpecialUse";
            this.chkCacheSpecialUse.Size = new System.Drawing.Size(83, 17);
            this.chkCacheSpecialUse.TabIndex = 2;
            this.chkCacheSpecialUse.Text = "Special Use";
            this.chkCacheSpecialUse.UseVisualStyleBackColor = true;
            // 
            // chkCacheChildren
            // 
            this.chkCacheChildren.AutoSize = true;
            this.chkCacheChildren.Location = new System.Drawing.Point(12, 42);
            this.chkCacheChildren.Name = "chkCacheChildren";
            this.chkCacheChildren.Size = new System.Drawing.Size(64, 17);
            this.chkCacheChildren.TabIndex = 1;
            this.chkCacheChildren.Text = "Children";
            this.chkCacheChildren.UseVisualStyleBackColor = true;
            // 
            // gbxOther
            // 
            this.gbxOther.Controls.Add(this.chkMailboxReferrals);
            this.gbxOther.Location = new System.Drawing.Point(0, 325);
            this.gbxOther.Name = "gbxOther";
            this.gbxOther.Size = new System.Drawing.Size(342, 43);
            this.gbxOther.TabIndex = 3;
            this.gbxOther.TabStop = false;
            this.gbxOther.Text = "Other";
            // 
            // chkMailboxReferrals
            // 
            this.chkMailboxReferrals.AutoSize = true;
            this.chkMailboxReferrals.Location = new System.Drawing.Point(15, 19);
            this.chkMailboxReferrals.Name = "chkMailboxReferrals";
            this.chkMailboxReferrals.Size = new System.Drawing.Size(107, 17);
            this.chkMailboxReferrals.TabIndex = 0;
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
            this.gbxCapabilities.Controls.Add(this.chkIgnoreSort);
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
            this.gbxCapabilities.Location = new System.Drawing.Point(0, 0);
            this.gbxCapabilities.Name = "gbxCapabilities";
            this.gbxCapabilities.Size = new System.Drawing.Size(342, 321);
            this.gbxCapabilities.TabIndex = 2;
            this.gbxCapabilities.TabStop = false;
            this.gbxCapabilities.Text = "Ignore Capabilities";
            // 
            // chkIgnoreEnable
            // 
            this.chkIgnoreEnable.AutoSize = true;
            this.chkIgnoreEnable.Location = new System.Drawing.Point(15, 42);
            this.chkIgnoreEnable.Name = "chkIgnoreEnable";
            this.chkIgnoreEnable.Size = new System.Drawing.Size(59, 17);
            this.chkIgnoreEnable.TabIndex = 1;
            this.chkIgnoreEnable.Text = "Enable";
            this.chkIgnoreEnable.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreStartTLS
            // 
            this.chkIgnoreStartTLS.AutoSize = true;
            this.chkIgnoreStartTLS.Location = new System.Drawing.Point(15, 19);
            this.chkIgnoreStartTLS.Name = "chkIgnoreStartTLS";
            this.chkIgnoreStartTLS.Size = new System.Drawing.Size(71, 17);
            this.chkIgnoreStartTLS.TabIndex = 0;
            this.chkIgnoreStartTLS.Text = "Start TLS";
            this.chkIgnoreStartTLS.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreCondStore
            // 
            this.chkIgnoreCondStore.AutoSize = true;
            this.chkIgnoreCondStore.Location = new System.Drawing.Point(15, 272);
            this.chkIgnoreCondStore.Name = "chkIgnoreCondStore";
            this.chkIgnoreCondStore.Size = new System.Drawing.Size(76, 17);
            this.chkIgnoreCondStore.TabIndex = 9;
            this.chkIgnoreCondStore.Text = "CondStore";
            this.chkIgnoreCondStore.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreESort
            // 
            this.chkIgnoreESort.AutoSize = true;
            this.chkIgnoreESort.Location = new System.Drawing.Point(159, 249);
            this.chkIgnoreESort.Name = "chkIgnoreESort";
            this.chkIgnoreESort.Size = new System.Drawing.Size(52, 17);
            this.chkIgnoreESort.TabIndex = 20;
            this.chkIgnoreESort.Text = "ESort";
            this.chkIgnoreESort.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreThreadReferences
            // 
            this.chkIgnoreThreadReferences.AutoSize = true;
            this.chkIgnoreThreadReferences.Location = new System.Drawing.Point(159, 226);
            this.chkIgnoreThreadReferences.Name = "chkIgnoreThreadReferences";
            this.chkIgnoreThreadReferences.Size = new System.Drawing.Size(121, 17);
            this.chkIgnoreThreadReferences.TabIndex = 19;
            this.chkIgnoreThreadReferences.Text = "Thread=References";
            this.chkIgnoreThreadReferences.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreThreadOrderedSubject
            // 
            this.chkIgnoreThreadOrderedSubject.AutoSize = true;
            this.chkIgnoreThreadOrderedSubject.Location = new System.Drawing.Point(159, 203);
            this.chkIgnoreThreadOrderedSubject.Name = "chkIgnoreThreadOrderedSubject";
            this.chkIgnoreThreadOrderedSubject.Size = new System.Drawing.Size(140, 17);
            this.chkIgnoreThreadOrderedSubject.TabIndex = 18;
            this.chkIgnoreThreadOrderedSubject.Text = "Thread=OrderedSubject";
            this.chkIgnoreThreadOrderedSubject.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreSortDisplay
            // 
            this.chkIgnoreSortDisplay.AutoSize = true;
            this.chkIgnoreSortDisplay.Location = new System.Drawing.Point(159, 180);
            this.chkIgnoreSortDisplay.Name = "chkIgnoreSortDisplay";
            this.chkIgnoreSortDisplay.Size = new System.Drawing.Size(85, 17);
            this.chkIgnoreSortDisplay.TabIndex = 17;
            this.chkIgnoreSortDisplay.Text = "Sort=Display";
            this.chkIgnoreSortDisplay.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreSort
            // 
            this.chkIgnoreSort.AutoSize = true;
            this.chkIgnoreSort.Location = new System.Drawing.Point(159, 157);
            this.chkIgnoreSort.Name = "chkIgnoreSort";
            this.chkIgnoreSort.Size = new System.Drawing.Size(45, 17);
            this.chkIgnoreSort.TabIndex = 16;
            this.chkIgnoreSort.Text = "Sort";
            this.chkIgnoreSort.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreESearch
            // 
            this.chkIgnoreESearch.AutoSize = true;
            this.chkIgnoreESearch.Location = new System.Drawing.Point(159, 134);
            this.chkIgnoreESearch.Name = "chkIgnoreESearch";
            this.chkIgnoreESearch.Size = new System.Drawing.Size(67, 17);
            this.chkIgnoreESearch.TabIndex = 15;
            this.chkIgnoreESearch.Text = "ESearch";
            this.chkIgnoreESearch.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreMailboxReferrals
            // 
            this.chkIgnoreMailboxReferrals.AutoSize = true;
            this.chkIgnoreMailboxReferrals.Location = new System.Drawing.Point(15, 157);
            this.chkIgnoreMailboxReferrals.Name = "chkIgnoreMailboxReferrals";
            this.chkIgnoreMailboxReferrals.Size = new System.Drawing.Size(107, 17);
            this.chkIgnoreMailboxReferrals.TabIndex = 5;
            this.chkIgnoreMailboxReferrals.Text = "Mailbox Referrals";
            this.chkIgnoreMailboxReferrals.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreSpecialUse
            // 
            this.chkIgnoreSpecialUse.AutoSize = true;
            this.chkIgnoreSpecialUse.Location = new System.Drawing.Point(15, 226);
            this.chkIgnoreSpecialUse.Name = "chkIgnoreSpecialUse";
            this.chkIgnoreSpecialUse.Size = new System.Drawing.Size(83, 17);
            this.chkIgnoreSpecialUse.TabIndex = 8;
            this.chkIgnoreSpecialUse.Text = "Special Use";
            this.chkIgnoreSpecialUse.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreListExtended
            // 
            this.chkIgnoreListExtended.AutoSize = true;
            this.chkIgnoreListExtended.Location = new System.Drawing.Point(15, 180);
            this.chkIgnoreListExtended.Name = "chkIgnoreListExtended";
            this.chkIgnoreListExtended.Size = new System.Drawing.Size(90, 17);
            this.chkIgnoreListExtended.TabIndex = 6;
            this.chkIgnoreListExtended.Text = "List Extended";
            this.chkIgnoreListExtended.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreListStatus
            // 
            this.chkIgnoreListStatus.AutoSize = true;
            this.chkIgnoreListStatus.Location = new System.Drawing.Point(15, 203);
            this.chkIgnoreListStatus.Name = "chkIgnoreListStatus";
            this.chkIgnoreListStatus.Size = new System.Drawing.Size(75, 17);
            this.chkIgnoreListStatus.TabIndex = 7;
            this.chkIgnoreListStatus.Text = "List Status";
            this.chkIgnoreListStatus.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreSASLIR
            // 
            this.chkIgnoreSASLIR.AutoSize = true;
            this.chkIgnoreSASLIR.Location = new System.Drawing.Point(159, 88);
            this.chkIgnoreSASLIR.Name = "chkIgnoreSASLIR";
            this.chkIgnoreSASLIR.Size = new System.Drawing.Size(67, 17);
            this.chkIgnoreSASLIR.TabIndex = 14;
            this.chkIgnoreSASLIR.Text = "SASL-IR";
            this.chkIgnoreSASLIR.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreId
            // 
            this.chkIgnoreId.AutoSize = true;
            this.chkIgnoreId.Location = new System.Drawing.Point(15, 88);
            this.chkIgnoreId.Name = "chkIgnoreId";
            this.chkIgnoreId.Size = new System.Drawing.Size(35, 17);
            this.chkIgnoreId.TabIndex = 3;
            this.chkIgnoreId.Text = "Id";
            this.chkIgnoreId.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreIdle
            // 
            this.chkIgnoreIdle.AutoSize = true;
            this.chkIgnoreIdle.Location = new System.Drawing.Point(159, 65);
            this.chkIgnoreIdle.Name = "chkIgnoreIdle";
            this.chkIgnoreIdle.Size = new System.Drawing.Size(43, 17);
            this.chkIgnoreIdle.TabIndex = 13;
            this.chkIgnoreIdle.Text = "Idle";
            this.chkIgnoreIdle.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreLiteralPlus
            // 
            this.chkIgnoreLiteralPlus.AutoSize = true;
            this.chkIgnoreLiteralPlus.Location = new System.Drawing.Point(159, 19);
            this.chkIgnoreLiteralPlus.Name = "chkIgnoreLiteralPlus";
            this.chkIgnoreLiteralPlus.Size = new System.Drawing.Size(68, 17);
            this.chkIgnoreLiteralPlus.TabIndex = 11;
            this.chkIgnoreLiteralPlus.Text = "Literal+/-";
            this.chkIgnoreLiteralPlus.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreUTF8
            // 
            this.chkIgnoreUTF8.AutoSize = true;
            this.chkIgnoreUTF8.Location = new System.Drawing.Point(15, 65);
            this.chkIgnoreUTF8.Name = "chkIgnoreUTF8";
            this.chkIgnoreUTF8.Size = new System.Drawing.Size(53, 17);
            this.chkIgnoreUTF8.TabIndex = 2;
            this.chkIgnoreUTF8.Text = "UTF8";
            this.chkIgnoreUTF8.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreQResync
            // 
            this.chkIgnoreQResync.AutoSize = true;
            this.chkIgnoreQResync.Location = new System.Drawing.Point(15, 295);
            this.chkIgnoreQResync.Name = "chkIgnoreQResync";
            this.chkIgnoreQResync.Size = new System.Drawing.Size(70, 17);
            this.chkIgnoreQResync.TabIndex = 10;
            this.chkIgnoreQResync.Text = "QResync";
            this.chkIgnoreQResync.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreBinary
            // 
            this.chkIgnoreBinary.AutoSize = true;
            this.chkIgnoreBinary.Location = new System.Drawing.Point(159, 42);
            this.chkIgnoreBinary.Name = "chkIgnoreBinary";
            this.chkIgnoreBinary.Size = new System.Drawing.Size(55, 17);
            this.chkIgnoreBinary.TabIndex = 12;
            this.chkIgnoreBinary.Text = "Binary";
            this.chkIgnoreBinary.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreNamespace
            // 
            this.chkIgnoreNamespace.AutoSize = true;
            this.chkIgnoreNamespace.Location = new System.Drawing.Point(15, 111);
            this.chkIgnoreNamespace.Name = "chkIgnoreNamespace";
            this.chkIgnoreNamespace.Size = new System.Drawing.Size(83, 17);
            this.chkIgnoreNamespace.TabIndex = 4;
            this.chkIgnoreNamespace.Text = "Namespace";
            this.chkIgnoreNamespace.UseVisualStyleBackColor = true;
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
            this.gbxFetchBodyWrite.Location = new System.Drawing.Point(217, 239);
            this.gbxFetchBodyWrite.Name = "gbxFetchBodyWrite";
            this.gbxFetchBodyWrite.Size = new System.Drawing.Size(208, 142);
            this.gbxFetchBodyWrite.TabIndex = 4;
            this.gbxFetchBodyWrite.TabStop = false;
            this.gbxFetchBodyWrite.Text = "Fetch Body Write";
            this.gbxFetchBodyWrite.Validating += new System.ComponentModel.CancelEventHandler(this.gbxFetchBodyWrite_Validating);
            this.gbxFetchBodyWrite.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdFWSet
            // 
            this.cmdFWSet.Location = new System.Drawing.Point(15, 108);
            this.cmdFWSet.Name = "cmdFWSet";
            this.cmdFWSet.Size = new System.Drawing.Size(100, 25);
            this.cmdFWSet.TabIndex = 8;
            this.cmdFWSet.Text = "Set";
            this.cmdFWSet.UseVisualStyleBackColor = true;
            this.cmdFWSet.Click += new System.EventHandler(this.cmdFWSet_Click);
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(12, 85);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(31, 13);
            this.label18.TabIndex = 6;
            this.label18.Text = "Initial";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(12, 64);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(53, 13);
            this.label19.TabIndex = 4;
            this.label19.Text = "Max Time";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(12, 43);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(27, 13);
            this.label20.TabIndex = 2;
            this.label20.Text = "Max";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(12, 22);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(24, 13);
            this.label21.TabIndex = 0;
            this.label21.Text = "Min";
            // 
            // txtFWInitial
            // 
            this.txtFWInitial.Location = new System.Drawing.Point(133, 82);
            this.txtFWInitial.Name = "txtFWInitial";
            this.txtFWInitial.Size = new System.Drawing.Size(50, 20);
            this.txtFWInitial.TabIndex = 7;
            this.txtFWInitial.Text = "1000";
            this.txtFWInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFWInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFWMaxTime
            // 
            this.txtFWMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtFWMaxTime.Name = "txtFWMaxTime";
            this.txtFWMaxTime.Size = new System.Drawing.Size(50, 20);
            this.txtFWMaxTime.TabIndex = 5;
            this.txtFWMaxTime.Text = "1000";
            this.txtFWMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtFWMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFWMax
            // 
            this.txtFWMax.Location = new System.Drawing.Point(133, 40);
            this.txtFWMax.Name = "txtFWMax";
            this.txtFWMax.Size = new System.Drawing.Size(50, 20);
            this.txtFWMax.TabIndex = 3;
            this.txtFWMax.Text = "1000";
            this.txtFWMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFWMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFWMin
            // 
            this.txtFWMin.Location = new System.Drawing.Point(133, 19);
            this.txtFWMin.Name = "txtFWMin";
            this.txtFWMin.Size = new System.Drawing.Size(50, 20);
            this.txtFWMin.TabIndex = 1;
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
            this.gbxFetchBodyRead.Location = new System.Drawing.Point(3, 239);
            this.gbxFetchBodyRead.Name = "gbxFetchBodyRead";
            this.gbxFetchBodyRead.Size = new System.Drawing.Size(208, 142);
            this.gbxFetchBodyRead.TabIndex = 2;
            this.gbxFetchBodyRead.TabStop = false;
            this.gbxFetchBodyRead.Text = "Fetch Body Read";
            this.gbxFetchBodyRead.Validating += new System.ComponentModel.CancelEventHandler(this.gbxFetchBodyRead_Validating);
            this.gbxFetchBodyRead.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdFRSet
            // 
            this.cmdFRSet.Location = new System.Drawing.Point(15, 108);
            this.cmdFRSet.Name = "cmdFRSet";
            this.cmdFRSet.Size = new System.Drawing.Size(100, 25);
            this.cmdFRSet.TabIndex = 8;
            this.cmdFRSet.Text = "Set";
            this.cmdFRSet.UseVisualStyleBackColor = true;
            this.cmdFRSet.Click += new System.EventHandler(this.cmdFRSet_Click);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(12, 85);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(31, 13);
            this.label14.TabIndex = 6;
            this.label14.Text = "Initial";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(12, 64);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(53, 13);
            this.label15.TabIndex = 4;
            this.label15.Text = "Max Time";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(12, 43);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(27, 13);
            this.label16.TabIndex = 2;
            this.label16.Text = "Max";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(12, 22);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(24, 13);
            this.label17.TabIndex = 0;
            this.label17.Text = "Min";
            // 
            // txtFRInitial
            // 
            this.txtFRInitial.Location = new System.Drawing.Point(133, 82);
            this.txtFRInitial.Name = "txtFRInitial";
            this.txtFRInitial.Size = new System.Drawing.Size(50, 20);
            this.txtFRInitial.TabIndex = 7;
            this.txtFRInitial.Text = "1000";
            this.txtFRInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFRInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFRMaxTime
            // 
            this.txtFRMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtFRMaxTime.Name = "txtFRMaxTime";
            this.txtFRMaxTime.Size = new System.Drawing.Size(50, 20);
            this.txtFRMaxTime.TabIndex = 5;
            this.txtFRMaxTime.Text = "1000";
            this.txtFRMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtFRMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFRMax
            // 
            this.txtFRMax.Location = new System.Drawing.Point(133, 40);
            this.txtFRMax.Name = "txtFRMax";
            this.txtFRMax.Size = new System.Drawing.Size(50, 20);
            this.txtFRMax.TabIndex = 3;
            this.txtFRMax.Text = "1000";
            this.txtFRMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFRMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFRMin
            // 
            this.txtFRMin.Location = new System.Drawing.Point(133, 19);
            this.txtFRMin.Name = "txtFRMin";
            this.txtFRMin.Size = new System.Drawing.Size(50, 20);
            this.txtFRMin.TabIndex = 1;
            this.txtFRMin.Text = "1000";
            this.txtFRMin.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtFRMin.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxFetchCacheItems
            // 
            this.gbxFetchCacheItems.Controls.Add(this.cmdFASet);
            this.gbxFetchCacheItems.Controls.Add(this.label13);
            this.gbxFetchCacheItems.Controls.Add(this.label12);
            this.gbxFetchCacheItems.Controls.Add(this.label7);
            this.gbxFetchCacheItems.Controls.Add(this.label6);
            this.gbxFetchCacheItems.Controls.Add(this.txtFAInitial);
            this.gbxFetchCacheItems.Controls.Add(this.txtFAMaxTime);
            this.gbxFetchCacheItems.Controls.Add(this.txtFAMax);
            this.gbxFetchCacheItems.Controls.Add(this.txtFAMin);
            this.gbxFetchCacheItems.Location = new System.Drawing.Point(217, 91);
            this.gbxFetchCacheItems.Name = "gbxFetchCacheItems";
            this.gbxFetchCacheItems.Size = new System.Drawing.Size(208, 142);
            this.gbxFetchCacheItems.TabIndex = 3;
            this.gbxFetchCacheItems.TabStop = false;
            this.gbxFetchCacheItems.Text = "Fetch Cache Items";
            this.gbxFetchCacheItems.Validating += new System.ComponentModel.CancelEventHandler(this.gbxFetchCacheItems_Validating);
            this.gbxFetchCacheItems.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdFASet
            // 
            this.cmdFASet.Location = new System.Drawing.Point(15, 108);
            this.cmdFASet.Name = "cmdFASet";
            this.cmdFASet.Size = new System.Drawing.Size(100, 25);
            this.cmdFASet.TabIndex = 8;
            this.cmdFASet.Text = "Set";
            this.cmdFASet.UseVisualStyleBackColor = true;
            this.cmdFASet.Click += new System.EventHandler(this.cmdFASet_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(12, 85);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(31, 13);
            this.label13.TabIndex = 6;
            this.label13.Text = "Initial";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(12, 64);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(53, 13);
            this.label12.TabIndex = 4;
            this.label12.Text = "Max Time";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 43);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(27, 13);
            this.label7.TabIndex = 2;
            this.label7.Text = "Max";
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
            // txtFAInitial
            // 
            this.txtFAInitial.Location = new System.Drawing.Point(133, 82);
            this.txtFAInitial.Name = "txtFAInitial";
            this.txtFAInitial.Size = new System.Drawing.Size(50, 20);
            this.txtFAInitial.TabIndex = 7;
            this.txtFAInitial.Text = "1000";
            this.txtFAInitial.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtFAInitial.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFAMaxTime
            // 
            this.txtFAMaxTime.Location = new System.Drawing.Point(133, 61);
            this.txtFAMaxTime.Name = "txtFAMaxTime";
            this.txtFAMaxTime.Size = new System.Drawing.Size(50, 20);
            this.txtFAMaxTime.TabIndex = 5;
            this.txtFAMaxTime.Text = "1000";
            this.txtFAMaxTime.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtFAMaxTime.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFAMax
            // 
            this.txtFAMax.Location = new System.Drawing.Point(133, 40);
            this.txtFAMax.Name = "txtFAMax";
            this.txtFAMax.Size = new System.Drawing.Size(50, 20);
            this.txtFAMax.TabIndex = 3;
            this.txtFAMax.Text = "1000";
            this.txtFAMax.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtFAMax.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // txtFAMin
            // 
            this.txtFAMin.Location = new System.Drawing.Point(133, 19);
            this.txtFAMin.Name = "txtFAMin";
            this.txtFAMin.Size = new System.Drawing.Size(50, 20);
            this.txtFAMin.TabIndex = 1;
            this.txtFAMin.Text = "1000";
            this.txtFAMin.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtFAMin.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // gbxTimeout
            // 
            this.gbxTimeout.Controls.Add(this.cmdTimeoutSet);
            this.gbxTimeout.Controls.Add(this.txtTimeout);
            this.gbxTimeout.Location = new System.Drawing.Point(0, 150);
            this.gbxTimeout.Name = "gbxTimeout";
            this.gbxTimeout.Size = new System.Drawing.Size(206, 83);
            this.gbxTimeout.TabIndex = 1;
            this.gbxTimeout.TabStop = false;
            this.gbxTimeout.Text = "Timeout";
            // 
            // cmdTimeoutSet
            // 
            this.cmdTimeoutSet.Location = new System.Drawing.Point(12, 49);
            this.cmdTimeoutSet.Name = "cmdTimeoutSet";
            this.cmdTimeoutSet.Size = new System.Drawing.Size(100, 25);
            this.cmdTimeoutSet.TabIndex = 1;
            this.cmdTimeoutSet.Text = "Set";
            this.cmdTimeoutSet.UseVisualStyleBackColor = true;
            this.cmdTimeoutSet.Click += new System.EventHandler(this.cmdTimeoutSet_Click);
            // 
            // txtTimeout
            // 
            this.txtTimeout.Location = new System.Drawing.Point(12, 19);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(50, 20);
            this.txtTimeout.TabIndex = 0;
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
            this.gbxIdle.Location = new System.Drawing.Point(0, 0);
            this.gbxIdle.Name = "gbxIdle";
            this.gbxIdle.Size = new System.Drawing.Size(206, 142);
            this.gbxIdle.TabIndex = 0;
            this.gbxIdle.TabStop = false;
            this.gbxIdle.Text = "Idle";
            // 
            // cmdIdleSet
            // 
            this.cmdIdleSet.Location = new System.Drawing.Point(12, 108);
            this.cmdIdleSet.Name = "cmdIdleSet";
            this.cmdIdleSet.Size = new System.Drawing.Size(100, 25);
            this.cmdIdleSet.TabIndex = 7;
            this.cmdIdleSet.Text = "Set";
            this.cmdIdleSet.UseVisualStyleBackColor = true;
            this.cmdIdleSet.Click += new System.EventHandler(this.cmdIdleSet_Click);
            // 
            // txtIdleRestartInterval
            // 
            this.txtIdleRestartInterval.Location = new System.Drawing.Point(133, 61);
            this.txtIdleRestartInterval.Name = "txtIdleRestartInterval";
            this.txtIdleRestartInterval.Size = new System.Drawing.Size(50, 20);
            this.txtIdleRestartInterval.TabIndex = 4;
            this.txtIdleRestartInterval.Text = "1200000";
            this.txtIdleRestartInterval.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtIdleRestartInterval.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(9, 64);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(87, 13);
            this.label10.TabIndex = 3;
            this.label10.Text = "Idle Restart Time";
            // 
            // txtIdlePollInterval
            // 
            this.txtIdlePollInterval.Location = new System.Drawing.Point(133, 82);
            this.txtIdlePollInterval.Name = "txtIdlePollInterval";
            this.txtIdlePollInterval.Size = new System.Drawing.Size(50, 20);
            this.txtIdlePollInterval.TabIndex = 6;
            this.txtIdlePollInterval.Text = "60000";
            this.txtIdlePollInterval.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtIdlePollInterval.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 85);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(99, 13);
            this.label9.TabIndex = 5;
            this.label9.Text = "Poll Interval (NoOp)";
            // 
            // txtIdleStartDelay
            // 
            this.txtIdleStartDelay.Location = new System.Drawing.Point(133, 40);
            this.txtIdleStartDelay.Name = "txtIdleStartDelay";
            this.txtIdleStartDelay.Size = new System.Drawing.Size(50, 20);
            this.txtIdleStartDelay.TabIndex = 2;
            this.txtIdleStartDelay.Text = "1000";
            this.txtIdleStartDelay.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsMilliseconds);
            this.txtIdleStartDelay.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 43);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 13);
            this.label8.TabIndex = 1;
            this.label8.Text = "Start Delay";
            // 
            // chkIdleAuto
            // 
            this.chkIdleAuto.AutoSize = true;
            this.chkIdleAuto.Checked = true;
            this.chkIdleAuto.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIdleAuto.Location = new System.Drawing.Point(12, 19);
            this.chkIdleAuto.Name = "chkIdleAuto";
            this.chkIdleAuto.Size = new System.Drawing.Size(68, 17);
            this.chkIdleAuto.TabIndex = 0;
            this.chkIdleAuto.Text = "Auto Idle";
            this.chkIdleAuto.UseVisualStyleBackColor = true;
            this.chkIdleAuto.CheckedChanged += new System.EventHandler(this.chkIdleAuto_CheckedChanged);
            // 
            // cmdSubscriptions
            // 
            this.cmdSubscriptions.Location = new System.Drawing.Point(133, 19);
            this.cmdSubscriptions.Name = "cmdSubscriptions";
            this.cmdSubscriptions.Size = new System.Drawing.Size(100, 25);
            this.cmdSubscriptions.TabIndex = 1;
            this.cmdSubscriptions.Text = "Subscriptions";
            this.cmdSubscriptions.UseVisualStyleBackColor = true;
            this.cmdSubscriptions.Click += new System.EventHandler(this.cmdSubscriptions_Click);
            // 
            // cmdEvents
            // 
            this.cmdEvents.Location = new System.Drawing.Point(15, 46);
            this.cmdEvents.Name = "cmdEvents";
            this.cmdEvents.Size = new System.Drawing.Size(100, 25);
            this.cmdEvents.TabIndex = 2;
            this.cmdEvents.Text = "Events";
            this.cmdEvents.UseVisualStyleBackColor = true;
            this.cmdEvents.Click += new System.EventHandler(this.cmdEvents_Click);
            // 
            // cmdMailboxes
            // 
            this.cmdMailboxes.Location = new System.Drawing.Point(15, 19);
            this.cmdMailboxes.Name = "cmdMailboxes";
            this.cmdMailboxes.Size = new System.Drawing.Size(100, 25);
            this.cmdMailboxes.TabIndex = 0;
            this.cmdMailboxes.Text = "Mailboxes";
            this.cmdMailboxes.UseVisualStyleBackColor = true;
            this.cmdMailboxes.Click += new System.EventHandler(this.cmdMailboxes_Click);
            // 
            // cmdDetails
            // 
            this.cmdDetails.Location = new System.Drawing.Point(21, 16);
            this.cmdDetails.Name = "cmdDetails";
            this.cmdDetails.Size = new System.Drawing.Size(100, 25);
            this.cmdDetails.TabIndex = 0;
            this.cmdDetails.Text = "Details";
            this.cmdDetails.UseVisualStyleBackColor = true;
            this.cmdDetails.Click += new System.EventHandler(this.cmdDetails_Click);
            // 
            // cmdSelectedMailbox
            // 
            this.cmdSelectedMailbox.Location = new System.Drawing.Point(15, 92);
            this.cmdSelectedMailbox.Name = "cmdSelectedMailbox";
            this.cmdSelectedMailbox.Size = new System.Drawing.Size(100, 25);
            this.cmdSelectedMailbox.TabIndex = 9;
            this.cmdSelectedMailbox.Text = "Selected Mailbox";
            this.cmdSelectedMailbox.UseVisualStyleBackColor = true;
            this.cmdSelectedMailbox.Click += new System.EventHandler(this.cmdSelectedMailbox_Click);
            // 
            // cmdNetworkActivity
            // 
            this.cmdNetworkActivity.Location = new System.Drawing.Point(15, 46);
            this.cmdNetworkActivity.Name = "cmdNetworkActivity";
            this.cmdNetworkActivity.Size = new System.Drawing.Size(100, 25);
            this.cmdNetworkActivity.TabIndex = 2;
            this.cmdNetworkActivity.Text = "Network Activity";
            this.cmdNetworkActivity.UseVisualStyleBackColor = true;
            this.cmdNetworkActivity.Click += new System.EventHandler(this.cmdNetworkActivity_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Location = new System.Drawing.Point(383, 463);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(100, 25);
            this.cmdCancel.TabIndex = 3;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.Location = new System.Drawing.Point(380, 444);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(32, 13);
            this.lblState.TabIndex = 2;
            this.lblState.Text = "State";
            // 
            // cmdDisconnect
            // 
            this.cmdDisconnect.Location = new System.Drawing.Point(595, 463);
            this.cmdDisconnect.Name = "cmdDisconnect";
            this.cmdDisconnect.Size = new System.Drawing.Size(100, 25);
            this.cmdDisconnect.TabIndex = 5;
            this.cmdDisconnect.Text = "Disconnect";
            this.cmdDisconnect.UseVisualStyleBackColor = true;
            this.cmdDisconnect.Click += new System.EventHandler(this.cmdDisconnect_Click);
            // 
            // erp
            // 
            this.erp.ContainerControl = this;
            // 
            // tabConnect
            // 
            this.tabConnect.Controls.Add(this.tbpDetails);
            this.tabConnect.Controls.Add(this.tbpCapabilities);
            this.tabConnect.Location = new System.Drawing.Point(12, 19);
            this.tabConnect.Name = "tabConnect";
            this.tabConnect.SelectedIndex = 0;
            this.tabConnect.Size = new System.Drawing.Size(349, 438);
            this.tabConnect.TabIndex = 0;
            // 
            // tbpDetails
            // 
            this.tbpDetails.Controls.Add(this.gbxServer);
            this.tbpDetails.Controls.Add(this.gbxMailboxCacheData);
            this.tbpDetails.Controls.Add(this.gbxCredentials);
            this.tbpDetails.Location = new System.Drawing.Point(4, 22);
            this.tbpDetails.Name = "tbpDetails";
            this.tbpDetails.Padding = new System.Windows.Forms.Padding(3);
            this.tbpDetails.Size = new System.Drawing.Size(341, 412);
            this.tbpDetails.TabIndex = 0;
            this.tbpDetails.Text = "Details";
            this.tbpDetails.UseVisualStyleBackColor = true;
            // 
            // tbpCapabilities
            // 
            this.tbpCapabilities.Controls.Add(this.gbxCapabilities);
            this.tbpCapabilities.Controls.Add(this.gbxOther);
            this.tbpCapabilities.Location = new System.Drawing.Point(4, 22);
            this.tbpCapabilities.Name = "tbpCapabilities";
            this.tbpCapabilities.Padding = new System.Windows.Forms.Padding(3);
            this.tbpCapabilities.Size = new System.Drawing.Size(341, 412);
            this.tbpCapabilities.TabIndex = 1;
            this.tbpCapabilities.Text = "Capabilities";
            this.tbpCapabilities.UseVisualStyleBackColor = true;
            // 
            // gbxConnect
            // 
            this.gbxConnect.Controls.Add(this.tabConnect);
            this.gbxConnect.Controls.Add(this.cmdConnect);
            this.gbxConnect.Location = new System.Drawing.Point(0, 0);
            this.gbxConnect.Name = "gbxConnect";
            this.gbxConnect.Size = new System.Drawing.Size(374, 496);
            this.gbxConnect.TabIndex = 0;
            this.gbxConnect.TabStop = false;
            this.gbxConnect.Text = "Connect";
            // 
            // gbxSelectedMailbox
            // 
            this.gbxSelectedMailbox.Controls.Add(this.label25);
            this.gbxSelectedMailbox.Controls.Add(this.txtSMTextBytes);
            this.gbxSelectedMailbox.Controls.Add(this.chkProgressBar);
            this.gbxSelectedMailbox.Controls.Add(this.chkTrackUIDNext);
            this.gbxSelectedMailbox.Controls.Add(this.chkTrackUnseen);
            this.gbxSelectedMailbox.Controls.Add(this.txtSMMessages);
            this.gbxSelectedMailbox.Controls.Add(this.label5);
            this.gbxSelectedMailbox.Controls.Add(this.cmdSelectedMailbox);
            this.gbxSelectedMailbox.Location = new System.Drawing.Point(6, 143);
            this.gbxSelectedMailbox.Name = "gbxSelectedMailbox";
            this.gbxSelectedMailbox.Size = new System.Drawing.Size(433, 127);
            this.gbxSelectedMailbox.TabIndex = 3;
            this.gbxSelectedMailbox.TabStop = false;
            this.gbxSelectedMailbox.Text = "Selected Mailbox";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(12, 44);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(80, 13);
            this.label25.TabIndex = 2;
            this.label25.Text = "Max Text Bytes";
            // 
            // txtSMTextBytes
            // 
            this.txtSMTextBytes.Location = new System.Drawing.Point(133, 41);
            this.txtSMTextBytes.Name = "txtSMTextBytes";
            this.txtSMTextBytes.Size = new System.Drawing.Size(50, 20);
            this.txtSMTextBytes.TabIndex = 3;
            this.txtSMTextBytes.Text = "10000";
            this.txtSMTextBytes.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfBytes);
            this.txtSMTextBytes.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // chkProgressBar
            // 
            this.chkProgressBar.AutoSize = true;
            this.chkProgressBar.Checked = true;
            this.chkProgressBar.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkProgressBar.Location = new System.Drawing.Point(244, 69);
            this.chkProgressBar.Name = "chkProgressBar";
            this.chkProgressBar.Size = new System.Drawing.Size(86, 17);
            this.chkProgressBar.TabIndex = 8;
            this.chkProgressBar.Text = "Progress Bar";
            this.chkProgressBar.UseVisualStyleBackColor = true;
            // 
            // chkTrackUIDNext
            // 
            this.chkTrackUIDNext.AutoSize = true;
            this.chkTrackUIDNext.Location = new System.Drawing.Point(15, 69);
            this.chkTrackUIDNext.Name = "chkTrackUIDNext";
            this.chkTrackUIDNext.Size = new System.Drawing.Size(98, 17);
            this.chkTrackUIDNext.TabIndex = 6;
            this.chkTrackUIDNext.Text = "Track UIDNext";
            this.chkTrackUIDNext.UseVisualStyleBackColor = true;
            // 
            // chkTrackUnseen
            // 
            this.chkTrackUnseen.AutoSize = true;
            this.chkTrackUnseen.Location = new System.Drawing.Point(133, 69);
            this.chkTrackUnseen.Name = "chkTrackUnseen";
            this.chkTrackUnseen.Size = new System.Drawing.Size(94, 17);
            this.chkTrackUnseen.TabIndex = 7;
            this.chkTrackUnseen.Text = "Track Unseen";
            this.chkTrackUnseen.UseVisualStyleBackColor = true;
            // 
            // txtSMMessages
            // 
            this.txtSMMessages.Location = new System.Drawing.Point(133, 19);
            this.txtSMMessages.Name = "txtSMMessages";
            this.txtSMMessages.Size = new System.Drawing.Size(50, 20);
            this.txtSMMessages.TabIndex = 1;
            this.txtSMMessages.Text = "100";
            this.txtSMMessages.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtSMMessages.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(114, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Max Messages Shown";
            // 
            // tabClient
            // 
            this.tabClient.Controls.Add(this.tbpSettings);
            this.tabClient.Controls.Add(this.tbpDefaults);
            this.tabClient.Controls.Add(this.tbpWindows);
            this.tabClient.Controls.Add(this.tpgResponseText);
            this.tabClient.Location = new System.Drawing.Point(383, 10);
            this.tabClient.Name = "tabClient";
            this.tabClient.SelectedIndex = 0;
            this.tabClient.Size = new System.Drawing.Size(454, 431);
            this.tabClient.TabIndex = 1;
            // 
            // tbpSettings
            // 
            this.tbpSettings.Controls.Add(this.gbxIdle);
            this.tbpSettings.Controls.Add(this.gbxTimeout);
            this.tbpSettings.Controls.Add(this.gbxFetchBodyRead);
            this.tbpSettings.Controls.Add(this.gbxFetchBodyWrite);
            this.tbpSettings.Controls.Add(this.gbxFetchCacheItems);
            this.tbpSettings.Location = new System.Drawing.Point(4, 22);
            this.tbpSettings.Name = "tbpSettings";
            this.tbpSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tbpSettings.Size = new System.Drawing.Size(446, 386);
            this.tbpSettings.TabIndex = 0;
            this.tbpSettings.Text = "Settings";
            this.tbpSettings.UseVisualStyleBackColor = true;
            // 
            // tbpDefaults
            // 
            this.tbpDefaults.Controls.Add(this.tabControl1);
            this.tbpDefaults.Controls.Add(this.gbxDefaultSort);
            this.tbpDefaults.Location = new System.Drawing.Point(4, 22);
            this.tbpDefaults.Name = "tbpDefaults";
            this.tbpDefaults.Padding = new System.Windows.Forms.Padding(3);
            this.tbpDefaults.Size = new System.Drawing.Size(446, 405);
            this.tbpDefaults.TabIndex = 3;
            this.tbpDefaults.Text = "Defaults";
            this.tbpDefaults.UseVisualStyleBackColor = true;
            // 
            // gbxDefaultSort
            // 
            this.gbxDefaultSort.Controls.Add(this.cmdSort);
            this.gbxDefaultSort.Controls.Add(this.lblSort);
            this.gbxDefaultSort.Location = new System.Drawing.Point(6, 6);
            this.gbxDefaultSort.Name = "gbxDefaultSort";
            this.gbxDefaultSort.Size = new System.Drawing.Size(433, 76);
            this.gbxDefaultSort.TabIndex = 0;
            this.gbxDefaultSort.TabStop = false;
            this.gbxDefaultSort.Text = "Sort";
            // 
            // cmdSort
            // 
            this.cmdSort.Location = new System.Drawing.Point(12, 41);
            this.cmdSort.Name = "cmdSort";
            this.cmdSort.Size = new System.Drawing.Size(100, 25);
            this.cmdSort.TabIndex = 2;
            this.cmdSort.Text = "Set";
            this.cmdSort.UseVisualStyleBackColor = true;
            this.cmdSort.Click += new System.EventHandler(this.cmdSort_Click);
            // 
            // lblSort
            // 
            this.lblSort.AutoSize = true;
            this.lblSort.Location = new System.Drawing.Point(9, 20);
            this.lblSort.Name = "lblSort";
            this.lblSort.Size = new System.Drawing.Size(90, 13);
            this.lblSort.TabIndex = 0;
            this.lblSort.Text = "<sort description>";
            // 
            // tbpWindows
            // 
            this.tbpWindows.Controls.Add(this.gbxMailboxes);
            this.tbpWindows.Controls.Add(this.gbxEvents);
            this.tbpWindows.Controls.Add(this.gbxNetworkActivity);
            this.tbpWindows.Controls.Add(this.cmdDetails);
            this.tbpWindows.Controls.Add(this.gbxSelectedMailbox);
            this.tbpWindows.Location = new System.Drawing.Point(4, 22);
            this.tbpWindows.Name = "tbpWindows";
            this.tbpWindows.Padding = new System.Windows.Forms.Padding(3);
            this.tbpWindows.Size = new System.Drawing.Size(446, 386);
            this.tbpWindows.TabIndex = 1;
            this.tbpWindows.Text = "Windows";
            this.tbpWindows.UseVisualStyleBackColor = true;
            // 
            // gbxMailboxes
            // 
            this.gbxMailboxes.Controls.Add(this.chkMStatus);
            this.gbxMailboxes.Controls.Add(this.chkMLSub);
            this.gbxMailboxes.Controls.Add(this.chkMList);
            this.gbxMailboxes.Controls.Add(this.cmdMailboxes);
            this.gbxMailboxes.Controls.Add(this.cmdSubscriptions);
            this.gbxMailboxes.Location = new System.Drawing.Point(6, 307);
            this.gbxMailboxes.Name = "gbxMailboxes";
            this.gbxMailboxes.Size = new System.Drawing.Size(249, 73);
            this.gbxMailboxes.TabIndex = 4;
            this.gbxMailboxes.TabStop = false;
            this.gbxMailboxes.Text = "Mailboxes";
            // 
            // chkMStatus
            // 
            this.chkMStatus.AutoSize = true;
            this.chkMStatus.Location = new System.Drawing.Point(177, 50);
            this.chkMStatus.Name = "chkMStatus";
            this.chkMStatus.Size = new System.Drawing.Size(56, 17);
            this.chkMStatus.TabIndex = 4;
            this.chkMStatus.Text = "Status";
            this.chkMStatus.UseVisualStyleBackColor = true;
            // 
            // chkMLSub
            // 
            this.chkMLSub.AutoSize = true;
            this.chkMLSub.Location = new System.Drawing.Point(97, 50);
            this.chkMLSub.Name = "chkMLSub";
            this.chkMLSub.Size = new System.Drawing.Size(51, 17);
            this.chkMLSub.TabIndex = 3;
            this.chkMLSub.Text = "LSub";
            this.chkMLSub.UseVisualStyleBackColor = true;
            // 
            // chkMList
            // 
            this.chkMList.AutoSize = true;
            this.chkMList.Location = new System.Drawing.Point(15, 50);
            this.chkMList.Name = "chkMList";
            this.chkMList.Size = new System.Drawing.Size(42, 17);
            this.chkMList.TabIndex = 2;
            this.chkMList.Text = "List";
            this.chkMList.UseVisualStyleBackColor = true;
            // 
            // gbxEvents
            // 
            this.gbxEvents.Controls.Add(this.txtEvents);
            this.gbxEvents.Controls.Add(this.label24);
            this.gbxEvents.Controls.Add(this.cmdEvents);
            this.gbxEvents.Location = new System.Drawing.Point(235, 56);
            this.gbxEvents.Name = "gbxEvents";
            this.gbxEvents.Size = new System.Drawing.Size(205, 77);
            this.gbxEvents.TabIndex = 2;
            this.gbxEvents.TabStop = false;
            this.gbxEvents.Text = "Events";
            // 
            // txtEvents
            // 
            this.txtEvents.Location = new System.Drawing.Point(133, 19);
            this.txtEvents.Name = "txtEvents";
            this.txtEvents.Size = new System.Drawing.Size(50, 20);
            this.txtEvents.TabIndex = 1;
            this.txtEvents.Text = "100";
            this.txtEvents.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtEvents.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(12, 22);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(114, 13);
            this.label24.TabIndex = 0;
            this.label24.Text = "Max Messages Shown";
            // 
            // gbxNetworkActivity
            // 
            this.gbxNetworkActivity.Controls.Add(this.txtNetworkActivity);
            this.gbxNetworkActivity.Controls.Add(this.label22);
            this.gbxNetworkActivity.Controls.Add(this.cmdNetworkActivity);
            this.gbxNetworkActivity.Location = new System.Drawing.Point(6, 56);
            this.gbxNetworkActivity.Name = "gbxNetworkActivity";
            this.gbxNetworkActivity.Size = new System.Drawing.Size(205, 77);
            this.gbxNetworkActivity.TabIndex = 1;
            this.gbxNetworkActivity.TabStop = false;
            this.gbxNetworkActivity.Text = "Network Activity";
            // 
            // txtNetworkActivity
            // 
            this.txtNetworkActivity.Location = new System.Drawing.Point(133, 19);
            this.txtNetworkActivity.Name = "txtNetworkActivity";
            this.txtNetworkActivity.Size = new System.Drawing.Size(50, 20);
            this.txtNetworkActivity.TabIndex = 1;
            this.txtNetworkActivity.Text = "100";
            this.txtNetworkActivity.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtNetworkActivity.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(12, 22);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(114, 13);
            this.label22.TabIndex = 0;
            this.label22.Text = "Max Messages Shown";
            // 
            // tpgResponseText
            // 
            this.tpgResponseText.Controls.Add(this.gbxResponseTextCode);
            this.tpgResponseText.Controls.Add(this.gbxResponseTextType);
            this.tpgResponseText.Controls.Add(this.label23);
            this.tpgResponseText.Controls.Add(this.txtResponseText);
            this.tpgResponseText.Controls.Add(this.cmdResponseText);
            this.tpgResponseText.Location = new System.Drawing.Point(4, 22);
            this.tpgResponseText.Name = "tpgResponseText";
            this.tpgResponseText.Padding = new System.Windows.Forms.Padding(3);
            this.tpgResponseText.Size = new System.Drawing.Size(446, 386);
            this.tpgResponseText.TabIndex = 2;
            this.tpgResponseText.Text = "Response Text Window";
            this.tpgResponseText.UseVisualStyleBackColor = true;
            // 
            // gbxResponseTextCode
            // 
            this.gbxResponseTextCode.Controls.Add(this.chkRTCUnknownCTE);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCUseAttr);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCReferral);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCRFC5530);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCTryCreate);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCParse);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCBadCharset);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCAlert);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCUnknown);
            this.gbxResponseTextCode.Controls.Add(this.chkRTCNone);
            this.gbxResponseTextCode.Location = new System.Drawing.Point(9, 168);
            this.gbxResponseTextCode.Name = "gbxResponseTextCode";
            this.gbxResponseTextCode.Size = new System.Drawing.Size(430, 111);
            this.gbxResponseTextCode.TabIndex = 3;
            this.gbxResponseTextCode.TabStop = false;
            this.gbxResponseTextCode.Text = "Text Code";
            // 
            // chkRTCUnknownCTE
            // 
            this.chkRTCUnknownCTE.AutoSize = true;
            this.chkRTCUnknownCTE.Checked = true;
            this.chkRTCUnknownCTE.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCUnknownCTE.Location = new System.Drawing.Point(232, 88);
            this.chkRTCUnknownCTE.Name = "chkRTCUnknownCTE";
            this.chkRTCUnknownCTE.Size = new System.Drawing.Size(96, 17);
            this.chkRTCUnknownCTE.TabIndex = 9;
            this.chkRTCUnknownCTE.Text = "Unknown CTE";
            this.chkRTCUnknownCTE.UseVisualStyleBackColor = true;
            // 
            // chkRTCUseAttr
            // 
            this.chkRTCUseAttr.AutoSize = true;
            this.chkRTCUseAttr.Checked = true;
            this.chkRTCUseAttr.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCUseAttr.Location = new System.Drawing.Point(122, 88);
            this.chkRTCUseAttr.Name = "chkRTCUseAttr";
            this.chkRTCUseAttr.Size = new System.Drawing.Size(64, 17);
            this.chkRTCUseAttr.TabIndex = 8;
            this.chkRTCUseAttr.Text = "Use Attr";
            this.chkRTCUseAttr.UseVisualStyleBackColor = true;
            // 
            // chkRTCReferral
            // 
            this.chkRTCReferral.AutoSize = true;
            this.chkRTCReferral.Checked = true;
            this.chkRTCReferral.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCReferral.Location = new System.Drawing.Point(12, 88);
            this.chkRTCReferral.Name = "chkRTCReferral";
            this.chkRTCReferral.Size = new System.Drawing.Size(63, 17);
            this.chkRTCReferral.TabIndex = 7;
            this.chkRTCReferral.Text = "Referral";
            this.chkRTCReferral.UseVisualStyleBackColor = true;
            // 
            // chkRTCRFC5530
            // 
            this.chkRTCRFC5530.AutoSize = true;
            this.chkRTCRFC5530.Checked = true;
            this.chkRTCRFC5530.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCRFC5530.Location = new System.Drawing.Point(12, 65);
            this.chkRTCRFC5530.Name = "chkRTCRFC5530";
            this.chkRTCRFC5530.Size = new System.Drawing.Size(74, 17);
            this.chkRTCRFC5530.TabIndex = 6;
            this.chkRTCRFC5530.Text = "RFC 5530";
            this.chkRTCRFC5530.UseVisualStyleBackColor = true;
            // 
            // chkRTCTryCreate
            // 
            this.chkRTCTryCreate.AutoSize = true;
            this.chkRTCTryCreate.Checked = true;
            this.chkRTCTryCreate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCTryCreate.Location = new System.Drawing.Point(342, 42);
            this.chkRTCTryCreate.Name = "chkRTCTryCreate";
            this.chkRTCTryCreate.Size = new System.Drawing.Size(75, 17);
            this.chkRTCTryCreate.TabIndex = 5;
            this.chkRTCTryCreate.Text = "Try Create";
            this.chkRTCTryCreate.UseVisualStyleBackColor = true;
            // 
            // chkRTCParse
            // 
            this.chkRTCParse.AutoSize = true;
            this.chkRTCParse.Checked = true;
            this.chkRTCParse.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCParse.Location = new System.Drawing.Point(232, 42);
            this.chkRTCParse.Name = "chkRTCParse";
            this.chkRTCParse.Size = new System.Drawing.Size(53, 17);
            this.chkRTCParse.TabIndex = 4;
            this.chkRTCParse.Text = "Parse";
            this.chkRTCParse.UseVisualStyleBackColor = true;
            // 
            // chkRTCBadCharset
            // 
            this.chkRTCBadCharset.AutoSize = true;
            this.chkRTCBadCharset.Checked = true;
            this.chkRTCBadCharset.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCBadCharset.Location = new System.Drawing.Point(122, 42);
            this.chkRTCBadCharset.Name = "chkRTCBadCharset";
            this.chkRTCBadCharset.Size = new System.Drawing.Size(84, 17);
            this.chkRTCBadCharset.TabIndex = 3;
            this.chkRTCBadCharset.Text = "Bad Charset";
            this.chkRTCBadCharset.UseVisualStyleBackColor = true;
            // 
            // chkRTCAlert
            // 
            this.chkRTCAlert.AutoSize = true;
            this.chkRTCAlert.Checked = true;
            this.chkRTCAlert.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCAlert.Location = new System.Drawing.Point(12, 42);
            this.chkRTCAlert.Name = "chkRTCAlert";
            this.chkRTCAlert.Size = new System.Drawing.Size(47, 17);
            this.chkRTCAlert.TabIndex = 2;
            this.chkRTCAlert.Text = "Alert";
            this.chkRTCAlert.UseVisualStyleBackColor = true;
            // 
            // chkRTCUnknown
            // 
            this.chkRTCUnknown.AutoSize = true;
            this.chkRTCUnknown.Checked = true;
            this.chkRTCUnknown.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTCUnknown.Location = new System.Drawing.Point(122, 19);
            this.chkRTCUnknown.Name = "chkRTCUnknown";
            this.chkRTCUnknown.Size = new System.Drawing.Size(72, 17);
            this.chkRTCUnknown.TabIndex = 1;
            this.chkRTCUnknown.Text = "Unknown";
            this.chkRTCUnknown.UseVisualStyleBackColor = true;
            // 
            // chkRTCNone
            // 
            this.chkRTCNone.AutoSize = true;
            this.chkRTCNone.Location = new System.Drawing.Point(12, 19);
            this.chkRTCNone.Name = "chkRTCNone";
            this.chkRTCNone.Size = new System.Drawing.Size(52, 17);
            this.chkRTCNone.TabIndex = 0;
            this.chkRTCNone.Text = "None";
            this.chkRTCNone.UseVisualStyleBackColor = true;
            // 
            // gbxResponseTextType
            // 
            this.gbxResponseTextType.Controls.Add(this.chkRTTContinue);
            this.gbxResponseTextType.Controls.Add(this.chkRTTProtocolError);
            this.gbxResponseTextType.Controls.Add(this.chkRTTAuthenticationCancelled);
            this.gbxResponseTextType.Controls.Add(this.chkRTTFailure);
            this.gbxResponseTextType.Controls.Add(this.chkRTTSuccess);
            this.gbxResponseTextType.Controls.Add(this.chkRTTError);
            this.gbxResponseTextType.Controls.Add(this.chkRTTWarning);
            this.gbxResponseTextType.Controls.Add(this.chkRTTInformation);
            this.gbxResponseTextType.Controls.Add(this.chkRTTBye);
            this.gbxResponseTextType.Controls.Add(this.chkRTTGreeting);
            this.gbxResponseTextType.Location = new System.Drawing.Point(9, 45);
            this.gbxResponseTextType.Name = "gbxResponseTextType";
            this.gbxResponseTextType.Size = new System.Drawing.Size(328, 115);
            this.gbxResponseTextType.TabIndex = 2;
            this.gbxResponseTextType.TabStop = false;
            this.gbxResponseTextType.Text = "Text Type";
            // 
            // chkRTTContinue
            // 
            this.chkRTTContinue.AutoSize = true;
            this.chkRTTContinue.Location = new System.Drawing.Point(122, 22);
            this.chkRTTContinue.Name = "chkRTTContinue";
            this.chkRTTContinue.Size = new System.Drawing.Size(68, 17);
            this.chkRTTContinue.TabIndex = 1;
            this.chkRTTContinue.Text = "Continue";
            this.chkRTTContinue.UseVisualStyleBackColor = true;
            // 
            // chkRTTProtocolError
            // 
            this.chkRTTProtocolError.AutoSize = true;
            this.chkRTTProtocolError.Checked = true;
            this.chkRTTProtocolError.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTTProtocolError.Location = new System.Drawing.Point(232, 68);
            this.chkRTTProtocolError.Name = "chkRTTProtocolError";
            this.chkRTTProtocolError.Size = new System.Drawing.Size(90, 17);
            this.chkRTTProtocolError.TabIndex = 8;
            this.chkRTTProtocolError.Text = "Protocol Error";
            this.chkRTTProtocolError.UseVisualStyleBackColor = true;
            // 
            // chkRTTAuthenticationCancelled
            // 
            this.chkRTTAuthenticationCancelled.AutoSize = true;
            this.chkRTTAuthenticationCancelled.Checked = true;
            this.chkRTTAuthenticationCancelled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTTAuthenticationCancelled.Location = new System.Drawing.Point(122, 91);
            this.chkRTTAuthenticationCancelled.Name = "chkRTTAuthenticationCancelled";
            this.chkRTTAuthenticationCancelled.Size = new System.Drawing.Size(144, 17);
            this.chkRTTAuthenticationCancelled.TabIndex = 9;
            this.chkRTTAuthenticationCancelled.Text = "Authentication Cancelled";
            this.chkRTTAuthenticationCancelled.UseVisualStyleBackColor = true;
            // 
            // chkRTTFailure
            // 
            this.chkRTTFailure.AutoSize = true;
            this.chkRTTFailure.Checked = true;
            this.chkRTTFailure.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTTFailure.Location = new System.Drawing.Point(122, 68);
            this.chkRTTFailure.Name = "chkRTTFailure";
            this.chkRTTFailure.Size = new System.Drawing.Size(57, 17);
            this.chkRTTFailure.TabIndex = 7;
            this.chkRTTFailure.Text = "Failure";
            this.chkRTTFailure.UseVisualStyleBackColor = true;
            // 
            // chkRTTSuccess
            // 
            this.chkRTTSuccess.AutoSize = true;
            this.chkRTTSuccess.Location = new System.Drawing.Point(12, 68);
            this.chkRTTSuccess.Name = "chkRTTSuccess";
            this.chkRTTSuccess.Size = new System.Drawing.Size(67, 17);
            this.chkRTTSuccess.TabIndex = 6;
            this.chkRTTSuccess.Text = "Success";
            this.chkRTTSuccess.UseVisualStyleBackColor = true;
            // 
            // chkRTTError
            // 
            this.chkRTTError.AutoSize = true;
            this.chkRTTError.Checked = true;
            this.chkRTTError.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTTError.Location = new System.Drawing.Point(232, 45);
            this.chkRTTError.Name = "chkRTTError";
            this.chkRTTError.Size = new System.Drawing.Size(48, 17);
            this.chkRTTError.TabIndex = 5;
            this.chkRTTError.Text = "Error";
            this.chkRTTError.UseVisualStyleBackColor = true;
            // 
            // chkRTTWarning
            // 
            this.chkRTTWarning.AutoSize = true;
            this.chkRTTWarning.Checked = true;
            this.chkRTTWarning.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTTWarning.Location = new System.Drawing.Point(122, 45);
            this.chkRTTWarning.Name = "chkRTTWarning";
            this.chkRTTWarning.Size = new System.Drawing.Size(66, 17);
            this.chkRTTWarning.TabIndex = 4;
            this.chkRTTWarning.Text = "Warning";
            this.chkRTTWarning.UseVisualStyleBackColor = true;
            // 
            // chkRTTInformation
            // 
            this.chkRTTInformation.AutoSize = true;
            this.chkRTTInformation.Location = new System.Drawing.Point(12, 45);
            this.chkRTTInformation.Name = "chkRTTInformation";
            this.chkRTTInformation.Size = new System.Drawing.Size(78, 17);
            this.chkRTTInformation.TabIndex = 3;
            this.chkRTTInformation.Text = "Information";
            this.chkRTTInformation.UseVisualStyleBackColor = true;
            // 
            // chkRTTBye
            // 
            this.chkRTTBye.AutoSize = true;
            this.chkRTTBye.Checked = true;
            this.chkRTTBye.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTTBye.Location = new System.Drawing.Point(232, 22);
            this.chkRTTBye.Name = "chkRTTBye";
            this.chkRTTBye.Size = new System.Drawing.Size(44, 17);
            this.chkRTTBye.TabIndex = 2;
            this.chkRTTBye.Text = "Bye";
            this.chkRTTBye.UseVisualStyleBackColor = true;
            // 
            // chkRTTGreeting
            // 
            this.chkRTTGreeting.AutoSize = true;
            this.chkRTTGreeting.Checked = true;
            this.chkRTTGreeting.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRTTGreeting.Location = new System.Drawing.Point(12, 22);
            this.chkRTTGreeting.Name = "chkRTTGreeting";
            this.chkRTTGreeting.Size = new System.Drawing.Size(66, 17);
            this.chkRTTGreeting.TabIndex = 0;
            this.chkRTTGreeting.Text = "Greeting";
            this.chkRTTGreeting.UseVisualStyleBackColor = true;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(6, 22);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(114, 13);
            this.label23.TabIndex = 0;
            this.label23.Text = "Max Messages Shown";
            // 
            // txtResponseText
            // 
            this.txtResponseText.Location = new System.Drawing.Point(126, 19);
            this.txtResponseText.Name = "txtResponseText";
            this.txtResponseText.Size = new System.Drawing.Size(50, 20);
            this.txtResponseText.TabIndex = 1;
            this.txtResponseText.Text = "100";
            this.txtResponseText.Validating += new System.ComponentModel.CancelEventHandler(this.ZValTextBoxIsNumberOfMessages);
            this.txtResponseText.Validated += new System.EventHandler(this.ZValControlValidated);
            // 
            // cmdResponseText
            // 
            this.cmdResponseText.Location = new System.Drawing.Point(9, 295);
            this.cmdResponseText.Name = "cmdResponseText";
            this.cmdResponseText.Size = new System.Drawing.Size(100, 25);
            this.cmdResponseText.TabIndex = 4;
            this.cmdResponseText.Text = "Response text";
            this.cmdResponseText.UseVisualStyleBackColor = true;
            this.cmdResponseText.Click += new System.EventHandler(this.cmdResponseText_Click);
            // 
            // cmdPoll
            // 
            this.cmdPoll.Location = new System.Drawing.Point(489, 463);
            this.cmdPoll.Name = "cmdPoll";
            this.cmdPoll.Size = new System.Drawing.Size(100, 25);
            this.cmdPoll.TabIndex = 4;
            this.cmdPoll.Text = "Poll";
            this.cmdPoll.UseVisualStyleBackColor = true;
            this.cmdPoll.Click += new System.EventHandler(this.cmdPoll_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(6, 88);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(433, 311);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.cmdAHSet);
            this.tabPage1.Controls.Add(this.chkAHBodyStructure);
            this.tabPage1.Controls.Add(this.txtAHHeaderFieldNames);
            this.tabPage1.Controls.Add(this.label26);
            this.tabPage1.Controls.Add(this.chkAHModSeq);
            this.tabPage1.Controls.Add(this.chkAHUID);
            this.tabPage1.Controls.Add(this.chkAHSize);
            this.tabPage1.Controls.Add(this.chkAHReceived);
            this.tabPage1.Controls.Add(this.chkAHFlags);
            this.tabPage1.Controls.Add(this.chkAHEnvelope);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(425, 285);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Cache Items by Attribute & Header";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.cmdPSet);
            this.tabPage2.Controls.Add(this.label28);
            this.tabPage2.Controls.Add(this.label27);
            this.tabPage2.Controls.Add(this.chkPReferences);
            this.tabPage2.Controls.Add(this.chkPPlainTextSize);
            this.tabPage2.Controls.Add(this.chkPImportance);
            this.tabPage2.Controls.Add(this.chkPAttachments);
            this.tabPage2.Controls.Add(this.chkPBodyStructure);
            this.tabPage2.Controls.Add(this.chkPModSeq);
            this.tabPage2.Controls.Add(this.chkPUID);
            this.tabPage2.Controls.Add(this.chkPSize);
            this.tabPage2.Controls.Add(this.chkPReceived);
            this.tabPage2.Controls.Add(this.chkPIsSubmitted);
            this.tabPage2.Controls.Add(this.chkPIsFlagged);
            this.tabPage2.Controls.Add(this.chkPIsAnswered);
            this.tabPage2.Controls.Add(this.chkPFlags);
            this.tabPage2.Controls.Add(this.chkPMessageId);
            this.tabPage2.Controls.Add(this.chkPSubject);
            this.tabPage2.Controls.Add(this.chkPSent);
            this.tabPage2.Controls.Add(this.chkPEnvelope);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(425, 285);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Cache Items by Property";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // cmdAHSet
            // 
            this.cmdAHSet.Location = new System.Drawing.Point(8, 254);
            this.cmdAHSet.Name = "cmdAHSet";
            this.cmdAHSet.Size = new System.Drawing.Size(100, 25);
            this.cmdAHSet.TabIndex = 9;
            this.cmdAHSet.Text = "Set";
            this.cmdAHSet.UseVisualStyleBackColor = true;
            this.cmdAHSet.Click += new System.EventHandler(this.cmdAHSet_Click);
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(5, 172);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(224, 13);
            this.label26.TabIndex = 7;
            this.label26.Text = "Header field names (separate with space or \':\')";
            // 
            // txtAHHeaderFieldNames
            // 
            this.txtAHHeaderFieldNames.Location = new System.Drawing.Point(8, 191);
            this.txtAHHeaderFieldNames.Name = "txtAHHeaderFieldNames";
            this.txtAHHeaderFieldNames.Size = new System.Drawing.Size(395, 20);
            this.txtAHHeaderFieldNames.TabIndex = 8;
            // 
            // chkAHBodyStructure
            // 
            this.chkAHBodyStructure.AutoSize = true;
            this.chkAHBodyStructure.Location = new System.Drawing.Point(8, 144);
            this.chkAHBodyStructure.Name = "chkAHBodyStructure";
            this.chkAHBodyStructure.Size = new System.Drawing.Size(93, 17);
            this.chkAHBodyStructure.TabIndex = 6;
            this.chkAHBodyStructure.Text = "BodyStructure";
            this.chkAHBodyStructure.UseVisualStyleBackColor = true;
            // 
            // chkAHModSeq
            // 
            this.chkAHModSeq.AutoSize = true;
            this.chkAHModSeq.Location = new System.Drawing.Point(8, 121);
            this.chkAHModSeq.Name = "chkAHModSeq";
            this.chkAHModSeq.Size = new System.Drawing.Size(66, 17);
            this.chkAHModSeq.TabIndex = 5;
            this.chkAHModSeq.Text = "ModSeq";
            this.chkAHModSeq.UseVisualStyleBackColor = true;
            // 
            // chkAHUID
            // 
            this.chkAHUID.AutoSize = true;
            this.chkAHUID.Location = new System.Drawing.Point(8, 98);
            this.chkAHUID.Name = "chkAHUID";
            this.chkAHUID.Size = new System.Drawing.Size(45, 17);
            this.chkAHUID.TabIndex = 4;
            this.chkAHUID.Text = "UID";
            this.chkAHUID.UseVisualStyleBackColor = true;
            // 
            // chkAHSize
            // 
            this.chkAHSize.AutoSize = true;
            this.chkAHSize.Location = new System.Drawing.Point(8, 75);
            this.chkAHSize.Name = "chkAHSize";
            this.chkAHSize.Size = new System.Drawing.Size(46, 17);
            this.chkAHSize.TabIndex = 3;
            this.chkAHSize.Text = "Size";
            this.chkAHSize.UseVisualStyleBackColor = true;
            // 
            // chkAHReceived
            // 
            this.chkAHReceived.AutoSize = true;
            this.chkAHReceived.Location = new System.Drawing.Point(8, 52);
            this.chkAHReceived.Name = "chkAHReceived";
            this.chkAHReceived.Size = new System.Drawing.Size(72, 17);
            this.chkAHReceived.TabIndex = 2;
            this.chkAHReceived.Text = "Received";
            this.chkAHReceived.UseVisualStyleBackColor = true;
            // 
            // chkAHFlags
            // 
            this.chkAHFlags.AutoSize = true;
            this.chkAHFlags.Location = new System.Drawing.Point(8, 29);
            this.chkAHFlags.Name = "chkAHFlags";
            this.chkAHFlags.Size = new System.Drawing.Size(51, 17);
            this.chkAHFlags.TabIndex = 1;
            this.chkAHFlags.Text = "Flags";
            this.chkAHFlags.UseVisualStyleBackColor = true;
            // 
            // chkAHEnvelope
            // 
            this.chkAHEnvelope.AutoSize = true;
            this.chkAHEnvelope.Location = new System.Drawing.Point(8, 6);
            this.chkAHEnvelope.Name = "chkAHEnvelope";
            this.chkAHEnvelope.Size = new System.Drawing.Size(71, 17);
            this.chkAHEnvelope.TabIndex = 0;
            this.chkAHEnvelope.Text = "Envelope";
            this.chkAHEnvelope.UseVisualStyleBackColor = true;
            // 
            // chkPEnvelope
            // 
            this.chkPEnvelope.AutoSize = true;
            this.chkPEnvelope.Location = new System.Drawing.Point(8, 6);
            this.chkPEnvelope.Name = "chkPEnvelope";
            this.chkPEnvelope.Size = new System.Drawing.Size(71, 17);
            this.chkPEnvelope.TabIndex = 0;
            this.chkPEnvelope.Text = "Envelope";
            this.chkPEnvelope.UseVisualStyleBackColor = true;
            // 
            // chkPSent
            // 
            this.chkPSent.AutoSize = true;
            this.chkPSent.Location = new System.Drawing.Point(115, 6);
            this.chkPSent.Name = "chkPSent";
            this.chkPSent.Size = new System.Drawing.Size(48, 17);
            this.chkPSent.TabIndex = 1;
            this.chkPSent.Text = "Sent";
            this.chkPSent.UseVisualStyleBackColor = true;
            // 
            // chkPSubject
            // 
            this.chkPSubject.AutoSize = true;
            this.chkPSubject.Location = new System.Drawing.Point(204, 6);
            this.chkPSubject.Name = "chkPSubject";
            this.chkPSubject.Size = new System.Drawing.Size(62, 17);
            this.chkPSubject.TabIndex = 2;
            this.chkPSubject.Text = "Subject";
            this.chkPSubject.UseVisualStyleBackColor = true;
            // 
            // chkPMessageId
            // 
            this.chkPMessageId.AutoSize = true;
            this.chkPMessageId.Location = new System.Drawing.Point(325, 6);
            this.chkPMessageId.Name = "chkPMessageId";
            this.chkPMessageId.Size = new System.Drawing.Size(81, 17);
            this.chkPMessageId.TabIndex = 4;
            this.chkPMessageId.Text = "Message Id";
            this.chkPMessageId.UseVisualStyleBackColor = true;
            // 
            // chkPFlags
            // 
            this.chkPFlags.AutoSize = true;
            this.chkPFlags.Location = new System.Drawing.Point(8, 29);
            this.chkPFlags.Name = "chkPFlags";
            this.chkPFlags.Size = new System.Drawing.Size(51, 17);
            this.chkPFlags.TabIndex = 5;
            this.chkPFlags.Text = "Flags";
            this.chkPFlags.UseVisualStyleBackColor = true;
            // 
            // chkPIsAnswered
            // 
            this.chkPIsAnswered.AutoSize = true;
            this.chkPIsAnswered.Location = new System.Drawing.Point(115, 29);
            this.chkPIsAnswered.Name = "chkPIsAnswered";
            this.chkPIsAnswered.Size = new System.Drawing.Size(73, 17);
            this.chkPIsAnswered.TabIndex = 6;
            this.chkPIsAnswered.Text = "Answered";
            this.chkPIsAnswered.UseVisualStyleBackColor = true;
            // 
            // chkPIsFlagged
            // 
            this.chkPIsFlagged.AutoSize = true;
            this.chkPIsFlagged.Location = new System.Drawing.Point(204, 29);
            this.chkPIsFlagged.Name = "chkPIsFlagged";
            this.chkPIsFlagged.Size = new System.Drawing.Size(64, 17);
            this.chkPIsFlagged.TabIndex = 7;
            this.chkPIsFlagged.Text = "Flagged";
            this.chkPIsFlagged.UseVisualStyleBackColor = true;
            // 
            // chkPIsSubmitted
            // 
            this.chkPIsSubmitted.AutoSize = true;
            this.chkPIsSubmitted.Location = new System.Drawing.Point(325, 29);
            this.chkPIsSubmitted.Name = "chkPIsSubmitted";
            this.chkPIsSubmitted.Size = new System.Drawing.Size(73, 17);
            this.chkPIsSubmitted.TabIndex = 9;
            this.chkPIsSubmitted.Text = "Submitted";
            this.chkPIsSubmitted.UseVisualStyleBackColor = true;
            // 
            // chkPReceived
            // 
            this.chkPReceived.AutoSize = true;
            this.chkPReceived.Location = new System.Drawing.Point(8, 52);
            this.chkPReceived.Name = "chkPReceived";
            this.chkPReceived.Size = new System.Drawing.Size(72, 17);
            this.chkPReceived.TabIndex = 10;
            this.chkPReceived.Text = "Received";
            this.chkPReceived.UseVisualStyleBackColor = true;
            // 
            // chkPSize
            // 
            this.chkPSize.AutoSize = true;
            this.chkPSize.Location = new System.Drawing.Point(8, 75);
            this.chkPSize.Name = "chkPSize";
            this.chkPSize.Size = new System.Drawing.Size(46, 17);
            this.chkPSize.TabIndex = 11;
            this.chkPSize.Text = "Size";
            this.chkPSize.UseVisualStyleBackColor = true;
            // 
            // chkPUID
            // 
            this.chkPUID.AutoSize = true;
            this.chkPUID.Location = new System.Drawing.Point(8, 98);
            this.chkPUID.Name = "chkPUID";
            this.chkPUID.Size = new System.Drawing.Size(45, 17);
            this.chkPUID.TabIndex = 12;
            this.chkPUID.Text = "UID";
            this.chkPUID.UseVisualStyleBackColor = true;
            // 
            // chkPModSeq
            // 
            this.chkPModSeq.AutoSize = true;
            this.chkPModSeq.Location = new System.Drawing.Point(8, 121);
            this.chkPModSeq.Name = "chkPModSeq";
            this.chkPModSeq.Size = new System.Drawing.Size(66, 17);
            this.chkPModSeq.TabIndex = 13;
            this.chkPModSeq.Text = "ModSeq";
            this.chkPModSeq.UseVisualStyleBackColor = true;
            // 
            // chkPBodyStructure
            // 
            this.chkPBodyStructure.AutoSize = true;
            this.chkPBodyStructure.Location = new System.Drawing.Point(8, 144);
            this.chkPBodyStructure.Name = "chkPBodyStructure";
            this.chkPBodyStructure.Size = new System.Drawing.Size(96, 17);
            this.chkPBodyStructure.TabIndex = 14;
            this.chkPBodyStructure.Text = "Body Structure";
            this.chkPBodyStructure.UseVisualStyleBackColor = true;
            // 
            // chkPAttachments
            // 
            this.chkPAttachments.AutoSize = true;
            this.chkPAttachments.Location = new System.Drawing.Point(115, 144);
            this.chkPAttachments.Name = "chkPAttachments";
            this.chkPAttachments.Size = new System.Drawing.Size(85, 17);
            this.chkPAttachments.TabIndex = 15;
            this.chkPAttachments.Text = "Attachments";
            this.chkPAttachments.UseVisualStyleBackColor = true;
            // 
            // chkPImportance
            // 
            this.chkPImportance.AutoSize = true;
            this.chkPImportance.Location = new System.Drawing.Point(8, 190);
            this.chkPImportance.Name = "chkPImportance";
            this.chkPImportance.Size = new System.Drawing.Size(79, 17);
            this.chkPImportance.TabIndex = 18;
            this.chkPImportance.Text = "Importance";
            this.chkPImportance.UseVisualStyleBackColor = true;
            // 
            // chkPPlainTextSize
            // 
            this.chkPPlainTextSize.AutoSize = true;
            this.chkPPlainTextSize.Location = new System.Drawing.Point(204, 144);
            this.chkPPlainTextSize.Name = "chkPPlainTextSize";
            this.chkPPlainTextSize.Size = new System.Drawing.Size(90, 17);
            this.chkPPlainTextSize.TabIndex = 16;
            this.chkPPlainTextSize.Text = "Plain text size";
            this.chkPPlainTextSize.UseVisualStyleBackColor = true;
            // 
            // chkPReferences
            // 
            this.chkPReferences.AutoSize = true;
            this.chkPReferences.Location = new System.Drawing.Point(8, 167);
            this.chkPReferences.Name = "chkPReferences";
            this.chkPReferences.Size = new System.Drawing.Size(81, 17);
            this.chkPReferences.TabIndex = 17;
            this.chkPReferences.Text = "References";
            this.chkPReferences.UseVisualStyleBackColor = true;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(282, 7);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(16, 13);
            this.label27.TabIndex = 3;
            this.label27.Text = "...";
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(282, 30);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(16, 13);
            this.label28.TabIndex = 8;
            this.label28.Text = "...";
            // 
            // cmdPSet
            // 
            this.cmdPSet.Location = new System.Drawing.Point(8, 254);
            this.cmdPSet.Name = "cmdPSet";
            this.cmdPSet.Size = new System.Drawing.Size(100, 25);
            this.cmdPSet.TabIndex = 19;
            this.cmdPSet.Text = "Set";
            this.cmdPSet.UseVisualStyleBackColor = true;
            this.cmdPSet.Click += new System.EventHandler(this.cmdPSet_Click);
            // 
            // frmClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(838, 496);
            this.Controls.Add(this.tabClient);
            this.Controls.Add(this.gbxConnect);
            this.Controls.Add(this.cmdPoll);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdDisconnect);
            this.Name = "frmClient";
            this.Text = "frmClient";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmClient_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmClient_FormClosed);
            this.Load += new System.EventHandler(this.frmClient_Load);
            this.gbxServer.ResumeLayout(false);
            this.gbxServer.PerformLayout();
            this.gbxCredentials.ResumeLayout(false);
            this.gbxCredentials.PerformLayout();
            this.gbxTLSRequirement.ResumeLayout(false);
            this.gbxTLSRequirement.PerformLayout();
            this.gbxMailboxCacheData.ResumeLayout(false);
            this.gbxMailboxCacheData.PerformLayout();
            this.gbxOther.ResumeLayout(false);
            this.gbxOther.PerformLayout();
            this.gbxCapabilities.ResumeLayout(false);
            this.gbxCapabilities.PerformLayout();
            this.gbxFetchBodyWrite.ResumeLayout(false);
            this.gbxFetchBodyWrite.PerformLayout();
            this.gbxFetchBodyRead.ResumeLayout(false);
            this.gbxFetchBodyRead.PerformLayout();
            this.gbxFetchCacheItems.ResumeLayout(false);
            this.gbxFetchCacheItems.PerformLayout();
            this.gbxTimeout.ResumeLayout(false);
            this.gbxTimeout.PerformLayout();
            this.gbxIdle.ResumeLayout(false);
            this.gbxIdle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.erp)).EndInit();
            this.tabConnect.ResumeLayout(false);
            this.tbpDetails.ResumeLayout(false);
            this.tbpCapabilities.ResumeLayout(false);
            this.gbxConnect.ResumeLayout(false);
            this.gbxSelectedMailbox.ResumeLayout(false);
            this.gbxSelectedMailbox.PerformLayout();
            this.tabClient.ResumeLayout(false);
            this.tbpSettings.ResumeLayout(false);
            this.tbpDefaults.ResumeLayout(false);
            this.gbxDefaultSort.ResumeLayout(false);
            this.gbxDefaultSort.PerformLayout();
            this.tbpWindows.ResumeLayout(false);
            this.gbxMailboxes.ResumeLayout(false);
            this.gbxMailboxes.PerformLayout();
            this.gbxEvents.ResumeLayout(false);
            this.gbxEvents.PerformLayout();
            this.gbxNetworkActivity.ResumeLayout(false);
            this.gbxNetworkActivity.PerformLayout();
            this.tpgResponseText.ResumeLayout(false);
            this.tpgResponseText.PerformLayout();
            this.gbxResponseTextCode.ResumeLayout(false);
            this.gbxResponseTextCode.PerformLayout();
            this.gbxResponseTextType.ResumeLayout(false);
            this.gbxResponseTextType.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
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
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtTrace;
        private System.Windows.Forms.RadioButton rdoCredNone;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.RadioButton rdoCredBasic;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton rdoCredAnon;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUserId;
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
        private System.Windows.Forms.CheckBox chkIgnoreSort;
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
        private System.Windows.Forms.Button cmdSelectedMailbox;
        private System.Windows.Forms.Button cmdNetworkActivity;
        private System.Windows.Forms.GroupBox gbxMailboxCacheData;
        private System.Windows.Forms.GroupBox gbxOther;
        private System.Windows.Forms.CheckBox chkMailboxReferrals;
        private System.Windows.Forms.Button cmdDetails;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.GroupBox gbxFetchCacheItems;
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
        private System.Windows.Forms.Button cmdSubscriptions;
        private System.Windows.Forms.Button cmdEvents;
        private System.Windows.Forms.CheckBox chkTryIfNotAdvertised;
        private System.Windows.Forms.TabControl tabConnect;
        private System.Windows.Forms.TabPage tbpDetails;
        private System.Windows.Forms.TabPage tbpCapabilities;
        private System.Windows.Forms.GroupBox gbxTLSRequirement;
        private System.Windows.Forms.RadioButton rdoTLSDisallowed;
        private System.Windows.Forms.GroupBox gbxConnect;
        private System.Windows.Forms.RadioButton rdoTLSRequired;
        private System.Windows.Forms.RadioButton rdoTLSIndifferent;
        private System.Windows.Forms.GroupBox gbxSelectedMailbox;
        private System.Windows.Forms.TextBox txtSMMessages;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabControl tabClient;
        private System.Windows.Forms.TabPage tbpSettings;
        private System.Windows.Forms.TabPage tbpWindows;
        private System.Windows.Forms.GroupBox gbxNetworkActivity;
        private System.Windows.Forms.TextBox txtNetworkActivity;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.TabPage tpgResponseText;
        private System.Windows.Forms.GroupBox gbxResponseTextCode;
        private System.Windows.Forms.CheckBox chkRTCUnknownCTE;
        private System.Windows.Forms.CheckBox chkRTCUseAttr;
        private System.Windows.Forms.CheckBox chkRTCReferral;
        private System.Windows.Forms.CheckBox chkRTCRFC5530;
        private System.Windows.Forms.CheckBox chkRTCTryCreate;
        private System.Windows.Forms.CheckBox chkRTCParse;
        private System.Windows.Forms.CheckBox chkRTCBadCharset;
        private System.Windows.Forms.CheckBox chkRTCAlert;
        private System.Windows.Forms.CheckBox chkRTCUnknown;
        private System.Windows.Forms.CheckBox chkRTCNone;
        private System.Windows.Forms.GroupBox gbxResponseTextType;
        private System.Windows.Forms.CheckBox chkRTTContinue;
        private System.Windows.Forms.CheckBox chkRTTProtocolError;
        private System.Windows.Forms.CheckBox chkRTTAuthenticationCancelled;
        private System.Windows.Forms.CheckBox chkRTTFailure;
        private System.Windows.Forms.CheckBox chkRTTSuccess;
        private System.Windows.Forms.CheckBox chkRTTError;
        private System.Windows.Forms.CheckBox chkRTTWarning;
        private System.Windows.Forms.CheckBox chkRTTInformation;
        private System.Windows.Forms.CheckBox chkRTTBye;
        private System.Windows.Forms.CheckBox chkRTTGreeting;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TextBox txtResponseText;
        private System.Windows.Forms.Button cmdResponseText;
        private System.Windows.Forms.TabPage tbpDefaults;
        private System.Windows.Forms.Button cmdPoll;
        private System.Windows.Forms.GroupBox gbxDefaultSort;
        private System.Windows.Forms.GroupBox gbxEvents;
        private System.Windows.Forms.TextBox txtEvents;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.GroupBox gbxMailboxes;
        private System.Windows.Forms.CheckBox chkMStatus;
        private System.Windows.Forms.CheckBox chkMLSub;
        private System.Windows.Forms.CheckBox chkMList;
        private System.Windows.Forms.CheckBox chkTrackUIDNext;
        private System.Windows.Forms.CheckBox chkTrackUnseen;
        private System.Windows.Forms.CheckBox chkProgressBar;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.TextBox txtSMTextBytes;
        private System.Windows.Forms.Button cmdSort;
        private System.Windows.Forms.Label lblSort;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button cmdAHSet;
        private System.Windows.Forms.CheckBox chkAHBodyStructure;
        private System.Windows.Forms.TextBox txtAHHeaderFieldNames;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.CheckBox chkAHModSeq;
        private System.Windows.Forms.CheckBox chkAHUID;
        private System.Windows.Forms.CheckBox chkAHSize;
        private System.Windows.Forms.CheckBox chkAHReceived;
        private System.Windows.Forms.CheckBox chkAHFlags;
        private System.Windows.Forms.CheckBox chkAHEnvelope;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button cmdPSet;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.CheckBox chkPReferences;
        private System.Windows.Forms.CheckBox chkPPlainTextSize;
        private System.Windows.Forms.CheckBox chkPImportance;
        private System.Windows.Forms.CheckBox chkPAttachments;
        private System.Windows.Forms.CheckBox chkPBodyStructure;
        private System.Windows.Forms.CheckBox chkPModSeq;
        private System.Windows.Forms.CheckBox chkPUID;
        private System.Windows.Forms.CheckBox chkPSize;
        private System.Windows.Forms.CheckBox chkPReceived;
        private System.Windows.Forms.CheckBox chkPIsSubmitted;
        private System.Windows.Forms.CheckBox chkPIsFlagged;
        private System.Windows.Forms.CheckBox chkPIsAnswered;
        private System.Windows.Forms.CheckBox chkPFlags;
        private System.Windows.Forms.CheckBox chkPMessageId;
        private System.Windows.Forms.CheckBox chkPSubject;
        private System.Windows.Forms.CheckBox chkPSent;
        private System.Windows.Forms.CheckBox chkPEnvelope;
    }
}