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
    public partial class frmStoreDialog : Form
    {
        public frmStoreDialog()
        {
            InitializeComponent();
        }

        private void frmStoreDialog_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - store dialog";
        }

        public eStoreOperation Operation
        {
            get
            {
                if (rdoAdd.Checked) return eStoreOperation.add;
                if (rdoRemove.Checked) return eStoreOperation.remove;
                return eStoreOperation.replace;
            }
        }

        public cStorableFlags Flags
        {
            get
            {
                cStorableFlagList lFlags = new cStorableFlagList();

                if (chkAnswered.Checked) lFlags.Add(kMessageFlag.Answered);
                if (chkDeleted.Checked) lFlags.Add(kMessageFlag.Deleted);
                if (chkDraft.Checked) lFlags.Add(kMessageFlag.Draft);
                if (chkFlagged.Checked) lFlags.Add(kMessageFlag.Flagged);
                if (chkSeen.Checked) lFlags.Add(kMessageFlag.Seen);

                if (chkForwarded.Checked) lFlags.Add(kMessageFlag.Forwarded);
                if (chkSubmitPending.Checked) lFlags.Add(kMessageFlag.SubmitPending);
                if (chkSubmitted.Checked) lFlags.Add(kMessageFlag.Submitted);

                // see comments in the library as to why this is commented out
                //if (chkMDNSent.Checked) lFlags.Add(kMessageFlagName.MDNSent);

                if (ZTryParseFlagNames(txtFlags.Text, out var lMoreFlags) && lMoreFlags != null) lFlags.Add(lMoreFlags);

                return lFlags;
            }
        }

        public ulong? IfUnchangedSinceModSeq
        {
            get
            {
                if (string.IsNullOrWhiteSpace(txtIfUnchangedSinceModSeq.Text)) return null;
                if (ulong.TryParse(txtIfUnchangedSinceModSeq.Text, out var lResult)) return lResult;
                return null;
            }
        }

        private void ZValTextBoxIsModSeqOrNull(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (string.IsNullOrWhiteSpace(lSender.Text)) return;

            if (!ulong.TryParse(lSender.Text, out var lResult) || lResult < 1)
            {
                e.Cancel = true;
                erp.SetError(lSender, "modseq should be an unsigned long greater than zero");
            }
        }

        private void ZValFlagNames(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lTextBox)) return;

            if (ZTryParseFlagNames(lTextBox.Text, out var lFlags)) lTextBox.Text = ZFlagNames(lFlags);
            else
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "must be valid rfc3501 flag names separated by spaces");
            }
        }

        private bool ZTryParseFlagNames(string pText, out cStorableFlags rFlags)
        {
            if (pText == null) { rFlags = cStorableFlags.None; return true; }

            List<string> lFlags = new List<string>();
            foreach (var lFlag in pText.Trim().Split(' ')) if (!string.IsNullOrWhiteSpace(lFlag)) lFlags.Add(lFlag);

            if (lFlags.Count == 0) { rFlags = cStorableFlags.None; return true; }

            try { rFlags = new cStorableFlags(lFlags); }
            catch { rFlags = null; return false; }

            return true;
        }

        private string ZFlagNames(cStorableFlags pFlags)
        {
            if (pFlags == null || pFlags.Count == 0) return string.Empty;

            StringBuilder lBuilder = new StringBuilder();
            bool lFirst = true;

            foreach (var lFlag in pFlags)
            {
                if (lFirst) lFirst = false;
                else lBuilder.Append(" ");
                lBuilder.Append(lFlag);
            }

            return lBuilder.ToString();
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void frmStoreDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // TODO: check if this is required
            // to allow closing with validation errors
            //e.Cancel = false;
        }

        private void cmdStore_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;
            DialogResult = DialogResult.OK;
        }
    }
}
