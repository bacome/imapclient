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

namespace testharness2
{
    public partial class frmResponseText : Form
    {
        private readonly cIMAPClient mClient;
        private readonly int mMaxMessages;
        private readonly IList<eIMAPResponseTextContext> mContexts;
        private readonly IList<eIMAPResponseTextCode> mCodes;
        private readonly Queue<string> mQueue = new Queue<string>();
        private readonly StringBuilder mBuilder = new StringBuilder();
        private bool mRefreshRequired = false;

        public frmResponseText(cIMAPClient pClient, int pMaxMessages, IList<eIMAPResponseTextContext> pContexts, IList<eIMAPResponseTextCode> pCodes)
        {
            mClient = pClient;
            mMaxMessages = pMaxMessages;
            mContexts = pContexts;
            mCodes = pCodes;
            InitializeComponent();
        }

        private void frmResponseText_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - response text - " + mClient.InstanceName + " [" + mMaxMessages + "]";
            mClient.ResponseText += mClient_ResponseText;
        }

        private void mClient_ResponseText(object sender, cIMAPResponseTextEventArgs e)
        {
            if (mContexts.Contains(e.Context) || mCodes.Contains(e.Text.Code))
            {
                mQueue.Enqueue(e.ToString());
                if (mQueue.Count > mMaxMessages) mQueue.Dequeue();
                mRefreshRequired = true;
            }
        }

        private void frmResponseText_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.ResponseText -= mClient_ResponseText;
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            if (mRefreshRequired)
            {
                foreach (var lString in mQueue) mBuilder.AppendLine(lString);

                rtx.Text = mBuilder.ToString();
                rtx.Select(mBuilder.Length, 0);
                rtx.ScrollToCaret();

                mBuilder.Clear();

                mRefreshRequired = false;
            }
        }
    }
}
