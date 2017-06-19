using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using work.bacome.imapclient;
using work.bacome.trace;

namespace testharness
{
    public partial class Form1 : Form
    {
        private int mTimer = 0;
        private cTrace.cContext mRootContext = Program.Trace.NewRoot(nameof(Form1), true);
        private cIMAPClient mIMAPClient = null;
        private CancellationTokenSource mCancellationTokenSource = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void ZEnable(bool pEnable, params Control[] pControls)
        {
            foreach (var lControl in pControls)
            {
                if (!pEnable) erp.SetError(lControl, null);
                lControl.Enabled = pEnable;
            }
        }

        private void ZSetState()
        {
            bool lDisconnected = (mIMAPClient == null || mIMAPClient.State == cIMAPClient.eState.notconnected || mIMAPClient.State == cIMAPClient.eState.disconnected);

            // enable things that should be enabled when disconnected
            ZEnable(lDisconnected, pnlConnection, pnlCredentials, cmdTests, cmdTestsQuick, cmdConnect, cmdConnectAsync);

            // enable things that should be enabled when connected
            ZEnable(!lDisconnected, cmdApply, tvwMailboxes);

            if (lDisconnected)
            {
                rtxState.Text = "disconnected";
                ZEnable(rdoCredAnon.Checked, txtTrace);
                ZEnable(rdoCredBasic.Checked, txtUserId, txtPassword);
                ZEnable(false, cmdDisconnect, cmdDisconnectAsync, cmdCancel);
            }
            else
            {
                rtxState.Text = $"State: {mIMAPClient.State}\n";
                if (mIMAPClient.ServerId != null) rtxState.AppendText($"connected to server {mIMAPClient.ServerId}\n");
                if (mIMAPClient.ConnectedAccountId != null) rtxState.AppendText($"connected using {mIMAPClient.ConnectedAccountId}\n");
                if (mIMAPClient.SelectedMailboxId != null) rtxState.AppendText($"selected mailbox {mIMAPClient.SelectedMailboxId}\n");

                ZEnable(mIMAPClient.AsyncCount == 0, cmdDisconnect, cmdDisconnectAsync);

                if (mIMAPClient.AsyncCount > 0)
                {
                    rtxState.AppendText($"{mIMAPClient.AsyncCount} operations in progress\n");
                    ZEnable(mCancellationTokenSource != null && !mCancellationTokenSource.IsCancellationRequested, cmdCancel);
                }
                else ZEnable(false, cmdCancel);
            }
        }

        private void ZSetState(object sender, EventArgs e)
        {
            ZSetState();
        }

        private const string kTVWPleaseWait = "<please wait>";

        private void ZTVWAddNamespaces(string pClass, ReadOnlyCollection<cNamespace> pNamespaces)
        {
            var lNode = tvwMailboxes.Nodes.Add(pClass);

            if (pNamespaces == null) return;

            if (pNamespaces.Count == 1)
            {
                lNode.Tag = new cTVWNodeTag(pNamespaces[0], lNode.Nodes.Add(kTVWPleaseWait));
                return;
            }

            foreach (var lNamespace in pNamespaces)
            {
                if (lNamespace.Prefix.Length == 0) lNode.Tag = new cTVWNodeTag(lNamespace, null);
                else
                {
                    var lChildNode = lNode.Nodes.Add(lNamespace.Prefix); // should remove the trailing delimitier if there is one
                    lChildNode.Tag = new cTVWNodeTag(lNamespace, lChildNode.Nodes.Add(kTVWPleaseWait));
                }
            }
        }

        private void ZConnectInit(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(Form1), nameof(ZConnectInit));

            // validate the form
            if (!ValidateChildren(ValidationConstraints.Enabled)) throw new Exception("there are some errors that need to be fixed first");

            // tidy up the old client
            if (mIMAPClient != null) mIMAPClient.Dispose();

            // create the new client
            mIMAPClient = new cIMAPClient();

            // clear the tree
            tvwMailboxes.Nodes.Clear();

            // set the server
            mIMAPClient.SetServer(txtHost.Text.Trim(), int.Parse(txtPort.Text), chkSSL.Checked);

            // set the credentials to use
            if (rdoCredNone.Checked) mIMAPClient.SetNoCredentials();
            else if (rdoCredAnon.Checked) mIMAPClient.SetAnonymousCredentials(txtTrace.Text.Trim());
            else if (rdoCredBasic.Checked) mIMAPClient.SetPlainCredentials(txtUserId.Text.Trim(), txtPassword.Text.Trim());

            ZApplySettings();

            // hook up events
            mIMAPClient.PropertyChanged += ZSetState;
            mIMAPClient.ResponseText += mIMAPClient_ResponseText;
            mIMAPClient.MailboxPropertyChanged += mIMAPClient_MailboxPropertyChanged;
        }

        private void mIMAPClient_MailboxPropertyChanged(object sender, cMailboxPropertyChangedEventArgs e)
        {
            foreach (TreeNode lNode in tvwMailboxes.Nodes) ZUpdateNode(lNode, e.MailboxId);
        }

        private void ZUpdateNode(TreeNode pNode, cMailboxId pMailboxId)
        {
            if (pNode.Tag is cTVWNodeTag lTag && lTag.Mailbox != null && lTag.Mailbox.MailboxId == pMailboxId)
            {
                if (lTag.Mailbox.Selected)
                {
                    if (pNode.NodeFont == null) pNode.NodeFont = new Font(tvwMailboxes.Font, FontStyle.Bold);
                }
                else
                {
                    if (pNode.NodeFont != null) pNode.NodeFont = null;
                }

                var lMessages = lTag.Mailbox.Properties?.Messages;
                var lUnseen = lTag.Mailbox.Properties?.Unseen;

                if (lMessages == null) pNode.Text = lTag.Mailbox.Name;
                else pNode.Text = lTag.Mailbox.Name + " (" + lUnseen + "/" + lMessages + ")";

                return;
            }

            foreach (TreeNode lNode in pNode.Nodes) ZUpdateNode(lNode, pMailboxId);
        }

        private void ZConnectComplete(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(Form1), nameof(ZConnectComplete));

            tvwMailboxes.BeginUpdate();

            var lNamespaces = mIMAPClient.Namespaces;

            ZTVWAddNamespaces("Personal", lNamespaces.Personal);
            ZTVWAddNamespaces("Other Users", lNamespaces.OtherUsers);
            ZTVWAddNamespaces("Shared", lNamespaces.Shared);

            tvwMailboxes.EndUpdate();
        }

        private void ZApplySettings()
        {
            mIMAPClient.Timeout = int.Parse(txtTimeouts.Text);

            if (chkAutoIdle.Checked) mIMAPClient.IdleConfiguration = new cIdleConfiguration(int.Parse(txtStartDelay.Text), int.Parse(txtIdleRestartInterval.Text), int.Parse(txtPollInterval.Text));

            fCapabilities lIgnoreCapabilities = 0;
            if (chkIgnoreNamespace.Checked) lIgnoreCapabilities |= fCapabilities.Namespace;

            if (lIgnoreCapabilities != 0) mIMAPClient.IgnoreCapabilities = lIgnoreCapabilities;
        }

        private void ZSetCancellationToken()
        {
            bool lSet = false;

            if (mCancellationTokenSource == null)
            {
                mCancellationTokenSource = new CancellationTokenSource();
                lSet = true;
            }
            else if (mCancellationTokenSource.IsCancellationRequested)
            {
                mCancellationTokenSource.Dispose();
                mCancellationTokenSource = new CancellationTokenSource();
                lSet = true;
            }

            if (lSet) mIMAPClient.CancellationToken = mCancellationTokenSource.Token;
        }

        private void mIMAPClient_ResponseText(object sender, cResponseTextEventArgs e)
        {
            rtxResponseText.AppendText(e.ToString());
            rtxResponseText.AppendText("\n");
            rtxResponseText.ScrollToCaret();
        }

        private void cmdTests_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdTests_Click));

            try
            {
                cIMAPClient.cTests.Tests(false);
                MessageBox.Show("all tests passed");
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"an error occurred: {ex}");
            }
        }

        private void cmdConnect_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdConnect_Click));

            try
            {
                ZConnectInit(lContext);
                mIMAPClient.Connect();
                ZConnectComplete(lContext);
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }
        }

        private async void cmdConnectAsync_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdConnectAsync_Click));

            try
            {
                ZConnectInit(lContext);
                ZSetCancellationToken();
                await mIMAPClient.ConnectAsync();
                ZConnectComplete(lContext);
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }
        }

        private void txtPort_Validating(object sender, CancelEventArgs e)
        {
            int i;

            if (!int.TryParse(txtPort.Text, out i) || i < 1 || i > 9999)
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "port should be a number 1 .. 9999");
            }
        }

        private void TextBoxNotBlank(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(((TextBox)sender).Text))
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "a host name is required");
            }
        }

        private void TextBoxIsMilliseconds(object sender, CancelEventArgs e)
        {
            int i;

            if (!int.TryParse(((TextBox)sender).Text, out i) || i < 100 || i > 99999)
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "time should be a number 100 .. 99999");
            }
        }

        private void ControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void rdoCredNone_CheckedChanged(object sender, EventArgs e)
        {
            ZSetState();
        }

        private void rdoCredAnon_CheckedChanged(object sender, EventArgs e)
        {
            ZSetState();
        }

        private void rdoCredBasic_CheckedChanged(object sender, EventArgs e)
        {
            ZSetState();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ZSetState();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(Form1_FormClosing));

            try
            {
                if (mIMAPClient != null)
                {
                    mIMAPClient.Dispose();
                    mIMAPClient = null;
                }

                if (mCancellationTokenSource != null)
                {
                    mCancellationTokenSource.Dispose();
                    mCancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }

            // to allow closing with validation errors
            e.Cancel = false;
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            mTimer++;
            lblTimer.Text = mTimer.ToString();
        }

        private void cmdTestsQuick_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdTestsQuick_Click));

            try
            {
                cIMAPClient.cTests.Tests(true);
                MessageBox.Show("all tests passed");
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }
        }

        private async void cmdDisconnectAsync_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdDisconnectAsync_Click));

            try
            {
                ZSetCancellationToken();
                await mIMAPClient.DisconnectAsync();
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }
        }

        private void cmdDisconnect_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdDisconnect_Click));
            try { mIMAPClient.Disconnect(); }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            mCancellationTokenSource.Cancel();
            ZSetState();
        }

        private void cmdApply_Click(object sender, EventArgs e)
        {
            // ideally I'd like to just validate the settings tab, but that doesn't seen possible
            //  as it isn't a containercontrol

            if (!ValidateChildren(ValidationConstraints.Enabled)) throw new Exception("there are some errors that need to be fixed first");

            ZApplySettings();
        }

        private async void tvwMailboxes_AfterExpand(object sender, TreeViewEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(tvwMailboxes_AfterExpand));

            if (!(e.Node.Tag is cTVWNodeTag lTag)) return;

            if (lTag.State != cTVWNodeTag.eState.neverexpanded) return;

            lTag.State = cTVWNodeTag.eState.expanding;

            List<cMailboxListItem> lMailboxes;

            try
            {
                if (lTag.Namespace != null) lMailboxes = await lTag.Namespace.ListAsync();
                else if (lTag.Mailbox != null) lMailboxes = await lTag.Mailbox.ListAsync();
                else lMailboxes = null;
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
                return;
            }

            if (lMailboxes != null && lMailboxes.Count != 0)
            {
                foreach (var lListItem in lMailboxes)
                {
                    var lNode = e.Node.Nodes.Add(lListItem.Name);

                    TreeNode lPleaseWait;

                    if (lListItem.HasChildren != false) lPleaseWait = lNode.Nodes.Add(kTVWPleaseWait);
                    else lPleaseWait = null;

                    lNode.Tag = new cTVWNodeTag(lListItem.Mailbox, lListItem.CanSelect ?? false, lPleaseWait);
                }
            }

            e.Node.Nodes.Remove(lTag.PleaseWait);

            lTag.State = cTVWNodeTag.eState.expanded;
        }

        private async void tvwMailboxes_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(tvwMailboxes_NodeMouseClick));

            if (!(e.Node.Tag is cTVWNodeTag lTag) || lTag.Mailbox == null || !lTag.CanSelect) return;

            try
            {
                await lTag.Mailbox.SelectAsync();
                await lTag.Mailbox.StatusAsync(fStatusAttributes.unseen); // force the unseen count to be calculated
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
                return;
            }
        }

        private void cmdTestsCurrent_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdTestsCurrent_Click));

            try
            {
                cIMAPClient.cTests.CurrentTest();
                MessageBox.Show("current test passed");
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"an error occurred: {ex}");
            }
        }
    }
}
