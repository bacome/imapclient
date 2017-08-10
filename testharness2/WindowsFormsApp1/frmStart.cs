using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testharness2
{
    public partial class frmStart : Form
    {
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
    }
}
