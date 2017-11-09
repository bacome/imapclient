using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// <para>Coordinates tasks that work together using a task.</para>
    /// <para>One task is the worker task that should do work when it is available. This task;
    /// <list type="bullet">
    /// <item><description>Uses the <see cref="Reset(cTrace.cContext)"/> method to indicate that it is working, then</description></item>
    /// <item><description>Checks for and does work, then</description></item>
    /// <item><description>Waits on the task returned by the <see cref="GetAwaitReleaseTask(cTrace.cContext)"/> method.</description></item>
    /// </list>
    /// </para>
    /// <para>The other tasks are work requesting tasks, they;
    /// <list type="bullet">
    /// <item><description>Queue the work somehow, then</description></item>
    /// <item><description>Use the <see cref="Release(cTrace.cContext)"/> method, which causes the task returned by <see cref="GetAwaitReleaseTask(cTrace.cContext)"/> to complete.</description></item>
    /// </list>
    /// </para>
    /// <para>Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// <para>Also note that before disposing an instance the cancellation token provided to the constructor must be cancelled, otherwise the dispose may never complete.</para>
    /// </summary>
    public sealed class cReleaser : IDisposable
    {
        private static int mInstanceSource = 7;

        private bool mDisposed = false;
        private readonly string mName;
        private readonly int mInstance;
        private readonly CancellationToken mCancellationToken;
        private readonly SemaphoreSlim mSemaphoreSlim = new SemaphoreSlim(0);
        private Task mTask = null;

        /// <summary>
        /// <para>Constructor.</para>
        /// </summary>
        /// <param name="pName">A name to use when tracing.</param>
        /// <param name="pCancellationToken">A cancellation token to use on the internal coordination task.</param>
        public cReleaser(string pName, CancellationToken pCancellationToken)
        {
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

        /// <summary>
        /// Get the current coordination task.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns>The coordination task.</returns>
        public Task GetAwaitReleaseTask(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(GetAwaitReleaseTask), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));
            return ZTask(lContext);
        }

        /// <summary>
        /// Complete the current coordination task.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
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

        /// <summary>
        /// True if the current coordination task is complete.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns>True if the current coordination task is complete.</returns>
        public bool IsReleased(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(IsReleased), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));
            return ZTask(lContext).IsCompleted;
        }

        /// <summary>
        /// Indicate that work is about to be checked for and done.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
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
