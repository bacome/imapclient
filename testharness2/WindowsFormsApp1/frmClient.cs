using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmClient : Form
    {
        private readonly cIMAPClient mClient;
        private readonly Dictionary<string, Form> mNamedChildren = new Dictionary<string, Form>();
        private readonly List<Form> mUnnamedChildren = new List<Form>();

        public frmClient(string pInstanceName)
        {
            mClient = new cIMAPClient(pInstanceName);
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
                cmdCancel.Text = "Cancel";
                cmdCancel.Enabled = false;
                cmdPoll.Enabled = false;
                cmdDisconnect.Enabled = false;
            }
            else
            {
                gbxConnect.Enabled = false;

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

                cmdPoll.Enabled = mClient.IsConnected;
                cmdDisconnect.Enabled = mClient.IsConnected;
            }

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
        }

        private void ZSetControlStateCredentials()
        {
            txtTrace.Enabled = rdoCredAnon.Checked;
            txtUserId.Enabled = rdoCredBasic.Checked;
            txtPassword.Enabled = rdoCredBasic.Checked;

            gbxTLSRequirement.Enabled = rdoCredBasic.Checked || rdoCredBasic.Checked;
            chkTryIfNotAdvertised.Enabled = rdoCredBasic.Checked || rdoCredBasic.Checked;
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
                mClient.SetServer(txtHost.Text.Trim(), int.Parse(txtPort.Text.Trim()), chkSSL.Checked);

                if (rdoCredNone.Checked) mClient.SetNoCredentials();
                else
                {
                    eTLSRequirement lTLSRequirement;
                    if (rdoTLSIndifferent.Checked) lTLSRequirement = eTLSRequirement.indifferent;
                    else if (rdoTLSRequired.Checked) lTLSRequirement = eTLSRequirement.required;
                    else lTLSRequirement = eTLSRequirement.disallowed;

                    if (rdoCredAnon.Checked) mClient.SetAnonymousCredentials(txtTrace.Text.Trim(), lTLSRequirement, chkTryIfNotAdvertised.Checked);
                    else mClient.SetPlainCredentials(txtUserId.Text.Trim(), txtPassword.Text.Trim(), lTLSRequirement, chkTryIfNotAdvertised.Checked);
                }

                fMailboxCacheData lMailboxCacheData = 0;
                if (chkCacheSubscribed.Checked) lMailboxCacheData |= fMailboxCacheData.subscribed;
                if (chkCacheChildren.Checked) lMailboxCacheData |= fMailboxCacheData.children;
                if (chkCacheSpecialUse.Checked) lMailboxCacheData |= fMailboxCacheData.specialuse;
                if (chkCacheMessageCount.Checked) lMailboxCacheData |= fMailboxCacheData.messagecount;
                if (chkCacheRecentCount.Checked) lMailboxCacheData |= fMailboxCacheData.recentcount;
                if (chkCacheUIDNext.Checked) lMailboxCacheData |= fMailboxCacheData.uidnext;
                if (chkCacheUIDValidity.Checked) lMailboxCacheData |= fMailboxCacheData.uidvalidity;
                if (chkCacheUnseenCount.Checked) lMailboxCacheData |= fMailboxCacheData.unseencount;
                if (chkCacheHighestModSeq.Checked) lMailboxCacheData |= fMailboxCacheData.highestmodseq;

                mClient.MailboxCacheData = lMailboxCacheData;

                fKnownCapabilities lKnownCapabilities = 0;

                if (chkIgnoreStartTLS.Checked) lKnownCapabilities |= fKnownCapabilities.starttls;
                if (chkIgnoreEnable.Checked) lKnownCapabilities |= fKnownCapabilities.enable;
                if (chkIgnoreUTF8.Checked) lKnownCapabilities |= fKnownCapabilities.utf8accept | fKnownCapabilities.utf8only;
                if (chkIgnoreId.Checked) lKnownCapabilities |= fKnownCapabilities.id;
                if (chkIgnoreNamespace.Checked) lKnownCapabilities |= fKnownCapabilities.namespaces;

                if (chkIgnoreMailboxReferrals.Checked) lKnownCapabilities |= fKnownCapabilities.mailboxreferrals;
                if (chkIgnoreListExtended.Checked) lKnownCapabilities |= fKnownCapabilities.listextended;
                if (chkIgnoreListStatus.Checked) lKnownCapabilities |= fKnownCapabilities.liststatus;
                if (chkIgnoreSpecialUse.Checked) lKnownCapabilities |= fKnownCapabilities.specialuse;

                if (chkIgnoreCondStore.Checked) lKnownCapabilities |= fKnownCapabilities.condstore;
                if (chkIgnoreQResync.Checked) lKnownCapabilities |= fKnownCapabilities.qresync;

                if (chkIgnoreLiteralPlus.Checked) lKnownCapabilities |= fKnownCapabilities.literalplus | fKnownCapabilities.literalminus;
                if (chkIgnoreBinary.Checked) lKnownCapabilities |= fKnownCapabilities.binary;
                if (chkIgnoreIdle.Checked) lKnownCapabilities |= fKnownCapabilities.idle;
                if (chkIgnoreSASLIR.Checked) lKnownCapabilities |= fKnownCapabilities.sasl_ir;

                if (chkIgnoreESearch.Checked) lKnownCapabilities |= fKnownCapabilities.esearch;
                if (chkIgnoreSort.Checked) lKnownCapabilities |= fKnownCapabilities.sort;
                if (chkIgnoreSortDisplay.Checked) lKnownCapabilities |= fKnownCapabilities.sortdisplay;
                if (chkIgnoreThreadOrderedSubject.Checked) lKnownCapabilities |= fKnownCapabilities.threadorderedsubject;
                if (chkIgnoreThreadReferences.Checked) lKnownCapabilities |= fKnownCapabilities.threadreferences;
                if (chkIgnoreESort.Checked) lKnownCapabilities |= fKnownCapabilities.esort;

                mClient.IgnoreCapabilities = lKnownCapabilities;

                mClient.MailboxReferrals = chkMailboxReferrals.Checked;

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

            if (!int.TryParse(lSender.Text, out var i) || i < 1 || i > 1000000)
            {
                e.Cancel = true;
                erp.SetError(lSender, "number of bytes should be 1 .. 1000000");
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void ZValFetchConfig(GroupBox pGBX, TextBox pMin, TextBox pMax, TextBox pInitial, CancelEventArgs e)
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

            if (ZTryParseHeaderFieldNames(txtMPHeaderFieldNames.Text, out var lNames)) lTextBox.Text = ZHeaderFieldNames(lNames);
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
            ZSetControlState();
            ZSetControlStateIdle();           
            mClient.PropertyChanged += mClient_PropertyChanged;

            var lMailboxCacheData = mClient.MailboxCacheData;
            chkCacheSubscribed.Checked = (lMailboxCacheData & fMailboxCacheData.subscribed) != 0;
            chkCacheChildren.Checked = (lMailboxCacheData & fMailboxCacheData.children) != 0;
            chkCacheSpecialUse.Checked = (lMailboxCacheData & fMailboxCacheData.specialuse) != 0;
            chkCacheMessageCount.Checked = (lMailboxCacheData & fMailboxCacheData.messagecount) != 0;
            chkCacheRecentCount.Checked = (lMailboxCacheData & fMailboxCacheData.recentcount) != 0;
            chkCacheUIDNext.Checked = (lMailboxCacheData & fMailboxCacheData.uidnext) != 0;
            chkCacheUIDValidity.Checked = (lMailboxCacheData & fMailboxCacheData.uidvalidity) != 0;
            chkCacheUnseenCount.Checked = (lMailboxCacheData & fMailboxCacheData.unseencount) != 0;
            chkCacheHighestModSeq.Checked = (lMailboxCacheData & fMailboxCacheData.highestmodseq) != 0;

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

            ZLoadFetchConfig(mClient.FetchCacheItemsConfiguration, txtFAMin, txtFAMax, txtFAMaxTime, txtFAInitial);
            ZLoadFetchConfig(mClient.FetchBodyReadConfiguration, txtFRMin, txtFRMax, txtFRMaxTime, txtFRInitial);
            ZLoadFetchConfig(mClient.FetchBodyWriteConfiguration, txtFWMin, txtFWMax, txtFWMaxTime, txtFWInitial);

            ZSortDescriptionSet();

            chkMPEnvelope.Checked = (mClient.DefaultCacheItems.Attributes & fCacheAttributes.envelope) != 0;
            chkMPFlags.Checked = (mClient.DefaultCacheItems.Attributes & fCacheAttributes.flags) != 0;
            chkMPReceived.Checked = (mClient.DefaultCacheItems.Attributes & fCacheAttributes.flags) != 0;
            chkMPSize.Checked = (mClient.DefaultCacheItems.Attributes & fCacheAttributes.flags) != 0;
            chkMPUID.Checked = (mClient.DefaultCacheItems.Attributes & fCacheAttributes.flags) != 0;
            chkMPModSeq.Checked = (mClient.DefaultCacheItems.Attributes & fCacheAttributes.flags) != 0;
            chkMPBodyStructure.Checked = (mClient.DefaultCacheItems.Attributes & fCacheAttributes.flags) != 0;
            txtMPHeaderFieldNames.Text = ZHeaderFieldNames(mClient.DefaultCacheItems.Names);
        }

        private void ZLoadFetchConfig(cBatchSizerConfiguration pConfig, TextBox pMin, TextBox pMax, TextBox pMaxTime, TextBox pInitial)
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
        }

        private void chkIdleAuto_CheckedChanged(object sender, EventArgs e)
        {
            ZSetControlStateIdle();
        }

        private void gbxFetchCacheItems_Validating(object sender, CancelEventArgs e)
        {
            ZValFetchConfig(gbxFetchCacheItems, txtFAMin, txtFAMax, txtFAInitial, e);
        }

        private void gbxFetchBodyRead_Validating(object sender, CancelEventArgs e)
        {
            ZValFetchConfig(gbxFetchBodyRead, txtFRMin, txtFRMax, txtFRInitial, e);
        }

        private void gbxFetchBodyWrite_Validating(object sender, CancelEventArgs e)
        {
            ZValFetchConfig(gbxFetchBodyWrite, txtFWMin, txtFWMax, txtFWInitial, e);
        }

        private void cmdIdleSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.IdleConfiguration = new cIdleConfiguration(int.Parse(txtIdleStartDelay.Text), int.Parse(txtIdleRestartInterval.Text), int.Parse(txtIdlePollInterval.Text));
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
        }

        private void ZUnnamedChildAdd(Form pForm)
        {
            mUnnamedChildren.Add(pForm);
            pForm.FormClosed += ZUnnamedChildClosed;
            Program.Centre(pForm, this, mUnnamedChildren);
            pForm.Show();
        }

        private void ZUnnamedChildClosed(object sender, EventArgs e)
        {
            if (!(sender is Form lForm)) return;
            lForm.FormClosed -= ZUnnamedChildClosed;
            mUnnamedChildren.Remove(lForm);
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

                List<eResponseTextType> lTypes = new List<eResponseTextType>();

                if (chkRTTGreeting.Checked) lTypes.Add(eResponseTextType.greeting);
                if (chkRTTContinue.Checked) lTypes.Add(eResponseTextType.continuerequest);
                if (chkRTTBye.Checked) lTypes.Add(eResponseTextType.bye);
                if (chkRTTInformation.Checked) lTypes.Add(eResponseTextType.information);
                if (chkRTTWarning.Checked) lTypes.Add(eResponseTextType.warning);
                if (chkRTTError.Checked) lTypes.Add(eResponseTextType.error);
                if (chkRTTSuccess.Checked) lTypes.Add(eResponseTextType.success);
                if (chkRTTFailure.Checked) lTypes.Add(eResponseTextType.failure);
                if (chkRTTProtocolError.Checked) lTypes.Add(eResponseTextType.protocolerror);
                if (chkRTTAuthenticationCancelled.Checked) lTypes.Add(eResponseTextType.authenticationcancelled);

                List<eResponseTextCode> lCodes = new List<eResponseTextCode>();

                if (chkRTCNone.Checked) lCodes.Add(eResponseTextCode.none);
                if (chkRTCUnknown.Checked) lCodes.Add(eResponseTextCode.unknown);
                if (chkRTCAlert.Checked) lCodes.Add(eResponseTextCode.alert);
                if (chkRTCBadCharset.Checked) lCodes.Add(eResponseTextCode.badcharset);
                if (chkRTCParse.Checked) lCodes.Add(eResponseTextCode.parse);
                if (chkRTCTryCreate.Checked) lCodes.Add(eResponseTextCode.trycreate);

                if (chkRTCRFC5530.Checked)
                {
                    lCodes.Add(eResponseTextCode.unavailable);
                    lCodes.Add(eResponseTextCode.authenticationfailed);
                    lCodes.Add(eResponseTextCode.authorizationfailed);
                    lCodes.Add(eResponseTextCode.expired);
                    lCodes.Add(eResponseTextCode.privacyrequired);
                    lCodes.Add(eResponseTextCode.contactadmin);
                    lCodes.Add(eResponseTextCode.noperm);
                    lCodes.Add(eResponseTextCode.inuse);
                    lCodes.Add(eResponseTextCode.expungeissued);
                    lCodes.Add(eResponseTextCode.corruption);
                    lCodes.Add(eResponseTextCode.serverbug);
                    lCodes.Add(eResponseTextCode.clientbug);
                    lCodes.Add(eResponseTextCode.cannot);
                    lCodes.Add(eResponseTextCode.limit);
                    lCodes.Add(eResponseTextCode.overquota);
                    lCodes.Add(eResponseTextCode.alreadyexists);
                    lCodes.Add(eResponseTextCode.nonexistent);
                }

                if (chkRTCReferral.Checked) lCodes.Add(eResponseTextCode.referral);
                if (chkRTCUseAttr.Checked) lCodes.Add(eResponseTextCode.useattr);
                if (chkRTCUnknownCTE.Checked) lCodes.Add(eResponseTextCode.unknowncte);

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

        private void cmdFRSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.FetchBodyReadConfiguration = new cBatchSizerConfiguration(int.Parse(txtFRMin.Text), int.Parse(txtFRMax.Text), int.Parse(txtFRMaxTime.Text), int.Parse(txtFRInitial.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set fetch body read error\n{ex}");
            }
        }

        private void cmdFASet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.FetchCacheItemsConfiguration = new cBatchSizerConfiguration(int.Parse(txtFAMin.Text), int.Parse(txtFAMax.Text), int.Parse(txtFAMaxTime.Text), int.Parse(txtFAInitial.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set fetch attributes error\n{ex}");
            }
        }

        private void cmdFWSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.FetchBodyWriteConfiguration = new cBatchSizerConfiguration(int.Parse(txtFWMin.Text), int.Parse(txtFWMax.Text), int.Parse(txtFWMaxTime.Text), int.Parse(txtFWInitial.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set fetch body write error\n{ex}");
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

            ZUnnamedChildAdd(new frmMailboxes(mClient, false, lDataSets, ZDisplaySelectedMailbox));
        }

        private void cmdSubscriptions_Click(object sender, EventArgs e)
        {
            fMailboxCacheDataSets lDataSets = 0;

            if (chkMList.Checked) lDataSets |= fMailboxCacheDataSets.list;
            if (chkMLSub.Checked) lDataSets |= fMailboxCacheDataSets.lsub;
            if (chkMStatus.Checked) lDataSets |= fMailboxCacheDataSets.status;

            ZUnnamedChildAdd(new frmMailboxes(mClient, true, lDataSets, ZDisplaySelectedMailbox));
        }

        private void cmdSelectedMailbox_Click(object sender, EventArgs e)
        {
            ZDisplaySelectedMailbox(this);
        }

        private void ZDisplaySelectedMailbox(Form pForm)
        {
            if (mNamedChildren.TryGetValue(nameof(frmSelectedMailbox), out var lForm)) Program.Focus(lForm);
            else if (ValidateChildren(ValidationConstraints.Enabled)) ZNamedChildAdd(new frmSelectedMailbox(mClient, int.Parse(txtSMMessages.Text), int.Parse(txtSMTextBytes.Text), chkTrackUIDNext.Checked, chkTrackUnseen.Checked, chkProgressBar.Checked));
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

        private void cmdMPSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            fCacheAttributes lAttributes = 0;

            if (chkMPEnvelope.Checked) lAttributes |= fCacheAttributes.envelope;
            if (chkMPFlags.Checked) lAttributes |= fCacheAttributes.flags;
            if (chkMPReceived.Checked) lAttributes |= fCacheAttributes.received;
            if (chkMPSize.Checked) lAttributes |= fCacheAttributes.size;
            if (chkMPUID.Checked) lAttributes |= fCacheAttributes.uid;
            if (chkMPModSeq.Checked) lAttributes |= fCacheAttributes.modseq;
            if (chkMPBodyStructure.Checked) lAttributes |= fCacheAttributes.bodystructure;

            ZTryParseHeaderFieldNames(txtMPHeaderFieldNames.Text, out var lNames);

            mClient.DefaultCacheItems = new cCacheItems(lAttributes, lNames ?? cHeaderFieldNames.None);
        }
    }
}
