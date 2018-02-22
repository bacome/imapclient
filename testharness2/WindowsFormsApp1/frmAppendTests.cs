using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmAppendTests : Form
    {
        private enum eAppendTestOrder { sendthenappend, appendthensend }

        private readonly cIMAPClient mClient; // inbox 
        private readonly cIMAPClient mSentItemsClient;

        private cMailbox mInbox;
        private cMailbox mSentItems;

        private bool mRunning = false;
        private cSMTPClient mSMTPClient = null;
        private CancellationTokenSource mCancellationTokenSource = null;
        private cCheckForMessagesFlag mCheckForMessagesFlag;

        public frmAppendTests(cIMAPClient pClient, cIMAPClient pSentItemsClient)
        {
            mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
            mSentItemsClient = pSentItemsClient ?? throw new ArgumentNullException(nameof(pSentItemsClient));
            InitializeComponent();
        }

        private void frmAppendTests_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - append tests - " + mClient.InstanceName;

            mClient.PropertyChanged += mClientPropertyChanged;
            mSentItemsClient.PropertyChanged += mClientPropertyChanged;

            ZSetControlState();


        }

        private void mClientPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(cIMAPClient.ConnectionState)) ZSetControlState();
        }

        private void ZSetControlState()
        {
            if (!mRunning && 
                mClient.IsConnected && mClient.SelectedMailbox?.IsInbox == true &&
                mSentItemsClient.IsConnected && mSentItemsClient.SelectedMailbox != null && !mSentItemsClient.SelectedMailbox.IsInbox && mSentItemsClient.SelectedMailbox.IsSelectedForUpdate)
            {
                cmdTests.Enabled = true;
                cmdCurrentTest.Enabled = true;
            }
            else
            {
                cmdTests.Enabled = false;
                cmdCurrentTest.Enabled = false;
            }

            cmdCancel.Enabled = mRunning;
        }

        private void ZValTextBoxNotBlank(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (string.IsNullOrWhiteSpace(lSender.Text))
            {
                e.Cancel = true;
                erp.SetError(lSender, "required field");
            }
        }

        private void ZValTextBoxIsPortNumber(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            if (!int.TryParse(lSender.Text, out var i) || i < 1 || i > 9999)
            {
                e.Cancel = true;
                erp.SetError(lSender, "port number should be 1 .. 9999");
            }
        }

        private void ZValTextBoxIsEmailAddress(object sender, CancelEventArgs e)
        {
            if (!(sender is TextBox lSender)) return;

            try
            {
                MailAddress lAddress = new MailAddress(lSender.Text);
            }
            catch
            {
                e.Cancel = true;
                erp.SetError(lSender, "doesn't appear to be an email address");
            }
        }

        private void ZValControlValidated(object sender, EventArgs e)
        {
            erp.SetError((Control)sender, null);
        }

        private void frmAppendTests_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mSMTPClient != null) mSMTPClient.SendAsyncCancel();
            if (mCancellationTokenSource != null) mCancellationTokenSource.Cancel();

            // to allow closing with validation errors
            e.Cancel = false;
        }

        private bool ZInit()
        {
            if (mRunning) return false;
            if (!ValidateChildren(ValidationConstraints.Enabled)) return false;

            mInbox = mClient.SelectedMailbox;
            mSentItems = mSentItemsClient.SelectedMailbox;

            if (MessageBox.Show(this, $"warning: this will send messages via SMTP expecting the messages to appear in the second instance and will append messages to the first instance", "send and append messages?", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return false;

            rtx.Clear();

            return true;
        }

        private void ZProgress(string pProgress)
        {
            rtx.AppendText(pProgress + "\n");
            rtx.ScrollToCaret();
        }

        private async void cmdTests_Click(object sender, EventArgs e)
        {
            if (!ZInit()) return;

            ZProgress("init");

            try
            {
                using (var lSMTPClient = new cSMTPClient(txtHost.Text.Trim(), int.Parse(txtPort.Text), chkSSL.Checked, txtUserId.Text.Trim(), txtPassword.Text.Trim()))
                using (var lCancellationTokenSource = new CancellationTokenSource())
                using (var lCheckForMessagesFlag = new cCheckForMessagesFlag(mInbox, lCancellationTokenSource.Token))
                {
                    mRunning = true;
                    mSMTPClient = lSMTPClient;
                    mCancellationTokenSource = lCancellationTokenSource;
                    mCheckForMessagesFlag = lCheckForMessagesFlag;
                    ZSetControlState();

                    ZProgress(" 1");
                    await ZSimpleTestAsync("imaptest1@dovecot.bacome.work", "a simple test message", "i want something here that shows it has been encoded so an '=' should do the trick.");


                    ZProgress(" 2");
                    await ZSimpleTestAsync("imaptest1@dovecot.bacome.work", "an encod€d word subject", "i want something here that will be base64 encoded so a few '€€€€€€€€€€€€€€€€€€€€' should do the trick.");
                    //;?;





                    ZProgress("tidy up");
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    ZProgress(ex.ToString());
                    MessageBox.Show(this, "an error occurred:\n" + ex.ToString());
                }
            }
            finally
            {
                mCancellationTokenSource = null;
                mSMTPClient = null;
                mRunning = false;
                ZSetControlState();
            }

            ZProgress("end");
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            if (mSMTPClient != null) mSMTPClient.SendAsyncCancel();
            if (mCancellationTokenSource != null) mCancellationTokenSource.Cancel();
        }

        private async Task ZSimpleTestAsync(string pFrom, string pSubject, string pBody)
        {
            using (var lMailMessage = new MailMessage(pFrom, txtSendTo.Text.Trim(), pSubject, pBody))
            {
                ZProgress("  append then send");
                await ZSimpleTestAsync(eAppendTestOrder.appendthensend, lMailMessage);
                ZProgress("  send then append");
                await ZSimpleTestAsync(eAppendTestOrder.sendthenappend, lMailMessage);
            }
        }

        private async Task ZSimpleTestAsync(eAppendTestOrder pOrder, MailMessage pMailMessage)
        {
            string lMessageId = cMessageIdGenerator.MsgId();
            cUID lUID = null; // assignment to shut the compiler up
            bool lRemoveDraft = false;

            pMailMessage.Headers.Set(kHeaderFieldName.MessageId, lMessageId);

            if (pOrder == eAppendTestOrder.appendthensend)
            {
                ZProgress("   async append with cancellation and feedback");

                // show setting flags and progress
                using (var lProgress = new frmProgress("append"))
                {
                    lProgress.ShowAndFocus(this);
                    lUID = await mSentItems.AppendAsync(pMailMessage, cStorableFlags.Draft, null, new cAppendConfiguration(lProgress.CancellationToken, lProgress.SetMaximum, lProgress.Increment));
                }

                // mark as draft
                lRemoveDraft = true;
            }

            ZProgress("   smtp send");
            await mSMTPClient.SendAsync(pMailMessage);

            if (pOrder == eAppendTestOrder.sendthenappend)
            {
                ZProgress("   synchronous append");
                // show the simple API
                lUID = mSentItems.Append(pMailMessage);
                lRemoveDraft = false;
            }
            else
            {
                if (lUID != null) // rfc 4315 response received
                {
                    ZProgress("   uid update appended message");
                    await mSentItems.UIDStoreAsync(lUID, eStoreOperation.remove, cStorableFlags.Draft);
                    lRemoveDraft = false;
                }
            }

            List<cMessage> lMessages;

            // find the message that was appended
            //
            ZProgress("   search for appended message");
            if (lUID == null) lMessages = await mSentItems.MessagesAsync(cFilter.HeaderFieldContains(kHeaderFieldName.MessageId, lMessageId));
            else lMessages = await mSentItems.MessagesAsync(cFilter.UID == lUID);
            if (lMessages.Count != 1) throw new cTestsException("couldn't find appended message");
            var lAppendedMessage = lMessages[0];

            // if we appended before sending but didn't get the UID, mark the message as not draft now
            //
            if (lRemoveDraft)
            {
                ZProgress("   update appended message");
                lAppendedMessage.Draft = false;
            }

            // find the message that was sent

            ZProgress("   search for sent message");

            while (true)
            {
                // note if the instance isn't idling a manual poll might be required
                await mCheckForMessagesFlag.GetTask();
                mCheckForMessagesFlag.ResetFlag();
                lMessages = mInbox.Messages(cFilter.HeaderFieldContains(kHeaderFieldName.MessageId, lMessageId));
                if (lMessages.Count != 0) break;
            }

            if (lMessages.Count != 1) throw new cTestsException("couldn't find sent message");

            var lSentMessage = lMessages[0];

            // then compare the two

            ZProgress("   compare");

            if (lSentMessage.From.ToString() != lAppendedMessage.From.ToString()) throw new cTestsException("from not same");
            if (lSentMessage.Subject.ToString() != lAppendedMessage.Subject.ToString()) throw new cTestsException("subject not same");

            var lPlainTexts = await Task.WhenAll(lSentMessage.PlainTextAsync(), lAppendedMessage.PlainTextAsync());

            if (lPlainTexts[0].Length < lPlainTexts[1].Length || !lPlainTexts[0].StartsWith(lPlainTexts[1], StringComparison.InvariantCulture)) throw new cTestsException("plain text not same");

            // alternative views

            if (lSentMessage.BodyStructure is cMultiPartBody lSentAlternativeViews)
            {
                if (lSentAlternativeViews.SubTypeCode == eMultiPartBodySubTypeCode.mixed) lSentAlternativeViews = lSentAlternativeViews.Parts[0] as cMultiPartBody;
            }
            else lSentAlternativeViews = null;

            if (lAppendedMessage.BodyStructure is cMultiPartBody lAppendedAlternativeViews)
            {
                if (lAppendedAlternativeViews.SubTypeCode == eMultiPartBodySubTypeCode.mixed) lAppendedAlternativeViews = lAppendedAlternativeViews.Parts[0] as cMultiPartBody;
            }
            else lAppendedAlternativeViews = null;

            if (pMailMessage.AlternateViews.Count == 0)
            {
                if (lSentAlternativeViews != null || lAppendedAlternativeViews != null) throw new cTestsException("found unexpected alternative views");
            }
            else
            {
                if (lSentAlternativeViews == null || lSentAlternativeViews.SubTypeCode != eMultiPartBodySubTypeCode.alternative) throw new cTestsException("can't find sent alternative views");
                if (lAppendedAlternativeViews == null || lAppendedAlternativeViews.SubTypeCode != eMultiPartBodySubTypeCode.alternative) throw new cTestsException("can't find appended alternative views");

                if (pMailMessage.AlternateViews.Count != lSentAlternativeViews.Parts.Count || pMailMessage.AlternateViews.Count != lAppendedAlternativeViews.Parts.Count) throw new cTestsException("alternative view count not the same");

                for (int i = 0; i < pMailMessage.AlternateViews.Count; i++)
                {
                    // do something 
                }
            }

            if (pMailMessage.Attachments.Count != lSentMessage.Attachments.Count || pMailMessage.Attachments.Count != lAppendedMessage.Attachments.Count) throw new cTestsException("attachment count not the same");

            for (int i = 0; i < pMailMessage.Attachments.Count; i++)
            {
                // do something 
            }
        }
    }
}
