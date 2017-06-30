using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testharness
{
    public partial class frmMessageView : Form
    {
        public frmMessageView()
        {
            InitializeComponent();

            // initilise the grid
            dgvAttachment.AutoGenerateColumns = false;
            dgvAttachment.Columns.Add(LColumn(nameof(cAttachmentHeader.Type)));
            dgvAttachment.Columns.Add(LColumn(nameof(cAttachmentHeader.SubType)));
            dgvAttachment.Columns.Add(LColumn(nameof(cAttachmentHeader.FileName)));
            dgvAttachment.Columns.Add(LColumn(nameof(cAttachmentHeader.SizeInBytes)));

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = new DataGridViewTextBoxCell();
                return lResult;
            }
        }

        private void dgvAttachment_CurrentCellChanged(object sender, EventArgs e)
        {
            cmdDownload.Enabled = (dgvAttachment.CurrentCell != null);
        }
    }
}
