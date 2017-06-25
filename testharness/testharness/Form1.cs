using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private BindingSource mMessageHeadersBindingSource = null;
        private List<cMessageHeader> mMessageHeaders = null;
        private object mCurrentMessageHeaderDataBoundItem = null;
        private TreeNode mCurrentBodyStructureNode = null;

        public Form1()
        {
            InitializeComponent();

            // initilise the grid
            dgvMessageHeaders.AutoGenerateColumns = false;
            dgvMessageHeaders.Columns.Add(LColumn(nameof(cMessageHeader.Expunged)));
            dgvMessageHeaders.Columns.Add(LColumn(nameof(cMessageHeader.Deleted)));
            dgvMessageHeaders.Columns.Add(LColumn(nameof(cMessageHeader.Seen)));
            dgvMessageHeaders.Columns.Add(LColumn(nameof(cMessageHeader.Received)));
            dgvMessageHeaders.Columns.Add(LColumn(nameof(cMessageHeader.From)));
            dgvMessageHeaders.Columns.Add(LColumn(nameof(cMessageHeader.Subject)));

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = new DataGridViewTextBoxCell();
                return lResult;
            }
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
                ZEnable(false, cmdDisconnect, cmdDisconnectAsync, cmdCancel, dgvMessageHeaders, tvwBodyStructure, rtxPartDetail, cmdInspect, cmdInspectRaw);
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

        private void ZTVWMailboxesAddNamespaces(string pClass, ReadOnlyCollection<cNamespace> pNamespaces)
        {
            var lNode = tvwMailboxes.Nodes.Add(pClass);

            if (pNamespaces == null) return;

            if (pNamespaces.Count == 1)
            {
                lNode.Tag = new cTVWMailboxesNodeTag(pNamespaces[0], lNode.Nodes.Add(kTVWPleaseWait));
                return;
            }

            foreach (var lNamespace in pNamespaces)
            {
                if (lNamespace.Prefix.Length == 0) lNode.Tag = new cTVWMailboxesNodeTag(lNamespace, null);
                else
                {
                    var lChildNode = lNode.Nodes.Add(lNamespace.Prefix); // should remove the trailing delimitier if there is one
                    lChildNode.Tag = new cTVWMailboxesNodeTag(lNamespace, lChildNode.Nodes.Add(kTVWPleaseWait));
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

            // clear the displays
            tvwMailboxes.Nodes.Clear();
            dgvMessageHeaders.DataSource = null;
            ZDGVMessageHeadersClearChildren(lContext);

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
            mIMAPClient.MailboxMessageDelivery += mIMAPClient_MailboxMessageDelivery;
            mIMAPClient.MessageExpunged += mIMAPClient_MessageExpunged;
            mIMAPClient.MessagePropertiesSet += mIMAPClient_MessagePropertiesSet;
        }

        private void mIMAPClient_MessagePropertiesSet(object sender, cMessagePropertiesSetEventArgs e)
        {
            if ((e.Set & fMessageProperties.flags) != 0) ZRefreshMessageHeader(e.Handle);
        }

        private void mIMAPClient_MessageExpunged(object sender, cMessageExpungedEventArgs e)
        {
            ZRefreshMessageHeader(e.Handle);
        }

        private void ZRefreshMessageHeader(iMessageHandle pHandle)
        {
            if (mMessageHeaders == null) return;

            for (int i = 0; i < mMessageHeaders.Count; i++)
                if (ReferenceEquals(mMessageHeaders[i].Message.Handle, pHandle))
                {
                    mMessageHeadersBindingSource.ResetItem(i);
                    break;
                }
        }

        private async void mIMAPClient_MailboxMessageDelivery(object sender, cMailboxMessageDeliveryEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(mIMAPClient_MailboxMessageDelivery));

            try
            {
                // keep the unseen count up to date
                await mIMAPClient.FetchAsync(e.MailboxId, e.Handles, fMessageProperties.flags);

                // could now add these to the grid - TODO
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }
        }

        private void mIMAPClient_MailboxPropertyChanged(object sender, cMailboxPropertyChangedEventArgs e)
        {
            foreach (TreeNode lNode in tvwMailboxes.Nodes) ZTVWMailboxesUpdateNode(lNode, e.MailboxId);
        }

        private void ZTVWMailboxesUpdateNode(TreeNode pNode, cMailboxId pMailboxId)
        {
            if (pNode.Tag is cTVWMailboxesNodeTag lTag && lTag.Mailbox != null && lTag.Mailbox.MailboxId == pMailboxId)
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

            foreach (TreeNode lNode in pNode.Nodes) ZTVWMailboxesUpdateNode(lNode, pMailboxId);
        }

        private void ZConnectComplete(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(Form1), nameof(ZConnectComplete));

            tvwMailboxes.BeginUpdate();

            var lNamespaces = mIMAPClient.Namespaces;

            ZTVWMailboxesAddNamespaces("Personal", lNamespaces.Personal);
            ZTVWMailboxesAddNamespaces("Other Users", lNamespaces.OtherUsers);
            ZTVWMailboxesAddNamespaces("Shared", lNamespaces.Shared);

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
                cIMAPClient._Tests(false);
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
                cIMAPClient._Tests(true);
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

            if (!(e.Node.Tag is cTVWMailboxesNodeTag lTag)) return;

            if (lTag.State != cTVWMailboxesNodeTag.eState.neverexpanded) return;

            lTag.State = cTVWMailboxesNodeTag.eState.expanding;

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

                    lNode.Tag = new cTVWMailboxesNodeTag(lListItem.Mailbox, lListItem.CanSelect ?? false, lPleaseWait);
                }
            }

            e.Node.Nodes.Remove(lTag.PleaseWait);

            lTag.State = cTVWMailboxesNodeTag.eState.expanded;
        }

        private void cmdTestsCurrent_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdTestsCurrent_Click));

            try
            {
                cIMAPClient._Tests_Current();
                MessageBox.Show("current test passed");
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"an error occurred: {ex}");
            }
        }

        private void ZDGVMessageHeadersClearChildren(cTrace.cContext pParentContext)
        {
            mCurrentMessageHeaderDataBoundItem = null;
            tvwBodyStructure.Nodes.Clear();
            mCurrentBodyStructureNode = null;
            rtxPartDetail.Clear();
            cmdInspect.Enabled = false;
            cmdInspectRaw.Enabled = false;
        }

        private async Task ZDGVMessageHeadersCoordinateChildrenAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(Form1), nameof(ZDGVMessageHeadersCoordinateChildrenAsync));

            if (dgvMessageHeaders.CurrentCell == null)
            {
                ZDGVMessageHeadersClearChildren(lContext);
                return;
            }

            ;?;
 || !(dgvMessageHeaders.CurrentCell.OwningRow.DataBoundItem is cMessageHeader lMessageHeader)

            if (!ReferenceEquals(dgvMessageHeaders.CurrentCell.OwningRow.DataBoundItem, mCurrentMessageHeader))
            { 
                mCurrentMessageHeader = lMessageHeader;

                tvwBodyStructure.Enabled = true;
                tvwBodyStructure.BeginUpdate();

                try
                {
                    // clear before the await to prevent a second pass through here from doing anything
                    tvwBodyStructure.Nodes.Clear();

                    rtxInfo.AppendText("await+\n");
                    rtxInfo.ScrollToCaret();

                    // make sure that we have the size and the bodystructure info
                    await mCurrentMessageHeader.Message.FetchAsync(fMessageProperties.size | fMessageProperties.bodystructureex);

                    rtxInfo.AppendText("await-\n");
                    rtxInfo.ScrollToCaret();

                    if (mCurrentMessageHeader.Message.BodyStructureEx != null)
                    {
                        var lRoot = tvwBodyStructure.Nodes.Add("root");
                        ZTVWBodyStructureAddPart(lRoot, mCurrentMessageHeader.Message.BodyStructureEx);
                    }
                }
                catch (Exception ex)
                {
                    lContext.TraceException(ex);
                    MessageBox.Show($"an error occurred: {ex}");
                }
                finally
                {
                    tvwBodyStructure.EndUpdate();
                }
            }

            // now coordinate the rtx to the selected node

            if (!ReferenceEquals(tvwBodyStructure.SelectedNode, mCurrentBodyStructureNode))
            {
                mCurrentBodyStructureNode = tvwBodyStructure.SelectedNode;

            ;?;
            mCurrentBodyStructureNode = null;



            if (tvwBodyStructure.SelectedNode == null)
            {
                rtxPartDetail.Clear();
                cmdInspect.Enabled = false;
                cmdInspectRaw.Enabled = false;
                return;
            }

            if (tvwBodyStructure.SelectedNode.Tag == null)
            {
                // root node




                ;?;
            }

            var lBodyPart = tvwBodyStructure.SelectedNode.Tag as cBodyPart;

            if (!ReferenceEquals(lBodyPart, mCurrentBodyPart))
            {
                mCurrentBodyPart = lBodyPart;

                rtxPartDetail.Clear();

                if (lBodyPart == null)
                {
                    cmdInspect.Enabled = false;
                    cmdInspectRaw.Enabled = false;
                }
                else
                {
                    rtxPartDetail.Enabled = true;

                    if (lBodyPart.Disposition != null) rtxPartDetail.AppendText($"Disposition: {lBodyPart.Disposition.Type} {lBodyPart.Disposition.FileName} {lBodyPart.Disposition.Size} {lBodyPart.Disposition.CreationDate}\n");
                    if (lBodyPart.Languages != null) rtxPartDetail.AppendText($"Languages: {lBodyPart.Languages}\n");
                    if (lBodyPart.Location != null) rtxPartDetail.AppendText($"Location: {lBodyPart.Location}\n");

                    if (mCurrentBodyPart.Section == cSection.All) rtxPartDetail.AppendText($"Message Size: {mCurrentMessage.Size}\n");

                    if (lBodyPart is cSinglePartBody lSingleBodyPart)
                    {
                        rtxPartDetail.AppendText($"Content Id: {lSingleBodyPart.ContentId}\n");
                        rtxPartDetail.AppendText($"Description: {lSingleBodyPart.Description}\n");
                        rtxPartDetail.AppendText($"ContentTransferEncoding: {lSingleBodyPart.ContentTransferEncoding}\n");
                        rtxPartDetail.AppendText($"Size: {lSingleBodyPart.SizeInBytes}\n");

                        if (lBodyPart is cTextBodyPart lTextBodyPart)
                        {
                            rtxPartDetail.AppendText($"Charset: {lTextBodyPart.Charset}\n");
                        }
                        else if (lBodyPart is cMessageBodyPart lMessageBodyPart)
                        {
                            var lFrom = lMessageBodyPart.Envelope?.From;
                            if (lFrom != null) rtxPartDetail.AppendText($"From: {lFrom[0].DisplayName}\n");
                            rtxPartDetail.AppendText($"Subject: {lMessageBodyPart.Envelope.Subject}\n");
                        }
                    }

                    cmdInspect.Enabled = true;
                    cmdInspectRaw.Enabled = false;
                }
            } */
        }

        private void ZTVWBodyStructureAddPart(TreeNode pParent, cBodyPart pBodyPart)
        {
            string lPart;
            if (pBodyPart.Section.Part == null) lPart = pBodyPart.Section.TextPart.ToString();
            else if (pBodyPart.Section.TextPart == eSectionPart.all) lPart = pBodyPart.Section.Part;
            else lPart = pBodyPart.Section.Part + "." + pBodyPart.Section.TextPart.ToString();

            var lNode = pParent.Nodes.Add(lPart + ": " + pBodyPart.Type + "/" + pBodyPart.SubType);
            lNode.Tag = pBodyPart;

            if (pBodyPart is cMessageBodyPart lMessage) ZTVWBodyStructureAddPart(lNode, lMessage.BodyStructureEx);
            else if (pBodyPart is cMultiPartBody lMultiPartPart) foreach (var lBodyPart in lMultiPartPart.Parts) ZTVWBodyStructureAddPart(lNode, lBodyPart);
        }

        private async void tvwBodyStructure_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(tvwBodyStructure_AfterSelect));
            await ZDGVMessageHeadersCoordinateChildrenAsync(lContext);
        }

        private async void tvwMailboxes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(tvwMailboxes_AfterSelect));

            if (!(e.Node.Tag is cTVWMailboxesNodeTag lTag) || lTag.Mailbox == null || !lTag.CanSelect || lTag.Mailbox.Selected) return;

            // should single thread these, after getting access to run should check that the clicked node is still the currently selected one
            //  if not then just exit
            //   TODO

            try
            {
                await lTag.Mailbox.SelectAsync();
                await lTag.Mailbox.StatusAsync(fStatusAttributes.unseen); // force the unseen count to be calculated
                var lMessages = await lTag.Mailbox.SearchAsync(cFilter.Received > DateTime.Today.AddDays(-100), new cSort(cSortItem.ReceivedDesc), fMessageProperties.flags | fMessageProperties.received | fMessageProperties.envelope);
                mMessageHeaders = new List<cMessageHeader>();
                foreach (var lMessage in lMessages) mMessageHeaders.Add(new cMessageHeader(lMessage));
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
                return;
            }

            mMessageHeadersBindingSource = new BindingSource();
            mMessageHeadersBindingSource.DataSource = mMessageHeaders;

            dgvMessageHeaders.Enabled = true;
            dgvMessageHeaders.DataSource = mMessageHeadersBindingSource;
        }

        private async void cmdInspect_Click(object sender, EventArgs e)
        {
            /*
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdInspect_Click));

            if (mCurrentMessage == null || mCurrentBodyPart == null) return;



            if (mCurrentBodyPart is cSinglePartBody lSingleBodyPart)
            {
                // handle special cases (e.g. jpeg etc ...)

                if (lSingleBodyPart.SizeInBytes > 10000)
                {
                    MessageBox.Show("The text is too long to show");
                    return;
                }

                string lText;

                try
                {
                    lText = await mCurrentMessage.FetchAsync(mCurrentBodyPart);
                }
                catch (Exception ex)
                {
                    lContext.TraceException(ex);
                    MessageBox.Show($"a problem occurred: {ex}");
                    return;
                }

                frmMessagePartText lForm = new frmMessagePartText();
                lForm.rtx.AppendText(lText);
                lForm.Show();
                return;
            }

            MessageBox.Show("This message should never show"); */
        }

        private async void dgvMessageHeaders_CurrentCellChanged(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(dgvMessageHeaders_CurrentCellChanged));
            await ZDGVMessageHeadersCoordinateChildrenAsync(lContext);
        }

        private void cmdInspectRaw_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdInspect_Click));
            MessageBox.Show("This message should never show");
        }
    }
}
