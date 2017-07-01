using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness
{
    public partial class frmDownload : Form
    {
        private cMessage mMessage;
        private cSection mSection;
        private eDecodingRequired mDecodingRequired;

        private cAttachment mAttachment;

        private string mFileName;
        private int mSize;
        private CancellationTokenSource mCancellationTokenSource;

        public frmDownload()
        {
            InitializeComponent();
        }

        public static void Download(cMessage pMessage, cSinglePartBody pPart, eDecodingRequired pDecodingRequired)
        {
            var lSaveFileDialog = new SaveFileDialog();
            if (pPart.Disposition?.FileName != null) lSaveFileDialog.FileName = pPart.Disposition?.FileName;
            if (lSaveFileDialog.ShowDialog() != DialogResult.OK) return;

            using (var lCancellationTokenSource = new CancellationTokenSource())
            using (var lDownloading = new frmDownload())
            {
                lDownloading.Text = lSaveFileDialog.FileName;

                lDownloading.mMessage = pMessage;
                lDownloading.mSection = pPart.Section;
                lDownloading.mDecodingRequired = pDecodingRequired;

                lDownloading.mAttachment = null;

                lDownloading.mFileName = lSaveFileDialog.FileName;
                lDownloading.mSize = (int)pPart.SizeInBytes;
                lDownloading.mCancellationTokenSource = lCancellationTokenSource;

                lDownloading.ShowDialog();
            }
        }

        public static void Download(cAttachment pAttachment)
        {
            var lSaveFileDialog = new SaveFileDialog();
            if (pAttachment.FileName != null) lSaveFileDialog.FileName = pAttachment.FileName;
            if (lSaveFileDialog.ShowDialog() != DialogResult.OK) return;

            using (var lCancellationTokenSource = new CancellationTokenSource())
            using (var lDownloading = new frmDownload())
            {
                lDownloading.Text = lSaveFileDialog.FileName;

                lDownloading.mMessage = null;
                lDownloading.mSection = null;
                lDownloading.mDecodingRequired = 0;

                lDownloading.mAttachment = pAttachment;

                lDownloading.mFileName = lSaveFileDialog.FileName;
                lDownloading.mSize = (int)pAttachment.SizeInBytes;
                lDownloading.mCancellationTokenSource = lCancellationTokenSource;

                lDownloading.ShowDialog();
            }
        }

        private async void frmDownloading_Shown(object sender, EventArgs e)
        {
            prg.Maximum = mSize;

            var lFC = new cFetchControl(mCancellationTokenSource.Token, prg.Increment);

            try
            {
                using (var lFileStream = new FileStream(mFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (mAttachment == null) await mMessage.FetchAsync(mSection, mDecodingRequired, lFileStream, lFC);
                    else await mAttachment.FetchAsync(lFileStream, lFC);
                }
            }
            catch (OperationCanceledException) when (mCancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"a problem occurred: {ex}");
                return;
            }
            finally
            {
                Hide();
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmDownloading_FormClosing(object sender, FormClosingEventArgs e)
        {
            mCancellationTokenSource.Cancel();
        }
    }
}
