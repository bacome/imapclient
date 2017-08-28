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
    public partial class frmSelectedMailbox : Form
    {
        private readonly cIMAPClient mClient;
        private readonly int mMaxMessages;
        private readonly bool mTrackUIDNext;
        private readonly bool mTrackUnseen;
        private cMailbox mSelectedMailbox = null;

        public frmSelectedMailbox(cIMAPClient pClient, int pMaxMessages, bool pTrackUIDNext, bool pTrackUnseen)
        {
            mClient = pClient;
            mMaxMessages = pMaxMessages;
            mTrackUIDNext = pTrackUIDNext;
            mTrackUnseen = pTrackUnseen;
            InitializeComponent();
        }

        private void frmSelectedMailbox_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - selected mailbox - " + mClient.InstanceName + " [" + mMaxMessages + "," + (mTrackUIDNext ? "UID" : "-") + "," + (mTrackUnseen ? "Unseen" : "-") + "]";
            mClient.PropertyChanged += mClient_PropertyChanged;
            ZSetControlState();

            // TODO: if tracking UIDNext, add it to the grid
        }

        private void ZSetControlState()
        {
            var lSelectedMailbox = mClient.SelectedMailbox;

            if (!ReferenceEquals(mSelectedMailbox, lSelectedMailbox))
            {
                ZUnsubscribeMailbox();
                mSelectedMailbox = lSelectedMailbox;
                ZSubscribeMailbox();
                ZQueryAsync(); // don't wait
            }

            if (mSelectedMailbox == null)
            {
                lblMailboxName.Text = "No selected mailbox";
                cmdExpunge.Enabled = false;
                cmdClose.Enabled = false;
            }
            else
            {
                lblMailboxName.Text = mSelectedMailbox.Name;
                cmdExpunge.Enabled = mSelectedMailbox.IsSelectedForUpdate && !mSelectedMailbox.IsAccessReadOnly;
                cmdClose.Enabled = mSelectedMailbox.IsSelectedForUpdate && !mSelectedMailbox.IsAccessReadOnly;
            }
        }

        private void ZSubscribeMailbox()
        {
            if (mSelectedMailbox == null) return;
            mSelectedMailbox.MessageDelivery += mSelectedMailbox_MessageDelivery;
            mSelectedMailbox.PropertyChanged += mSelectedMailbox_PropertyChanged;
        }

        private void ZUnsubscribeMailbox()
        {
            if (mSelectedMailbox == null) return;
            mSelectedMailbox.MessageDelivery -= mSelectedMailbox_MessageDelivery;
            mSelectedMailbox.PropertyChanged -= mSelectedMailbox_PropertyChanged;
        }

        private void mSelectedMailbox_MessageDelivery(object sender, cMessageDeliveryEventArgs e)
        {
            // TODO: add the new messages to the grid
        }

        private void mSelectedMailbox_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cMailbox.UIDValidity)) ZQueryAsync(); // don't wait
        }

        private int mQueryAsyncEntryNumber = 0;

        private async void ZQueryAsync()
        {
            // defend against re-entry during awaits
            int lQueryAsyncEntryNumber = ++mQueryAsyncEntryNumber;

            if (mSelectedMailbox == null) return;




            // TODO: defend against re-entry

            // TODO

            // if null exit
            //  otherwise clear grid and requery it



            // after the query, if the unseen unknown count > 0
            if (mSelectedMailbox.UnseenUnknownCount > 0 && mTrackUnseen)
            {
                try { await mSelectedMailbox.SetUnseenAsync(); }
                catch (Exception ex) { MessageBox.Show($"an error occurred while setting unseen: {ex}"); }
            }
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.ConnectionState)) ZSetControlState();
        }

        private void frmSelectedMailbox_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
            ZUnsubscribeMailbox();
        }

        private async void cmdExpunge_Click(object sender, EventArgs e)
        {
            try { await mSelectedMailbox.ExpungeAsync(); }
            catch (Exception ex) { MessageBox.Show($"an error occurred while expunging: {ex}"); }
        }

        private async void cmdClose_Click(object sender, EventArgs e)
        {
            try { await mSelectedMailbox.ExpungeAsync(true); }
            catch (Exception ex) { MessageBox.Show($"an error occurred while closing: {ex}"); }
        }
    }
}
