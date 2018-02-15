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
    public partial class frmQPEncoder : Form
    {
        private List<Form> mEncodes = new List<Form>();


        public frmQPEncoder()
        {
            InitializeComponent();
        }

        private void ZEncodesAdd(frmProgress pProgress)
        {
            mEncodes.Add(pProgress);
            pProgress.FormClosed += ZEncodeClosed;
            Program.Centre(pProgress, this, mEncodes);
            pProgress.Show();
        }

        private void ZEncodeClosed(object sender, EventArgs e)
        {
            if (!(sender is frmProgress lForm)) return;
            lForm.FormClosed -= ZEncodeClosed;
            mEncodes.Remove(lForm);
        }

        private void ZEncodesClose()
        {
            List<Form> lForms = new List<Form>();

            foreach (var lForm in mEncodes)
            {
                lForms.Add(lForm);
                lForm.FormClosed -= ZEncodeClosed;
            }

            mEncodes.Clear();

            foreach (var lForm in lForms)
            {
                try { lForm.Close(); }
                catch { }
            }
        }

        private void ZValTextBoxIsTimeout(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < -1 || i > 99999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "timeout should be a number -1 .. 99999");
            }
        }

        private void ZValTextBoxIsMilliseconds(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 100 || i > 9999999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "time in ms should be a number 100 .. 9999999");
            }
        }

        private void ZValTextBoxIsNumberOfBytes(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 1)
            {
                e.Cancel = true;
                erp.SetError(lSender, "number of bytes should be 1 .. " + int.MaxValue);
            }
        }

        private void ZValBatchSizerConfiguration(GroupBox pGBX, TextBox pMin, TextBox pMax, TextBox pInitial, CancelEventArgs e)
        {
            if (!int.TryParse(pMin.Text, out var lMin)) return;
            if (!int.TryParse(pMax.Text, out var lMax)) return;
            if (!int.TryParse(pInitial.Text, out var lInitial)) return;

            if (lMin > lInitial || lInitial > lMax)
            {
                e.Cancel = true;
                erp.SetError(pGBX, "min <= inital <= max");
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void gbxRead_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxRead, txtRMin, txtRMax, txtRInitial, e);
        }

        private void gbxWrite_Validating(object sender, CancelEventArgs e)
        {
            ZValBatchSizerConfiguration(gbxWrite, txtWMin, txtWMax, txtWInitial, e);
        }

        private async void cmdEncode_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;


            var lOpenFileDialog = new OpenFileDialog();
            if (lOpenFileDialog.ShowDialog() != DialogResult.OK) return;

            var lSaveFileDialog = new SaveFileDialog();
            lSaveFileDialog.FileName = lOpenFileDialog.FileName + ".qpe";
            if (lSaveFileDialog.ShowDialog() != DialogResult.OK) return;

            eQuotedPrintableSourceType lSourceType;

            if (rdoBinary.Checked) lSourceType = eQuotedPrintableSourceType.Binary;
            else if (rdoLF.Checked) lSourceType = eQuotedPrintableSourceType.LFTerminatedLines;
            else lSourceType = eQuotedPrintableSourceType.CRLFTerminatedLines;

            eQuotedPrintableQuotingRule lQuotingRule;

            if (rdoEBCDIC.Checked) lQuotingRule = eQuotedPrintableQuotingRule.EBCDIC;
            else lQuotingRule = eQuotedPrintableQuotingRule.Minimal;

            frmProgress lProgress = null;

            try
            {
                int lTimeout = int.Parse(txtTimeout.Text);

                cBatchSizerConfiguration lReadConfiguration= new cBatchSizerConfiguration(int.Parse(txtRMin.Text), int.Parse(txtRMax.Text), int.Parse(txtRMaxTime.Text), int.Parse(txtRInitial.Text));
                cBatchSizerConfiguration lWriteConfiguration = new cBatchSizerConfiguration(int.Parse(txtWMin.Text), int.Parse(txtWMax.Text), int.Parse(txtWMaxTime.Text), int.Parse(txtWInitial.Text));

                using (var lSource = new FileStream(lOpenFileDialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    lProgress = new frmProgress("encoding " + lOpenFileDialog.FileName, lSource.Length);
                    ZEncodesAdd(lProgress);

                    using (FileStream lTarget = new FileStream(lSaveFileDialog.FileName, FileMode.Create))
                    {
                        await cQuotedPrintableEncoder.EncodeAsync(lSource, lSourceType, lQuotingRule, lTarget, lTimeout, lProgress.CancellationToken, lProgress.Increment, lReadConfiguration, lWriteConfiguration);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"problem when encoding'\n{ex}");
            }
            finally
            {
                if (lProgress != null) lProgress.Complete();
            }
        }

        private void frmQPEncoder_FormClosing(object sender, FormClosingEventArgs e)
        {
            ZEncodesClose();

            // to allow closing with validation errors
            e.Cancel = false;
        }
    }
}
