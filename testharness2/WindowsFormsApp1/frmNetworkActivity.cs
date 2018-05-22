using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.mailclient;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmNetworkActivity : Form
    {
        private readonly cIMAPClient mClient;
        private readonly int mMaxMessages;
        private readonly Queue<string> mQueue = new Queue<string>();
        private readonly StringBuilder mBuilder = new StringBuilder();
        private bool mRefreshRequired = false;

        public frmNetworkActivity(cIMAPClient pClient, int pMaxMessages)
        {
            mClient = pClient;
            mMaxMessages = pMaxMessages;
            InitializeComponent();
        }

        private void frmNetworkActivity_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - network activity - " + mClient.InstanceName + " [" + mMaxMessages + "]";
            mClient.NetworkReceive += mClient_NetworkReceive;
            mClient.NetworkSend += mClient_NetworkSend;
        }

        private void mClient_NetworkReceive(object sender, cNetworkReceiveEventArgs e)
        {
            mQueue.Enqueue(e.ToString());
            if (mQueue.Count > mMaxMessages) mQueue.Dequeue();
            mRefreshRequired = true;
        }

        private void mClient_NetworkSend(object sender, cNetworkSendEventArgs e)
        {
            mQueue.Enqueue(e.ToString());
            if (mQueue.Count > mMaxMessages) mQueue.Dequeue();
            mRefreshRequired = true;
        }

        private void frmNetworkActivity_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.NetworkReceive -= mClient_NetworkReceive;
            mClient.NetworkSend -= mClient_NetworkSend;
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
