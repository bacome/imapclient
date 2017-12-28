using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testharness2
{
    public partial class frmProgress : Form
    {
        private readonly string mTitle;
        private readonly long mInitialMaximum;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();

        private long mMaximum = 0;
        private long mValue = 0;
        private bool mComplete = false;

        public frmProgress(string pTitle, long pInitialMaximum = 0)
        {
            mTitle = pTitle;
            mInitialMaximum = pInitialMaximum;
            InitializeComponent();
        }

        private void frmProgress_Load(object sender, EventArgs e)
        {
            Text = mTitle;
            SetMaximum(mInitialMaximum);
        }

        public void ShowAndFocus(Form pCentringOnThis)
        {
            Program.Centre(this, pCentringOnThis);
            Show();
            Focus();
        }

        public CancellationToken CancellationToken => mCancellationTokenSource.Token;

        public void SetMaximum(long pMaximum)
        {
            mMaximum = pMaximum;

            if (mMaximum <= 0) prg.Style = ProgressBarStyle.Marquee;
            else
            {
                prg.Maximum = 100;
                prg.Style = ProgressBarStyle.Continuous;
            }

            ZSetValue();
        }

        public void SetMaximum(int pMaximum) => SetMaximum((long)pMaximum);

        public void Increment(int pIncrement)
        {
            mValue += pIncrement;
            ZSetValue();
        }

        private void ZSetValue()
        {
            if (mMaximum <= 0) Text = mTitle + " - " + mValue.ToString();
            else
            {
                double lValue = (double)mValue / (double)mMaximum;

                if (lValue > 1) prg.Value = 100;
                else prg.Value = (int)(lValue * 100);

                Text = mTitle + " - " + lValue.ToString("##0.0%");
            }
        }

        public void Cancel()
        {
            mCancellationTokenSource.Cancel();
            prg.Style = ProgressBarStyle.Marquee;
            cmdCancel.Enabled = false;
        }

        private void cmdCancel_Click(object sender, EventArgs e) => Cancel();

        public void Complete()
        {
            mComplete = true;
            Close();
        }

        private void frmProgress_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cancel();
            if (!mComplete && e.CloseReason != CloseReason.ApplicationExitCall && e.CloseReason != CloseReason.TaskManagerClosing && e.CloseReason != CloseReason.WindowsShutDown) e.Cancel = true;
        }

        private void frmProgress_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }
        }
    }
}
