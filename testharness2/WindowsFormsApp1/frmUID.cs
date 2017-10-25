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
        private readonly cIMAPClient mClient;

        private List<Form> mDownloads = new List<Form>();

        public frmUID(cIMAPClient pClient)
        {
            mClient = pClient;
            InitializeComponent();
        }

        private void frmUID_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - uid - " + mClient.InstanceName;
            mClient.PropertyChanged += mClient_PropertyChanged;
            ZEnable();
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
            if (string.IsNullOrWhiteSpace(txtPart.Text))
            {
                rdoFields.Enabled = false;
                rdoFieldsNot.Enabled = false;
                txtFieldNames.Enabled = false;
                rdoMime.Enabled = false;
            }
            else
            {
                rdoFields.Enabled = true;
                rdoFieldsNot.Enabled = true;
                rdoMime.Enabled = true;

                if (rdoFields.Checked || rdoFieldsNot.Checked) txtFieldNames.Enabled = true;
                else txtFieldNames.Enabled = false;
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void ZValTextBoxIsID(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            string lID = lSender.Text.Trim();
            if (string.IsNullOrWhiteSpace(lID)) return;

            if (!uint.TryParse(lID, out var u) || u == 0)
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

        private void ZDownloadAdd(frmProgress pProgress)
        {
            mDownloads.Add(pProgress);
            pProgress.FormClosed += ZDownloadClosed;
            Program.Centre(pProgress, this, mDownloads);
            pProgress.Show();
        }

        private void ZDownloadClosed(object sender, EventArgs e)
        {
            if (!(sender is frmProgress lForm)) return;
            lForm.FormClosed -= ZDownloadClosed;
            mDownloads.Remove(lForm);
        }

        private void ZDownloadsClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mDownloads)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZDownloadClosed;
            }

            mDownloads.Clear();

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }
        }

        private async void cmdSaveAs_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            frmProgress lProgress = null;

            try
            {
                cUID lUID = new cUID(uint.Parse(txtUIDValidity.Text), uint.Parse(txtUID.Text));

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

                cSection lSection;

                if (lTextPart == eSectionTextPart.headerfields || lTextPart == eSectionTextPart.headerfieldsnot)
                {
                    if (string.IsNullOrWhiteSpace(txtFieldNames.Text))
                    {
                        MessageBox.Show(this, "must enter some field names");
                        return;
                    }

                    if (!ZTryParseHeaderFieldNames(txtFieldNames.Text, out var lNames))
                    {
                        MessageBox.Show(this, "must enter valid field names");
                        return;
                    }

                    lSection = new cSection(lPart, lNames, rdoFieldsNot.Checked);
                }
                else lSection = new cSection(lPart, lTextPart);

                eDecodingRequired lDecoding;

                if (rdoNone.Checked) lDecoding = eDecodingRequired.none;
                else if (rdoQuotedPrintable.Checked) lDecoding = eDecodingRequired.quotedprintable;
                else if (rdoQuotedPrintable.Checked) lDecoding = eDecodingRequired.base64;
                else lDecoding = eDecodingRequired.unknown;

                string lFileName = lUID.UID.ToString();

                if (lPart != null) lFileName += "." + lPart;

                if (lTextPart == eSectionTextPart.header) lFileName += ".header";
                else if (lTextPart == eSectionTextPart.headerfields || lTextPart == eSectionTextPart.headerfieldsnot) lFileName += ".fields";
                else if (lTextPart == eSectionTextPart.text) lFileName += ".text";
                else if (lTextPart == eSectionTextPart.mime) lFileName += ".mime";

                var lSaveFileDialog = new SaveFileDialog();
                lSaveFileDialog.FileName = lFileName;
                if (lSaveFileDialog.ShowDialog() != DialogResult.OK) return;

                lProgress = new frmProgress("saving " + lSaveFileDialog.FileName);

                ZDownloadAdd(lProgress);

                using (FileStream lStream = new FileStream(lSaveFileDialog.FileName, FileMode.Create))
                {
                    await mClient.SelectedMailbox.UIDFetchAsync(lUID, lSection, lDecoding, lStream, new cBodyFetchConfiguration(lProgress.CancellationToken, lProgress.Increment));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"problem when saving\n{ex}");
            }
            finally
            {
                if (lProgress != null) lProgress.Complete();
            }
        }

        private void frmUID_FormClosing(object sender, FormClosingEventArgs e)
        {
            ZDownloadsClose();

            // to allow closing with validation errors
            e.Cancel = false;
        }

        private void frmUID_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
        }
    }
}
