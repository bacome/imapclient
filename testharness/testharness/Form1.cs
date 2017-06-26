using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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
        private int? mDaysToGet = null;
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
                ZEnable(false, cmdDisconnect, cmdDisconnectAsync, cmdCancel, dgvMessageHeaders, tvwBodyStructure, rtxPartDetail, cmdInspect, cmdInspectRaw, cmdDownload);
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
            ZDGVMessageHeadersCoordinateChildrenAsync(lContext); // if the datasource is null this runs synchronously

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
            var lBindingSource = dgvMessageHeaders.DataSource as BindingSource;

            if (lBindingSource == null) return;

            for (int i = 0; i < lBindingSource.List.Count; i++)
            {
                var lMessageHeader = lBindingSource.List[i] as cMessageHeader;

                if (lMessageHeader != null && lMessageHeader.Message.Handle == pHandle)
                {
                    lBindingSource.ResetItem(i);
                    break;
                }
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
            if (chkIgnoreBinary.Checked) lIgnoreCapabilities |= fCapabilities.Binary;
            mIMAPClient.IgnoreCapabilities = lIgnoreCapabilities;
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
            if (!int.TryParse(txtPort.Text, out var i) || i < 1 || i > 9999)
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
            if (!int.TryParse(((TextBox)sender).Text, out var i) || i < 100 || i > 99999)
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

        private async Task ZDGVMessageHeadersCoordinateChildrenAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(Form1), nameof(ZDGVMessageHeadersCoordinateChildrenAsync));

            var lDataBoundItem = dgvMessageHeaders.CurrentCell?.OwningRow.DataBoundItem;

            if (!ReferenceEquals(lDataBoundItem, mCurrentMessageHeaderDataBoundItem))
            {
                mCurrentMessageHeaderDataBoundItem = lDataBoundItem;

                tvwBodyStructure.Enabled = true;
                tvwBodyStructure.BeginUpdate();
                tvwBodyStructure.Nodes.Clear();
                tvwBodyStructure.EndUpdate();

                var lMessageHeader = lDataBoundItem as cMessageHeader;

                if (lMessageHeader != null)
                {
                    try
                    { 
                        // make sure that we have the size and the bodystructure info
                        await lMessageHeader.Message.FetchAsync(fMessageProperties.size | fMessageProperties.bodystructureex);
                    }
                    catch (Exception ex)
                    {
                        rtxInfo.AppendText($"an error occurred: {ex}\n");
                        rtxInfo.ScrollToCaret();
                    }

                    if (lMessageHeader.Message.BodyStructureEx != null)
                    {
                        tvwBodyStructure.BeginUpdate();
                        tvwBodyStructure.Nodes.Clear();
                        var lRoot = tvwBodyStructure.Nodes.Add("root");
                        lRoot.Tag = new cTVWBodyStructureNodeTag(lMessageHeader.Message);
                        ZTVWBodyStructureAddSection(lRoot, lMessageHeader.Message, "header", cSection.Header);
                        ZTVWBodyStructureAddPart(lRoot, lMessageHeader.Message, lMessageHeader.Message.BodyStructureEx);
                        tvwBodyStructure.EndUpdate();
                    }
                }
            }

            if (!ReferenceEquals(tvwBodyStructure.SelectedNode, mCurrentBodyStructureNode))
            {
                mCurrentBodyStructureNode = tvwBodyStructure.SelectedNode;

                rtxPartDetail.Enabled = true;
                rtxPartDetail.Clear();

                var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

                cmdInspect.Enabled = false;
                cmdInspectRaw.Enabled = false;
                cmdDownload.Enabled = false;

                if (lTag?.BodyPart != null)
                {
                    if (lTag.BodyPart.Disposition != null)
                    {
                        rtxPartDetail.AppendText($"Disposition: {lTag.BodyPart.Disposition.Type} {lTag.BodyPart.Disposition.FileName} {lTag.BodyPart.Disposition.Size} {lTag.BodyPart.Disposition.CreationDate}\n");
                        if (lTag.BodyPart.Disposition.TypeCode == eDispositionTypeCode.attachment) cmdDownload.Enabled = true;
                    }

                    if (lTag.BodyPart.Languages != null) rtxPartDetail.AppendText($"Languages: {lTag.BodyPart.Languages}\n");
                    if (lTag.BodyPart.Location != null) rtxPartDetail.AppendText($"Location: {lTag.BodyPart.Location}\n");

                    if (lTag.BodyPart is cSinglePartBody lSingleBodyPart)
                    {
                        rtxPartDetail.AppendText($"Content Id: {lSingleBodyPart.ContentId}\n");
                        rtxPartDetail.AppendText($"Description: {lSingleBodyPart.Description}\n");
                        rtxPartDetail.AppendText($"ContentTransferEncoding: {lSingleBodyPart.ContentTransferEncoding}\n");
                        rtxPartDetail.AppendText($"Size: {lSingleBodyPart.SizeInBytes}\n");

                        if (lTag.BodyPart is cTextBodyPart lTextBodyPart)
                        {
                            rtxPartDetail.AppendText($"Charset: {lTextBodyPart.Charset}\n");
                            cmdInspectRaw.Enabled = true; // to see it not decoded
                        }
                        else if (lTag.BodyPart is cMessageBodyPart lMessageBodyPart) ZDisplayEnvelope(lMessageBodyPart.Envelope);

                        cmdInspect.Enabled = true;
                    }
                }
                else if (lTag?.Section != null) rtxPartDetail.AppendText(await lTag.Message.FetchAsync(lTag.Section));
                else if (lTag?.Message != null)
                {
                    rtxPartDetail.AppendText($"Message Size: {lTag.Message.Size}\n");
                    ZDisplayEnvelope(lTag.Message.Envelope);
                    cmdInspect.Enabled = true;
                }
            }
        }

        private void ZDisplayEnvelope(cEnvelope pEnvelope)
        {
            if (pEnvelope == null) return;
            rtxPartDetail.AppendText($"Message Id: {pEnvelope.MessageId}\n");
            rtxPartDetail.AppendText($"Sent: {pEnvelope.Sent}\n");
            ZDisplayAddresses("From: ", pEnvelope.From);
            ZDisplayAddresses("Sender: ", pEnvelope.Sender);
            ZDisplayAddresses("ReplyTo: ", pEnvelope.ReplyTo);
            ZDisplayAddresses("To: ", pEnvelope.To);
            ZDisplayAddresses("CC: ", pEnvelope.CC);
            ZDisplayAddresses("BCC: ", pEnvelope.BCC);
            rtxPartDetail.AppendText($"Subject: {pEnvelope.Subject}\n");
            rtxPartDetail.AppendText($"Base Subject: {pEnvelope.BaseSubject}\n");
            rtxPartDetail.AppendText($"InReplyTo: {pEnvelope.InReplyTo}\n");
        }

        private void ZDisplayAddresses(string pAddressType, cAddresses pAddresses)
        {
            if (pAddresses == null) return;

            rtxPartDetail.AppendText(pAddressType);

            foreach (var lAddress in pAddresses)
            {
                if (lAddress.DisplayName != null) rtxPartDetail.AppendText(lAddress.DisplayName);
                if (lAddress is cEmailAddress lEmailAddress) rtxPartDetail.AppendText($"<{lEmailAddress.DisplayAddress}>");
                rtxPartDetail.AppendText(", ");
            }

            rtxPartDetail.AppendText("\n");
        }

        private void ZTVWBodyStructureAddSection(TreeNode pParent, cMessage pMessage, string pText, cSection pSection)
        {
            var lNode = pParent.Nodes.Add(pText);
            lNode.Tag = new cTVWBodyStructureNodeTag(pMessage, pSection);
        }

        private void ZTVWBodyStructureAddPart(TreeNode pParent, cMessage pMessage, cBodyPart pBodyPart)
        {
            string lPart;
            if (pBodyPart.Section.Part == null) lPart = pBodyPart.Section.TextPart.ToString();
            else if (pBodyPart.Section.TextPart == eSectionPart.all) lPart = pBodyPart.Section.Part;
            else lPart = pBodyPart.Section.Part + "." + pBodyPart.Section.TextPart.ToString();

            var lNode = pParent.Nodes.Add(lPart + ": " + pBodyPart.Type + "/" + pBodyPart.SubType);
            lNode.Tag = new cTVWBodyStructureNodeTag(pMessage, pBodyPart);

            if (pBodyPart is cMessageBodyPart lMessage)
            {
                ZTVWBodyStructureAddSection(lNode, pMessage, "header", new cSection(pBodyPart.Section.Part, eSectionPart.header));
                ZTVWBodyStructureAddPart(lNode, pMessage, lMessage.BodyStructureEx);
            }
            else if (pBodyPart is cMultiPartBody lMultiPartPart)
            {
                if (pBodyPart.Section.Part != null) ZTVWBodyStructureAddSection(lNode, pMessage, "mime", new cSection(pBodyPart.Section.Part, eSectionPart.mime));
                foreach (var lBodyPart in lMultiPartPart.Parts) ZTVWBodyStructureAddPart(lNode, pMessage, lBodyPart);
            }
            else
            {
                ZTVWBodyStructureAddSection(lNode, pMessage, "mime", new cSection(pBodyPart.Section.Part, eSectionPart.mime));
            }
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

            try
            {
                await lTag.Mailbox.SelectAsync();
                await lTag.Mailbox.StatusAsync(fStatusAttributes.unseen); // force the unseen count to be calculated

                if (mDaysToGet == null)
                {
                    dgvMessageHeaders.Enabled = false;
                    dgvMessageHeaders.DataSource = null;
                }
                else
                {
                    var lMessages = await lTag.Mailbox.SearchAsync(cFilter.Received > DateTime.Today.AddDays(-mDaysToGet.Value), new cSort(cSortItem.ReceivedDesc), fMessageProperties.flags | fMessageProperties.received | fMessageProperties.envelope);

                    BindingSource lBindingSource = new BindingSource();
                    foreach (var lMessage in lMessages) lBindingSource.Add(new cMessageHeader(lMessage));
                    dgvMessageHeaders.Enabled = true;
                    dgvMessageHeaders.DataSource = lBindingSource;
                }
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
                return;
            }
        }

        private async void cmdInspect_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdInspect_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            if (lTag == null) return;

            string lText;

            if (lTag.BodyPart != null)
            {
                if (!(lTag?.BodyPart is cSinglePartBody lPart)) return;


                // handle special cases (e.g. jpeg etc ...)


                if (lPart.SizeInBytes > 10000)
                {
                    MessageBox.Show("The text is too long to show");
                    return;
                }

                try
                {
                    lText = await lTag.Message.FetchAsync(lPart);
                }
                catch (Exception ex)
                {
                    lContext.TraceException(ex);
                    MessageBox.Show($"a problem occurred: {ex}");
                    return;
                }
            }
            else 
            {
                // this is the message root
                if (lTag.Message.Size > 10000)
                {
                    MessageBox.Show("The text is too long to show");
                    return;
                }

                try
                {
                    lText = await lTag.Message.FetchAsync(cSection.All);
                }
                catch (Exception ex)
                {
                    lContext.TraceException(ex);
                    MessageBox.Show($"a problem occurred: {ex}");
                    return;
                }
            }

            frmMessagePartText lForm = new frmMessagePartText();
            lForm.rtx.AppendText(lText);
            lForm.Show();
        }

        private async void dgvMessageHeaders_CurrentCellChanged(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(dgvMessageHeaders_CurrentCellChanged));
            await ZDGVMessageHeadersCoordinateChildrenAsync(lContext);
        }

        private async void cmdInspectRaw_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdInspect_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            var lPart = lTag?.BodyPart as cSinglePartBody;

            if (lPart == null) return;

            if (lPart.SizeInBytes > 10000)
            {
                MessageBox.Show("The text is too long to show");
                return;
            }

            string lText;

            try
            {
                lText = await lTag.Message.FetchAsync(lPart.Section);
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
        }

        private void txtDaysToGet_Validating(object sender, CancelEventArgs e)
        {
            if (int.TryParse(((TextBox)sender).Text, out var lDaysToGet) || lDaysToGet < 1 || lDaysToGet > 100)
            {
                mDaysToGet = lDaysToGet;
            }
            else
            {
                mDaysToGet = null;
                e.Cancel = true;
                erp.SetError((Control)sender, "days of messages should be a number 1 .. 100");
            }
        }

        private void cmdDownload_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdDownload_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            var lPart = lTag?.BodyPart as cSinglePartBody;

            if (lPart == null) return;

            var lSaveFileDialog = new SaveFileDialog();

            if (lPart.Disposition?.FileName != null) lSaveFileDialog.FileName = lPart.Disposition?.FileName;

            if (lSaveFileDialog.ShowDialog() == DialogResult.OK) 
            {
                frmDownloading.Download(lTag.Message, lTag.BodyPart, lSaveFileDialog.FileName, mCancellationTokenSource);
                ZSetCancellationToken();
            }
        }
    }
}
