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
    public partial class frmSortDialog : Form
    {
        private readonly BindingList<cGridRowData> mBindingList = new BindingList<cGridRowData>();

        private cSort mSort;
        private int mRank = 1;

        public frmSortDialog(cSort pSort)
        {
            mSort = pSort ?? throw new ArgumentNullException(nameof(pSort));
            InitializeComponent();
        }

        public cSort Sort
        {
            get
            {
                if (rdoNone.Checked) return cSort.None;
                //if (rdoThreadOrderedSubject.Checked) return cSort.ThreadOrderedSubject;
                //if (rdoThreadReferences.Checked) return cSort.ThreadReferences;
                return new cSort(from r in mBindingList where r.Rank != null orderby r.Rank select r.SortItem);
            }
        }

        private void frmSort_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - sort dialog";

            mBindingList.Add(new cGridRowData(eSortItem.received));
            mBindingList.Add(new cGridRowData(eSortItem.cc));
            mBindingList.Add(new cGridRowData(eSortItem.sent));
            mBindingList.Add(new cGridRowData(eSortItem.from));
            mBindingList.Add(new cGridRowData(eSortItem.size));
            mBindingList.Add(new cGridRowData(eSortItem.subject));
            mBindingList.Add(new cGridRowData(eSortItem.to));
            mBindingList.Add(new cGridRowData(eSortItem.displayfrom));
            mBindingList.Add(new cGridRowData(eSortItem.displayto));

            if (mSort == cSort.None) rdoNone.Checked = true;
            //else if (ReferenceEquals(mSort, cSort.ThreadOrderedSubject)) rdoThreadOrderedSubject.Checked = true;
            //else if (ReferenceEquals(mSort, cSort.ThreadReferences)) rdoThreadReferences.Checked = true;
            else
            {
                rdoOther.Checked = true;

                foreach (var lItem in mSort.Items)
                {
                    var lRow = (from r in mBindingList where r.Item == lItem.Item select r).Single();
                    lRow.Rank = mRank++;
                    lRow.Desc = lItem.Desc;
                }
            }

            ZGridInitialise();
        }

        private void ZGridInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Item)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Rank)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Desc)));

            dgv.DataSource = mBindingList;

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lTemplate;
                return lResult;
            }
        }

        private void ZCheckedChanged(object sender, EventArgs e)
        {
            dgv.Enabled = rdoOther.Checked;
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string lDataPropertyName = dgv.Columns[e.ColumnIndex].DataPropertyName;

            if (lDataPropertyName == nameof(cGridRowData.Rank))
            {
                if (dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null) dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = mRank++;
                else dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = null;
            }
            else if (lDataPropertyName == nameof(cGridRowData.Desc))
            {
                if (dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is bool lDesc) dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = !lDesc;
            }
        }

        private void dgv_Validating(object sender, CancelEventArgs e)
        {
            if (!mBindingList.Any(r => r.Rank != null))
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "must rank one item");
            }
        }

        private void dgv_Validated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void frmSortDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // TODO: check if this is required
            // to allow closing with validation errors
            e.Cancel = false;
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;
            DialogResult = DialogResult.OK;
        }

        private class cGridRowData : IComparable<cGridRowData>
        {
            public cGridRowData(eSortItem pItem)
            {
                Item = pItem;
            }

            public eSortItem Item { get; private set; }
            public int? Rank { get; set; } = null;
            public bool Desc { get; set; } = false;

            public cSortItem SortItem => new cSortItem(Item, Desc);

            public int CompareTo(cGridRowData pOther)
            {
                if (pOther == null) return 1;

                if (Rank == null)
                {
                    if (pOther.Rank == null) return Item.CompareTo(pOther.Item);
                    return -1;
                }

                if (pOther.Rank == null) return 1;

                var lCompareTo = Rank.Value.CompareTo(pOther.Rank.Value);

                if (lCompareTo != 0) return lCompareTo;

                return Item.CompareTo(pOther.Item);
            }
        }
    }
}
