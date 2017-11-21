using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmUID : Form
    {
        private readonly BindingList<cUIDsRowData> mUIDsBindingList = new BindingList<cUIDsRowData>();
        private readonly cIMAPClient mClient;

        private List<Form> mChildren = new List<Form>();

        public frmUID(cIMAPClient pClient)
        {
            mClient = pClient;
            InitializeComponent();
        }

        private void frmUID_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - uid - " + mClient.InstanceName;
            mClient.PropertyChanged += mClient_PropertyChanged;
            ZGridInitialise();
            ZEnable();
        }

        private void ZGridInitialise()
        {
            var lTemplate = new DataGridViewTextBoxCell();

            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(LColumn(nameof(cUIDsRowData.UID)));

            dgv.DataSource = mUIDsBindingList;

            DataGridViewColumn LColumn(string pName)
            {
                DataGridViewColumn lResult = new DataGridViewColumn();
                lResult.DataPropertyName = pName;
                lResult.HeaderCell.Value = pName;
                lResult.CellTemplate = lTemplate;
                return lResult;
            }
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.IsConnected) || e.PropertyName == nameof(cIMAPClient.SelectedMailbox)) ZEnable();
        }

        private void ZEnable()
        {
            cMailbox lSelectedMailbox;

            if (mClient.IsConnected) lSelectedMailbox = mClient.SelectedMailbox;
            else lSelectedMailbox = null;

            if (lSelectedMailbox == null)
            {
                lblSelectedMailbox.Text = "No mailbox selected";
                tab.Enabled = false;
                return;
            }

            lblSelectedMailbox.Text = "Mailbox: " + lSelectedMailbox.Path;
            tab.Enabled = true;
            ZEnableFetch();
        }

        private void ZEnableFetch()
        {
            if (string.IsNullOrWhiteSpace(txtPart.Text)) rdoMime.Enabled = false;
            else
            {
                rdoMime.Enabled = true;
                if (rdoFields.Checked || rdoFieldsNot.Checked) txtFieldNames.Enabled = true;
                else txtFieldNames.Enabled = false;
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void ZValTextBoxIsUIDValidity(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (string.IsNullOrWhiteSpace(lSender.Text) || !uint.TryParse(lSender.Text, out var u) || u == 0)
            {
                e.Cancel = true;
                erp.SetError(lSender, "ID should be a non-zero uint");
            }
        }

        private void ZValTextBoxIsUID(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (string.IsNullOrWhiteSpace(lSender.Text)) return;

            if (!uint.TryParse(lSender.Text, out var u) || u == 0)
            {
                e.Cancel = true;
                erp.SetError(lSender, "ID should be a non-zero uint");
            }
        }

        private void ZValTextBoxIsPart(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            string lPart = lSender.Text.Trim();
            if (string.IsNullOrWhiteSpace(lPart)) return;

            try
            {
                cSection lSection = new cSection(lPart);
            }
            catch
            {
                e.Cancel = true;
                erp.SetError(lSender, "part should be a list of dot separated non-zero integers");
            }
        }

        private void ZValHeaderFieldNames(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lTextBox)) return;

            if (string.IsNullOrWhiteSpace(lTextBox.Text)) return;

            if (ZTryParseHeaderFieldNames(lTextBox.Text, out var lNames)) lTextBox.Text = ZHeaderFieldNames(lNames);
            else
            {
                e.Cancel = true;
                erp.SetError((Control)sender, "header field names must be printable ascii only");
            }
        }

        private bool ZTryParseHeaderFieldNames(string pText, out cHeaderFieldNames rNames)
        {
            if (pText == null) { rNames = null; return false; }

            List<string> lNames = new List<string>();
            foreach (var lName in pText.Trim().Split(' ', ':')) if (!string.IsNullOrWhiteSpace(lName)) lNames.Add(lName);

            if (lNames.Count == 0) { rNames = null; return false; }

            try { rNames = new cHeaderFieldNames(lNames); }
            catch { rNames = null; return false; }

            return true;
        }

        private string ZHeaderFieldNames(cHeaderFieldNames pNames)
        {
            if (pNames == null) return string.Empty;

            StringBuilder lBuilder = new StringBuilder();
            bool lFirst = true;

            foreach (var lName in pNames)
            {
                if (lFirst) lFirst = false;
                else lBuilder.Append(" ");
                lBuilder.Append(lName);
            }

            return lBuilder.ToString();
        }

        private void txtPart_TextChanged(object sender, EventArgs e)
        {
            ZEnableFetch();
        }

        private void rdoFields_CheckedChanged(object sender, EventArgs e)
        {
            ZEnableFetch();
        }

        private void rdoFieldsNot_CheckedChanged(object sender, EventArgs e)
        {
            ZEnableFetch();
        }

        private void ZChildAdd(Form pChild)
        {
            mChildren.Add(pChild);
            pChild.FormClosed += ZChildClosed;
            Program.Centre(pChild, this, mChildren);
            pChild.Show();
        }

        private void ZChildClosed(object sender, EventArgs e)
        {
            if (!(sender is frmProgress lForm)) return;
            lForm.FormClosed -= ZChildClosed;
            mChildren.Remove(lForm);
        }

        private void ZChildrenClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mChildren)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZChildClosed;
            }

            mChildren.Clear();

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }
        }

        private bool ZFetchParameters(out cUID rUID, out cSection rSection, out eDecodingRequired rDecoding)
        {
            rUID = null;
            rSection = null;
            rDecoding = eDecodingRequired.unknown;

            if (!ValidateChildren(ValidationConstraints.Enabled))
            {
                MessageBox.Show(this, "there are issues with the data entered");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUID.Text))
            {
                MessageBox.Show(this, "you must enter a UID");
                return false;
            }

            try
            {
                rUID = new cUID(uint.Parse(txtUIDValidity.Text), uint.Parse(txtUID.Text));

                string lPart;
                if (string.IsNullOrWhiteSpace(txtPart.Text)) lPart = null;
                else lPart = txtPart.Text.Trim();

                eSectionTextPart lTextPart;

                if (rdoAll.Checked) lTextPart = eSectionTextPart.all;
                else if (rdoHeader.Checked) lTextPart = eSectionTextPart.header;
                else if (rdoFields.Checked) lTextPart = eSectionTextPart.headerfields;
                else if (rdoFieldsNot.Checked) lTextPart = eSectionTextPart.headerfieldsnot;
                else if (rdoText.Checked) lTextPart = eSectionTextPart.text;
                else lTextPart = eSectionTextPart.mime;

                if (lTextPart == eSectionTextPart.headerfields || lTextPart == eSectionTextPart.headerfieldsnot)
                {
                    if (string.IsNullOrWhiteSpace(txtFieldNames.Text))
                    {
                        MessageBox.Show(this, "must enter some field names");
                        return false;
                    }

                    if (!ZTryParseHeaderFieldNames(txtFieldNames.Text, out var lNames))
                    {
                        MessageBox.Show(this, "must enter valid field names");
                        return false;
                    }

                    rSection = new cSection(lPart, lNames, rdoFieldsNot.Checked);
                }
                else rSection = new cSection(lPart, lTextPart);

                if (rdoNone.Checked) rDecoding = eDecodingRequired.none;
                else if (rdoQuotedPrintable.Checked) rDecoding = eDecodingRequired.quotedprintable;
                else if (rdoQuotedPrintable.Checked) rDecoding = eDecodingRequired.base64;

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, $"there are issues with the data entered:\n{e.ToString()}");
                return false;
            }
        }

        private async Task<bool> ZFetchToStream(cUID pUID, cSection pSection, eDecodingRequired pDecoding, string pTitle, Stream pStream)
        {
            frmProgress lProgress = null;

            try
            { 
                lProgress = new frmProgress(pTitle);
                ZChildAdd(lProgress);
                await mClient.SelectedMailbox.UIDFetchAsync(pUID, pSection, pDecoding, pStream, new cBodyFetchConfiguration(lProgress.CancellationToken, lProgress.Increment));
                return true;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"problem when fetching\n{ex}");
            }
            finally
            {
                if (lProgress != null) lProgress.Complete();
            }

            return false;
        }

        private async void cmdDisplay_Click(object sender, EventArgs e)
        {
            if (!ZFetchParameters(out var lUID, out var lSection, out var lDecoding)) return;

            string lData;

            using (MemoryStream lStream = new MemoryStream())
            {
                if (!await ZFetchToStream(lUID, lSection, lDecoding, $"fetching {lUID} {lSection}", lStream)) return;
                if (IsDisposed) return;

                lStream.Position = 0;
                bool lBufferedCR = false;
                StringBuilder lBuilder = new StringBuilder();

                while (lStream.Position < lStream.Length)
                {
                    int lByte = lStream.ReadByte();

                    if (lBufferedCR)
                    {
                        lBufferedCR = false;

                        if (lByte == 10)
                        {
                            lBuilder.AppendLine();
                            continue;
                        }

                        lBuilder.Append('¿');
                    }

                    if (lByte == 13) lBufferedCR = true;
                    else
                    {
                        if (lByte < 32 || lByte > 126) lBuilder.Append('¿');
                        else lBuilder.Append((char)lByte);
                    }
                }

                if (lBufferedCR) lBuilder.Append('¿');

                lData = lBuilder.ToString();
            }

            ZChildAdd(new frmMessageData(lUID, lSection, lDecoding, lData));
        }

        private async void cmdSaveAs_Click(object sender, EventArgs e)
        {
            if (!ZFetchParameters(out var lUID, out var lSection, out var lDecoding)) return;

            string lFileName = lUID.UID.ToString();

            if (lSection.Part != null) lFileName += "." + lSection.Part;

            if (lSection.TextPart == eSectionTextPart.header) lFileName += ".header";
            else if (lSection.TextPart == eSectionTextPart.headerfields || lSection.TextPart == eSectionTextPart.headerfieldsnot) lFileName += ".fields";
            else if (lSection.TextPart == eSectionTextPart.text) lFileName += ".text";
            else if (lSection.TextPart == eSectionTextPart.mime) lFileName += ".mime";

            var lSaveFileDialog = new SaveFileDialog();
            lSaveFileDialog.FileName = lFileName;
            if (lSaveFileDialog.ShowDialog() != DialogResult.OK) return;

            using (FileStream lStream = new FileStream(lSaveFileDialog.FileName, FileMode.Create))
            {
                await ZFetchToStream(lUID, lSection, lDecoding, $"saving {lUID} {lSection} as {lSaveFileDialog.FileName}", lStream);
            }
        }

        private void dgv_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (!(dgv.Rows[e.RowIndex].DataBoundItem is cUIDsRowData lRowData)) return;

            string lErrorText = lRowData.ErrorText;

            if (lErrorText != null)
            {
                e.Cancel = true;
                dgv.Rows[e.RowIndex].ErrorText = lErrorText;
            }
        }

        private void dgv_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            dgv.Rows[e.RowIndex].ErrorText = null;
        }

        private bool ZUIDs(out List<cUID> rUIDs)
        {
            rUIDs = new List<cUID>();

            if (!ValidateChildren(ValidationConstraints.Enabled))
            {
                MessageBox.Show(this, "there are issues with the data entered");
                return false;
            }

            if (mUIDsBindingList.Distinct().Count() < mUIDsBindingList.Count)
            {
                MessageBox.Show(this, "there are duplicate UIDs");
                return false;
            }

            try
            {
                var lUIDValidity = uint.Parse(txtUIDValidity.Text);
                foreach (var lUID in mUIDsBindingList) rUIDs.Add(new cUID(lUIDValidity, lUID.mUID));
            }
            catch (Exception e)
            {
                MessageBox.Show(this, $"there are issues with the data entered:\n{e.ToString()}");
                return false;
            }

            return true;
        }

        private async void cmdCopy_Click(object sender, EventArgs e)
        {
            if (!ZUIDs(out var lUIDs)) return;

            cMailbox lMailbox;

            using (frmMailboxDialog lMailboxDialog = new frmMailboxDialog(mClient))
            {
                if (lMailboxDialog.ShowDialog(this) != DialogResult.OK) return;
                lMailbox = lMailboxDialog.Mailbox;
            }

            cCopyFeedback lFeedback;

            try { lFeedback = await mClient.SelectedMailbox.UIDCopyAsync(lUIDs, lMailbox); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"copy error\n{ex}");
                return;
            }

            if (!IsDisposed && lFeedback != null) MessageBox.Show(this, $"copied as {lFeedback}");
        }

        private async void cmdStore_Click(object sender, EventArgs e)
        {
            if (!ZUIDs(out var lUIDs)) return;

            eStoreOperation lOperation;
            cStorableFlags lFlags;
            ulong? lIfUnchangedSinceModSeq;

            using (frmStoreDialog lStoreDialog = new frmStoreDialog())
            {
                if (lStoreDialog.ShowDialog(this) != DialogResult.OK) return;

                lOperation = lStoreDialog.Operation;
                lFlags = lStoreDialog.Flags;
                lIfUnchangedSinceModSeq = lStoreDialog.IfUnchangedSinceModSeq;
            }

            cUIDStoreFeedback lFeedback;

            try { lFeedback = await mClient.SelectedMailbox.UIDStoreAsync(lUIDs, lOperation, lFlags, lIfUnchangedSinceModSeq); }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"store error\n{ex}");
                return;
            }

            var lSummary = lFeedback.Summary();

            if (lSummary.LikelyOKCount == lFeedback.Count) return; // all messages were updated or didn't need updating

            if (lSummary.LikelyWorthPolling)
            {
                // see if polling the server helps explain any possible failures
                try { await mClient.PollAsync(); }
                catch { }

                // re-get the summary
                lSummary = lFeedback.Summary();

                // re-check the summary
                if (lSummary.LikelyOKCount == lFeedback.Count) return; // all messages were updated or didn't need updating
            }

            if (IsDisposed) return;
            MessageBox.Show(this, $"(some of) the messages don't appear to have been updated: {lSummary}");
        }

        private void frmUID_FormClosing(object sender, FormClosingEventArgs e)
        {
            ZChildrenClose();

            // to allow closing with validation errors
            e.Cancel = false;
        }

        private void frmUID_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
        }











        private class cUIDsRowData
        {
            private uint? _UID = null;

            public cUIDsRowData() { }

            public string UID
            {
                get
                {
                    if (_UID == null) return null;
                    return _UID.Value.ToString();
                }

                set
                {
                    if (uint.TryParse(value, out var lUID) && lUID > 0) _UID = lUID;
                    else _UID = null;
                }
            }

            public string ErrorText
            {
                get
                {
                    if (_UID == null) return "must specify a UID";
                    return null;
                }
            }

            public uint mUID => _UID.Value;

            public override bool Equals(object pObject) => this == pObject as cUIDsRowData;

            public override int GetHashCode() => _UID.GetHashCode();

            public static bool operator ==(cUIDsRowData pA, cUIDsRowData pB)
            {
                if (ReferenceEquals(pA, pB)) return true;
                if (ReferenceEquals(pA, null)) return false;
                if (ReferenceEquals(pB, null)) return false;
                return (pA._UID == pB._UID);
            }

            public static bool operator !=(cUIDsRowData pA, cUIDsRowData pB) => !(pA == pB);
        }
    }
}
