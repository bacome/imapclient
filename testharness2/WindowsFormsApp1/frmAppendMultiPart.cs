using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testharness2
{
    public partial class frmAppendMultiPart : Form
    {
        private readonly string mInstanceName;

        private int mDataPaste;
        private int mDataFile;
        private int mDataFileAsStream;
        private int mChanged;

        private int mChangeNumber = 1;

        public frmAppendMultiPart(string pInstanceName)
        {
            mInstanceName = pInstanceName;
            InitializeComponent();
        }

        private void frmAppendMultiPart_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - multi part append - " + mInstanceName;
            ZInitialiseGrid();
        }

        private void ZInitialiseGrid()
        {
            var lDisplayColumnTemplate = new DataGridViewTextBoxCell();
            var lButtonColumnTemplate = new DataGridViewButtonCell();

            dgv.AutoGenerateColumns = false;
            mDataPaste = dgv.Columns.Add(LButtonColumn("Paste Data"));
            mDataFile = dgv.Columns.Add(LButtonColumn("File Data"));
            mDataFileAsStream = dgv.Columns.Add(LButtonColumn("File as stream"));
            dgv.Columns.Add(LDisplayColumn(nameof(cGridRowData.Data)));
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

            if (e.ColumnIndex == mDataPaste)
            {
                if ((Clipboard.GetDataObject()?.GetFormats()?.Length ?? 0) == 0)
                {
                    if (cAppendDataSource.CurrentData != null)
                    {
                        switch (cAppendDataSource.CurrentData)
                        {
                            case cAppendDataSourceMessage _:
                            case cAppendDataSourceMessagePart _:
                            case cAppendDataSourceStream _:
                            case cAppendDataSourceAttachment _:
                            case cAppendDataSourceUIDSection _:

                                lData.Data = cAppendDataSource.CurrentData;
                                dgv.Rows[e.RowIndex].Cells[mChanged].Value = mChangeNumber++;
                                return;
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

        private void cmdCopy_Click(object sender, EventArgs e)
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
                MessageBox.Show(this, "nothing to copy");
                return;
            }

            ;?;
        }


















        private class cGridRowData : INotifyPropertyChanged
        {
            private cAppendDataSource mData = null;

            public event PropertyChangedEventHandler PropertyChanged;

            public cGridRowData() { }

            public cAppendDataSource Data
            {
                get => mData;

                set
                {
                    mData = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayData)));
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
