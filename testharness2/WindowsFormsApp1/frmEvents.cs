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
    public partial class frmEvents : Form
    {
        private readonly cIMAPClient mClient;
        private readonly int mMaxMessages;
        private readonly Queue<string> mQueue = new Queue<string>();
        private readonly StringBuilder mBuilder = new StringBuilder();
        private bool mRefreshRequired = false;

        public frmEvents(cIMAPClient pClient, int pMaxMessages)
        {
            mClient = pClient;
            mMaxMessages = pMaxMessages;
            InitializeComponent();
        }

        private void frmEvents_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - events - " + mClient.InstanceName + " [" + mMaxMessages + "]";
            mClient.PropertyChanged += mClient_PropertyChanged;
            mClient.MailboxPropertyChanged += mClient_MailboxPropertyChanged;
            mClient.MailboxMessageDelivery += mClient_MailboxMessageDelivery;
            mClient.MessagePropertyChanged += mClient_MessagePropertyChanged;
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string lValue;

            if (e.PropertyName == nameof(cIMAPClient.AsyncCount)) lValue = mClient.AsyncCount.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.Capabilities)) lValue = ZCapabilityValue();
            else if (e.PropertyName == nameof(cIMAPClient.ConnectionState)) lValue = mClient.ConnectionState.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.IsConnected)) lValue = mClient.IsConnected.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.IsUnconnected)) lValue = mClient.IsUnconnected.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.ConnectedAccountId)) lValue = mClient.ConnectedAccountId?.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.EnabledExtensions)) lValue = mClient.EnabledExtensions.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.HomeServerReferral)) lValue = mClient.HomeServerReferral?.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.Inbox)) { if (mClient.Inbox == null) lValue = "null"; else lValue = "set"; }
            else if (e.PropertyName == nameof(cIMAPClient.ServerId)) lValue = mClient.ServerId?.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.Namespaces)) lValue = mClient.Namespaces?.ToString();
            else if (e.PropertyName == nameof(cIMAPClient.SelectedMailbox)) lValue = mClient.SelectedMailbox?.Name;
            else lValue = null;

            mQueue.Enqueue($"{nameof(PropertyChangedEventArgs)}({e.PropertyName}) [='{lValue}']");
            if (mQueue.Count > mMaxMessages) mQueue.Dequeue();
            mRefreshRequired = true;
        }

        private string ZCapabilityValue()
        {
            var lCapabilities = mClient.Capabilities;
            if (lCapabilities == null) return null;
            return string.Join(", ", lCapabilities.Capabilities) + " auth=(" + string.Join(", ", lCapabilities.AuthenticationMechanisms) + ")";
        }

        private void ZDisplayStrings(StringBuilder pBuilder, ICollection<string> pStrings)
        {
            bool lFirst = true;

            foreach (var lString in pStrings)
            {
                if (lFirst) lFirst = false;
                else pBuilder.Append(", ");
                pBuilder.Append(lString);
            }

            pBuilder.AppendLine();
        }

        private void mClient_MailboxPropertyChanged(object sender, cMailboxPropertyChangedEventArgs e)
        {
            mQueue.Enqueue(e.ToString());
            if (mQueue.Count > mMaxMessages) mQueue.Dequeue();
            mRefreshRequired = true;
        }

        private void mClient_MailboxMessageDelivery(object sender, cMailboxMessageDeliveryEventArgs e)
        {
            mQueue.Enqueue(e.ToString());
            if (mQueue.Count > mMaxMessages) mQueue.Dequeue();
            mRefreshRequired = true;
        }

        private void mClient_MessagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            mQueue.Enqueue(e.ToString());
            if (mQueue.Count > mMaxMessages) mQueue.Dequeue();
            mRefreshRequired = true;
        }

        private void frmEvents_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
            mClient.MailboxPropertyChanged -= mClient_MailboxPropertyChanged;
            mClient.MailboxMessageDelivery -= mClient_MailboxMessageDelivery;
            mClient.MessagePropertyChanged -= mClient_MessagePropertyChanged;
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            if (mRefreshRequired)
            {
                foreach (var lString in mQueue) mBuilder.AppendLine(lString);

                rtx.Text = mBuilder.ToString();
                rtx.ScrollToCaret();

                mBuilder.Clear();

                mRefreshRequired = false;
            }
        }
    }
}
