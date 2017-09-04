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
    public partial class frmSelectedMailbox : Form
    {
        private readonly cIMAPClient mClient;
        private readonly int mMaxMessages;
        private readonly uint mMaxTextBytes;
        private readonly bool mTrackUIDNext;
        private readonly bool mTrackUnseen;
        private readonly bool mProgressBar;
        private readonly List<Form> mMessages = new List<Form>();
        private cMailbox mSelectedMailbox = null;
        private frmProgress mQueryMessagesProgress = null;

        public frmSelectedMailbox(cIMAPClient pClient, int pMaxMessages, uint pMaxTextBytes, bool pTrackUIDNext, bool pTrackUnseen, bool pProgressBar)
        {
            mClient = pClient;
            mMaxMessages = pMaxMessages;
            mMaxTextBytes = pMaxTextBytes;
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

            ZQuery();
        }

        private void ZGridInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Indent)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Seen)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Deleted)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Expunged)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.From)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Subject)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Received)));
            if (mTrackUIDNext) dgv.Columns.Add(LColumn(nameof(cGridRowData.UID)));

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lTemplate;
                return lResult;
            }
        }

        private void ZQuery()
        {
            ZQueryMessagesCancel();

            ZUnsubscribeMailbox();

            if (mClient.IsConnected) mSelectedMailbox = mClient.SelectedMailbox;
            else mSelectedMailbox = null;

            ZSubscribeMailbox();

            ZQueryProperties();
            ZQueryMessagesAsync(); // don't wait
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
            ZQueryProperties();
            if (e.PropertyName == nameof(cMailbox.UIDValidity)) ZQueryMessagesAsync(); // don't wait
        }

        private void mSelectedMailbox_MessageDelivery(object sender, cMessageDeliveryEventArgs e)
        {
            // TODO: add the new messages to the grid
        }

        private void mClient_MessagePropertyChanged(object sender, cMessagePropertyChangedEventArgs e)
        {
            var lBindingSource = dgv.DataSource as BindingSource;

            if (lBindingSource == null) return;

            for (int i = 0; i < lBindingSource.List.Count; i++)
            {
                var lData = lBindingSource.List[i] as cGridRowData;

                if (lData != null && lData.Message.Handle == e.Handle)
                {
                    lBindingSource.ResetItem(i);
                    break;
                }
            }
        }

        public cMessage Next(cMessage pMessage)
        {
            var lBindingSource = dgv.DataSource as BindingSource;

            if (lBindingSource == null) return null;

            for (int i = 0; i < lBindingSource.List.Count - 1; i++)
            {
                var lData = lBindingSource.List[i] as cGridRowData;

                if (lData != null && lData.Message.Handle == pMessage.Handle)
                {
                    lData = lBindingSource.List[i + 1] as cGridRowData;
                    if (lData == null) return null;
                    return lData.Message;
                }
            }

            return null;
        }

        public cMessage Previous(cMessage pMessage)
        {
            var lBindingSource = dgv.DataSource as BindingSource;

            if (lBindingSource == null) return null;

            for (int i = 1; i < lBindingSource.List.Count; i++)
            {
                var lData = lBindingSource.List[i] as cGridRowData;

                if (lData != null && lData.Message.Handle == pMessage.Handle)
                {
                    lData = lBindingSource.List[i - 1] as cGridRowData;
                    if (lData == null) return null;
                    return lData.Message;
                }
            }

            return null;
        }

        private void ZQueryProperties()
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
        private object mChangedMessagesLastCache = new object();

        private async void ZQueryMessagesAsync()
        {
            // defend against being called twice with the same settings
            //  (note this is inevitable as events from the client are delivered asyncronously - therefore two events may arrive together after two changes)
            //
            var lCache = mClient.SelectedMailboxDetails?.Cache;
            if (ReferenceEquals(lCache, mChangedMessagesLastCache)) return;
            mChangedMessagesLastCache = lCache;

            // defend against re-entry during awaits
            int lChangedMessagesAsyncEntryNumber = ++mChangedMessagesAsyncEntryNumber;

            dgv.Enabled = false;
            ZMessagesClose();

            if (mSelectedMailbox == null) return;

            // get the messages

            frmProgress lProgress = null;
            List<cMessage> lMessages;

            try
            {
                cMessageFetchConfiguration lConfiguration;

                if (mProgressBar)
                {
                    lProgress = new frmProgress("loading messages");
                    Program.Centre(lProgress, this);
                    lProgress.Show();
                    lConfiguration = new cMessageFetchConfiguration(lProgress.CancellationToken, lProgress.SetCount, lProgress.Increment);
                    mQueryMessagesProgress = lProgress; // so it can be cancelled from code
                }
                else lConfiguration = null;

                if (mSelectedMailbox.MessageCount > mMaxMessages)
                {
                    lMessages = await mSelectedMailbox.MessagesAsync(null, null, 0, lConfiguration);
                    if (IsDisposed || lChangedMessagesAsyncEntryNumber != mChangedMessagesAsyncEntryNumber) return;
                    lMessages.RemoveRange(mMaxMessages, lMessages.Count - mMaxMessages);
                    await mSelectedMailbox.FetchAsync(lMessages, fMessageProperties.clientdefault, lConfiguration);
                }
                else if (lConfiguration == null) lMessages = await mSelectedMailbox.MessagesAsync();
                else lMessages = await mSelectedMailbox.MessagesAsync(null, null, fMessageProperties.clientdefault, lConfiguration);
            }
            catch (Exception e)
            {
                if (!IsDisposed) MessageBox.Show(this, $"a problem occurred: {e}");
                return;
            }
            finally
            {
                if (lProgress != null) lProgress.Complete();
            }

            // check that while getting the messages we haven't been closed or re-entered
            if (IsDisposed || lChangedMessagesAsyncEntryNumber != mChangedMessagesAsyncEntryNumber) return;

            // load the grid with data
            BindingSource lBindingSource = new BindingSource();
            foreach (var lMessage in lMessages) lBindingSource.Add(new cGridRowData(lMessage));
            dgv.Enabled = true;
            dgv.DataSource = lBindingSource;

            // initialise unseen count
            if (mSelectedMailbox.UnseenUnknownCount > 0 && mTrackUnseen)
            {
                try { await mSelectedMailbox.SetUnseenAsync(); }
                catch (Exception ex)
                {
                    if (!IsDisposed) MessageBox.Show(this, $"an error occurred while setting unseen: {ex}");
                }
            }
        }

        private void ZQueryMessagesCancel()
        {
            if (mQueryMessagesProgress != null)
            {
                try { mQueryMessagesProgress.Cancel(); }
                catch { }

                mQueryMessagesProgress = null;
            }
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.IsConnected) || e.PropertyName == nameof(cIMAPClient.SelectedMailbox)) ZQuery();
        }

        private void ZMessageAdd(frmMessage pForm)
        {
            mMessages.Add(pForm);
            pForm.FormClosed += ZMessageClosed;
            Program.Centre(pForm, this, mMessages);
            pForm.Show();
        }

        private void ZMessageClosed(object sender, EventArgs e)
        {
            if (!(sender is frmMessage lForm)) return;
            lForm.FormClosed -= ZMessageClosed;
            mMessages.Remove(lForm);
        }

        private void ZMessagesClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mMessages)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZMessageClosed;
            }

            mMessages.Clear();

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }
        }

        private async void cmdExpunge_Click(object sender, EventArgs e)
        {
            try { await mSelectedMailbox.ExpungeAsync(); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"an error occurred while expunging: {ex}");
            }
        }

        private async void cmdClose_Click(object sender, EventArgs e)
        {
            try { await mSelectedMailbox.ExpungeAsync(true); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"an error occurred while closing: {ex}");
            }
        }

        private void dgv_RowHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var lData = dgv.Rows[e.RowIndex].DataBoundItem as cGridRowData;
            if (lData == null) return;
            ZMessageAdd(new frmMessage(mClient.InstanceName, this, mProgressBar, mSelectedMailbox, mMaxTextBytes, lData.Message));
        }

        private void frmSelectedMailbox_FormClosing(object sender, FormClosingEventArgs e)
        {
            ZQueryMessagesCancel();
        }

        private void frmSelectedMailbox_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
            mClient.MessagePropertyChanged -= mClient_MessagePropertyChanged;
            ZUnsubscribeMailbox();
            ZMessagesClose();
        }















        private class cGridRowData
        {
            public readonly cMessage Message;

            public cGridRowData(cMessage pMessage)
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
