using System;
using System.Windows.Forms;
using work.bacome.imapclient;
using work.bacome.trace;

namespace testharness
{
    public partial class frmMessageStructure : Form
    {
        private cTrace.cContext mRootContext = Program.Trace.NewRoot(nameof(frmMessageStructure), true);

        public frmMessageStructure()
        {
            InitializeComponent();
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

        private void cmdDownload_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdDownload_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            var lPart = lTag?.BodyPart as cSinglePartBody;

            if (lPart == null) return;

            var lSaveFileDialog = new SaveFileDialog();

            if (lPart.Disposition?.FileName != null) lSaveFileDialog.FileName = lPart.Disposition?.FileName;

            if (lSaveFileDialog.ShowDialog() == DialogResult.OK) frmDownloading.Download(lTag.Message, lPart.Section, lPart.DecodingRequired, lSaveFileDialog.FileName, (int)lPart.SizeInBytes);
        }

        private void cmdDownloadRaw_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(cmdDownloadRaw_Click));

            var lTag = tvwBodyStructure.SelectedNode?.Tag as cTVWBodyStructureNodeTag;

            var lPart = lTag?.BodyPart as cSinglePartBody;

            if (lPart == null) return;

            var lSaveFileDialog = new SaveFileDialog();

            lSaveFileDialog.DefaultExt = "txt";

            if (lSaveFileDialog.ShowDialog() == DialogResult.OK) frmDownloading.Download(lTag.Message, lPart.Section, eDecodingRequired.none, lSaveFileDialog.FileName, (int)lPart.SizeInBytes);
        }
    }
}