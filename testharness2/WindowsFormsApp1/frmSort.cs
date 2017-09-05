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
    public partial class frmSort : Form
    {
        private readonly string mInstanceName;

        public frmSort(string pInstanceName)
        {
            mInstanceName = pInstanceName;
            InitializeComponent();
        }

        private void frmSort_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - selected mailbox sort - " + mInstanceName;
            ZGridInitialise();
        }

        private void ZGridInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Item)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Order)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Descending)));

            BindingSource lBindingSource = new BindingSource();
            lBindingSource.Add(new cGridRowData(cSortItem.Received, cSortItem.ReceivedDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.CC, cSortItem.CCDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.Sent, cSortItem.SentDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.From, cSortItem.FromDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.Size, cSortItem.SizeDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.Subject, cSortItem.SubjectDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.To, cSortItem.ToDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.DisplayFrom, cSortItem.DisplayFromDesc));
            lBindingSource.Add(new cGridRowData(cSortItem.DisplayTo, cSortItem.DisplayToDesc));
            dgv.DataSource = lBindingSource;

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lTemplate;
                return lResult;
            }
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].DataPropertyName == nameof(cGridRowData.Descending))
            {
                if (dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is bool lDesc) dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = !lDesc;
            }
        }

        private cSort ZSort()
        {
            List<cGridRowData> lOrderBy = new List<cGridRowData>();

            foreach (var lRow in dgv.DataSource as BindingSource)
            {
                var lRowData = lRow as cGridRowData;
                if (lRowData.Order != null) lOrderBy.Add(lRowData);
            }

            if (lOrderBy.Count == 0) return null;

            lOrderBy.Sort();

            List<cSortItem> lItems = new List<cSortItem>();

            foreach (var lTerm in lOrderBy) lItems.Add(lTerm.SortItem);

            return new cSort(lItems);
        }

        private class cGridRowData : IComparable<cGridRowData>
        {
            private readonly cSortItem Asc;
            private readonly cSortItem Desc;

            public cGridRowData(cSortItem pAsc, cSortItem pDesc)
            {
                Asc = pAsc ?? throw new ArgumentNullException(nameof(pAsc));
                Desc = pDesc ?? throw new ArgumentNullException(nameof(pDesc));
                if (pAsc.Property != pDesc.Property) throw new ArgumentOutOfRangeException(nameof(pDesc));
            }

            public string Item => Asc.Property.ToString();
            public int? Order { get; set; } = null;
            public bool Descending { get; set; } = false;
            public cSortItem SortItem => Descending ? Asc : Desc;

            public int CompareTo(cGridRowData pOther)
            {
                if (pOther == null) return 1;

                if (Order == null)
                {
                    if (pOther.Order == null) return Asc.Property.CompareTo(pOther.Asc.Property);
                    return -1;
                }

                if (pOther.Order == null) return 1;

                var lCompareTo = Order.Value.CompareTo(pOther.Order.Value);

                if (lCompareTo != 0) return lCompareTo;

                return Asc.Property.CompareTo(pOther.Asc.Property);
            }
        }
    }
}
