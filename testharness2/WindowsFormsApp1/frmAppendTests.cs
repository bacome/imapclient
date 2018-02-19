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

            pMailMessage.Headers.Set(kHeaderFieldName.MessageId, lMessageId);

            if (pMailMessage.AlternateViews.Count > 0 || pMailMessage.Attachments.Count > 0)
            {
                // in this case the conversion to cMailMessageAppendData may need to use temporary files, so we have to dispose the converted object

                ZProgress("   disposable");

                cStorableFlags lFlags;

                if (pOrder == eAppendTestOrder.sendthenappend)
                {
                    ZProgress("    smtp send");
                    await mSMTPClient.SendAsync(pMailMessage);
                    lFlags = cStorableFlags.Empty;
                }
                else lFlags = cStorableFlags.Draft;

                ZProgress("    append");

                using (var lProgress = new frmProgress("convert message"))
                {
                    lProgress.ShowAndFocus(this);
                    
                    using (var lAppendData = await cMailMessageAppendData.ConstructAsync(pMailMessage, lFlags, null, null, -1, lProgress.CancellationToken, lProgress.Increment))
                    {
                        lUID = await mSentItems.AppendAsync(lAppendData, new cAppendConfiguration(lProgress.CancellationToken, lProgress.SetMaximum, lProgress.Increment));
                    }
                }

                if (pOrder == eAppendTestOrder.appendthensend)
                {
                    ZProgress("    smtp send");
                    await mSMTPClient.SendAsync(pMailMessage);
                }
            }
            else
            {
                ZProgress("   not disposable");

                if (pOrder == eAppendTestOrder.appendthensend)
                {
                    ZProgress("    append");
                    // mark as draft
                    lUID = await mSentItems.AppendAsync(new cMailMessageAppendData(pMailMessage, cStorableFlags.Draft));
                }

                ZProgress("    smtp send");
                await mSMTPClient.SendAsync(pMailMessage);

                if (pOrder == eAppendTestOrder.sendthenappend)
                {
                    ZProgress("    append");
                    // demonstrate simple API; uses the instance default flags
                    lUID = await mSentItems.AppendAsync(pMailMessage); 
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

            // if we appended before sending, mark the message as not draft
            //
            if (pOrder == eAppendTestOrder.appendthensend)
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

            if (lSentMessage.From.ToString() != lAppendedMessage.From.ToString()) throw new cTestsException("from not same");
            if (lSentMessage.Subject.ToString() != lAppendedMessage.Subject.ToString()) throw new cTestsException("subject not same");

            var lPlainTexts = await Task.WhenAll(lSentMessage.PlainTextAsync(), lAppendedMessage.PlainTextAsync());

            // note that smtpclient may add some crlfs at the end 
            //  and that the bodystructure is a simple one 
            //  maybe just compare the number of alternates and the number of attachments and the attachment contents


            // TODO
            // if (!ZCompareBodyStructure(lSentMessage.BodyStructure, lAppendedMessage.BodyStructure)) throw new cTestsException("body structure not same");






        }
    }
}
