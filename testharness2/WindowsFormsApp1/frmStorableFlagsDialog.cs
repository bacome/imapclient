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
    public partial class frmStorableFlagsDialog : Form
    {
        private readonly cStorableFlags mFlags;

        public frmStorableFlagsDialog(cStorableFlags pFlags)
        {
            mFlags = pFlags;
            InitializeComponent();
        }

        private void frmStorableFlagsDialog_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - storable flags dialog";

            if (mFlags == null) return;

            string lFlags = null;

            foreach (var lFlag in mFlags)
            {
                if (lFlag.Equals(kMessageFlag.Answered, StringComparison.InvariantCultureIgnoreCase)) chkAnswered.Checked = true;
                else if (lFlag.Equals(kMessageFlag.Deleted, StringComparison.InvariantCultureIgnoreCase)) chkDeleted.Checked = true;
                else if (lFlag.Equals(kMessageFlag.Draft, StringComparison.InvariantCultureIgnoreCase)) chkDraft.Checked = true;
                else if (lFlag.Equals(kMessageFlag.Flagged, StringComparison.InvariantCultureIgnoreCase)) chkFlagged.Checked = true;
                else if (lFlag.Equals(kMessageFlag.Seen, StringComparison.InvariantCultureIgnoreCase)) chkSeen.Checked = true;
                else if (lFlag.Equals(kMessageFlag.Forwarded, StringComparison.InvariantCultureIgnoreCase)) chkForwarded.Checked = true;
                else if (lFlag.Equals(kMessageFlag.SubmitPending, StringComparison.InvariantCultureIgnoreCase)) chkSubmitPending.Checked = true;
                else if (lFlag.Equals(kMessageFlag.Submitted, StringComparison.InvariantCultureIgnoreCase)) chkSubmitted.Checked = true;
                else
                {
                    if (lFlags == null) lFlags = lFlag;
                    else lFlags += " " + lFlag;
                }
            }

            txtFlags.Text = lFlags;
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

                if (ZTryParseFlagNames(txtFlags.Text, out var lMoreFlags) && lMoreFlags != null) lFlags.Add(lMoreFlags);

                return lFlags;
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
            if (pText == null) { rFlags = cStorableFlags.Empty; return true; }

            List<string> lFlags = new List<string>();
            foreach (var lFlag in pText.Trim().Split(' ')) if (!string.IsNullOrWhiteSpace(lFlag)) lFlags.Add(lFlag);

            if (lFlags.Count == 0) { rFlags = cStorableFlags.Empty; return true; }

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

        private void cmdOK_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;
            DialogResult = DialogResult.OK;
        }
    }
}
