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
        private const string kUsingDefault = "<using default>";

        private readonly cIMAPClient mClient;
        private readonly int mMaxMessages;
        private readonly int mMaxTextBytes;
        private readonly bool mTrackUIDNext;
        private readonly bool mTrackUnseen;
        private readonly bool mProgressBar;
        private readonly List<Form> mMessageForms = new List<Form>();
        private readonly List<Form> mMessagesLoadingForms = new List<Form>();
        private readonly List<Form> mFilterForms = new List<Form>();
        private cMailbox mSelectedMailbox = null;
        private cFilter mFilter = null;
        private cSort mOverrideSort = null;

        public frmSelectedMailbox(cIMAPClient pClient, int pMaxMessages, int pMaxTextBytes, bool pTrackUIDNext, bool pTrackUnseen, bool pProgressBar)
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
            lblOverrideSort.Text = kUsingDefault;
            ZGridInitialise();
            mClient.PropertyChanged += mClient_PropertyChanged;
            ZQuery();
        }

        private void ZGridInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgvMessages.AutoGenerateColumns = false;
            dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.Indent)));
            dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.Seen)));
            dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.Deleted)));
            dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.Expunged)));
            dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.From)));
            dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.Subject)));
            dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.Received)));
            if (mTrackUIDNext) dgvMessages.Columns.Add(LColumn(nameof(cGridRowData.UID)));

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lTemplate;
                return lResult;
            }
        }

        private object mQueriedMessageCache = new object();

        private void ZQuery()
        {
            // defend against querying the same mailbox twice in a row
            //
            //  (note that this will regularly happen without this code as 
            //    1) events from the client object are delivered asyncronously - therefore two events may arrive after two changes
            //    2) the client object is updated on an independant thread to the UI therefore as far as UI code is concerned all of the client object properties may dynamically change
            //
            //    as an example: consider that the client may become selected whilst we are processing the unselect (e.g. before the setting of the mSelectedMailbox below)
            //  )
            //
            if (ReferenceEquals(mClient.SelectedMailboxDetails?.Cache, mQueriedMessageCache)) return;

            ZMessageFormsClose();

            ZUnsubscribeMailbox();

            if (mClient.IsConnected) mSelectedMailbox = mClient.SelectedMailbox;
            else mSelectedMailbox = null;

            // defense part two
            var lDetails = mClient.SelectedMailboxDetails;
            if (!ReferenceEquals(lDetails?.Handle, mSelectedMailbox.Handle)) return;
            mQueriedMessageCache = lDetails?.Cache;

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
            if (e.PropertyName == nameof(cMailbox.UIDValidity)) ZQuery();
        }

        private async void mSelectedMailbox_MessageDelivery(object sender, cMessageDeliveryEventArgs e)
        {
            if (mSelectedMailbox == null) return;
            var lBindingSource = dgvMessages.DataSource as BindingSource;
            if (lBindingSource == null) return;

            // if filtering is on, ignore it
            //  TODO!

            // cautions;
            //  the event could be for the previously selected mailbox
            //  this could arrive during the query
            //   therefore some of the messages could be on the grid already
            //  we could be processing one of these when the next one arrives

            if (!ReferenceEquals(e.Handles[0].Cache, mClient.SelectedMailboxDetails?.Cache)) return;

            frmProgress lProgress = null;
            List<cMessage> lMessages;

            try
            {
                if (mProgressBar)
                {
                    lProgress = new frmProgress("loading new messages");
                    Program.Centre(lProgress, this);
                    lProgress.Show();
                    cMessageFetchConfiguration lConfiguration = new cMessageFetchConfiguration(lProgress.CancellationToken, lProgress.SetCount, lProgress.Increment);
                    ZMessagesLoadingAdd(lProgress); // so it can be cancelled from code
                    lMessages = await mSelectedMailbox.MessagesAsync(e.Handles, mClient.DefaultCacheItems, lConfiguration);
                }
                else lMessages = await mSelectedMailbox.MessagesAsync(e.Handles);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"a problem occurred: {ex}");
                return;
            }
            finally
            {
                if (lProgress != null) lProgress.Complete();
            }

            // check that while getting the messages we haven't been closed or the mailbox changed
            if (IsDisposed || !ReferenceEquals(e.Handles[0].Cache, mClient.SelectedMailboxDetails?.Cache)) return;

            // load the grid with data
            ZAddMessagesToGrid(lMessages);
        }

        public cMessage Next(cMessage pMessage)
        {
            var lBindingSource = dgvMessages.DataSource as BindingSource;

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
            var lBindingSource = dgvMessages.DataSource as BindingSource;

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
                cmdStore.Enabled = false;
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
            cmdStore.Enabled = mSelectedMailbox.IsSelectedForUpdate;
            cmdExpunge.Enabled = mSelectedMailbox.IsSelectedForUpdate && !mSelectedMailbox.IsAccessReadOnly;
            cmdClose.Enabled = mSelectedMailbox.IsSelectedForUpdate && !mSelectedMailbox.IsAccessReadOnly;
        }

        private int mQueryMessagesAsyncEntryNumber = 0;

        private async void ZQueryMessagesAsync()
        {
            // defend against re-entry during awaits
            int lQueryMessagesAsyncEntryNumber = ++mQueryMessagesAsyncEntryNumber;

            // terminate any outstanding message loading
            ZMessagesLoadingClose();

            dgvMessages.Enabled = false;
            dgvMessages.DataSource = new BindingSource();

            if (mSelectedMailbox == null) return;

            // cautions;
            //  message delivery could arrive during the query
            //   therefore some of the messages could be on the grid already

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
                    ZMessagesLoadingAdd(lProgress); // so it can be cancelled from code
                }
                else lConfiguration = null;

                if (mSelectedMailbox.MessageCount > mMaxMessages)
                {
                    // first get the messages, sorted, but don't get the requested properties yet (as this would be wasteful)
                    lMessages = await mSelectedMailbox.MessagesAsync(mFilter, mOverrideSort, cCacheItems.None, lConfiguration);
                    if (IsDisposed || lQueryMessagesAsyncEntryNumber != mQueryMessagesAsyncEntryNumber) return;

                    // remove any excess messages (the filter may have removed enough or the mailbox may have changed in the meantime)
                    if (lMessages.Count > mMaxMessages) lMessages.RemoveRange(mMaxMessages, lMessages.Count - mMaxMessages);

                    // get any missing attributes
                    await mSelectedMailbox.FetchAsync(lMessages, mClient.DefaultCacheItems, lConfiguration);
                }
                else if (mFilter != null || mOverrideSort != null || lConfiguration != null) lMessages = await mSelectedMailbox.MessagesAsync(mFilter, mOverrideSort, null, lConfiguration); // demonstrate the full API (note that we could have specified non default message properties if required)
                else lMessages = await mSelectedMailbox.MessagesAsync(); // show that getting the full set of messages in a mailbox is trivial if no restrictions are required and the defaults are set correctly
            }
            /* this is commented out as it hides problems in the gating code 
            catch (OperationCanceledException e)
            {
                if (lProgress != null && lProgress.CancellationToken.IsCancellationRequested) return; // ignore the cancellation if we cancelled it
                if (!IsDisposed) MessageBox.Show(this, $"a problem occurred: {e}");
                return;
            } */
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
            if (IsDisposed || lQueryMessagesAsyncEntryNumber != mQueryMessagesAsyncEntryNumber) return;

            // load the grid with data
            ZAddMessagesToGrid(lMessages);

            // enable
            dgvMessages.Enabled = true;

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

        private void ZAddMessagesToGrid(List<cMessage> pMessages)
        {
            var lBindingSource = dgvMessages.DataSource as BindingSource;
            if (lBindingSource == null) return;

            foreach (var lMessage in pMessages)
            {
                // TODO: check it isn't on the grid already
                lBindingSource.Add(new cGridRowData(lMessage));
            }
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.IsConnected) || e.PropertyName == nameof(cIMAPClient.SelectedMailbox)) ZQuery();
        }

        private void ZMessagesLoadingAdd(frmProgress pProgress)
        {
            mMessagesLoadingForms.Add(pProgress);
            pProgress.FormClosed += ZMessagesLoadingClosed;
            Program.Centre(pProgress, this, mMessagesLoadingForms);
            pProgress.Show();
        }

        private void ZMessagesLoadingClosed(object sender, EventArgs e)
        {
            if (!(sender is frmProgress lForm)) return;
            lForm.FormClosed -= ZMessagesLoadingClosed;
            mMessagesLoadingForms.Remove(lForm);
        }

        private void ZMessagesLoadingClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mMessagesLoadingForms)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZMessagesLoadingClosed;
            }

            mMessagesLoadingForms.Clear();

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }
        }

        private void ZMessageFormAdd(frmMessage pForm)
        {
            mMessageForms.Add(pForm);
            pForm.FormClosed += ZMessageFormClosed;
            Program.Centre(pForm, this, mMessageForms);
            pForm.Show();
        }

        private void ZMessageFormClosed(object sender, EventArgs e)
        {
            if (!(sender is frmMessage lForm)) return;
            lForm.FormClosed -= ZMessageFormClosed;
            mMessageForms.Remove(lForm);
        }

        private void ZMessageFormsClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mMessageForms)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZMessageFormClosed;
            }

            mMessageForms.Clear();

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }
        }

        private void ZFilterFormAdd(frmFilter pForm, Form pCentreOnThis)
        {
            mFilterForms.Add(pForm);
            pForm.FormClosed += ZFilterFormClosed;
            Program.Centre(pForm, pCentreOnThis, mFilterForms);
            pForm.Show();
        }

        private void ZFilterFormClosed(object sender, EventArgs e)
        {
            if (!(sender is frmFilter lForm)) return;
            lForm.FormClosed -= ZFilterFormClosed;
            mFilterForms.Remove(lForm);
        }

        private void ZFilterFormsClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mFilterForms)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZFilterFormClosed;
            }

            mFilterForms.Clear();

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
            var lData = dgvMessages.Rows[e.RowIndex].DataBoundItem as cGridRowData;
            if (lData == null) return;
            ZMessageFormAdd(new frmMessage(mClient.InstanceName, this, mProgressBar, mSelectedMailbox, mMaxTextBytes, lData.Message));
        }

        private void cmdOverrideSort_Click(object sender, EventArgs e)
        {
            using (frmSortDialog lSortDialog = new frmSortDialog(mOverrideSort ?? mClient.DefaultSort))
            {
                if (lSortDialog.ShowDialog(this) == DialogResult.OK) ZOverrideSortSet(lSortDialog.Sort);
            }
        }

        private void cmdOverrideSortClear_Click(object sender, EventArgs e)
        {
            ZOverrideSortSet(null);
        }

        private void ZOverrideSortSet(cSort pSort)
        {
            mOverrideSort = pSort;
            if (mOverrideSort == null) lblOverrideSort.Text = kUsingDefault;
            else lblOverrideSort.Text = $"Set to: {mOverrideSort}";
            ZQueryMessagesAsync();
        }

        private void cmdFilter_Click(object sender, EventArgs e)
        {
            if (mFilterForms.Count == 0)
            {
                ZFilterFormAdd(new frmFilter(mClient.InstanceName, this), this);
                return;
            }

            foreach (var lForm in mFilterForms) Program.Focus(lForm);
        }

        private void cmdFilterClear_Click(object sender, EventArgs e)
        {
            ZFilterFormsClose();
            mFilter = null;
            ZQueryMessagesAsync();
        }

        public void FilterOr(frmFilter pCentreOnThis) => ZFilterFormAdd(new frmFilter(mClient.InstanceName, this), pCentreOnThis);

        public void FilterApply()
        {
            cFilter lFilter = null;

            foreach (frmFilter lForm in mFilterForms)
            {
                if (!lForm.ValidateChildren(ValidationConstraints.Enabled))
                {
                    Program.Focus(lForm);
                    return;
                }

                if (lFilter == null) lFilter = lForm.Filter();
                else lFilter = lFilter | lForm.Filter();
            }

            mFilter = lFilter;
            ZQueryMessagesAsync();
        }

        private async void cmdStore_Click(object sender, EventArgs e)
        {
            var lBindingSource = dgvMessages.DataSource as BindingSource;

            if (lBindingSource == null) return;
            if (lBindingSource.Count == 0) MessageBox.Show("there have to be some messages to update");

            // get them now: some could be delivered while the dialog is up (TODO: test that theory)
            List<cMessage> lMessages = new List<cMessage>(from cGridRowData lItem in lBindingSource select lItem.Message);

            eStoreOperation lOperation;
            cSettableFlags lFlags;
            ulong? lIfUnchangedSinceModSeq;

            using (frmStoreDialog lStoreDialog = new frmStoreDialog())
            {
                if (lStoreDialog.ShowDialog(this) != DialogResult.OK) return;

                lOperation = lStoreDialog.Operation;
                lFlags = lStoreDialog.Flags;
                lIfUnchangedSinceModSeq = lStoreDialog.IfUnchangedSinceModSeq;
            }

            cStoreFeedback lFeedback;

            try { lFeedback = await mSelectedMailbox.StoreAsync(lMessages, lOperation, lFlags, lIfUnchangedSinceModSeq); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"store error\n{ex}");
                return;
            }

            if (lFeedback.AllUpdated) return;

            if (IsDisposed) return;

            var lSummary = lFeedback.Summary(lOperation, lFlags);

            if (lSummary.WasNotUnchangedSinceCount == 0 && lSummary.ReflectsOperationCount == lFeedback.Count) return;

            if (lSummary.NotReflectsOperationCount > 0)
            {
                // see if polling the server helps explain the "not reflects" ones (maybe the message is expunged, maybe there are pending changes to be sent)
                try { await mClient.PollAsync(); }
                catch { }
                if (IsDisposed) return;
                lSummary = lFeedback.Summary(lOperation, lFlags);
            }

            MessageBox.Show(this, $"(some of) the messages don't appear to have been updated - {lSummary}");
        }

        private void frmSelectedMailbox_FormClosing(object sender, FormClosingEventArgs e)
        {
            ZMessagesLoadingClose();
        }

        private void frmSelectedMailbox_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
            ZUnsubscribeMailbox();
            ZMessageFormsClose();
            ZFilterFormsClose();
        }















        private class cGridRowData : INotifyPropertyChanged
        {
            public readonly cMessage Message;

            public cGridRowData(cMessage pMessage)
            {
                Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            }

            public event PropertyChangedEventHandler PropertyChanged
            {
                add => Message.PropertyChanged += value;
                remove => Message.PropertyChanged -= value;
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
