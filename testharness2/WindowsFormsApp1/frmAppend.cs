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
    public partial class frmAppend : Form
    {
        private readonly cIMAPClient mClient;

        private int mFlagsSet;
        private int mFlagsClear;
        private int mDataPaste;
        private int mDataFile;
        private int mDataCompose;
        private int mChanged;

        private int mChangeNumber = 1;

        public frmAppend(cIMAPClient pClient)
        {
            mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
            InitializeComponent();
        }

        private void frmAppend_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - append - " + mClient.InstanceName;
            ZInitialiseGrid();
        }

        private void ZInitialiseGrid()
        {
            var lDisplayColumnTemplate = new DataGridViewTextBoxCell();
            var lButtonColumnTemplate = new DataGridViewButtonCell();

            dgv.AutoGenerateColumns = false;
            mFlagsSet = dgv.Columns.Add(LButtonColumn("Set Flags"));
            mFlagsClear = dgv.Columns.Add(LButtonColumn("Clear Flags"));
            dgv.Columns.Add(LDisplayColumn(nameof(cGridRowData.Flags)));
            dgv.Columns.Add(LDisplayColumn(nameof(cGridRowData.Received)));
            mDataPaste = dgv.Columns.Add(LButtonColumn("Paste Data"));
            mDataFile = dgv.Columns.Add(LButtonColumn("File Data"));
            mDataCompose = dgv.Columns.Add(LButtonColumn("Compose Data"));
            dgv.Columns.Add(LDisplayColumn(nameof(cGridRowData.Data)));
            mChanged = dgv.Columns.Add(LInvisibleColumn(nameof(cGridRowData.Changed)));

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.ColumnHeader);

            var lBindingSource = new BindingSource();
            lBindingSource.DataSource = typeof(cGridRowData);
            dgv.DataSource = lBindingSource;

            DataGridViewColumn LDisplayColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();

                lResult.DataPropertyName = "Display" + pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lDisplayColumnTemplate;
                return lResult;
            }

            DataGridViewColumn LButtonColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();

                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lButtonColumnTemplate;
                return lResult;
            }

            DataGridViewColumn LInvisibleColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();

                lResult.DataPropertyName = pName;
                lResult.CellTemplate = lDisplayColumnTemplate;
                lResult.Visible = false;

                return lResult;
            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            cGridRowData lData;

            lData = dgv.Rows[e.RowIndex].DataBoundItem as cGridRowData;

            if (lData == null)
            {
                dgv.NotifyCurrentCellDirty(true);
                dgv.NotifyCurrentCellDirty(false);
                lData = dgv.Rows[e.RowIndex].DataBoundItem as cGridRowData;
            }

            if (lData == null) return;

            if (e.ColumnIndex == mFlagsSet)
            {
                using (frmStorableFlagsDialog lFlagsDialog = new frmStorableFlagsDialog(lData.Flags))
                {
                    if (lFlagsDialog.ShowDialog(this) == DialogResult.OK) lData.Flags = lFlagsDialog.Flags;
                }

                return;
            }

            if (e.ColumnIndex == mFlagsClear)
            {
                lData.Flags = null;
                return;
            }

            if (e.ColumnIndex == mDataPaste) 
            {
                if ((Clipboard.GetDataObject()?.GetFormats()?.Length ?? 0) == 0)
                {
                    if (cAppendDataSource.CurrentData != null)
                    {
                        switch (cAppendDataSource.CurrentData)
                        {
                            case cAppendDataSourceMessage lMessage:

                                lData.Data = cAppendDataSource.CurrentData;
                                dgv.Rows[e.RowIndex].Cells[mChanged].Value = mChangeNumber++;
                                return;

                            case cAppendDataSourceMessagePart lMessagePart:

                                if (lMessagePart.Part is cMessageBodyPart)
                                {
                                    lData.Data = cAppendDataSource.CurrentData;
                                    dgv.Rows[e.RowIndex].Cells[mChanged].Value = mChangeNumber++;
                                    return;
                                }

                                break;
                        }
                    }
                }
                else
                {
                    if (Clipboard.ContainsText(TextDataFormat.Text))
                    {
                        lData.Data = new cAppendDataSourceString(Clipboard.GetText(TextDataFormat.Text));
                        dgv.Rows[e.RowIndex].Cells[mChanged].Value = mChangeNumber++;
                        return;
                    }

                    if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                    {
                        lData.Data = new cAppendDataSourceString(Clipboard.GetText(TextDataFormat.UnicodeText));
                        dgv.Rows[e.RowIndex].Cells[mChanged].Value = mChangeNumber++;
                        return;
                    }
                }

                MessageBox.Show(this, "can't paste that");
                return;
            }

            if (e.ColumnIndex == mDataFile)
            {
                var lOpenFileDialog = new OpenFileDialog();
                if (lOpenFileDialog.ShowDialog() != DialogResult.OK) return;
                lData.Data = new cAppendDataSourceString(lOpenFileDialog.FileName);
                dgv.Rows[e.RowIndex].Cells[mChanged].Value = mChangeNumber++;
            }
        }

        private void dgv_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            // TODO: a check is required here for: edit a few rows, have an error on the last one; then press esc. (Can't believe that this is required ...)

            if (!(dgv.Rows[e.RowIndex].DataBoundItem is cGridRowData lRowData)) return;

            string lErrorText = lRowData.ErrorText;

            if (lErrorText != null)
            {
                e.Cancel = true;
                dgv.Rows[e.RowIndex].ErrorText = lErrorText;
            }

        }

        private void dgv_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            dgv.Rows[e.RowIndex].ErrorText = null;
        }

        private async void cmdAppend_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled))
            {
                MessageBox.Show(this, "data errors");
                return;
            }

            if (!(dgv.DataSource is BindingSource lBindingSource))
            {
                MessageBox.Show(this, "internal error");
                return;
            }

            if (lBindingSource.Count == 0)
            {
                MessageBox.Show(this, "nothing to append");
                return;
            }

            cMailbox lMailbox;

            using (frmMailboxDialog lMailboxDialog = new frmMailboxDialog(mClient, false))
            {
                if (lMailboxDialog.ShowDialog(this) != DialogResult.OK) return;
                lMailbox = lMailboxDialog.Mailbox;
            }

            {
                if (lBindingSource.Count == 1 && lBindingSource[0] is cGridRowData lRow && lRow.Flags == null && lRow.Received == null && lRow.Data is cAppendDataSourceString lString)
                {
                    // to demonstrate the simple API

                    cUID lUID;

                    try { lUID = await lMailbox.AppendAsync(lString.String); }
                    catch (Exception ex)
                    {
                        if (!IsDisposed) MessageBox.Show(this, $"append error\n{ex}");
                        return;
                    }

                    if (!IsDisposed) MessageBox.Show(this, $"appended {lUID}");

                    return;
                }
            }


            // DO THE mailmessage as a simple API example also ...

            List<cAppendData> lMessages = new List<cAppendData>();

            foreach (cGridRowData lRow in lBindingSource)
            {
                switch (lRow.Data)
                {
                    case cAppendDataSourceMessage lMessage:

                        if (lRow.Flags == null && lRow.Received == null) lMessages.Add(lMessage.Message);
                        else lMessages.Add(new cMessageAppendData(lMessage.Message, lRow.Flags, lRow.Received));
                        break;

                    case cAppendDataSourceMessagePart lPart:

                        lMessages.Add(new cMessagePartAppendData(lPart.Message, lPart.Part as cMessageBodyPart, lRow.Flags, lRow.Received));
                        break;

                    case cAppendDataSourceString lString:

                        if (lRow.Flags == null && lRow.Received == null) lMessages.Add(lString.String);
                        else lMessages.Add(new cStringAppendData(lString.String, lRow.Flags, lRow.Received));
                        break;

                    case cAppendDataSourceFile lFile:

                        lMessages.Add(new cFileAppendData(lFile.Path, lRow.Flags, lRow.Received));
                        break;

                    default:

                        MessageBox.Show(this, "internal error");
                        return;
                }
            }

            cAppendFeedback lFeedback;

            try { lFeedback = await lMailbox.AppendAsync(lMessages); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"append error\n{ex}");
                return;
            }

            if (!IsDisposed) MessageBox.Show(this, $"appended {lFeedback}");
        }








        private class cGridRowData : INotifyPropertyChanged
        {
            private cStorableFlags mFlags = null;
            private DateTime? mReceived = null;
            private cAppendDataSource mData = null;

            public event PropertyChangedEventHandler PropertyChanged;

            public cGridRowData() { }

            public cStorableFlags Flags
            {
                get => mFlags;

                set
                {
                    mFlags = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayFlags)));
                }
            }

            public DateTime? Received => mReceived;

            public cAppendDataSource Data
            {
                get => mData;

                set
                {
                    mData = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayData)));
                }
            }

            public string DisplayFlags => Flags?.ToString();

            public string DisplayReceived
            {
                get
                {
                    if (mReceived == null) return null;
                    return mReceived.Value.ToString("R");
                }

                set
                {
                    if (DateTime.TryParse(value, out var lReceived)) mReceived = lReceived;
                    else mReceived = null;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Received)));
                }
            }

            public string DisplayData => Data?.ToString();

            public string Changed { get; set; }

            public string ErrorText
            {
                get
                {
                    if (mData == null) return "must specify some data";
                    return null;
                }
            }
        }
    }
}
