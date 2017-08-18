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
using work.bacome.trace;

namespace testharness2
{
    public partial class frmClient : Form
    {
        private readonly cTrace.cContext mRootContext;
        private readonly cIMAPClient mClient;
        private CancellationTokenSource mCancellationTokenSource = null;
        private frmNetworkActivity mNetworkActivity = null;

        public frmClient(string pInstanceName)
        {
            mRootContext = Program.Trace.NewRoot(pInstanceName, true);
            mClient = new cIMAPClient(pInstanceName);
            InitializeComponent();
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(mClient_PropertyChanged), e);
            if (e.PropertyName == nameof(cIMAPClient.ConnectionState) || e.PropertyName == nameof(cIMAPClient.AsyncCount)) ZSetControlState(lContext);
        }

        private void ZSetControlState(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(frmClient), nameof(ZSetControlState));

            lblState.Text = mClient.ConnectionState.ToString();

            if (mClient.IsUnconnected)
            {
                gbxConnect.Enabled = true;
                ZSetControlStateCredentials(lContext);
                cmdCancel.Text = "Cancel";
                cmdCancel.Enabled = false;
                cmdDisconnect.Enabled = false;
            }
            else
            {
                gbxConnect.Enabled = false;

                var lAsyncCount = mClient.AsyncCount;

                if (lAsyncCount == 0)
                {
                    cmdCancel.Text = "Cancel";
                    cmdCancel.Enabled = false;

                    if (mCancellationTokenSource != null && mCancellationTokenSource.IsCancellationRequested)
                    {
                        mCancellationTokenSource.Dispose();
                        mCancellationTokenSource = null;
                    }
                }

                if (mCancellationTokenSource == null)
                {
                    mCancellationTokenSource = new CancellationTokenSource();
                    mClient.CancellationToken = mCancellationTokenSource.Token;
                }
                
                if (lAsyncCount != 0)
                {
                    cmdCancel.Text = "Cancel " + lAsyncCount.ToString();
                    cmdCancel.Enabled = !mCancellationTokenSource.IsCancellationRequested;
                }

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

        private void ZSetControlStateCredentials(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(frmClient), nameof(ZSetControlStateCredentials));

            txtTrace.Enabled = rdoCredAnon.Checked;
            txtUserId.Enabled = rdoCredBasic.Checked;
            txtPassword.Enabled = rdoCredBasic.Checked;

            gbxTLSRequirement.Enabled = rdoCredBasic.Checked || rdoCredBasic.Checked;
            chkTryIfNotAdvertised.Enabled = rdoCredBasic.Checked || rdoCredBasic.Checked;
        }

        private void ZSetControlStateIdle(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(frmClient), nameof(ZSetControlStateIdle));

            txtIdleStartDelay.Enabled = chkIdleAuto.Checked;
            txtIdleRestartInterval.Enabled = chkIdleAuto.Checked;
            txtIdlePollInterval.Enabled = chkIdleAuto.Checked;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(cmdCancel_Click));
            mCancellationTokenSource.Cancel();
            ZSetControlState(lContext);
        }

        private async void cmdConnect_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(cmdConnect_Click));

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
                MessageBox.Show($"Connect error\n{ex}");
            }
        }

        private async void cmdDisconnect_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(cmdDisconnect_Click));
            
            try { await mClient.DisconnectAsync(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Disconnect error\n{ex}");
            }
        }

        private void ZCredCheckedChanged(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(ZCredCheckedChanged));
            ZSetControlStateCredentials(lContext);
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

        private void ZValEnableChanged(object sender, EventArgs e)
        {
            // TODO: check if this is required
            if (!((Control)sender).Enabled) erp.SetError((Control)sender, null);
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

        private void frmClient_Load(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(frmClient_Load));

            Text = "imapclient testharness - client - " + mClient.InstanceName;
            ZSetControlState(lContext);
            ZSetControlStateIdle(lContext);
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

            ZLoadFetchConfig(mClient.FetchAttributesConfiguration, txtFAMin, txtFAMax, txtFAMaxTime, txtFAInitial);
            ZLoadFetchConfig(mClient.FetchBodyReadConfiguration, txtFRMin, txtFRMax, txtFRMaxTime, txtFRInitial);
            ZLoadFetchConfig(mClient.FetchBodyWriteConfiguration, txtFWMin, txtFWMax, txtFWMaxTime, txtFWInitial);
        }

        private void ZLoadFetchConfig(cFetchSizeConfiguration pConfig, TextBox pMin, TextBox pMax, TextBox pMaxTime, TextBox pInitial)
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
            if (mNetworkActivity != null)
            {
                try { mNetworkActivity.Close(); }
                catch { }
            }

            if (mClient != null)
            {
                try { mClient.Dispose(); }
                catch { }
            }

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }
        }

        private void chkIdleAuto_CheckedChanged(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(chkIdleAuto_CheckedChanged));
            ZSetControlStateIdle(lContext);
        }

        private void gbxFetchAttributes_Validating(object sender, CancelEventArgs e)
        {
            ZValFetchConfig(gbxFetchAttributes, txtFAMin, txtFAMax, txtFAInitial, e);
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
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(cmdIdleSet_Click));

            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            try
            {
                mClient.IdleConfiguration = new cIdleConfiguration(int.Parse(txtIdleStartDelay.Text), int.Parse(txtIdleRestartInterval.Text), int.Parse(txtIdlePollInterval.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Set idle error\n{ex}");
            }
        }

        private void cmdEvents_Click(object sender, EventArgs e)
        {
            ;?;
        }

        private void ZChildFormsNew(Form pForm)
        {
            pForm.FormClosed += ZChildFormClosed;
            pForm.Show();
        }

        private void ZChildFormClosed(object sender, EventArgs e)
        {
            if (!(sender is Form lForm)) return;
            lForm.FormClosed -= ZChildFormClosed;
        }

        private void ZChildFormsFocus(Form pForm)
        {
            if (pForm.WindowState == FormWindowState.Minimized) pForm.WindowState = FormWindowState.Normal;
            pForm.Focus();
        }

        private void cmdNetworkActivity_Click(object sender, EventArgs e)
        {
            if (mNetworkActivity == null || mNetworkActivity.IsDisposed)
            {
                mNetworkActivity = new frmNetworkActivity(mClient);
                ZChildFormsNew(mNetworkActivity);
            }
            else ZChildFormsFocus(mNetworkActivity);
        }

        private void cmdResponseText_Click(object sender, EventArgs e)
        {
            ;?;
        }
    }
}
