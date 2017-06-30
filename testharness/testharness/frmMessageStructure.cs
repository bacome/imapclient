using System;
using System.Windows.Forms;
using work.bacome.imapclient;
using work.bacome.trace;

namespace testharness
{
    public partial class frmMessageStructure : Form
    {
        private cTrace.cContext mRootContext = Program.Trace.NewRoot(nameof(frmMessageStructure), true);

        private TreeNode mCurrentBodyStructureNode = null;

        public frmMessageStructure()
        {
            InitializeComponent();
        }

        private async void cmdInspect_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmMessageStructure), nameof(cmdInspect_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            if (lTag == null) return;

            string lText;

            if (lTag.BodyPart != null)
            {
                if (!(lTag?.BodyPart is cSinglePartBody lPart)) return;

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

        private async void cmdInspectRaw_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmMessageStructure), nameof(cmdInspect_Click));

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

        private void cmdDownload_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmMessageStructure), nameof(cmdDownload_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            var lPart = lTag?.BodyPart as cSinglePartBody;

            if (lPart == null) return;

            var lSaveFileDialog = new SaveFileDialog();

            if (lPart.Disposition?.FileName != null) lSaveFileDialog.FileName = lPart.Disposition?.FileName;

            if (lSaveFileDialog.ShowDialog() == DialogResult.OK) frmDownloading.Download(lTag.Message, lPart.Section, lPart.DecodingRequired, lSaveFileDialog.FileName, (int)lPart.SizeInBytes);
        }

        private void cmdDownloadRaw_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmMessageStructure), nameof(cmdDownloadRaw_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            var lPart = lTag?.BodyPart as cSinglePartBody;

            if (lPart == null) return;

            var lSaveFileDialog = new SaveFileDialog();

            lSaveFileDialog.DefaultExt = "txt";

            if (lSaveFileDialog.ShowDialog() == DialogResult.OK) frmDownloading.Download(lTag.Message, lPart.Section, eDecodingRequired.none, lSaveFileDialog.FileName, (int)lPart.SizeInBytes);
        }

        private int mtvwBodyStructure_AfterSelect = 0;

        private async void tvwBodyStructure_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmMessageStructure), nameof(tvwBodyStructure_AfterSelect));

            // defend against re-entry during the awaits
            var ltvwBodyStructure_AfterSelect = ++mtvwBodyStructure_AfterSelect;

            if (!ReferenceEquals(tvwBodyStructure.SelectedNode, mCurrentBodyStructureNode))
            {
                mCurrentBodyStructureNode = tvwBodyStructure.SelectedNode;

                rtxPartDetail.Clear();

                var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

                cmdInspect.Enabled = false;
                cmdInspectRaw.Enabled = false;
                cmdDownload.Enabled = false;
                cmdDownloadRaw.Enabled = false;

                if (lTag?.BodyPart != null)
                {
                    if (lTag.BodyPart.Disposition != null)
                    {
                        rtxPartDetail.AppendText($"Disposition: {lTag.BodyPart.Disposition.Type} {lTag.BodyPart.Disposition.FileName} {lTag.BodyPart.Disposition.Size} {lTag.BodyPart.Disposition.CreationDate}\n");

                        if (lTag.BodyPart.Disposition.TypeCode == eDispositionTypeCode.attachment)
                        {
                            cmdDownload.Enabled = true;
                            cmdDownloadRaw.Enabled = true;
                        }
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
                        else if (lTag.BodyPart is cMessageBodyPart lMessageBodyPart)
                        {
                            ZDisplayEnvelope(lMessageBodyPart.Envelope);
                        }

                        cmdInspect.Enabled = true;
                    }
                }
                else if (lTag?.Section != null)
                {
                    var lSectionText = await lTag.Message.FetchAsync(lTag.Section);

                    // check if we've been re-entered during the await
                    if (ltvwBodyStructure_AfterSelect != mtvwBodyStructure_AfterSelect) return;

                    rtxPartDetail.AppendText(lSectionText);
                }
                else if (lTag?.Message != null)
                {
                    rtxPartDetail.AppendText($"Message Size: {lTag.Message.Size}\n");

                    await lTag.Message.FetchAsync(fMessageProperties.envelope);

                    // check if we've been re-entered during the await
                    if (ltvwBodyStructure_AfterSelect != mtvwBodyStructure_AfterSelect) return;

                    ZDisplayEnvelope(lTag.Message.Handle.Envelope);

                    cmdInspect.Enabled = true;
                }
            }
        }

        private void ZDisplayEnvelope(cEnvelope pEnvelope)
        {
            rtxPartDetail.AppendText($"Message Id: {pEnvelope.MessageId}\n");
            rtxPartDetail.AppendText($"Sent: {pEnvelope.Sent}\n");
            Program.DisplayAddresses(rtxPartDetail, "From: ", pEnvelope.From);
            Program.DisplayAddresses(rtxPartDetail, "Sender: ", pEnvelope.Sender);
            Program.DisplayAddresses(rtxPartDetail, "ReplyTo: ", pEnvelope.ReplyTo);
            Program.DisplayAddresses(rtxPartDetail, "To: ", pEnvelope.To);
            Program.DisplayAddresses(rtxPartDetail, "CC: ", pEnvelope.CC);
            Program.DisplayAddresses(rtxPartDetail, "BCC: ", pEnvelope.BCC);
            rtxPartDetail.AppendText($"Subject: {pEnvelope.Subject}\n");
            rtxPartDetail.AppendText($"Base Subject: {pEnvelope.BaseSubject}\n");
            rtxPartDetail.AppendText($"InReplyTo: {pEnvelope.InReplyTo}\n");
        }
    }
}