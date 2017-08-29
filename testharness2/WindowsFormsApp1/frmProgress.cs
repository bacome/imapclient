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

        public CancellationToken CancellationToken => mCancellationTokenSource.Token;

        public void SetCount(int pCount)
        {
            if (pCount == 0) prg.Style = ProgressBarStyle.Marquee;
            else
            {
                prg.Maximum = pCount;
                prg.Style = ProgressBarStyle.Continuous;
            }
        }

        public void Increment(int pCount)
        {
            prg.Increment(pCount);
        }

        public void Cancel()
        {
            prg.Style = ProgressBarStyle.Marquee;
            mCancellationTokenSource.Cancel();
            cmdCancel.Enabled = false;
        }

        private void frmProgress_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e) => Cancel();
    }
}
