using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public sealed class cCheckForMessagesFlag : IDisposable
    {
        private bool mDisposed = false;
        private readonly cIMAPClient mClient;
        private readonly iMailboxHandle mMailboxHandle;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource mLinkedCancellationTokenSource;
        private readonly CancellationToken mLinkedCancellationToken;
        private readonly SemaphoreSlim mSemaphoreSlim = new SemaphoreSlim(0);
        private Task mTask = null;

        public cCheckForMessagesFlag(cMailbox pMailbox, CancellationToken pCancellationToken)
        {
            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));

            mClient = pMailbox.Client;
            mMailboxHandle = pMailbox.MailboxHandle;

            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(pCancellationToken, mCancellationTokenSource.Token);
            mLinkedCancellationToken = mLinkedCancellationTokenSource.Token;

            mClient.MailboxPropertyChanged += ZMailboxPropertyChanged;
            mClient.MessagePropertyChanged += ZMessagePropertyChanged;
            mClient.MailboxMessageDelivery += ZMailboxMessageDelivery;
        }

        public Task GetTask()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cCheckForMessagesFlag));
            return ZTask();
        }

        public void ResetFlag()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cCheckForMessagesFlag));
            if (!ZTask().IsCompleted) return;
            mTask.Dispose();
            mTask = null;
        }

        private void ZMailboxPropertyChanged(object pSender, cMailboxPropertyChangedEventArgs pArgs)
        {
            if (pArgs.PropertyName == nameof(cMailbox.IsSelected)) ZSetFlag();
        }

        private void ZMessagePropertyChanged(object pSender, cMessagePropertyChangedEventArgs pArgs) => ZSetFlag();

        private void ZMailboxMessageDelivery(object pSender, cMailboxMessageDeliveryEventArgs pArgs) => ZSetFlag();

        private void ZSetFlag()
        {
            if (mDisposed || mSemaphoreSlim.CurrentCount != 0) return;
            if (ReferenceEquals(mClient.SelectedMailboxDetails?.MailboxHandle, mMailboxHandle)) mSemaphoreSlim.Release();
        }

        private Task ZTask()
        {
            if (mTask == null) mTask = mSemaphoreSlim.WaitAsync(mLinkedCancellationToken);
            return mTask;
        }

        public void Dispose()
        {
            if (mDisposed) return;

            mCancellationTokenSource.Cancel();

            mClient.MailboxPropertyChanged -= ZMailboxPropertyChanged;
            mClient.MessagePropertyChanged -= ZMessagePropertyChanged;
            mClient.MailboxMessageDelivery -= ZMailboxMessageDelivery;

            if (mTask != null)
            {
                try { mTask.Wait(); }
                catch { }

                mTask.Dispose();

                mTask = null;
            }

            try { mLinkedCancellationTokenSource.Dispose(); }
            catch { }

            try { mCancellationTokenSource.Dispose(); }
            catch { }

            try { mSemaphoreSlim.Dispose(); }
            catch { }

            mDisposed = true;
        }
    }
}