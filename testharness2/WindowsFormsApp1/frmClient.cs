using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.mailclient;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmClient : Form
    {
        private readonly cIMAPClient mFirst;
        private readonly cIMAPClient mClient;
        private readonly Dictionary<string, Form> mNamedChildren = new Dictionary<string, Form>();
        private readonly List<Form> mUnnamedChildren = new List<Form>();
        private CancellationTokenSource mCTS = null;

        public frmClient(string pInstanceName)
        {
            mFirst = null;
            mClient = new cIMAPClient(pInstanceName);
            mClient.DefaultMessageCacheItems = fMessageCacheAttributes.envelope | fMessageCacheAttributes.flags | fMessageCacheAttributes.received | fMessageCacheAttributes.uid;
            InitializeComponent();
        }

        public frmClient(cIMAPClient pFirst)
        {
            mFirst = pFirst ?? throw new ArgumentNullException(nameof(pFirst));
            mClient = new cIMAPClient(pFirst.InstanceName + "_second");
            mClient.DefaultMessageCacheItems = fMessageCacheAttributes.envelope | fMessageCacheAttributes.flags | fMessageCacheAttributes.received | fMessageCacheAttributes.uid;
            InitializeComponent();
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.ConnectionState) || e.PropertyName == nameof(cIMAPClient.CancellableCount)) ZSetControlState();
        }

        private void ZSetControlState()
        {
            lblState.Text = mClient.ConnectionState.ToString();

            if (mClient.IsUnconnected)
            {
                gbxConnect.Enabled = true;

                ZSetControlStateCredentials();

                cmdPoll.Enabled = false;
                cmdDisconnect.Enabled = false;
            }
            else
            {
                gbxConnect.Enabled = false;

                cmdPoll.Enabled = mClient.IsConnected;
                cmdDisconnect.Enabled = mClient.IsConnected;
            }

            var lCancellableCount = mClient.CancellableCount;

            if (lCancellableCount == 0)
            {
                cmdCancel.Text = "Cancel";
                cmdCancel.Enabled = false;
            }
            else
            {
                cmdCancel.Text = "Cancel " + lCancellableCount.ToString();
                cmdCancel.Enabled = true;
            }

            cmdAppendTestsSecond.Enabled = mFirst == null && mClient.IsConnected && rdoCredBasic.Checked;

            if (mClient.Namespaces == null)
            {
                cmdMailboxes.Enabled = false;
                cmdSubscriptions.Enabled = false;
            }
            else
            {
                cmdMailboxes.Enabled = true;
                cmdSubscriptions.Enabled = true;
            }

            if (mCTS == null)
            {
                cmdCTSAllocate.Enabled = true;
                cmdCTSCancel.Enabled = false;
                cmdCTSDispose.Enabled = false;
            }
            else
            {
                cmdCTSAllocate.Enabled = false;
                cmdCTSCancel.Enabled = !mCTS.IsCancellationRequested;
                cmdCTSDispose.Enabled = true;
            }
        }

        private void ZSetControlStateCredentials()
        {
            txtTrace.Enabled = rdoCredAnon.Checked;
            txtUserId.Enabled = rdoCredBasic.Checked;
            txtPassword.Enabled = rdoCredBasic.Checked;

            gbxTLSRequirement.Enabled = rdoCredBasic.Checked || rdoCredBasic.Checked;
            chkTryIfNotAdvertised.Enabled = rdoCredBasic.Checked || rdoCredBasic.Checked;
        }

        private void ZSetControlStateServerCredentials()
        {
            bool lEnabled;

            if (mFirst == null) lEnabled = !mNamedChildren.ContainsKey(nameof(frmClient));
            else lEnabled = false;

            gbxServer.Enabled = lEnabled;
            gbxCredentials.Enabled = lEnabled;
        }

        private void ZSetControlStateIdle()
        {
            txtIdleStartDelay.Enabled = chkIdleAuto.Checked;
            txtIdleRestartInterval.Enabled = chkIdleAuto.Checked;
            txtIdlePollInterval.Enabled = chkIdleAuto.Checked;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            mClient.Cancel();
            ZSetControlState();
        }

        private async void cmdConnect_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                if (mFirst == null)
                {
                    mClient.SetServer(txtHost.Text.Trim(), int.Parse(txtPort.Text), chkSSL.Checked);

                    if (rdoCredNone.Checked) mClient.Authentication = cIMAPAuthentication.None;
                    else
                    {
                        eTLSRequirement lTLSRequirement;
                        if (rdoTLSIndifferent.Checked) lTLSRequirement = eTLSRequirement.indifferent;
                        else if (rdoTLSRequired.Checked) lTLSRequirement = eTLSRequirement.required;
                        else lTLSRequirement = eTLSRequirement.disallowed;

                        if (rdoCredAnon.Checked) mClient.Authentication = cIMAPAuthentication.GetAnonymous(txtTrace.Text.Trim(), lTLSRequirement, chkTryIfNotAdvertised.Checked);
                        else mClient.SetPlainAuthentication(txtUserId.Text.Trim(), txtPassword.Text.Trim(), lTLSRequirement, chkTryIfNotAdvertised.Checked);
                    }
                }
                else
                {
                    mClient.Server = mFirst.Server;
                    mClient.Authentication = mFirst.Authentication;
                }

                fMailboxCacheDataItems lMailboxCacheData = 0;
                if (chkCacheSubscribed.Checked) lMailboxCacheData |= fMailboxCacheDataItems.subscribed;
                if (chkCacheChildren.Checked) lMailboxCacheData |= fMailboxCacheDataItems.children;
                if (chkCacheSpecialUse.Checked) lMailboxCacheData |= fMailboxCacheDataItems.specialuse;
                if (chkCacheMessageCount.Checked) lMailboxCacheData |= fMailboxCacheDataItems.messagecount;
                if (chkCacheRecentCount.Checked) lMailboxCacheData |= fMailboxCacheDataItems.recentcount;
                if (chkCacheUIDNext.Checked) lMailboxCacheData |= fMailboxCacheDataItems.uidnext;
                if (chkCacheUIDValidity.Checked) lMailboxCacheData |= fMailboxCacheDataItems.uidvalidity;
                if (chkCacheUnseenCount.Checked) lMailboxCacheData |= fMailboxCacheDataItems.unseencount;
                if (chkCacheHighestModSeq.Checked) lMailboxCacheData |= fMailboxCacheDataItems.highestmodseq;

                mClient.MailboxCacheDataItems = lMailboxCacheData;

                fIMAPCapabilities lKnownCapabilities = 0;

                if (chkIgnoreStartTLS.Checked) lKnownCapabilities |= fIMAPCapabilities.starttls;
                if (chkIgnoreEnable.Checked) lKnownCapabilities |= fIMAPCapabilities.enable;
                if (chkIgnoreUTF8.Checked) lKnownCapabilities |= fIMAPCapabilities.utf8accept | fIMAPCapabilities.utf8only;
                if (chkIgnoreId.Checked) lKnownCapabilities |= fIMAPCapabilities.id;
                if (chkIgnoreNamespace.Checked) lKnownCapabilities |= fIMAPCapabilities.namespaces;

                if (chkIgnoreMailboxReferrals.Checked) lKnownCapabilities |= fIMAPCapabilities.mailboxreferrals;
                if (chkIgnoreListExtended.Checked) lKnownCapabilities |= fIMAPCapabilities.listextended;
                if (chkIgnoreListStatus.Checked) lKnownCapabilities |= fIMAPCapabilities.liststatus;
                if (chkIgnoreSpecialUse.Checked) lKnownCapabilities |= fIMAPCapabilities.specialuse;

                if (chkIgnoreCondStore.Checked) lKnownCapabilities |= fIMAPCapabilities.condstore;
                if (chkIgnoreQResync.Checked) lKnownCapabilities |= fIMAPCapabilities.qresync;

                if (chkIgnoreLiteralPlus.Checked) lKnownCapabilities |= fIMAPCapabilities.literalplus | fIMAPCapabilities.literalminus;
                if (chkIgnoreBinary.Checked) lKnownCapabilities |= fIMAPCapabilities.binary;
                if (chkIgnoreIdle.Checked) lKnownCapabilities |= fIMAPCapabilities.idle;
                if (chkIgnoreSASLIR.Checked) lKnownCapabilities |= fIMAPCapabilities.sasl_ir;

                if (chkIgnoreESearch.Checked) lKnownCapabilities |= fIMAPCapabilities.esearch;
                if (chkIgnoreSort.Checked) lKnownCapabilities |= fIMAPCapabilities.sort;
                if (chkIgnoreSortDisplay.Checked) lKnownCapabilities |= fIMAPCapabilities.sortdisplay;
                //if (chkIgnoreThreadOrderedSubject.Checked) lKnownCapabilities |= fIMAPCapabilities.threadorderedsubject;
                //if (chkIgnoreThreadReferences.Checked) lKnownCapabilities |= fIMAPCapabilities.threadreferences;
                if (chkIgnoreESort.Checked) lKnownCapabilities |= fIMAPCapabilities.esort;

                if (chkIgnoreMultiAppend.Checked) lKnownCapabilities |= fIMAPCapabilities.multiappend;
                if (chkIgnoreCatenate.Checked) lKnownCapabilities |= fIMAPCapabilities.catenate;

                mClient.IgnoreCapabilities = lKnownCapabilities;

                mClient.MailboxReferrals = chkMailboxReferrals.Checked;

                mClient.NetworkWriteConfiguration = new cBatchSizerConfiguration(int.Parse(txtNWMin.Text), int.Parse(txtNWMax.Text), int.Parse(txtNWMaxTime.Text), int.Parse(txtNWInitial.Text));

                await mClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"Connect error\n{ex}");
            }
        }

        private async void cmdDisconnect_Click(object sender, EventArgs e)
        {
            try { await mClient.DisconnectAsync(); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"Disconnect error\n{ex}");
            }
        }

        private void ZCredCheckedChanged(object sender, EventArgs e)
        {
            ZSetControlStateCredentials();
        }

        private void ZValTextBoxNotBlank(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (string.IsNullOrWhiteSpace(lSender.Text))
            {
                e.Cancel = true;
                erp.SetError(lSender, "required field");
            }
        }

        private void ZValTextBoxIsTimeout(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < -1 || i > 99999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "timeout should be a number -1 .. 99999");
            }
        }

        private void ZValTextBoxIsMilliseconds(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 100 || i > 9999999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "time in ms should be a number 100 .. 9999999");
            }
        }

        private void ZValTextBoxIsPortNumber(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 1 || i > 9999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "port number should be 1 .. 9999");
            }
        }

        private void ZValTextBoxIsNumberOfMessages(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 1 || i > 9999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "number of messages should be 1 .. 9999");
            }
        }

        private void ZValTextBoxIsNumberOfBytes(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 1)
            {
                e.Cancel = true;
                erp.SetError(lSender, "number of bytes should be 1 .. " + int.MaxValue);
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void ZValBatchSizerConfiguration(GroupBox pGBX, TextBox pMin, TextBox pMax, TextBox pInitial, CancelEventArgs e)
        {
            if (!int.TryParse(pMin.Text, out var lMin)) return;
            if (!int.TryParse(pMax.Text, out var lMax)) return;
            if (!int.TryParse(pInitial.Text, out var lInitial)) return;

            if (lMin > lInitial || lInitial > lMax)
            {
                e.Cancel = true;
                erp.SetError(pGBX, "min <= inital <= max");
            }
        }

        private void ZValHeaderFieldNames(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lTextBox)) return;

            if (ZTryParseHeaderFieldNames(lTextBox.Text, out var lNames)) lTextBox.Text = ZHeaderFieldNames(lNames);
            else
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "header field names must be printable ascii only");
            }
        }

        private bool ZTryParseHeaderFieldNames(string pText, out cHeaderFieldNames rNames)
        {
            if (pText == null) { rNames = null; return true; }

            List<string> lNames = new List<string>();
            foreach (var lName in pText.Trim().Split(' ', ':')) if (!string.IsNullOrWhiteSpace(lName)) lNames.Add(lName);

            if (lNames.Count == 0) { rNames = null; return true; }

            try { rNames = new cHeaderFieldNames(lNames); }
            catch { rNames = null; return false; }

            return true;
        }

        private string ZHeaderFieldNames(cHeaderFieldNames pNames)
        {
            if (pNames == null) return string.Empty;

            StringBuilder lBuilder = new StringBuilder();
            bool lFirst = true;

            foreach (var lName in pNames)
            {
                if (lFirst) lFirst = false;
                else lBuilder.Append(" ");
                lBuilder.Append(lName);
            }

            return lBuilder.ToString();
        }

        private void frmClient_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - client - " + mClient.InstanceName;

            if (mFirst == null) txtUserId.Text = "imaptest1";

            cmdAppendTests.Enabled = mFirst != null;

            ZSetControlState();
            ZSetControlStateServerCredentials();
            ZSetControlStateIdle();

            mClient.PropertyChanged += mClient_PropertyChanged;

            var lMailboxCacheData = mClient.MailboxCacheDataItems;
            chkCacheSubscribed.Checked = (lMailboxCacheData & fMailboxCacheDataItems.subscribed) != 0;
            chkCacheChildren.Checked = (lMailboxCacheData & fMailboxCacheDataItems.children) != 0;
            chkCacheSpecialUse.Checked = (lMailboxCacheData & fMailboxCacheDataItems.specialuse) != 0;
            chkCacheMessageCount.Checked = (lMailboxCacheData & fMailboxCacheDataItems.messagecount) != 0;
            chkCacheRecentCount.Checked = (lMailboxCacheData & fMailboxCacheDataItems.recentcount) != 0;
            chkCacheUIDNext.Checked = (lMailboxCacheData & fMailboxCacheDataItems.uidnext) != 0;
            chkCacheUIDValidity.Checked = (lMailboxCacheData & fMailboxCacheDataItems.uidvalidity) != 0;
            chkCacheUnseenCount.Checked = (lMailboxCacheData & fMailboxCacheDataItems.unseencount) != 0;
            chkCacheHighestModSeq.Checked = (lMailboxCacheData & fMailboxCacheDataItems.highestmodseq) != 0;

            ZLoadBatchSizerConfiguration(mClient.NetworkWriteConfiguration, txtNWMin, txtNWMax, txtNWMaxTime, txtNWInitial);

            var lIdleConfiguration = mClient.IdleConfiguration;

            if (lIdleConfiguration == null) chkIdleAuto.Checked = false;
            else
            {
                chkIdleAuto.Checked = true;
                txtIdleStartDelay.Text = lIdleConfiguration.StartDelay.ToString();
                txtIdleRestartInterval.Text = lIdleConfiguration.IdleRestartInterval.ToString();
                txtIdlePollInterval.Text = lIdleConfiguration.PollInterval.ToString();
            }

            txtTimeout.Text = mClient.Timeout.ToString();

            ZLoadBatchSizerConfiguration(mClient.FetchCacheItemsConfiguration, txtFAMin, txtFAMax, txtFAMaxTime, txtFAInitial);
            ZLoadBatchSizerConfiguration(mClient.FetchBodyConfiguration, txtFRMin, txtFRMax, txtFRMaxTime, txtFRInitial);

            ZSortDescriptionSet();

            chkAHEnvelope.Checked = (mClient.DefaultMessageCacheItems.Attributes & fMessageCacheAttributes.envelope) != 0;
            chkAHFlags.Checked = (mClient.DefaultMessageCacheItems.Attributes & fMessageCacheAttributes.flags) != 0;
            chkAHReceived.Checked = (mClient.DefaultMessageCacheItems.Attributes & fMessageCacheAttributes.received) != 0;
            chkAHSize.Checked = (mClient.DefaultMessageCacheItems.Attributes & fMessageCacheAttributes.size) != 0;
            chkAHUID.Checked = (mClient.DefaultMessageCacheItems.Attributes & fMessageCacheAttributes.uid) != 0;
            chkAHModSeq.Checked = (mClient.DefaultMessageCacheItems.Attributes & fMessageCacheAttributes.modseq) != 0;
            chkAHBodyStructure.Checked = (mClient.DefaultMessageCacheItems.Attributes & fMessageCacheAttributes.bodystructure) != 0;
            txtAHHeaderFieldNames.Text = ZHeaderFieldNames(mClient.DefaultMessageCacheItems.Names);

            ZDefaultFlagsDescriptionSet();

            ZLoadBatchSizerConfiguration(mClient.AppendBatchConfiguration, txtABMin, txtABMax, txtABMaxTime, txtABInitial);
        }

        private void ZLoadBatchSizerConfiguration(cBatchSizerConfiguration pConfig, TextBox pMin, TextBox pMax, TextBox pMaxTime, TextBox pInitial)
        {
            pMin.Text = pConfig.Min.ToString();
            pMax.Text = pConfig.Max.ToString();
            pMaxTime.Text = pConfig.MaxTime.ToString();
            pInitial.Text = pConfig.Initial.ToString();
        }

        private void frmClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            // to allow closing with validation errors
            e.Cancel = false;
        }

        private void frmClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mNamedChildren.Values)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZNamedChildClosed;
            }

            foreach (var lForm in mUnnamedChildren)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZUnnamedChildClosed;
            }

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }

            if (mClient != null)
            {
                try { mClient.Dispose(); }
                catch { }
            }

            if (mCTS != null)
            {
                try { mCTS.Dispose(); }
                catch { }
            }
        }

        private void chkIdleAuto_CheckedChanged(object sender, EventArgs e)
        {
            ZSetControlStateIdle();
        }

        private void gbxFetchCacheItems_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxFetchCacheItems, txtFAMin, txtFAMax, txtFAInitial, e);
        }

        private void gbxFetchBody_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxFetchBody, txtFRMin, txtFRMax, txtFRInitial, e);
        }

        private void gbxNetworkWrite_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxNetworkWrite, txtNWMin, txtNWMax, txtNWInitial, e);
        }

        private void cmdIdleSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                if (chkIdleAuto.Checked) mClient.IdleConfiguration = new cIdleConfiguration(int.Parse(txtIdleStartDelay.Text), int.Parse(txtIdleRestartInterval.Text), int.Parse(txtIdlePollInterval.Text));
                else mClient.IdleConfiguration = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set idle error\n{ex}");
            }
        }

        private void cmdEvents_Click(object sender, EventArgs e)
        {
            if (mNamedChildren.TryGetValue(nameof(frmEvents), out var lForm)) Program.Focus(lForm);
            else if (ValidateChildren(ValidationConstraints.Enabled)) ZNamedChildAdd(new frmEvents(mClient, int.Parse(txtEvents.Text)));
        }

        private void ZNamedChildAdd(Form pForm)
        {
            mNamedChildren.Add(pForm.Name, pForm);
            pForm.FormClosed += ZNamedChildClosed;
            Program.Centre(pForm, this);
            pForm.Show();
            ZNamedChildrenSetControlState();
        }

        private void ZNamedChildClosed(object sender, EventArgs e)
        {
            if (!(sender is Form lForm)) return;
            lForm.FormClosed -= ZNamedChildClosed;
            mNamedChildren.Remove(lForm.Name);
            ZNamedChildrenSetControlState();
        }

        private void ZNamedChildrenSetControlState()
        {
            txtNetworkActivity.Enabled = !mNamedChildren.ContainsKey(nameof(frmNetworkActivity));
            txtEvents.Enabled = !mNamedChildren.ContainsKey(nameof(frmEvents));

            bool lSelectedMailbox = !mNamedChildren.ContainsKey(nameof(frmSelectedMailbox));
            txtSMMessages.Enabled = lSelectedMailbox;
            txtSMTextBytes.Enabled = lSelectedMailbox;
            chkTrackUIDNext.Enabled = lSelectedMailbox;
            chkTrackUnseen.Enabled = lSelectedMailbox;
            chkProgressBar.Enabled = lSelectedMailbox;

            bool lResponseText = !mNamedChildren.ContainsKey(nameof(frmResponseText));

            txtResponseText.Enabled = lResponseText;
            gbxResponseTextType.Enabled = lResponseText;
            gbxResponseTextCode.Enabled = lResponseText;

            ZSetControlStateServerCredentials();
        }

        private void ZUnnamedChildAdd(Form pForm, Form pCentreOnThis)
        {
            mUnnamedChildren.Add(pForm);
            pForm.FormClosed += ZUnnamedChildClosed;
            Program.Centre(pForm, pCentreOnThis, mUnnamedChildren);
            pForm.Show();
        }

        private void ZUnnamedChildClosed(object sender, EventArgs e)
        {
            if (!(sender is Form lForm)) return;
            lForm.FormClosed -= ZUnnamedChildClosed;
            mUnnamedChildren.Remove(lForm);
        }

        private void cmdAppendTestsSecond_Click(object sender, EventArgs e)
        {
            if (mNamedChildren.TryGetValue(nameof(frmClient), out var lForm)) Program.Focus(lForm);
            else if (ValidateChildren(ValidationConstraints.Enabled)) ZNamedChildAdd(new frmClient(mClient));
        }

        private void cmdNetworkActivity_Click(object sender, EventArgs e)
        {
            if (mNamedChildren.TryGetValue(nameof(frmNetworkActivity), out var lForm)) Program.Focus(lForm);
            else if (ValidateChildren(ValidationConstraints.Enabled)) ZNamedChildAdd(new frmNetworkActivity(mClient, int.Parse(txtNetworkActivity.Text)));
        }

        private void cmdResponseText_Click(object sender, EventArgs e)
        {
            if (mNamedChildren.TryGetValue(nameof(frmResponseText), out var lForm)) Program.Focus(lForm);
            else if (ValidateChildren(ValidationConstraints.Enabled))
            {
                int lMaxMessages = int.Parse(txtResponseText.Text);

                List<eIMAPResponseTextContext> lTypes = new List<eIMAPResponseTextContext>();

                if (chkRTTOKGreeting.Checked) lTypes.Add(eIMAPResponseTextContext.greetingok);
                if (chkRTTPreAuthGreeting.Checked) lTypes.Add(eIMAPResponseTextContext.greetingpreauth);
                if (chkRTTByeGreeting.Checked) lTypes.Add(eIMAPResponseTextContext.greetingbye);
                if (chkRTTContinue.Checked) lTypes.Add(eIMAPResponseTextContext.continuerequest);
                if (chkRTTBye.Checked) lTypes.Add(eIMAPResponseTextContext.bye);
                if (chkRTTInformation.Checked) lTypes.Add(eIMAPResponseTextContext.information);
                if (chkRTTWarning.Checked) lTypes.Add(eIMAPResponseTextContext.warning);
                if (chkRTTError.Checked) lTypes.Add(eIMAPResponseTextContext.error);
                if (chkRTTSuccess.Checked) lTypes.Add(eIMAPResponseTextContext.success);
                if (chkRTTFailure.Checked) lTypes.Add(eIMAPResponseTextContext.failure);
                if (chkRTTProtocolError.Checked) lTypes.Add(eIMAPResponseTextContext.protocolerror);
                if (chkRTTAuthenticationCancelled.Checked) lTypes.Add(eIMAPResponseTextContext.authenticationcancelled);

                List<eIMAPResponseTextCode> lCodes = new List<eIMAPResponseTextCode>();

                if (chkRTCNone.Checked) lCodes.Add(eIMAPResponseTextCode.none);
                if (chkRTCOther.Checked) lCodes.Add(eIMAPResponseTextCode.other);
                if (chkRTCAlert.Checked) lCodes.Add(eIMAPResponseTextCode.alert);
                if (chkRTCBadCharset.Checked) lCodes.Add(eIMAPResponseTextCode.badcharset);
                if (chkRTCParse.Checked) lCodes.Add(eIMAPResponseTextCode.parse);
                if (chkRTCTryCreate.Checked) lCodes.Add(eIMAPResponseTextCode.trycreate);

                if (chkRTCRFC5530.Checked)
                {
                    lCodes.Add(eIMAPResponseTextCode.unavailable);
                    lCodes.Add(eIMAPResponseTextCode.authenticationfailed);
                    lCodes.Add(eIMAPResponseTextCode.authorizationfailed);
                    lCodes.Add(eIMAPResponseTextCode.expired);
                    lCodes.Add(eIMAPResponseTextCode.privacyrequired);
                    lCodes.Add(eIMAPResponseTextCode.contactadmin);
                    lCodes.Add(eIMAPResponseTextCode.noperm);
                    lCodes.Add(eIMAPResponseTextCode.inuse);
                    lCodes.Add(eIMAPResponseTextCode.expungeissued);
                    lCodes.Add(eIMAPResponseTextCode.corruption);
                    lCodes.Add(eIMAPResponseTextCode.serverbug);
                    lCodes.Add(eIMAPResponseTextCode.clientbug);
                    lCodes.Add(eIMAPResponseTextCode.cannot);
                    lCodes.Add(eIMAPResponseTextCode.limit);
                    lCodes.Add(eIMAPResponseTextCode.overquota);
                    lCodes.Add(eIMAPResponseTextCode.alreadyexists);
                    lCodes.Add(eIMAPResponseTextCode.nonexistent);
                }

                if (chkRTCReferral.Checked) lCodes.Add(eIMAPResponseTextCode.referral);
                if (chkRTCUseAttr.Checked) lCodes.Add(eIMAPResponseTextCode.useattr);
                if (chkRTCUnknownCTE.Checked) lCodes.Add(eIMAPResponseTextCode.unknowncte);

                ZNamedChildAdd(new frmResponseText(mClient, lMaxMessages, lTypes, lCodes));
            }
        }

        private void cmdTimeoutSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.Timeout = int.Parse(txtTimeout.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set timeout error\n{ex}");
            }
        }

        private void cmdFetchCacheItemsSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.FetchCacheItemsConfiguration = new cBatchSizerConfiguration(int.Parse(txtFAMin.Text), int.Parse(txtFAMax.Text), int.Parse(txtFAMaxTime.Text), int.Parse(txtFAInitial.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set fetch cache items error\n{ex}");
            }
        }

        private void cmdDetails_Click(object sender, EventArgs e)
        {
            if (mNamedChildren.TryGetValue(nameof(frmDetails), out var lForm)) Program.Focus(lForm);
            else ZNamedChildAdd(new frmDetails(mClient));
        }

        private async void cmdPoll_Click(object sender, EventArgs e)
        {
            try
            {
                await mClient.PollAsync();
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"Poll error\n{ex}");
            }
        }

        private void cmdMailboxes_Click(object sender, EventArgs e)
        {
            fMailboxCacheDataSets lDataSets = 0;

            if (chkMList.Checked) lDataSets |= fMailboxCacheDataSets.list;
            if (chkMLSub.Checked) lDataSets |= fMailboxCacheDataSets.lsub;
            if (chkMStatus.Checked) lDataSets |= fMailboxCacheDataSets.status;

            ZUnnamedChildAdd(new frmMailboxes(mClient, false, lDataSets, ZDisplaySelectedMailbox, ZDisplayUID), this);
        }

        private void cmdSubscriptions_Click(object sender, EventArgs e)
        {
            fMailboxCacheDataSets lDataSets = 0;

            if (chkMList.Checked) lDataSets |= fMailboxCacheDataSets.list;
            if (chkMLSub.Checked) lDataSets |= fMailboxCacheDataSets.lsub;
            if (chkMStatus.Checked) lDataSets |= fMailboxCacheDataSets.status;

            ZUnnamedChildAdd(new frmMailboxes(mClient, true, lDataSets, ZDisplaySelectedMailbox, ZDisplayUID), this);
        }

        private void cmdSelectedMailbox_Click(object sender, EventArgs e)
        {
            ZDisplaySelectedMailbox(this);
        }

        private void ZDisplaySelectedMailbox(Form pForm)
        {
            if (mNamedChildren.TryGetValue(nameof(frmSelectedMailbox), out var lForm)) Program.Focus(lForm);
            else if (ValidateChildren(ValidationConstraints.Enabled)) ZNamedChildAdd(new frmSelectedMailbox(mClient, ZUnnamedChildAdd, int.Parse(txtSMMessages.Text), int.Parse(txtSMTextBytes.Text), chkTrackUIDNext.Checked, chkTrackUnseen.Checked, chkProgressBar.Checked));
        }

        private void cmdUID_Click(object sender, EventArgs e)
        {
            ZDisplayUID(this);
        }

        private void ZDisplayUID(Form pForm)
        {
            if (mNamedChildren.TryGetValue(nameof(frmUID), out var lForm)) Program.Focus(lForm);
            else ZNamedChildAdd(new frmUID(mClient));
        }

        private void cmdSort_Click(object sender, EventArgs e)
        {
            using (frmSortDialog lSortDialog = new frmSortDialog(mClient.DefaultSort))
            {
                if (lSortDialog.ShowDialog(this) == DialogResult.OK)
                {
                    mClient.DefaultSort = lSortDialog.Sort;
                    ZSortDescriptionSet();
                }
            }
        }

        private void ZSortDescriptionSet()
        {
            lblSort.Text = mClient.DefaultSort.ToString();
        }

        private void ZDefaultFlagsDescriptionSet()
        {
            lblDefaultFlags.Text = mClient.DefaultAppendFlags?.ToString();
        }

        private void cmdAHSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            fMessageCacheAttributes lAttributes = 0;

            if (chkAHEnvelope.Checked) lAttributes |= fMessageCacheAttributes.envelope;
            if (chkAHFlags.Checked) lAttributes |= fMessageCacheAttributes.flags;
            if (chkAHReceived.Checked) lAttributes |= fMessageCacheAttributes.received;
            if (chkAHSize.Checked) lAttributes |= fMessageCacheAttributes.size;
            if (chkAHUID.Checked) lAttributes |= fMessageCacheAttributes.uid;
            if (chkAHModSeq.Checked) lAttributes |= fMessageCacheAttributes.modseq;
            if (chkAHBodyStructure.Checked) lAttributes |= fMessageCacheAttributes.bodystructure;

            ZTryParseHeaderFieldNames(txtAHHeaderFieldNames.Text, out var lNames);

            mClient.DefaultMessageCacheItems = new cMessageCacheItems(lAttributes, lNames ?? cHeaderFieldNames.Empty);
        }

        private void cmdPSet_Click(object sender, EventArgs e)
        {
            fIMAPMessageProperties lProperties = 0;

            if (chkPEnvelope.Checked) lProperties |= fIMAPMessageProperties.envelope;
            if (chkPSent.Checked) lProperties |= fIMAPMessageProperties.sent;
            if (chkPSubject.Checked) lProperties |= fIMAPMessageProperties.subject;
            if (chkPMessageId.Checked) lProperties |= fIMAPMessageProperties.messageid;
            if (chkPFlags.Checked) lProperties |= fIMAPMessageProperties.flags;
            if (chkPAnswered.Checked) lProperties |= fIMAPMessageProperties.answered;
            if (chkPFlagged.Checked) lProperties |= fIMAPMessageProperties.flagged;
            if (chkPSubmitted.Checked) lProperties |= fIMAPMessageProperties.submitted;
            if (chkPReceived.Checked) lProperties |= fIMAPMessageProperties.received;
            if (chkPSize.Checked) lProperties |= fIMAPMessageProperties.size;
            if (chkPUID.Checked) lProperties |= fIMAPMessageProperties.uid;
            if (chkPModSeq.Checked) lProperties |= fIMAPMessageProperties.modseq;
            if (chkPBodyStructure.Checked) lProperties |= fIMAPMessageProperties.bodystructure;
            if (chkPAttachments.Checked) lProperties |= fIMAPMessageProperties.attachments;
            if (chkPPlainTextSize.Checked) lProperties |= fIMAPMessageProperties.plaintextsizeinbytes;
            if (chkPReferences.Checked) lProperties |= fIMAPMessageProperties.references;
            if (chkPImportance.Checked) lProperties |= fIMAPMessageProperties.importance;

            mClient.DefaultMessageCacheItems = lProperties;
        }

        private void cmdCTSAllocate_Click(object sender, EventArgs e)
        {
            mCTS = new CancellationTokenSource();
            mClient.CancellationToken = mCTS.Token;
            ZSetControlState();
        }

        private void cmdCTSCancel_Click(object sender, EventArgs e)
        {
            mCTS.Cancel();
            ZSetControlState();
        }

        private void cmdCTSDispose_Click(object sender, EventArgs e)
        {
            try { mCTS.Dispose(); }
            catch { }
            mClient.CancellationToken = CancellationToken.None;
            mCTS = null;
            ZSetControlState();
        }

        private void cmdABSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.AppendBatchConfiguration = new cBatchSizerConfiguration(int.Parse(txtABMin.Text), int.Parse(txtABMax.Text), int.Parse(txtABMaxTime.Text), int.Parse(txtABInitial.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set append batch error\n{ex}");
            }
        }

        private void gbxAppendBatch_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxFetchCacheItems, txtABMin, txtABMax, txtABInitial, e);
        }

        private void cmdAppend_Click(object sender, EventArgs e)
        {
            if (mNamedChildren.TryGetValue(nameof(frmAppend), out var lForm)) Program.Focus(lForm);
            else ZNamedChildAdd(new frmAppend(mClient));
        }

        private void cmdDefaultFlags_Click(object sender, EventArgs e)
        {
            using (frmStorableFlagsDialog lFlagsDialog = new frmStorableFlagsDialog(mClient.DefaultAppendFlags))
            {
                if (lFlagsDialog.ShowDialog(this) == DialogResult.OK)
                {
                    mClient.DefaultAppendFlags = lFlagsDialog.Flags;
                    ZDefaultFlagsDescriptionSet();
                }
            }
        }

        private void cmdDefaultFlagsClear_Click(object sender, EventArgs e)
        {
            mClient.DefaultAppendFlags = null;
            ZDefaultFlagsDescriptionSet();
        }

        private void cmdAppendTests_Click(object sender, EventArgs e)
        {
            if (mNamedChildren.TryGetValue(nameof(frmAppendTests), out var lForm)) Program.Focus(lForm);
            else ZNamedChildAdd(new frmAppendTests(mClient, mFirst));
        }

        private async void cmdEncode_Click(object sender, EventArgs e)
        {
            var lOpenFileDialog = new OpenFileDialog();
            if (lOpenFileDialog.ShowDialog() != DialogResult.OK) return;

            var lSaveFileDialog = new SaveFileDialog();
            lSaveFileDialog.FileName = lOpenFileDialog.FileName + ".qpe";
            if (lSaveFileDialog.ShowDialog() != DialogResult.OK) return;

            eQuotedPrintableEncodeSourceType lSourceType;

            if (rdoBinary.Checked) lSourceType = eQuotedPrintableEncodeSourceType.Binary;
            else if (rdoLF.Checked) lSourceType = eQuotedPrintableEncodeSourceType.LFTerminatedLines;
            else lSourceType = eQuotedPrintableEncodeSourceType.CRLFTerminatedLines;

            eQuotedPrintableEncodeQuotingRule lQuotingRule;

            if (rdoEBCDIC.Checked) lQuotingRule = eQuotedPrintableEncodeQuotingRule.EBCDIC;
            else lQuotingRule = eQuotedPrintableEncodeQuotingRule.Minimal;

            frmProgress lProgress = null;

            try
            {
                using (var lSource = new FileStream(lOpenFileDialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    lProgress = new frmProgress("encoding " + lOpenFileDialog.FileName, lSource.Length);
                    ZUnnamedChildAdd(lProgress, this);

                    using (FileStream lTarget = new FileStream(lSaveFileDialog.FileName, FileMode.Create))
                    {
                        await mClient.QuotedPrintableEncodeAsync(lSource, lSourceType, lQuotingRule, lTarget, new cIncrementConfiguration(lProgress.CancellationToken, lProgress.Increment));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"problem when encoding'\n{ex}");
            }
            finally
            {
                if (lProgress != null) lProgress.Complete();
            }
        }

        private void cmdFetchBodySet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.FetchBodyConfiguration = new cBatchSizerConfiguration(int.Parse(txtFRMin.Text), int.Parse(txtFRMax.Text), int.Parse(txtFRMaxTime.Text), int.Parse(txtFRInitial.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set fetch body error\n{ex}");
            }
        }
    }
}
