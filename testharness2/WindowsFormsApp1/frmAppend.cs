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
        private readonly string mInstanceName;

        private readonly BindingList<cGridRowData> mBindingList = new BindingList<cGridRowData>();

        public frmAppend(string pInstanceName)
        {
            mInstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            InitializeComponent();
        }

        private void frmAppend_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - append - " + mInstanceName;
            ZInitialiseGrid();
        }

        private void ZInitialiseGrid()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Flags)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.Received)));
            dgv.Columns.Add(LColumn(nameof(cGridRowData.DataSource)));

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






        private class cGridRowData
        {
            public cStorableFlags Flags { get; set; }
            public DateTime Received { get; set; }
            public cAppendDataSource DataSource { get; set; }
        }
    }
}
