using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;
using work.bacome.trace;

namespace testharness2
{
    public partial class frmClient : Form
    {
        private cTrace.cContext mRootContext;
        private cIMAPClient mClient;

        public frmClient(string pInstanceName)
        {
            mRootContext = Program.Trace.NewRoot(pInstanceName, true);
            mClient = new cIMAPClient(pInstanceName);
            InitializeComponent();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
