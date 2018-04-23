using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// For a worker thread that must wait for work supplied by work-creator thread(s);
    ///  The worker resets, checks for and does work, then gets and awaits a release task.
    ///  Work-creators queue work and then release.
    /// For observer thread(s) that must wait for work to be done by a worker thread;
    ///  The observer gets a release task, checks for work having been done, then (optionally) awaits the release task. (note that if the work is finished the release task will always be complete so there is a danger of a tight loop).
    ///  The worker does some work and then if there is more work does a releasereset or if the work is complete does a release.
    /// </summary>
    internal sealed class cReleaser : IDisposable
    {
        private static int mInstanceSource = 7;

        private bool mDisposed = false;
        private readonly string mName;
        private readonly int mInstance;
        private readonly CancellationToken mCancellationToken;
        private readonly SemaphoreSlim mSemaphoreSlim = new SemaphoreSlim(0);
        private readonly object mTaskLock = new object();
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

            lock (mTaskLock)
            {
                if (mTask == null)
                {
                    lContext.TraceVerbose("starting a new wait task");
                    mTask = mSemaphoreSlim.WaitAsync(mCancellationToken);
                }
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

            lock (mTaskLock)
            {
                if (mSemaphoreSlim.CurrentCount == 0)
                {
                    lContext.TraceVerbose("releasing semaphore");
                    mSemaphoreSlim.Release();
                }
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

            // note that the task cannot be disposed as in the observer/worker case it may still be in use

            lock (mTaskLock)
            {
                if (mTask == null) return;
                if (mTask.IsCompleted) mTask = null;
            }
        }

        public void ReleaseReset(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(ReleaseReset), mName, mInstance);

            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));

            lock (mTaskLock)
            {
                if (mSemaphoreSlim.CurrentCount == 0)
                {
                    lContext.TraceVerbose("releasing semaphore");
                    mSemaphoreSlim.Release();
                }

                if (mTask == null) return;
                if (mTask.IsCompleted) mTask = null;
            }
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
