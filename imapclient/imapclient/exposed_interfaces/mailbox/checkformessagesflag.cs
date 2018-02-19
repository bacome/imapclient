using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public sealed class cCheckForMessagesFlag : IDisposable
    {
        private bool mDisposed = false;
        private readonly cMailbox mMailbox;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource mLinkedCancellationTokenSource;
        private readonly CancellationToken mLinkedCancellationToken;
        private readonly SemaphoreSlim mSemaphoreSlim = new SemaphoreSlim(0);
        private Task mTask = null;

        public cCheckForMessagesFlag(cMailbox pMailbox, CancellationToken pCancellationToken)
        {
            mMailbox = pMailbox ?? throw new ArgumentNullException(nameof(pMailbox));
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(pCancellationToken, mCancellationTokenSource.Token);
            mLinkedCancellationToken = mLinkedCancellationTokenSource.Token;
            mMailbox.PropertyChanged += ZPropertyChanged;
            mMailbox.MessageDelivery += ZMessageDelivery;
        }

        private void ZPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (mDisposed) return;

            if (mSemaphoreSlim.CurrentCount != 0) return;

            if (e.PropertyName == nameof(cMailbox.HighestModSeq))
            {
                // indicates that a message in the mailbox has changed: NOTE: this only works if the mailbox supports mod-sequence (RFC 4551)
                mSemaphoreSlim.Release(); 
                return;
            }

            if (e.PropertyName == nameof(cMailbox.IsSelected) && mMailbox.IsSelected)
            {
                // indicates that the mailbox has become selected: NOTE: this is only effective if the session is the same (so re-connecting stops this from working)
                //  also note that a quick select then unselect may not result in a release due to the way events are delivered
                mSemaphoreSlim.Release(); 
                return;
            }
        }

        private void ZMessageDelivery(object pSender, cMessageDeliveryEventArgs e)
        {
            if (mDisposed) return;
            if (mSemaphoreSlim.CurrentCount == 0) mSemaphoreSlim.Release();
        }

        private Task ZTask()
        {
            if (mTask == null) mTask = mSemaphoreSlim.WaitAsync(mLinkedCancellationToken);
            return mTask;
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

        public void Dispose()
        {
            if (mDisposed) return;

            mCancellationTokenSource.Cancel();

            mMailbox.PropertyChanged -= ZPropertyChanged;
            mMailbox.MessageDelivery -= ZMessageDelivery;

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