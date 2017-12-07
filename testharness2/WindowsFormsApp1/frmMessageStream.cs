using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmMessageStream : Form
    {
        private readonly string mInstanceName;
        private readonly cMessage mMessage;
        private readonly cMailbox mMailbox;
        private readonly cUID mUID;
        private readonly cSection mSection;
        private readonly eDecodingRequired mDecoding;
        private readonly int mTargetBufferSize;
        private readonly string mPath;

        private cMessageStream mMessageStream = null;
        private FileStream mFileStream = null;
        private bool mEOF = false;

        public frmMessageStream(string pInstanceName, cMessage pMessage, cSection pSection, eDecodingRequired pDecoding, int pTargetBufferSize, string pPath)
        {
            mInstanceName = pInstanceName;
            mMessage = pMessage;
            mMailbox = null;
            mUID = null;
            mSection = pSection;
            mDecoding = pDecoding;
            mTargetBufferSize = pTargetBufferSize;
            mPath = pPath;
            InitializeComponent();
        }

        public frmMessageStream(string pInstanceName, cMailbox pMailbox, cUID pUID, cSection pSection, eDecodingRequired pDecoding, int pTargetBufferSize, string pPath)
        {
            mInstanceName = pInstanceName;
            mMessage = null;
            mMailbox = pMailbox;
            mUID = pUID;
            mSection = pSection;
            mDecoding = pDecoding;
            mTargetBufferSize = pTargetBufferSize;
            mPath = pPath;
            InitializeComponent();
        }

        private void frmMessageStream_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - message stream - " + mInstanceName + " - " + mPath;
        }

        private void frmMessageStream_Shown(object sender, EventArgs e)
        {
            try
            {
                if (mMessage == null) mMessageStream = new cMessageStream(mMailbox, mUID, mSection, mDecoding, mTargetBufferSize);
                else mMessageStream = new cMessageStream(mMessage, mSection, mDecoding, mTargetBufferSize);
                mFileStream = new FileStream(mPath, FileMode.Create);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"a problem occurred\n{ex}");
                return;
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

            if (!int.TryParse(lSender.Text, out var i) || i < 1 || i > 1000000)
            {
                e.Cancel = true;
                erp.SetError(lSender, "number of bytes should be 1 .. 1000000");
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private async void cmdCopy_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled)) return;

            if (mMessageStream == null || mFileStream == null) return;

            cmdCopy.Enabled = false;

            try
            {
                int lTimeout = int.Parse(txtTimeout.Text.Trim());
                int lBytesToCopy = int.Parse(txtBytesToCopy.Text.Trim());

                byte[] lBuffer = new byte[lBytesToCopy];
                mMessageStream.ReadTimeout = lTimeout;

                lblCopied.Text = "reading ...";

                int lBytesRead = await mMessageStream.ReadAsync(lBuffer, 0, lBytesToCopy);

                lblCopied.Text = lBytesRead.ToString() + " read";

                if (lBytesRead == 0)
                {
                    mEOF = true;
                    lblCopied.Text = "EOF!";
                }
                else
                {
                    await mFileStream.WriteAsync(lBuffer, 0, lBytesRead);
                    lblCopied.Text = lBytesRead.ToString() + " written";
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"a problem occurred\n{ex}");
                return;
            }
            finally
            {
                if (!IsDisposed && !mEOF) cmdCopy.Enabled = true;
            }
        }

        private void cmdRefresh_Click(object sender, EventArgs e)
        {
            if (mMessageStream == null) lblCurrentBufferSize.Text = "<no buffer>";
            else lblCurrentBufferSize.Text = mMessageStream.CurrentBufferSize.ToString();
        }

        private void frmMessageStream_FormClosing(object sender, FormClosingEventArgs e)
        {
            // to allow closing with validation errors
            e.Cancel = false;
        }

        private void frmMessageStream_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (mMessageStream != null) mMessageStream.Dispose();
            if (mFileStream != null) mFileStream.Dispose();
        }
    }
}
