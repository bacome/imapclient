﻿using System;
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
    public partial class frmFilter : Form
    {
        private BindingList<cPartsRowData> mPartsBindingList = new BindingList<cPartsRowData>();
        private BindingList<cHeadersRowData> mHeadersBindingList = new BindingList<cHeadersRowData>();

        private readonly string mInstanceName;
        private readonly frmSelectedMailbox mParent;

        public frmFilter(string pInstanceName, frmSelectedMailbox pParent)
        {
            mInstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            mParent = pParent ?? throw new ArgumentNullException(nameof(pParent));
            InitializeComponent();
        }

        public cFilter Filter()
        {
            List<cFilter> lTerms = new List<cFilter>();

            // build

            cFetchableFlags lFlags;

            lFlags = new cFetchableFlags();
            lFlags.IsAnswered = chkAnswered.Checked;
            lFlags.IsDeleted = chkDeleted.Checked;
            lFlags.IsDraft = chkDraft.Checked;
            lFlags.IsFlagged = chkFlagged.Checked;
            lFlags.IsRecent = chkRecent.Checked;
            lFlags.IsSeen = chkSeen.Checked;
            if (chkFred.Checked) lFlags.Add(Program.FlagFred);
            if (lFlags.Count != 0) lTerms.Add(cFilter.FlagsContain(lFlags));

            lFlags = new cFetchableFlags();
            lFlags.IsAnswered = chkUnanswered.Checked;
            lFlags.IsDeleted = chkUndeleted.Checked;
            lFlags.IsDraft = chkUndraft.Checked;
            lFlags.IsFlagged = chkUnflagged.Checked;
            lFlags.IsRecent = chkUnrecent.Checked;
            lFlags.IsSeen = chkUnseen.Checked;
            if (chkUnfred.Checked) lFlags.Add(Program.FlagFred);
            if (lFlags.Count != 0) lTerms.Add(!cFilter.FlagsContain(lFlags));

            lTerms.AddRange(from r in mPartsBindingList where !string.IsNullOrWhiteSpace(r.Contains) select r.FilterPartContains);

            if (dtpRAfter.Format != DateTimePickerFormat.Custom) lTerms.Add(cFilter.Received > dtpRAfter.Value);
            if (dtpROn.Format != DateTimePickerFormat.Custom) lTerms.Add(cFilter.Received == dtpROn.Value);
            if (dtpRBefore.Format != DateTimePickerFormat.Custom) lTerms.Add(cFilter.Received < dtpRBefore.Value);

            if (dtpSAfter.Format != DateTimePickerFormat.Custom) lTerms.Add(cFilter.Sent > dtpSAfter.Value);
            if (dtpSOn.Format != DateTimePickerFormat.Custom) lTerms.Add(cFilter.Sent == dtpSOn.Value);
            if (dtpSBefore.Format != DateTimePickerFormat.Custom) lTerms.Add(cFilter.Sent < dtpSBefore.Value);

            lTerms.AddRange(from r in mHeadersBindingList where !string.IsNullOrWhiteSpace(r.Header) select r.FilterHeaderFieldContains);

            uint lUInt;

            if (uint.TryParse(txtSizeLarger.Text, out lUInt)) lTerms.Add(cFilter.Size > lUInt);
            if (uint.TryParse(txtSizeSmaller.Text, out lUInt)) lTerms.Add(cFilter.Size < lUInt);

            // return

            if (lTerms.Count == 0) return null;

            cFilter lResult;

            if (lTerms.Count == 1) lResult = lTerms[0];
            else lResult = new cFilterAnd(lTerms);

            if (chkInvert.Checked) lResult = !lResult;

            return lResult;
        }

        private void frmFilter_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - filter - " + mInstanceName;

            mPartsBindingList.Add(new cPartsRowData(eFilterPart.bcc));
            mPartsBindingList.Add(new cPartsRowData(eFilterPart.body));
            mPartsBindingList.Add(new cPartsRowData(eFilterPart.cc));
            mPartsBindingList.Add(new cPartsRowData(eFilterPart.from));
            mPartsBindingList.Add(new cPartsRowData(eFilterPart.subject));
            mPartsBindingList.Add(new cPartsRowData(eFilterPart.text));
            mPartsBindingList.Add(new cPartsRowData(eFilterPart.to));

            ZGridsInitialise();
        }

        private void ZGridsInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgvParts.AutoGenerateColumns = false;
            dgvParts.Columns.Add(LColumn(nameof(cPartsRowData.Part)));
            dgvParts.Columns.Add(LColumn(nameof(cPartsRowData.Contains)));

            dgvParts.DataSource = mPartsBindingList;

            dgvHeaders.AutoGenerateColumns = false;
            dgvHeaders.Columns.Add(LColumn(nameof(cHeadersRowData.Header)));
            dgvHeaders.Columns.Add(LColumn(nameof(cHeadersRowData.Contains)));

            dgvHeaders.DataSource = mHeadersBindingList;

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lTemplate;
                return lResult;
            }
        }

        private int mDateGate = 0;

        private void ZDateKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is DateTimePicker lSender && (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete))
            {
                mDateGate++;
                lSender.Format = DateTimePickerFormat.Custom;
                ZDatesEnable();
                mDateGate--;
            }
        }

        private void ZDateEnter(object sender, EventArgs e)
        {
            if (sender is DateTimePicker lSender && mDateGate == 0)
            {
                lSender.Format = DateTimePickerFormat.Short;
                ZDatesEnable();
            }
        }

        private void ZDateDropDown(object sender, EventArgs e)
        {
            if (sender is DateTimePicker lSender)
            {
                lSender.Format = DateTimePickerFormat.Short;
                ZDatesEnable();
            }
        }

        private void ZDatesEnable()
        {
            dtpRAfter.Enabled = dtpROn.Format == DateTimePickerFormat.Custom;
            dtpROn.Enabled = dtpRAfter.Format == DateTimePickerFormat.Custom && dtpRBefore.Format == DateTimePickerFormat.Custom;
            dtpRBefore.Enabled = dtpROn.Format == DateTimePickerFormat.Custom;

            dtpSAfter.Enabled = dtpSOn.Format == DateTimePickerFormat.Custom;
            dtpSOn.Enabled = dtpSAfter.Format == DateTimePickerFormat.Custom && dtpSBefore.Format == DateTimePickerFormat.Custom;
            dtpSBefore.Enabled = dtpSOn.Format == DateTimePickerFormat.Custom;
        }

        private void ZValTextBoxIsNumberOfBytes(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (string.IsNullOrWhiteSpace(lSender.Text)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 1 || i > 1000000)
            {
                e.Cancel = true;
                erp.SetError(lSender, "number of bytes should be 1 .. 1000000");
            }
        }

        private void ZValTextBoxIsUID(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (string.IsNullOrWhiteSpace(lSender.Text)) return;

            if (!uint.TryParse(lSender.Text, out var i))
            {
                e.Cancel = true;
                erp.SetError(lSender, "should be a uint");
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void cmdOr_Click(object sender, EventArgs e)
        {
            if (ValidateChildren(ValidationConstraints.Enabled)) mParent.FilterOr(this);
        }

        private void cmdApply_Click(object sender, EventArgs e)
        {
            if (ValidateChildren(ValidationConstraints.Enabled)) mParent.FilterApply();
        }

        private void dgvHeaders_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgvHeaders.Columns[e.ColumnIndex].DataPropertyName == nameof(cHeadersRowData.Header))
            {
                if (string.IsNullOrWhiteSpace(e.FormattedValue.ToString()))
                {
                    e.Cancel = true;
                    dgvHeaders.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "a header field name is required";
                    ;?; // this error is not picked up by ValidateChildren ?
                }
            }
        }

        private void dgvHeaders_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            dgvHeaders.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = null;
        }

        private void tab_Validating(object sender, CancelEventArgs e)
        {
            if (Filter() == null)
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "should be some restriction");
            }
        }

        private void frmFilter_FormClosing(object sender, FormClosingEventArgs e)
        {
            // to allow closing with validation errors
            e.Cancel = false;
        }









        private class cPartsRowData
        {
            public cPartsRowData(eFilterPart pPart)
            {
                Part = pPart;
            }

            public eFilterPart Part { get; private set; }
            public string Contains { get; set; } = null;

            public cFilterPartContains FilterPartContains => new cFilterPartContains(Part, Contains);
        }

        private class cHeadersRowData
        {
            public cHeadersRowData() { }

            public string Header { get; set; } = null;
            public string Contains { get; set; } = null;

            public cFilterHeaderFieldContains FilterHeaderFieldContains => new cFilterHeaderFieldContains(Header, Contains ?? "");
        }
    }
}
