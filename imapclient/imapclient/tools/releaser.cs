using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>Instances coordinate tasks (one worker and possibly many work creators) that work together using internal coordinating tasks.</summary>
    /// <remarks>
    /// <para>One of the coordinated tasks is the worker task. This task does work when it is available. This task should;
    /// <list type="number">
    /// <item>Call the <see cref="Reset(cTrace.cContext)"/> method to indicate that it is about to start working.</item>
    /// <item>Check for and do all the work available.</item>
    /// <item>Call the <see cref="GetAwaitReleaseTask(cTrace.cContext)"/> method to get a coordinating task, and await that task.</item>
    /// </list>
    /// </para>
    /// <para>The other coordinated tasks are work requesting tasks. These tasks should;
    /// <list type="number">
    /// <item>Queue items of work.</item>
    /// <item>Call the <see cref="Release(cTrace.cContext)"/> method (this causes the current coordinating task to complete).</item>
    /// </list>
    /// </para>
    /// <para>Note that this class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// <para>Also note that before disposing an instance the <see cref="System.Threading.CancellationToken"/> provided to the constructor must be cancelled, otherwise the dispose may never complete.</para>
    /// </remarks>
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
        /// <para>Initialises a new instance with a name and a <see cref="CancellationToken"/>.</para>
        /// </summary>
        /// <param name="pName">A name to use when tracing.</param>
        /// <param name="pCancellationToken">A cancellation token to use on the coordinating tasks.</param>
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
        /// Gets the current coordinating task.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns></returns>
        public Task GetAwaitReleaseTask(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(GetAwaitReleaseTask), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));
            return ZTask(lContext);
        }

        /// <summary>
        /// Completes the current coordinating task.
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
        /// Determines if the current coordinating task is complete.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns></returns>
        public bool IsReleased(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cReleaser), nameof(IsReleased), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cReleaser));
            return ZTask(lContext).IsCompleted;
        }

        /// <summary>
        /// Indicates that work is about to be checked for and done.
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
