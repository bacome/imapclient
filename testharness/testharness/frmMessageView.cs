using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;
using work.bacome.trace;

namespace testharness
{
    public partial class frmMessageView : Form
    {
        private cTrace.cContext mRootContext = Program.Trace.NewRoot(nameof(frmMessageStructure), true);
        private cMessage mMessage;

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

        private int mDisplayMessageAsync = 0;

        public async Task DisplayMessageAsync(cMessage pMessage, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(frmMessageStructure), nameof(DisplayMessageAsync));

            // defend against re-entry during the awaits
            var lDisplayMessageAsync = ++mDisplayMessageAsync;

            dgvAttachment.DataSource = null;
            lblFlags.Text = "";
            rtxTextPlain.Clear();

            if (mMessage != null)
            {
                mMessage.Expunged -= ZExpunged;
                mMessage.PropertiesSet -= ZPropertiesSet;
                mMessage = null;
            }

            if (pMessage != null)
            {
                mMessage = pMessage;
                mMessage.Expunged += ZExpunged;
                mMessage.PropertiesSet += ZPropertiesSet;

                ZDisplayFlags();

                if (!mMessage.IsExpunged)
                {
                    BindingSource lBindingSource = new BindingSource();
                    foreach (var lAttachment in mMessage.Attachments) lBindingSource.Add(new cAttachmentHeader(lAttachment));

                    dgvAttachment.DataSource = lBindingSource;

                    rtxTextPlain.Clear();
                    Program.DisplayAddresses(rtxTextPlain, "From: ", mMessage.From);
                    Program.DisplayAddresses(rtxTextPlain, "To: ", mMessage.To);
                    Program.DisplayAddresses(rtxTextPlain, "CC: ", mMessage.CC);
                    rtxTextPlain.AppendText("Subject: " + mMessage.Subject + "\n");
                    rtxTextPlain.AppendText("\n");

                    string lText;

                    try { lText = await mMessage.GetPlainTextAsync(); }
                    catch (Exception e) { lText = $"there was an error getting the text: {e}"; }

                    if (lDisplayMessageAsync != mDisplayMessageAsync) return; // check if we've been re-entered during the await

                    rtxTextPlain.AppendText(lText);
                }
            }

            ZDGVAttachmentCoordinateChildren();
        }

        private void ZExpunged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, mMessage)) ZDGVAttachmentCoordinateChildren();
        }

        private void ZPropertiesSet(object sender, cPropertiesSetEventArgs e)
        {
            if (ReferenceEquals(sender, mMessage) && (e.Set & fMessageProperties.flags) != 0) ZDisplayFlags();
        }

        private void dgvAttachment_CurrentCellChanged(object sender, EventArgs e)
        {
            ZDGVAttachmentCoordinateChildren();
        }

        private void ZDGVAttachmentCoordinateChildren()
        {
            cmdDownload.Enabled = mMessage != null && !mMessage.IsExpunged && (dgvAttachment.CurrentCell != null);
        }

        private void ZDisplayFlags()
        {
            lblFlags.Text = mMessage.Flags.ToString();
        }

        private void cmdDownload_Click(object sender, EventArgs e)
        {
            if (dgvAttachment.CurrentCell?.OwningRow.DataBoundItem is cAttachmentHeader lAH) frmDownload.Download(lAH.Attachment);
        }
    }
}
