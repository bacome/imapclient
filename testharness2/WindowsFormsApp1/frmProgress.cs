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
        private readonly int mCount;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private int mTotal = 0;
        private bool mComplete = false;

        public frmProgress(string pTitle, int pCount = 0)
        {
            mTitle = pTitle;
            mCount = pCount;
            InitializeComponent();
        }

        private void frmProgress_Load(object sender, EventArgs e)
        {
            Text = mTitle;
            SetCount(mCount);
        }

        public void ShowAndFocus(Form pCentringOnThis)
        {
            Program.Centre(this, pCentringOnThis);
            Show();
            Focus();
        }

        public CancellationToken CancellationToken => mCancellationTokenSource.Token;

        public void SetCount(int pCount)
        {
            if (pCount == 0) prg.Style = ProgressBarStyle.Marquee;
            else
            {
                prg.Maximum = pCount;
                prg.Value = mTotal;
                prg.Style = ProgressBarStyle.Continuous;
            }
        }

        public void Increment(int pCount)
        {
            mTotal += pCount;
            lblTotal.Text = mTotal.ToString();
            if (prg.Style != ProgressBarStyle.Marquee) prg.Increment(pCount);
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
