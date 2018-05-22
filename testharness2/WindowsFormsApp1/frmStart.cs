using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.mailclient.support;

namespace testharness2
{
    public partial class frmStart : Form
    {
        private static cTrace kTrace = new cTrace("testharness2");

        private cTrace.cContext mRootContext = kTrace.NewRoot(nameof(frmStart), true);
        private int mTimer = 7;
        private int mInstanceNumber = 7;

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
            string lInstanceName = txtInstanceName.Text.Trim();
            if (lInstanceName.Length == 0) lInstanceName = "Client_" + (++mInstanceNumber).ToString();
            var lClient = new frmClient(lInstanceName);
            Program.Centre(lClient, this);
            lClient.Show();
        }

        private void cmdTests_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmStart), nameof(cmdTests_Click));

            try
            {
                tab.Enabled = false;
                cTests.Tests(false, lContext);
                MessageBox.Show(this, "all tests passed");
            }
            catch (Exception ex) { MessageBox.Show(this, ex.ToString()); }
            finally
            {
                tab.Enabled = true;
            }
        }

        private void cmdQuickTests_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmStart), nameof(cmdQuickTests_Click));

            try
            {
                tab.Enabled = false;
                cTests.Tests(true, lContext);
                MessageBox.Show(this, "quick tests passed");
            }
            catch (Exception ex) { MessageBox.Show(this, ex.ToString()); }
            finally
            {
                tab.Enabled = true;
            }
        }

        private void cmdCurrentTest_Click(object sender, EventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmStart), nameof(cmdCurrentTest_Click));

            try
            {
                tab.Enabled = false;
                cTests.CurrentTest(lContext);
                MessageBox.Show(this, "current test passed");

            }
            catch (Exception ex) { MessageBox.Show(this, ex.ToString()); }
            finally
            {
                tab.Enabled = true;
            }
        }

        private void cmdDirectory_Click(object sender, EventArgs e)
        {

        }
    }
}
