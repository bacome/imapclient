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
    public partial class frmMessage : Form
    {
        private readonly frmSelectedMailbox mParent; // for previous/next
        private readonly cMailbox mMailbox;
        private cMessage mMessage;
        private bool mSubscribed = false;

        public frmMessage(frmSelectedMailbox pParent, cMailbox pMailbox, cMessage pMessage)
        {
            mParent = pParent;
            mMailbox = pMailbox;
            mMessage = pMessage;
            InitializeComponent();
        }

        private void frmMessage_Load(object sender, EventArgs e)
        {
            if (mMailbox.IsSelectedForUpdate)
            {
                chkSeen.Enabled = true;
                chkDeleted.Enabled = true;
                chkFred.Enabled = mMailbox.MessageFlags.ContainsCreateNewPossible;
            }
            else
            {
                chkSeen.Enabled = false;
                chkDeleted.Enabled = false;
                chkFred.Enabled = false;
            }

            ZQuery();
        }

        private void ZQuery()
        {
            StringBuilder lBuilder = new StringBuilder();

            try
            {
                // envelope
                lBuilder.Clear();
                lBuilder.AppendLine("Sent: " + mMessage.Sent);
                lBuilder.AppendLine("Subject: " + mMessage.Subject);
                lBuilder.AppendLine("Base Subject: " + mMessage.BaseSubject);
                lBuilder.AppendLine("From: " + mMessage.From);
                lBuilder.AppendLine("Sender: " + mMessage.Sender);
                lBuilder.AppendLine("Reply To: " + mMessage.ReplyTo);
                lBuilder.AppendLine("To: " + mMessage.To);
                lBuilder.AppendLine("CC: " + mMessage.CC);
                lBuilder.AppendLine("BCC: " + mMessage.BCC);
                lBuilder.AppendLine("In Reply To: " + mMessage.InReplyTo);
                lBuilder.AppendLine("Message Id: " + mMessage.MessageId);
                rtxEnvelope.Text = lBuilder.ToString();

                // text
                rtxText.Text = mMessage.PlainText();

                // attachments
                // TODO

                // flags
                ZSubscribe(chkAutoRefresh.Checked);
                ZFlags();

                // bodystructure
                // TODO

                // other
                lBuilder.Clear();
                lBuilder.AppendLine("Received: " + mMessage.Received);
                lBuilder.AppendLine("Size: " + mMessage.Size);
                lBuilder.AppendLine("UID: " + mMessage.UID);
                lBuilder.AppendLine("References: " + mMessage.References);
                rtxOther.Text = lBuilder.ToString();
            }
            catch (Exception ex)
            {
                lBuilder.Clear();
                lBuilder.AppendLine("there was an error");
                lBuilder.AppendLine(ex.ToString());
                rtxOther.Text = lBuilder.ToString();
            }
        }

        private void frmMessage_FormClosed(object sender, FormClosedEventArgs e)
        {
            ZSubscribe(false);
        }

        private void chkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            ZSubscribe(chkAutoRefresh.Checked);
            if (mSubscribed) ZFlags();
        }

        private void ZSubscribe(bool pSubscribe)
        {
            if (mSubscribed == pSubscribe) return;

            if (pSubscribe) mMessage.PropertyChanged += ZRefresh;
            else mMessage.PropertyChanged -= ZRefresh;

            mSubscribed = pSubscribe;
        }

        private void ZRefresh(object sender, PropertyChangedEventArgs e)
        {
            ZFlags();
        }

        private void ZFlags()
        {
            StringBuilder lBuilder = new StringBuilder();

            try
            {
                if (mMailbox.HighestModSeq != 0) lBuilder.AppendLine("Modseq: " + mMessage.ModSeq);
                lBuilder.AppendLine("Flags: " + mMessage.Flags);
                rtxFlags.Text = lBuilder.ToString();
                chkSeen.Checked = mMessage.IsSeen;
                chkDeleted.Checked = mMessage.IsDeleted;
                chkFred.Checked = mMessage.FlagsContain("fred");
            }
            catch (Exception e)
            {
                lBuilder.AppendLine("there was an error");
                lBuilder.AppendLine(e.ToString());
                rtxFlags.Text = lBuilder.ToString();
            }
        }

        private void cmdPrevious_Click(object sender, EventArgs e)
        {
            var lMessage = mParent.Previous(mMessage);

            if (lMessage == null)
            {
                MessageBox.Show("no previous");
                return;
            }

            ZSubscribe(false);
            mMessage = lMessage;
            ZQuery();
        }

        private void cmdNext_Click(object sender, EventArgs e)
        {
            var lMessage = mParent.Next(mMessage);

            if (lMessage == null)
            {
                MessageBox.Show("no next");
                return;
            }

            ZSubscribe(false);
            mMessage = lMessage;
            ZQuery();
        }
    }
}
