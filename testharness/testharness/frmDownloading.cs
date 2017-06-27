using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness
{
    public partial class frmDownloading : Form
    {
        private CancellationTokenSource mCancellationTokenSource;
        private string mFileName;
        private cMessage mMessage;
        private cSection mSection;
        private eDecodingRequired mDecodingRequired;

        public frmDownloading()
        {
            InitializeComponent();
        }

        public static void Download(cMessage pMessage, cSection pSection, eDecodingRequired pDecodingRequired, string pFileName, CancellationTokenSource pCancellationTokenSource)
        {
            using (var lDownloading = new frmDownloading())
            {
                lDownloading.lblFileName.Text = "Downloading " + pFileName;
                lDownloading.mMessage = pMessage;
                lDownloading.mSection = pSection;
                lDownloading.mDecodingRequired = pDecodingRequired;
                lDownloading.mFileName = pFileName;
                lDownloading.mCancellationTokenSource = pCancellationTokenSource;
                lDownloading.ShowDialog();
            }
        }

        private async void frmDownloading_Shown(object sender, EventArgs e)
        {
            try
            {
                using (var lFileStream = new FileStream(mFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await mMessage.FetchAsync(mSection, mDecodingRequired, lFileStream);
                }
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
            mCancellationTokenSource.Cancel();
        }
    }
}
