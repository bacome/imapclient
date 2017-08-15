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
                mClient.Dispose();
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
            this.gbxServer = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.chkSSL = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.gbxCredentials = new System.Windows.Forms.GroupBox();
            this.gbxBeforeConnect = new System.Windows.Forms.GroupBox();
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
            this.gbxCapabilities = new System.Windows.Forms.GroupBox();
            this.chkIgnoreQResync = new System.Windows.Forms.CheckBox();
            this.chkIgnoreBinary = new System.Windows.Forms.CheckBox();
            this.chkIgnoreNamespace = new System.Windows.Forms.CheckBox();
            this.chkIgnoreUTF8 = new System.Windows.Forms.CheckBox();
            this.chkIgnoreLiteralPlus = new System.Windows.Forms.CheckBox();
            this.chkIgnoreIdle = new System.Windows.Forms.CheckBox();
            this.chkIgnoreId = new System.Windows.Forms.CheckBox();
            this.chkIgnoreSASLIR = new System.Windows.Forms.CheckBox();
            this.chkIgnoreListStatus = new System.Windows.Forms.CheckBox();
            this.chkIgnoreListExtended = new System.Windows.Forms.CheckBox();
            this.chkIgnoreSpecialUse = new System.Windows.Forms.CheckBox();
            this.chkIgnoreMailboxReferrals = new System.Windows.Forms.CheckBox();
            this.chkIgnoreESearch = new System.Windows.Forms.CheckBox();
            this.chkSort = new System.Windows.Forms.CheckBox();
            this.chkIgnoreSortDisplay = new System.Windows.Forms.CheckBox();
            this.chkIgnoreThreadOrderedSubject = new System.Windows.Forms.CheckBox();
            this.chkIgnoreThreadReferences = new System.Windows.Forms.CheckBox();
            this.chkIgnoreESort = new System.Windows.Forms.CheckBox();
            this.chkIgnoreCondStore = new System.Windows.Forms.CheckBox();
            this.chkIgnoreStartTLS = new System.Windows.Forms.CheckBox();
            this.chkIgnoreEnable = new System.Windows.Forms.CheckBox();
            this.gbxAnyTime = new System.Windows.Forms.GroupBox();
            this.txtTimeouts = new System.Windows.Forms.TextBox();
            this.gbxIdle = new System.Windows.Forms.GroupBox();
            this.txtIdleRestartInterval = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtPollInterval = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtStartDelay = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chkAutoIdle = new System.Windows.Forms.CheckBox();
            this.gbxTimeout = new System.Windows.Forms.GroupBox();
            this.gbxServer.SuspendLayout();
            this.gbxCredentials.SuspendLayout();
            this.gbxBeforeConnect.SuspendLayout();
            this.gbxCapabilities.SuspendLayout();
            this.gbxAnyTime.SuspendLayout();
            this.gbxIdle.SuspendLayout();
            this.gbxTimeout.SuspendLayout();
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
            this.txtHost.Size = new System.Drawing.Size(154, 20);
            this.txtHost.TabIndex = 4;
            this.txtHost.Text = "192.168.56.101";
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
            // gbxBeforeConnect
            // 
            this.gbxBeforeConnect.Controls.Add(this.gbxCapabilities);
            this.gbxBeforeConnect.Controls.Add(this.gbxServer);
            this.gbxBeforeConnect.Controls.Add(this.gbxCredentials);
            this.gbxBeforeConnect.Location = new System.Drawing.Point(2, 1);
            this.gbxBeforeConnect.Name = "gbxBeforeConnect";
            this.gbxBeforeConnect.Size = new System.Drawing.Size(409, 696);
            this.gbxBeforeConnect.TabIndex = 31;
            this.gbxBeforeConnect.TabStop = false;
            this.gbxBeforeConnect.Text = "Set Before Connect";
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
            this.txtTrace.Size = new System.Drawing.Size(164, 20);
            this.txtTrace.TabIndex = 19;
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
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(159, 89);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(164, 20);
            this.txtPassword.TabIndex = 17;
            this.txtPassword.Text = "imaptest1";
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
            this.txtUserId.Size = new System.Drawing.Size(164, 20);
            this.txtUserId.TabIndex = 16;
            this.txtUserId.Text = "imaptest1";
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
            // chkIgnoreIdle
            // 
            this.chkIgnoreIdle.AutoSize = true;
            this.chkIgnoreIdle.Location = new System.Drawing.Point(159, 65);
            this.chkIgnoreIdle.Name = "chkIgnoreIdle";
            this.chkIgnoreIdle.Size = new System.Drawing.Size(43, 17);
            this.chkIgnoreIdle.TabIndex = 22;
            this.chkIgnoreIdle.Text = "Idle";
            this.chkIgnoreIdle.UseVisualStyleBackColor = true;
            this.chkIgnoreIdle.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
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
            // gbxAnyTime
            // 
            this.gbxAnyTime.Controls.Add(this.gbxTimeout);
            this.gbxAnyTime.Controls.Add(this.gbxIdle);
            this.gbxAnyTime.Location = new System.Drawing.Point(444, 12);
            this.gbxAnyTime.Name = "gbxAnyTime";
            this.gbxAnyTime.Size = new System.Drawing.Size(460, 640);
            this.gbxAnyTime.TabIndex = 32;
            this.gbxAnyTime.TabStop = false;
            this.gbxAnyTime.Text = "Set Any Time";
            // 
            // txtTimeouts
            // 
            this.txtTimeouts.Location = new System.Drawing.Point(133, 19);
            this.txtTimeouts.Name = "txtTimeouts";
            this.txtTimeouts.Size = new System.Drawing.Size(40, 20);
            this.txtTimeouts.TabIndex = 10;
            this.txtTimeouts.Text = "60000";
            // 
            // gbxIdle
            // 
            this.gbxIdle.Controls.Add(this.txtIdleRestartInterval);
            this.gbxIdle.Controls.Add(this.label10);
            this.gbxIdle.Controls.Add(this.txtPollInterval);
            this.gbxIdle.Controls.Add(this.label9);
            this.gbxIdle.Controls.Add(this.txtStartDelay);
            this.gbxIdle.Controls.Add(this.label8);
            this.gbxIdle.Controls.Add(this.chkAutoIdle);
            this.gbxIdle.Location = new System.Drawing.Point(6, 18);
            this.gbxIdle.Name = "gbxIdle";
            this.gbxIdle.Size = new System.Drawing.Size(229, 114);
            this.gbxIdle.TabIndex = 28;
            this.gbxIdle.TabStop = false;
            this.gbxIdle.Text = "Idle";
            // 
            // txtIdleRestartInterval
            // 
            this.txtIdleRestartInterval.Location = new System.Drawing.Point(133, 62);
            this.txtIdleRestartInterval.Name = "txtIdleRestartInterval";
            this.txtIdleRestartInterval.Size = new System.Drawing.Size(40, 20);
            this.txtIdleRestartInterval.TabIndex = 19;
            this.txtIdleRestartInterval.Text = "60000";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(13, 65);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(78, 13);
            this.label10.TabIndex = 16;
            this.label10.Text = "Idle restart time";
            // 
            // txtPollInterval
            // 
            this.txtPollInterval.Location = new System.Drawing.Point(133, 83);
            this.txtPollInterval.Name = "txtPollInterval";
            this.txtPollInterval.Size = new System.Drawing.Size(40, 20);
            this.txtPollInterval.TabIndex = 20;
            this.txtPollInterval.Text = "60000";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(13, 86);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(99, 13);
            this.label9.TabIndex = 15;
            this.label9.Text = "Poll Interval (NoOp)";
            // 
            // txtStartDelay
            // 
            this.txtStartDelay.Location = new System.Drawing.Point(133, 40);
            this.txtStartDelay.Name = "txtStartDelay";
            this.txtStartDelay.Size = new System.Drawing.Size(40, 20);
            this.txtStartDelay.TabIndex = 18;
            this.txtStartDelay.Text = "1000";
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
            // chkAutoIdle
            // 
            this.chkAutoIdle.AutoSize = true;
            this.chkAutoIdle.Checked = true;
            this.chkAutoIdle.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoIdle.Location = new System.Drawing.Point(16, 19);
            this.chkAutoIdle.Name = "chkAutoIdle";
            this.chkAutoIdle.Size = new System.Drawing.Size(68, 17);
            this.chkAutoIdle.TabIndex = 17;
            this.chkAutoIdle.Text = "Auto Idle";
            this.chkAutoIdle.UseVisualStyleBackColor = true;
            // 
            // gbxTimeout
            // 
            this.gbxTimeout.Controls.Add(this.txtTimeouts);
            this.gbxTimeout.Location = new System.Drawing.Point(6, 138);
            this.gbxTimeout.Name = "gbxTimeout";
            this.gbxTimeout.Size = new System.Drawing.Size(229, 52);
            this.gbxTimeout.TabIndex = 29;
            this.gbxTimeout.TabStop = false;
            this.gbxTimeout.Text = "Timeout";
            // 
            // frmClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1091, 749);
            this.Controls.Add(this.gbxAnyTime);
            this.Controls.Add(this.gbxBeforeConnect);
            this.Name = "frmClient";
            this.Text = "frmClient";
            this.gbxServer.ResumeLayout(false);
            this.gbxServer.PerformLayout();
            this.gbxCredentials.ResumeLayout(false);
            this.gbxCredentials.PerformLayout();
            this.gbxBeforeConnect.ResumeLayout(false);
            this.gbxCapabilities.ResumeLayout(false);
            this.gbxCapabilities.PerformLayout();
            this.gbxAnyTime.ResumeLayout(false);
            this.gbxIdle.ResumeLayout(false);
            this.gbxIdle.PerformLayout();
            this.gbxTimeout.ResumeLayout(false);
            this.gbxTimeout.PerformLayout();
            this.ResumeLayout(false);

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
        private System.Windows.Forms.GroupBox gbxBeforeConnect;
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
        private System.Windows.Forms.GroupBox gbxAnyTime;
        private System.Windows.Forms.GroupBox gbxTimeout;
        private System.Windows.Forms.TextBox txtTimeouts;
        private System.Windows.Forms.GroupBox gbxIdle;
        private System.Windows.Forms.TextBox txtIdleRestartInterval;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtPollInterval;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtStartDelay;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkAutoIdle;
    }
}