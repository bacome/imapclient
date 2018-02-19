using System;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public sealed class cMessageDeliveryMonitor : IDisposable
    {
        private bool mDisposed = false;
        private readonly cMailbox mMailbox;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource mLinkedCancellationTokenSource;
        private readonly CancellationToken mLinkedCancellationToken;
        private readonly SemaphoreSlim mSemaphoreSlim = new SemaphoreSlim(0);
        private Task mTask = null;

        public cMessageDeliveryMonitor(cMailbox pMailbox, CancellationToken pCancellationToken)
        {
            mMailbox = pMailbox ?? throw new ArgumentNullException(nameof(pMailbox));
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(pCancellationToken, mCancellationTokenSource.Token);
            mLinkedCancellationToken = mLinkedCancellationTokenSource.Token;
            mMailbox.MessageDelivery += ZMessageDelivery;
        }

        private Task ZTask()
        {
            if (mTask == null) mTask = mSemaphoreSlim.WaitAsync(mLinkedCancellationToken);
            return mTask;
        }

        public Task GetAwaitMessageDeliveryTask()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cMessageDeliveryMonitor));
            return ZTask();
        }

        private void ZMessageDelivery(object pSender, cMessageDeliveryEventArgs e)
        {
            if (mDisposed) return;
            if (mSemaphoreSlim.CurrentCount == 0) mSemaphoreSlim.Release();
        }

        public void Reset()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cMessageDeliveryMonitor));
            if (!ZTask().IsCompleted) return;
            mTask.Dispose();
            mTask = null;
        }

        public void Dispose()
        {
            if (mDisposed) return;

            mCancellationTokenSource.Cancel();

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