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
        private CancellationTokenSource mCTS = null;

        public frmClient(string pInstanceName)
        {
            mClient = new cIMAPClient(pInstanceName);
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
                mClient.SetServer(txtHost.Text.Trim(), int.Parse(txtPort.Text), chkSSL.Checked);

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

                fCapabilities lKnownCapabilities = 0;

                if (chkIgnoreStartTLS.Checked) lKnownCapabilities |= fCapabilities.starttls;
                if (chkIgnoreEnable.Checked) lKnownCapabilities |= fCapabilities.enable;
                if (chkIgnoreUTF8.Checked) lKnownCapabilities |= fCapabilities.utf8accept | fCapabilities.utf8only;
                if (chkIgnoreId.Checked) lKnownCapabilities |= fCapabilities.id;
                if (chkIgnoreNamespace.Checked) lKnownCapabilities |= fCapabilities.namespaces;

                if (chkIgnoreMailboxReferrals.Checked) lKnownCapabilities |= fCapabilities.mailboxreferrals;
                if (chkIgnoreListExtended.Checked) lKnownCapabilities |= fCapabilities.listextended;
                if (chkIgnoreListStatus.Checked) lKnownCapabilities |= fCapabilities.liststatus;
                if (chkIgnoreSpecialUse.Checked) lKnownCapabilities |= fCapabilities.specialuse;

                if (chkIgnoreCondStore.Checked) lKnownCapabilities |= fCapabilities.condstore;
                if (chkIgnoreQResync.Checked) lKnownCapabilities |= fCapabilities.qresync;

                if (chkIgnoreLiteralPlus.Checked) lKnownCapabilities |= fCapabilities.literalplus | fCapabilities.literalminus;
                if (chkIgnoreBinary.Checked) lKnownCapabilities |= fCapabilities.binary;
                if (chkIgnoreIdle.Checked) lKnownCapabilities |= fCapabilities.idle;
                if (chkIgnoreSASLIR.Checked) lKnownCapabilities |= fCapabilities.sasl_ir;

                if (chkIgnoreESearch.Checked) lKnownCapabilities |= fCapabilities.esearch;
                if (chkIgnoreSort.Checked) lKnownCapabilities |= fCapabilities.sort;
                if (chkIgnoreSortDisplay.Checked) lKnownCapabilities |= fCapabilities.sortdisplay;
                //if (chkIgnoreThreadOrderedSubject.Checked) lKnownCapabilities |= fCapabilities.threadorderedsubject;
                //if (chkIgnoreThreadReferences.Checked) lKnownCapabilities |= fCapabilities.threadreferences;
                if (chkIgnoreESort.Checked) lKnownCapabilities |= fCapabilities.esort;

                if (chkIgnoreMultiAppend.Checked) lKnownCapabilities |= fCapabilities.multiappend;
                if (chkIgnoreCatenate.Checked) lKnownCapabilities |= fCapabilities.catenate;

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
            ZSetControlState();
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
            ZLoadBatchSizerConfiguration(mClient.FetchBodyReadConfiguration, txtFRMin, txtFRMax, txtFRMaxTime, txtFRInitial);
            ZLoadBatchSizerConfiguration(mClient.FetchBodyWriteConfiguration, txtFWMin, txtFWMax, txtFWMaxTime, txtFWInitial);

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
            txtAppendTargetBufferSize.Text = mClient.AppendTargetBufferSize.ToString();
            ZLoadBatchSizerConfiguration(mClient.AppendStreamReadConfiguration, txtASMin, txtASMax, txtASMaxTime, txtASInitial);
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

        private void gbxFetchBodyRead_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxFetchBodyRead, txtFRMin, txtFRMax, txtFRInitial, e);
        }

        private void gbxFetchBodyWrite_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxFetchBodyWrite, txtFWMin, txtFWMax, txtFWInitial, e);
        }

        private void gbxNetworkWrite_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxNetworkWrite, txtNWMin, txtNWMax, txtNWInitial, e);
        }

        private void gbxAppendStreamRead_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxAppendStreamRead, txtASMin, txtASMax, txtASInitial, e);
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

                List<eResponseTextContext> lTypes = new List<eResponseTextContext>();

                if (chkRTTOKGreeting.Checked) lTypes.Add(eResponseTextContext.greetingok);
                if (chkRTTPreAuthGreeting.Checked) lTypes.Add(eResponseTextContext.greetingpreauth);
                if (chkRTTByeGreeting.Checked) lTypes.Add(eResponseTextContext.greetingbye);
                if (chkRTTContinue.Checked) lTypes.Add(eResponseTextContext.continuerequest);
                if (chkRTTBye.Checked) lTypes.Add(eResponseTextContext.bye);
                if (chkRTTInformation.Checked) lTypes.Add(eResponseTextContext.information);
                if (chkRTTWarning.Checked) lTypes.Add(eResponseTextContext.warning);
                if (chkRTTError.Checked) lTypes.Add(eResponseTextContext.error);
                if (chkRTTSuccess.Checked) lTypes.Add(eResponseTextContext.success);
                if (chkRTTFailure.Checked) lTypes.Add(eResponseTextContext.failure);
                if (chkRTTProtocolError.Checked) lTypes.Add(eResponseTextContext.protocolerror);
                if (chkRTTAuthenticationCancelled.Checked) lTypes.Add(eResponseTextContext.authenticationcancelled);

                List<eResponseTextCode> lCodes = new List<eResponseTextCode>();

                if (chkRTCNone.Checked) lCodes.Add(eResponseTextCode.none);
                if (chkRTCOther.Checked) lCodes.Add(eResponseTextCode.other);
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

        private void cmdASSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.AppendStreamReadConfiguration = new cBatchSizerConfiguration(int.Parse(txtASMin.Text), int.Parse(txtASMax.Text), int.Parse(txtASMaxTime.Text), int.Parse(txtASInitial.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set append stream read error\n{ex}");
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
            fMessageProperties lProperties = 0;

            if (chkPEnvelope.Checked) lProperties |= fMessageProperties.envelope;
            if (chkPSent.Checked) lProperties |= fMessageProperties.sent;
            if (chkPSubject.Checked) lProperties |= fMessageProperties.subject;
            if (chkPMessageId.Checked) lProperties |= fMessageProperties.messageid;
            if (chkPFlags.Checked) lProperties |= fMessageProperties.flags;
            if (chkPAnswered.Checked) lProperties |= fMessageProperties.answered;
            if (chkPFlagged.Checked) lProperties |= fMessageProperties.flagged;
            if (chkPSubmitted.Checked) lProperties |= fMessageProperties.submitted;
            if (chkPReceived.Checked) lProperties |= fMessageProperties.received;
            if (chkPSize.Checked) lProperties |= fMessageProperties.size;
            if (chkPUID.Checked) lProperties |= fMessageProperties.uid;
            if (chkPModSeq.Checked) lProperties |= fMessageProperties.modseq;
            if (chkPBodyStructure.Checked) lProperties |= fMessageProperties.bodystructure;
            if (chkPAttachments.Checked) lProperties |= fMessageProperties.attachments;
            if (chkPPlainTextSize.Checked) lProperties |= fMessageProperties.plaintextsizeinbytes;
            if (chkPReferences.Checked) lProperties |= fMessageProperties.references;
            if (chkPImportance.Checked) lProperties |= fMessageProperties.importance;

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

        private void cmdATSet_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.AppendTargetBufferSize = int.Parse(txtAppendTargetBufferSize.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Set timeout error\n{ex}");
            }
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
    }
}
