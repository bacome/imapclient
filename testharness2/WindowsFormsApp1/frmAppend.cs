using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        private int mDataFileAsStream;
        private int mChanged;

        private int mChangeNumber = 1;

        private readonly List<Form> mMultiPartForms = new List<Form>();

        public frmAppend(cIMAPClient pClient)
        {
            mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
            InitializeComponent();
        }

        private void frmAppend_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - append - " + mClient.InstanceName;
            ZInitialiseGrid();
            mClient.PropertyChanged += mClient_PropertyChanged;
        }

        private void frmAppend_Shown(object sender, EventArgs e)
        {
            ZEnable();
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.IsConnected)) ZEnable();
        }

        private void ZEnable()
        {
            cmdAppend.Enabled = mClient.IsConnected;
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
            mDataFileAsStream = dgv.Columns.Add(LButtonColumn("File as stream"));
            dgv.Columns.Add(LDisplayColumn(nameof(cGridRowData.Data)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Fb_Type)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Fb_UID)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Fb_Result)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Fb_TryIgnoring)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Fb_Exception)));
            mChanged = dgv.Columns.Add(LInvisibleColumn(nameof(cGridRowData.Changed))); // so rows don't get lost because the grid doesn't know they've been edited

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

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();

                lResult.DataPropertyName = pName;
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
                            case cAppendDataSourceStream lStream:

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

            if (e.ColumnIndex == mDataFile || e.ColumnIndex == mDataFileAsStream)
            {
                var lOpenFileDialog = new OpenFileDialog();
                if (lOpenFileDialog.ShowDialog() != DialogResult.OK) return;
                lData.Data = new cAppendDataSourceFile(lOpenFileDialog.FileName, e.ColumnIndex == mDataFileAsStream);
                dgv.Rows[e.RowIndex].Cells[mChanged].Value = mChangeNumber++;
            }
        }

        private void dgv_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            // the try/catch is here for: edit a few rows, have an error on the last one; then press esc.

            try
            {
                // this fails with an index out of range error
                if (!(dgv.Rows[e.RowIndex].DataBoundItem is cGridRowData lRowData)) return;

                string lErrorText = lRowData.ErrorText;

                if (lErrorText != null)
                {
                    e.Cancel = true;
                    dgv.Rows[e.RowIndex].ErrorText = lErrorText;
                }
            }
            catch { }
        }

        private void dgv_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            dgv.Rows[e.RowIndex].ErrorText = null;
        }

        private void ZMultiPartFormAdd(frmAppendMultiPart pForm)
        {
            mMultiPartForms.Add(pForm);
            pForm.FormClosed += ZMultiPartFormClosed;
            Program.Centre(pForm, this, mMultiPartForms);
            pForm.Show();
        }

        private void ZMultiPartFormClosed(object sender, EventArgs e)
        {
            if (!(sender is frmMessage lForm)) return;
            lForm.FormClosed -= ZMultiPartFormClosed;
            mMultiPartForms.Remove(lForm);
        }

        private void ZMultiPartFormsClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mMultiPartForms)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZMultiPartFormClosed;
            }

            mMultiPartForms.Clear();

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }
        }

        private void cmdMultiPart_Click(object sender, EventArgs e)
        {
            ZMultiPartFormAdd(new frmAppendMultiPart(mClient.InstanceName));
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


            // TODO THE mailmessage as a simple API example also ...
            //  don't forget to dispose any streams in it

            List<cGridRowData> lRows = new List<cGridRowData>();
            List<cAppendData> lMessages = new List<cAppendData>();
            List<cMessageDataStream> lMessageDataStreams = new List<cMessageDataStream>();
            List<FileStream> lFileStreams = new List<FileStream>();


            try
            {
                foreach (cGridRowData lRow in lBindingSource)
                {
                    lRows.Add(lRow);

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

                            if (lFile.AsStream)
                            {
                                FileStream lStream = new FileStream(lFile.Path, FileMode.Open, FileAccess.Read);
                                lFileStreams.Add(lStream);
                                lMessages.Add(new cStreamAppendData(lStream, lRow.Flags, lRow.Received));
                            }
                            else lMessages.Add(new cFileAppendData(lFile.Path, lRow.Flags, lRow.Received));

                            break;

                        case cAppendDataSourceStream lStream:

                            lMessages.Add(new cStreamAppendData(lStream.Stream, lStream.Length));
                            lMessageDataStreams.Add(lStream.Stream); 
                            break;

                        // when implementing multipart, don't forget the dispose requirement

                        default:

                            MessageBox.Show(this, "internal error");
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"data conversion error\n{ex}");
                foreach (var lStream in lFileStreams) lStream.Dispose();
                return;
            }

            cAppendFeedback lFeedback;

            using (frmProgress lProgress = new frmProgress("appending"))
            {
                lProgress.ShowAndFocus(this);

                cAppendConfiguration lConfiguration = new cAppendConfiguration(lProgress.CancellationToken, lProgress.SetMaximum, lProgress.Increment);

                try { lFeedback = await lMailbox.AppendAsync(lMessages, lConfiguration); }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    if (!IsDisposed) MessageBox.Show(this, $"append error\n{ex}");
                    return;
                }
                finally
                {
                    foreach (var lStream in lMessageDataStreams) lStream.Dispose();
                    foreach (var lStream in lFileStreams) lStream.Dispose();
                }
            }

            if (!IsDisposed)
            {
                for (int i = 0; i < Math.Min(lFeedback.Count, lMessages.Count); i++) lRows[i].Feedback = lFeedback[i];
                MessageBox.Show(this, $"appended {lFeedback.SucceededCount} messages, {lFeedback.FailedWithResultCount + lFeedback.FailedWithExceptionCount} failed, {lFeedback.ResultUnknownCount} unknown, {lFeedback.NotAttemptedCount} didn't try");
            }
        }

        private void frmAppend_FormClosed(object sender, FormClosedEventArgs e)
        {
            ZMultiPartFormsClose();
        }







        private class cGridRowData : INotifyPropertyChanged
        {
            private cStorableFlags mFlags = null;
            private DateTime? mReceived = null;
            private cAppendDataSource mData = null;
            private cAppendFeedbackItem mFeedback = null;

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

            public cAppendFeedbackItem Feedback
            {
                get => mFeedback;

                set
                {
                    mFeedback = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fb_Type)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fb_UID)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fb_Result)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fb_TryIgnoring)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fb_Exception)));
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

            public string Fb_Type => mFeedback?.Type.ToString();
            public string Fb_UID => mFeedback?.UID?.ToString();
            public string Fb_Result => mFeedback?.Result?.ToString();
            public string Fb_TryIgnoring => mFeedback?.TryIgnoring.ToString();
            public string Fb_Exception => mFeedback?.Exception?.ToString();

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
