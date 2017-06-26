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
        private cBodyPart mBodyPart;

        public frmDownloading()
        {
            InitializeComponent();
        }

        private void frmDownloading_Load(object sender, EventArgs e)
        {

        }

        public static void Download(cMessage pMessage, cBodyPart pBodyPart, string pFileName, CancellationTokenSource pCancellationTokenSource)
        {
            using (var lDownloading = new frmDownloading())
            {
                lDownloading.lblFileName.Text = "Downloading " + pFileName;
                lDownloading.mMessage = pMessage;
                lDownloading.mBodyPart = pBodyPart;
                lDownloading.mFileName = pFileName;
                lDownloading.mCancellationTokenSource = pCancellationTokenSource;
                lDownloading.ShowDialog();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mCancellationTokenSource.Cancel();
        }

        private async void frmDownloading_Shown(object sender, EventArgs e)
        {
            try
            {
                using (var lFileStream = new FileStream(mFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await mMessage.FetchAsync(mBodyPart, lFileStream);
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
    }
}
