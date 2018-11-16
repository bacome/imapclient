using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal sealed class cCountdownTimer : IDisposable
    {
        private bool mDisposed = false;
        private readonly int mTimeout;

        private CancellationTokenSource mCancellationTokenSource;
        private Task mTask;

        public cCountdownTimer(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewObject(nameof(cCountdownTimer), pTimeout);
            mTimeout = pTimeout;

            mCancellationTokenSource = new CancellationTokenSource();
            mTask = Task.Delay(mTimeout, mCancellationTokenSource.Token);
        }

        public Task GetAwaitCountdownTask()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cCountdownTimer));
            return mTask;
        }

        public void Restart(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCountdownTimer), nameof(Restart));

            if (mDisposed) throw new ObjectDisposedException(nameof(cCountdownTimer));

            if (mTask.IsCompleted) mTask.Dispose();
            {
                mCancellationTokenSource.Cancel();

                try { mTask.Wait(); }
                catch { }

                mTask.Dispose();
                mCancellationTokenSource.Dispose();

                mCancellationTokenSource = new CancellationTokenSource();
            }

            mTask = Task.Delay(mTimeout, mCancellationTokenSource.Token);
        }

        public void Dispose()
        {
            if (mDisposed) return;

            if (mCancellationTokenSource != null && !mCancellationTokenSource.IsCancellationRequested) mCancellationTokenSource.Cancel();

            if (mTask != null)
            {
                try { mTask.Wait(); }
                catch { }

                mTask.Dispose();
            }

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }
        }
    }
}