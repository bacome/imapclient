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
        private const string kNone = "<none>";
        private const string kUsingDefault = "<using default>";

        private readonly cIMAPClient mClient;
        private readonly Action<Form, Form> mAddChildForm;
        private readonly int mMaxMessages;
        private readonly int mMaxTextBytes;
        private readonly bool mTrackUIDNext;
        private readonly bool mTrackUnseen;
        private readonly bool mProgressBar;
        private readonly List<Form> mMessageForms = new List<Form>();
        private readonly List<Form> mMessagesLoadingForms = new List<Form>();
        private readonly List<Form> mFilterForms = new List<Form>();
        private cMailbox mCurrentMailbox = null;
        private object mCurrentMessageCache = null;
        private cFilter mFilter = null;
        private cSort mOverrideSort = null;

        public frmSelectedMailbox(cIMAPClient pClient, Action<Form, Form> pAddChildForm, int pMaxMessages, int pMaxTextBytes, bool pTrackUIDNext, bool pTrackUnseen, bool pProgressBar)
        {
            mClient = pClient;
            mAddChildForm = pAddChildForm;
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
        }

        private void frmSelectedMailbox_Shown(object sender, EventArgs e)
        {
            ZQuery();
        }

        private void ZGridInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgvMessages.AutoGenerateColumns = false;
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


        private void ZQuery()
        {
            // defend against querying the same mailbox twice in a row
            //
            //  (note that this will regularly happen without this code as 
            //    events from the client object are delivered asyncronously - therefore two events may arrive after two changes
            //  )
            //
            var lSelectedMailbox = mClient.SelectedMailbox;
            var lSelectedMessageCache = lSelectedMailbox?.MessageCache;
            if (lSelectedMailbox == mCurrentMailbox && lSelectedMessageCache == mCurrentMessageCache) return;

            ZMessageFormsClose();

            ZUnsubscribeMailbox();

            // note that I check the message cache only: in theory if the mailbox is being unselected as we pass through this code it could be null, even though the lSelectedMailbox isn't
            if (lSelectedMessageCache == null)
            {
                mCurrentMailbox = null;
                mCurrentMessageCache = null;
            }
            else
            {
                mCurrentMailbox = lSelectedMailbox;
                mCurrentMessageCache = lSelectedMessageCache;
            }

            ZSubscribeMailbox();

            ZQueryProperties();
            ZQueryMessagesAsync(); // don't wait
        }

        private void ZSubscribeMailbox()
        {
            if (mCurrentMailbox == null) return;
            mCurrentMailbox.PropertyChanged += mCurrentMailbox_PropertyChanged;
            mCurrentMailbox.MessageDelivery += mCurrentMailbox_MessageDelivery;
        }

        private void ZUnsubscribeMailbox()
        {
            if (mCurrentMailbox == null) return;
            mCurrentMailbox.PropertyChanged -= mCurrentMailbox_PropertyChanged;
            mCurrentMailbox.MessageDelivery -= mCurrentMailbox_MessageDelivery;
        }

        private void mCurrentMailbox_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZQueryProperties();
            if (e.PropertyName == nameof(cMailbox.UIDValidity)) ZQuery();
        }

        private async void mCurrentMailbox_MessageDelivery(object sender, cMessageDeliveryEventArgs e)
        {
            // cautions;
            //  the event could be for the previously selected mailbox
            //  this could arrive during the query
            //   therefore some of the messages could be on the grid already
            //  we could be processing one of these when the next one arrives

            if (e.MessageHandles[0].MessageCache != mCurrentMessageCache) return;
            if (mFilter != null) return;

            var lBindingSource = dgvMessages.DataSource as BindingSource;
            if (lBindingSource == null) return;

            frmProgress lProgress = null;
            List<cMessage> lMessages;

            try
            {
                if (mProgressBar)
                {
                    lProgress = new frmProgress("loading new messages", e.MessageHandles.Count);
                    lProgress.ShowAndFocus(this);
                    var lConfiguration = new cCacheItemFetchConfiguration(lProgress.CancellationToken, lProgress.Increment);
                    ZMessagesLoadingAdd(lProgress); // so it can be cancelled from code
                    lMessages = await mCurrentMailbox.MessagesAsync(e.MessageHandles, null, lConfiguration);
                }
                else lMessages = await mCurrentMailbox.MessagesAsync(e.MessageHandles);
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
            if (IsDisposed || e.MessageHandles[0].MessageCache != mCurrentMessageCache) return;

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

                if (lData != null && lData.Message.MessageHandle == pMessage.MessageHandle)
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

                if (lData != null && lData.Message.MessageHandle == pMessage.MessageHandle)
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
            if (mCurrentMailbox == null)
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
                lBuilder.AppendLine("Path: '" + mCurrentMailbox.Path + "'");
                lBuilder.AppendLine("Delimiter: '" + mCurrentMailbox.Delimiter + "'");
                lBuilder.AppendLine("Parent Path: '" + mCurrentMailbox.ParentPath + "'");
                lBuilder.AppendLine("Name: '" + mCurrentMailbox.Name + "'");
                lBuilder.AppendLine();
                lBuilder.AppendLine("Messages: " + mCurrentMailbox.MessageCount);
                lBuilder.AppendLine("Recent: " + mCurrentMailbox.RecentCount);
                lBuilder.AppendLine("UIDNext: " + mCurrentMailbox.UIDNext);
                lBuilder.AppendLine("UIDNextUnknownCount: " + mCurrentMailbox.UIDNextUnknownCount);
                lBuilder.AppendLine("UIDValidity: " + mCurrentMailbox.UIDValidity);
                lBuilder.AppendLine("Unseen: " + mCurrentMailbox.UnseenCount);
                lBuilder.AppendLine("UnseenUnknownCount: " + mCurrentMailbox.UnseenUnknownCount);
                lBuilder.AppendLine("HighestModSeq: " + mCurrentMailbox.HighestModSeq);
                lBuilder.AppendLine();
                lBuilder.AppendLine("Flags: " + mCurrentMailbox.MessageFlags.ToString());
                lBuilder.AppendLine();
                if (mCurrentMailbox.IsSelectedForUpdate) lBuilder.AppendLine("PermanentFlags: " + mCurrentMailbox.ForUpdatePermanentFlags.ToString());
                else lBuilder.AppendLine("PermanentFlags: " + mCurrentMailbox.ReadOnlyPermanentFlags.ToString());

            }
            catch (Exception e)
            {
                lBuilder.AppendLine("An error occurred:");
                lBuilder.AppendLine(e.ToString());
            }

            rtx.Text = lBuilder.ToString();
            cmdStore.Enabled = mCurrentMailbox.IsSelectedForUpdate;
            cmdExpunge.Enabled = mCurrentMailbox.IsSelectedForUpdate && !mCurrentMailbox.IsAccessReadOnly;
            cmdClose.Enabled = mCurrentMailbox.IsSelectedForUpdate && !mCurrentMailbox.IsAccessReadOnly;
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

            if (mCurrentMailbox == null) return;

            // cautions;
            //  message delivery could arrive during the query
            //   therefore some of the messages could be on the grid already

            frmProgress lProgress = null;
            List<cMessage> lMessages;

            try
            {
                if (mProgressBar)
                {
                    lProgress = new frmProgress("loading messages");
                    lProgress.ShowAndFocus(this);
                    ZMessagesLoadingAdd(lProgress); // so it can be cancelled from code
                }

                if (mCurrentMailbox.MessageCount > mMaxMessages)
                {
                    cMessageFetchConfiguration lMFConfiguration;

                    if (lProgress == null) lMFConfiguration = null;
                    else lMFConfiguration = new cMessageFetchConfiguration(lProgress.CancellationToken, null, null); // the setcount and progress will never be used as we aren't asked for items to be cached

                    // first get the messages, sorted, but don't get the requested properties yet (as this would be wasteful if we are about to trim the set of messages we are going to display)
                    lMessages = await mCurrentMailbox.MessagesAsync(mFilter, mOverrideSort, cMessageCacheItems.Empty, lMFConfiguration);
                    if (IsDisposed || lQueryMessagesAsyncEntryNumber != mQueryMessagesAsyncEntryNumber) return;

                    // remove any excess messages (the filter may have removed enough or the mailbox may have changed in the meantime)
                    if (lMessages.Count > mMaxMessages) lMessages.RemoveRange(mMaxMessages, lMessages.Count - mMaxMessages);

                    cCacheItemFetchConfiguration lCIFConfiguration;

                    if (lProgress == null) lCIFConfiguration = null;
                    else
                    {
                        lProgress.SetMaximum(lMessages.Count);
                        lCIFConfiguration = new cCacheItemFetchConfiguration(lProgress.CancellationToken, lProgress.Increment);
                    }

                    // get any missing cache items
                    await mClient.FetchAsync(lMessages, mClient.DefaultMessageCacheItems, lCIFConfiguration);
                }
                else if (mFilter != null || mOverrideSort != null || lProgress != null)
                {
                    cMessageFetchConfiguration lConfiguration;

                    if (lProgress == null) lConfiguration = null;
                    else lConfiguration = new cMessageFetchConfiguration(lProgress.CancellationToken, lProgress.SetMaximum, lProgress.Increment);

                    lMessages = await mCurrentMailbox.MessagesAsync(mFilter, mOverrideSort, null, lConfiguration); // demonstrate the full API (note that we could have specified non default message properties if required)
                }
                else lMessages = await mCurrentMailbox.MessagesAsync(); // show that getting the full set of messages in a mailbox is trivial if no restrictions are required and the defaults are set correctly
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
            if (mCurrentMailbox.UnseenUnknownCount > 0 && mTrackUnseen)
            {
                try { await mCurrentMailbox.SetUnseenCountAsync(); }
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
            try { await mCurrentMailbox.ExpungeAsync(); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"an error occurred while expunging: {ex}");
            }
        }

        private async void cmdClose_Click(object sender, EventArgs e)
        {
            try { await mCurrentMailbox.ExpungeAsync(true); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"an error occurred while closing: {ex}");
            }
        }

        private void dgv_RowHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var lData = dgvMessages.Rows[e.RowIndex].DataBoundItem as cGridRowData;
            if (lData == null) return;
            ZMessageFormAdd(new frmMessage(mClient.InstanceName, this, mAddChildForm, mProgressBar, mCurrentMailbox, mMaxTextBytes, lData.Message));
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
            lblFilter.Text = kNone;
            ZQueryMessagesAsync();
        }

        public string UIDValidity
        {
            get
            {
                var lUIDValidity = mCurrentMailbox?.UIDValidity;
                if (lUIDValidity == null) return string.Empty;
                return lUIDValidity.Value.ToString();
            }
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
            if (mFilter == null) lblFilter.Text = kNone;
            else lblFilter.Text = mFilter.ToString();
            ZQueryMessagesAsync();
        }

        private async void cmdStore_Click(object sender, EventArgs e)
        {
            var lBindingSource = dgvMessages.DataSource as BindingSource;

            if (lBindingSource == null) return;
            if (lBindingSource.Count == 0) MessageBox.Show("there have to be some messages to update");

            // get them now: some could be delivered while the dialog is up (TODO: test that theory)
            var lMessages = new List<cMessage>(from cGridRowData lItem in lBindingSource select lItem.Message);

            eStoreOperation lOperation;
            cStorableFlags lFlags;
            ulong? lIfUnchangedSinceModSeq;

            using (frmStoreDialog lStoreDialog = new frmStoreDialog())
            {
                if (lStoreDialog.ShowDialog(this) != DialogResult.OK) return;

                lOperation = lStoreDialog.Operation;
                lFlags = lStoreDialog.Flags;
                lIfUnchangedSinceModSeq = lStoreDialog.IfUnchangedSinceModSeq;
            }

            cStoreFeedback lFeedback;

            try { lFeedback = await mClient.StoreAsync(lMessages, lOperation, lFlags, lIfUnchangedSinceModSeq); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"store error\n{ex}");
                return;
            }

            var lSummary = lFeedback.Summary();

            if (lSummary.LikelyOKCount == lFeedback.Count) return; // all messages were updated or didn't need updating

            if (lSummary.LikelyWorthPolling)
            {
                // see if polling the server helps explain any possible failures
                try { await mClient.PollAsync(); }
                catch { }

                // re-get the summary
                lSummary = lFeedback.Summary(); 

                // re-check the summary
                if (lSummary.LikelyOKCount == lFeedback.Count) return; // all messages were updated or didn't need updating
            }

            if (IsDisposed) return;
            MessageBox.Show(this, $"(some of) the messages don't appear to have been updated: {lSummary}");
        }

        private async void cmdCopyTo_Click(object sender, EventArgs e)
        {
            var lBindingSource = dgvMessages.DataSource as BindingSource;

            if (lBindingSource == null) return;
            if (lBindingSource.Count == 0) MessageBox.Show("there have to be some messages to copy");

            // get them now: some could be delivered while the dialog is up (TODO: test that theory)
            var lMessages = new List<cMessage>(from cGridRowData lItem in lBindingSource select lItem.Message);

            cMailbox lMailbox;

            using (frmMailboxDialog lMailboxDialog = new frmMailboxDialog(mClient, false))
            {
                if (lMailboxDialog.ShowDialog(this) != DialogResult.OK) return;
                lMailbox = lMailboxDialog.Mailbox;
            }

            cCopyFeedback lFeedback;

            try { lFeedback = await lMailbox.CopyAsync(lMessages); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"copy error\n{ex}");
                return;
            }

            if (!IsDisposed) MessageBox.Show(this, $"copied {lFeedback}");
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

            public bool? Seen
            {
                get
                {
                    try { return Message.Seen; }
                    catch { return null; }
                }
            }

            public bool Deleted
            {
                get
                {
                    try { return Message.Deleted; }
                    catch { return true; }
                }
            }

            public bool Expunged => Message.Expunged;

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
                    try { return Message.ReceivedDateTime; }
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
