using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.trace;

namespace testharness2
{
    public partial class frmStart : Form
    {
        private cTrace.cContext mRootContext = Program.Trace.NewRoot(nameof(frmStart), true);
        private int mTimer = 7;

        public frmStart()
        {
            InitializeComponent();
        }

        private void tmrProofOfASync_Tick(object sender, EventArgs e)
        {
            mTimer++;
            lblProofOfASync.Text = mTimer.ToString();
        }

        private void cmdCreate_Click(object sender, EventArgs e)
        {
            // TODO
        }

        private void cmdTests_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmStart), nameof(cmdTests_Click));
            try { cTests.Tests(false, lContext); }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void cmdQuickTests_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmStart), nameof(cmdQuickTests_Click));
            try { cTests.Tests(true, lContext); }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void cmdCurrentTest_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmStart), nameof(cmdCurrentTest_Click));
            try { cTests.CurrentTest(lContext); }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
    }
}
