using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness
{
    public partial class frmDownloading : Form
    {
        private cMessage mMessage;
        private cSection mSection;
        private eDecodingRequired mDecodingRequired;
        private string mFileName;
        private int mSize;

        private CancellationTokenSource mCancellationTokenSource;

        public frmDownloading()
        {
            InitializeComponent();
        }

        public static void Download(cMessage pMessage, cSection pSection, eDecodingRequired pDecodingRequired, string pFileName, int pSize)
        {
            using (var lDownloading = new frmDownloading())
            {
                lDownloading.Text = pFileName;
                lDownloading.mMessage = pMessage;
                lDownloading.mSection = pSection;
                lDownloading.mDecodingRequired = pDecodingRequired;
                lDownloading.mFileName = pFileName;
                lDownloading.mSize = pSize;
                lDownloading.ShowDialog();
            }
        }

        private async void frmDownloading_Shown(object sender, EventArgs e)
        {
            prg.Maximum = mSize;

            using (var lCancellationTokenSource = new CancellationTokenSource())
            {
                mCancellationTokenSource = lCancellationTokenSource;

                var lFC = new cFetchControl(mCancellationTokenSource.Token, prg.Increment);

                try
                {
                    using (var lFileStream = new FileStream(mFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await mMessage.FetchAsync(mSection, mDecodingRequired, lFileStream, lFC);
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
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            mCancellationTokenSource.Cancel();
        }

        private void frmDownloading_FormClosing(object sender, FormClosingEventArgs e)
        {
            mCancellationTokenSource.Cancel();
        }
    }
}
