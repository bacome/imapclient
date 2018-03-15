using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal sealed class cReleaser : IDisposable
    {
        private static int mInstanceSource = 7;

        private bool mDisposed = false;
        private readonly string mName;
        private readonly int mInstance;
        private readonly CancellationToken mCancellationToken;
        private readonly SemaphoreSlim mSemaphoreSlim = new SemaphoreSlim(0);
        private Task mTask = null;

        public cReleaser(string pName, CancellationToken pCancellationToken)
        {
            if (!pCancellationToken.CanBeCanceled) throw new ArgumentOutOfRangeException(nameof(pCancellationToken));
            mName = pName;
            mInstance = Interlocked.Increment(ref mInstanceSource);
            mCancellationToken = pCancellationToken;
        }

        private Task ZTask(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(ZTask), mName, mInstance);

            if (mTask == null)
            {
                lContext.TraceVerbose("starting a new wait task");
                mTask = mSemaphoreSlim.WaitAsync(mCancellationToken);
            }

            return mTask;
        }

        public Task GetAwaitReleaseTask(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(GetAwaitReleaseTask), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));
            return ZTask(lContext);
        }

        public void Release(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(Release), mName, mInstance);

            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));

            if (mSemaphoreSlim.CurrentCount == 0)
            {
                lContext.TraceVerbose("releasing semaphore");
                mSemaphoreSlim.Release();
            }
        }

        public bool IsReleased(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(IsReleased), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));
            return ZTask(lContext).IsCompleted;
        }

        public void Reset(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(Reset), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));
            if (!ZTask(lContext).IsCompleted) return;
            mTask.Dispose();
            mTask = null;
        }

        public void Dispose()
        {
            if (mDisposed) return;

            // either the release should have been done (without a reset) or the cancellation token should be cancelled
            //  otherwise the wait on the task below will not complete

            if (mTask != null)
            {
                try { mTask.Wait(); }
                catch { }

                mTask.Dispose();

                mTask = null;
            }

            try { mSemaphoreSlim.Dispose(); }
            catch { }

            mDisposed = true;
        }
    }
}
