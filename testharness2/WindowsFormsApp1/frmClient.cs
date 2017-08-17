﻿using System;
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
        private readonly string mInstanceName;
        private readonly cTrace.cContext mRootContext;
        private readonly cIMAPClient mClient;
        private CancellationTokenSource mCancellationTokenSource = null;

        public frmClient(string pInstanceName)
        {
            mInstanceName = pInstanceName;
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

                    if (mCancellationTokenSource == null)
                    {
                        mCancellationTokenSource = new CancellationTokenSource();
                        mClient.CancellationToken = mCancellationTokenSource.Token;
                    }
                }
                else
                {
                    cmdCancel.Text = "Cancel " + lAsyncCount.ToString();
                    cmdCancel.Enabled = !mCancellationTokenSource.IsCancellationRequested;
                }

                cmdDisconnect.Enabled = mClient.IsConnected;
            }
        }

        private void ZSetControlStateCredentials(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(frmClient), nameof(ZSetControlStateCredentials));

            txtTrace.Enabled = rdoCredAnon.Checked;
            txtUserId.Enabled = rdoCredBasic.Checked;
            txtPassword.Enabled = rdoCredBasic.Checked;
            chkRequireTLS.Enabled = rdoCredBasic.Checked || rdoCredBasic.Checked;
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

        private void cmdConnect_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmClient), nameof(cmdConnect_Click));

            /*
            ValidateChildren

            try
            {
                mClient.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connect error\n{ex}");
            } */
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

            if (!int.TryParse(lSender.Text, out var i) || i < 100 || i > 99999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "time should be a number 100 .. 99999");
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
            Text = "imapclient testharness - client - " + mInstanceName;
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

        private void frmClient_FormClosing(object sender, FormClosingEventArgs e)
        {
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

            // to allow closing with validation errors
            e.Cancel = false;
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
    }
}
