using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace testharness2
{
    public sealed class cSMTPClient : IDisposable
    {
        private bool mDisposed = false;
        private SmtpClient mClient;

        public cSMTPClient(string pHost, int pPort, bool pSSL, string pUserId, string pPassword)
        {
            try
            {
                mClient = new SmtpClient(pHost, pPort);
                mClient.EnableSsl = pSSL;
                mClient.Credentials = new NetworkCredential(pUserId, pPassword);
                mClient.SendCompleted += ZSendCompleted;
            }
            catch
            {
                if (mClient != null) mClient.Dispose();
                throw;
            }
        }

        public Task SendAsync(MailMessage pMailMessage)
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cSMTPClient));
            var lTCS = new TaskCompletionSource<bool>();
            mClient.SendAsync(pMailMessage, lTCS);
            return lTCS.Task;
        }

        public void SendAsyncCancel()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cSMTPClient));
            mClient.SendAsyncCancel();
        }

        private void ZSendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var lTCS = e.UserState as TaskCompletionSource<bool>;

            if (lTCS == null) return;

            if (e.Cancelled)
            {
                lTCS.SetCanceled();
                return;
            }

            if (e.Error == null)
            {
                lTCS.SetResult(true);
                return;
            }

            lTCS.SetException(e.Error);
        }

        public void Dispose()
        {
            if (mDisposed) return;
            if (mClient != null) mClient.Dispose();
            mDisposed = true;
        }
    }
}