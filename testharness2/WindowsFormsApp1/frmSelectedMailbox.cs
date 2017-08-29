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
        private readonly bool mProgressBar;
        private cMailbox mSelectedMailbox = null;

        public frmSelectedMailbox(cIMAPClient pClient, int pMaxMessages, bool pTrackUIDNext, bool pTrackUnseen, bool pProgressBar)
        {
            mClient = pClient;
            mMaxMessages = pMaxMessages;
            mTrackUIDNext = pTrackUIDNext;
            mTrackUnseen = pTrackUnseen;
            mProgressBar = pProgressBar;
            InitializeComponent();
        }

        private void frmSelectedMailbox_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - selected mailbox - " + mClient.InstanceName + " [" + mMaxMessages + "]";

            ZGridInitialise();

            mClient.PropertyChanged += mClient_PropertyChanged;
            mClient.MessagePropertyChanged += mClient_MessagePropertyChanged;

            ZChangedSelectedMailbox();
        }

        private void ZGridInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(LColumn(nameof(cGridRow.Indent)));
            dgv.Columns.Add(LColumn(nameof(cGridRow.Seen)));
            dgv.Columns.Add(LColumn(nameof(cGridRow.Deleted)));
            dgv.Columns.Add(LColumn(nameof(cGridRow.Expunged)));
            dgv.Columns.Add(LColumn(nameof(cGridRow.From)));
            dgv.Columns.Add(LColumn(nameof(cGridRow.Subject)));
            dgv.Columns.Add(LColumn(nameof(cGridRow.Received)));
            if (mTrackUIDNext) dgv.Columns.Add(LColumn(nameof(cGridRow.UID)));

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lTemplate;
                return lResult;
            }
        }

        private void ZChangedSelectedMailbox()
        {
            ZUnsubscribeMailbox();
            mSelectedMailbox = mClient.SelectedMailbox;
            ZSubscribeMailbox();

            ZChangedMailboxProperties();

            ZChangedMessagesAsync(); // don't wait
        }

        private void ZSubscribeMailbox()
        {
            if (mSelectedMailbox == null) return;
            mSelectedMailbox.PropertyChanged += mSelectedMailbox_PropertyChanged;
            mSelectedMailbox.MessageDelivery += mSelectedMailbox_MessageDelivery;
        }

        private void ZUnsubscribeMailbox()
        {
            if (mSelectedMailbox == null) return;
            mSelectedMailbox.PropertyChanged -= mSelectedMailbox_PropertyChanged;
            mSelectedMailbox.MessageDelivery -= mSelectedMailbox_MessageDelivery;
        }

        private void mSelectedMailbox_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZChangedMailboxProperties();
            if (e.PropertyName == nameof(cMailbox.UIDValidity)) ZChangedMessagesAsync(); // don't wait
        }

        private void mSelectedMailbox_MessageDelivery(object sender, cMessageDeliveryEventArgs e)
        {
            // TODO: add the new messages to the grid
        }

        private void mClient_MessagePropertyChanged(object sender, cMessagePropertyChangedEventArgs e)
        {
            // TODO: change the message if it is on the grid
        }

        private void ZChangedMailboxProperties()
        {
            if (mSelectedMailbox == null)
            {
                rtx.Text = "no selected mailbox";
                cmdExpunge.Enabled = false;
                cmdClose.Enabled = false;
                return;
            }

            StringBuilder lBuilder = new StringBuilder();

            try
            {
                lBuilder.AppendLine("Path: '" + mSelectedMailbox.Path + "'");
                lBuilder.AppendLine("Delimiter: '" + mSelectedMailbox.Delimiter + "'");
                lBuilder.AppendLine("Parent Path: '" + mSelectedMailbox.ParentPath + "'");
                lBuilder.AppendLine("Name: '" + mSelectedMailbox.Name + "'");
                lBuilder.AppendLine();
                lBuilder.AppendLine("Messages: " + mSelectedMailbox.MessageCount);
                lBuilder.AppendLine("Recent: " + mSelectedMailbox.RecentCount);
                lBuilder.AppendLine("UIDNext: " + mSelectedMailbox.UIDNext);
                lBuilder.AppendLine("UIDNextUnknownCount: " + mSelectedMailbox.UIDNextUnknownCount);
                lBuilder.AppendLine("UIDValidity: " + mSelectedMailbox.UIDValidity);
                lBuilder.AppendLine("Unseen: " + mSelectedMailbox.UnseenCount);
                lBuilder.AppendLine("UnseenUnknownCount: " + mSelectedMailbox.UnseenUnknownCount);
                lBuilder.AppendLine("HighestModSeq: " + mSelectedMailbox.HighestModSeq);
                lBuilder.AppendLine();
                lBuilder.AppendLine("Flags: " + mSelectedMailbox.MessageFlags.ToString());
                lBuilder.AppendLine();
                if (mSelectedMailbox.IsSelectedForUpdate) lBuilder.AppendLine("PermanentFlags: " + mSelectedMailbox.ForUpdatePermanentFlags.ToString());
                else lBuilder.AppendLine("PermanentFlags: " + mSelectedMailbox.ReadOnlyPermanentFlags.ToString());

            }
            catch (Exception e)
            {
                lBuilder.AppendLine("An error occurred:");
                lBuilder.AppendLine(e.ToString());
            }

            rtx.Text = lBuilder.ToString();
            cmdExpunge.Enabled = mSelectedMailbox.IsSelectedForUpdate && !mSelectedMailbox.IsAccessReadOnly;
            cmdClose.Enabled = mSelectedMailbox.IsSelectedForUpdate && !mSelectedMailbox.IsAccessReadOnly;
        }

        private int mChangedMessagesAsyncEntryNumber = 0;

        private async void ZChangedMessagesAsync()
        {
            // defend against re-entry during awaits
            int lChangedMessagesAsyncEntryNumber = ++mChangedMessagesAsyncEntryNumber;

            dgv.Enabled = false;

            if (mSelectedMailbox == null) return;

            // magic for implementing the restriction on message count; note that the message collection is live and maintained on a different thread, so we have to be careful

            cFilter lFilter;

            var lDetails = mClient.SelectedMailboxDetails;

            if (!ReferenceEquals(lDetails.Handle, mSelectedMailbox.Handle)) return;

            try
            {
                if (lDetails.Cache.Count > mMaxMessages)
                {
                    var lHandle = lDetails.Cache[lDetails.Cache.Count - mMaxMessages];
                    lFilter = cFilter.MessageHandle > lHandle;
                }
                else lFilter = null;
            }
            catch
            {
                lFilter = null;
            }

            // end magic

            // get the messages

            frmProgress lProgress = null;
            List<cMessage> lMessages;

            try
            {
                cMessageFetchConfiguration lConfiguration;

                if (mProgressBar)
                {
                    lProgress = new frmProgress("loading messages");
                    lConfiguration = new cMessageFetchConfiguration(lProgress.CancellationToken, lProgress.SetCount, lProgress.Increment);
                }
                else
                {
                    lConfiguration = null;
                }

                lMessages = await mSelectedMailbox.MessagesAsync(lFilter, null, fMessageProperties.clientdefault, lConfiguration);
            }
            catch (Exception e)
            {
                if (lProgress != null) lProgress.Cancel();
                MessageBox.Show($"a problem occurred: {e}");
                return;
            }
            finally
            {
                if (lProgress != null) lProgress.Close();
            }

            // check that while getting the messages we haven't been re-entered
            if (lChangedMessagesAsyncEntryNumber != mChangedMessagesAsyncEntryNumber) return;

            // load the grid with data
            BindingSource lBindingSource = new BindingSource();
            foreach (var lMessage in lMessages) lBindingSource.Add(new cGridRow(lMessage));
            dgv.Enabled = true;
            dgv.DataSource = lBindingSource;

            // initialise unseen count
            if (mSelectedMailbox.UnseenUnknownCount > 0 && mTrackUnseen)
            {
                try { await mSelectedMailbox.SetUnseenAsync(); }
                catch (Exception ex) { MessageBox.Show($"an error occurred while setting unseen: {ex}"); }
            }
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.SelectedMailbox)) ZChangedSelectedMailbox();
        }

        private void frmSelectedMailbox_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
            mClient.MessagePropertyChanged -= mClient_MessagePropertyChanged;
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
















        private class cGridRow
        {
            public readonly cMessage Message;

            public cGridRow(cMessage pMessage)
            {
                Message = pMessage;
            }

            public int Indent => Message.Indent;

            public bool? Seen
            {
                get
                {
                    try { return Message.IsSeen; }
                    catch { return null; }
                }
            }

            public bool Deleted
            {
                get
                {
                    try { return Message.IsDeleted; }
                    catch { return true; }
                }
            }

            public bool Expunged => Message.IsExpunged;

            public string From
            {
                get
                {
                    try { return Message.From.DisplaySortString; }
                    catch { return null; }
                }
            }

            public string Subject
            {
                get
                {
                    try { return Message.Subject; }
                    catch { return null; }
                }
            }

            public DateTime? Received
            {
                get
                {
                    try { return Message.Received; }
                    catch { return null; }
                }
            }

            public uint? UID
            {
                get
                {
                    try { return Message.UID.UID; }
                    catch { return null; }
                }
            }
        }
    }
}
