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
        private readonly int mMaxTextBytes;
        private readonly bool mTrackUIDNext;
        private readonly bool mTrackUnseen;
        private readonly bool mProgressBar;
        private readonly List<Form> mMessageForms = new List<Form>();
        private readonly List<Form> mMessagesLoadingForms = new List<Form>();
        private cMailbox mSelectedMailbox = null;
        private frmSort mSort = null;

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

        private void ZQuery()
        {
            ZMessagesLoadingClose();

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
                cMessageFetchConfiguration lConfiguration;

                if (mProgressBar)
                {
                    lProgress = new frmProgress("loading new messages");
                    Program.Centre(lProgress, this);
                    lProgress.Show();
                    lConfiguration = new cMessageFetchConfiguration(lProgress.CancellationToken, lProgress.SetCount, lProgress.Increment);
                    ZMessagesLoadingAdd(lProgress); // so it can be cancelled from code
                }
                else lConfiguration = null;

                lMessages = await mSelectedMailbox.MessagesAsync(e.Handles, mClient.DefaultMessageProperties, lConfiguration);
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

        private int mQueryMessagesAsyncEntryNumber = 0;

        private async void ZQueryMessagesAsync()
        {
            // defend against re-entry during awaits
            int lQueryMessagesAsyncEntryNumber = ++mQueryMessagesAsyncEntryNumber;

            dgvMessages.Enabled = false;
            dgvMessages.DataSource = new BindingSource();
            ZMessageFormsClose();

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
                    lMessages = await mSelectedMailbox.MessagesAsync(null, null, 0, lConfiguration);
                    if (IsDisposed || lQueryMessagesAsyncEntryNumber != mQueryMessagesAsyncEntryNumber) return;
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

        private object mClient_PropertyChanged_CurrentMessageCache = new object();

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.IsConnected) || e.PropertyName == nameof(cIMAPClient.SelectedMailbox))
            {
                // defend against being called twice in a row with the same settings
                //  (note this is inevitable as events from the client are delivered asyncronously - therefore two events may arrive together after two changes)
                //
                var lCache = mClient.SelectedMailboxDetails?.Cache;
                if (ReferenceEquals(lCache, mClient_PropertyChanged_CurrentMessageCache)) return;
                mClient_PropertyChanged_CurrentMessageCache = lCache;
                ZQuery();
            }
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

        private void cmdSort_Click(object sender, EventArgs e)
        {
            if (mSort == null)
            {
                mSort = new frmSort(mClient.InstanceName);
                mSort.FormClosed += ZSortClosed;
                Program.Centre(mSort, this);
                mSort.Show();
            }
            else
            {
                if (mSort.WindowState == FormWindowState.Minimized) mSort.WindowState = FormWindowState.Normal;
                mSort.Focus();
            }
        }

        private void ZSortClosed(object sender, EventArgs e)
        {
            if (!(sender is Form lForm)) return;
            lForm.FormClosed -= ZSortClosed;
            mSort = null;
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
            if (mSort != null) mSort.Close();
        }















        private class cGridRowData : INotifyPropertyChanged
        {
            public readonly cMessage Message;

            public cGridRowData(cMessage pMessage)
            {
                Message = pMessage;
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
