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
        private readonly IList<eResponseTextType> mTypes;
        private readonly IList<eResponseTextCode> mCodes;
        private readonly Queue<cResponseTextEventArgs> mQueue = new Queue<cResponseTextEventArgs>();
        private readonly StringBuilder mBuilder = new StringBuilder();
        private bool mRefreshRequired = false;

        public frmResponseText(cIMAPClient pClient, int pMaxMessages, IList<eResponseTextType> pTypes, IList<eResponseTextCode> pCodes)
        {
            mClient = pClient;
            mMaxMessages = pMaxMessages;
            mTypes = pTypes;
            mCodes = pCodes;
            InitializeComponent();
        }

        private void frmResponseText_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - response text - " + mClient.InstanceName + " [" + mMaxMessages + "]";
            mClient.ResponseText += mClient_ResponseText;
        }

        private void mClient_ResponseText(object sender, cResponseTextEventArgs e)
        {
            if (mTypes.Contains(e.TextType) || mCodes.Contains(e.Text.Code))
            {
                mQueue.Enqueue(e);
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
                foreach (var lArgs in mQueue) mBuilder.AppendLine(lArgs.ToString());

                rtx.Text = mBuilder.ToString();
                rtx.ScrollToCaret();

                mBuilder.Clear();

                mRefreshRequired = false;
            }
        }
    }
}
