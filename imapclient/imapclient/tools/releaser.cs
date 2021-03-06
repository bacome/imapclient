﻿using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Provides services for coordinating one worker task and many work creating tasks using coordinating tasks.
    /// </summary>
    /// <remarks>
    /// <para>The worker task should;
    /// <list type="number">
    /// <item>Call <see cref="Reset(cTrace.cContext)"/> to indicate that it is about to start working.</item>
    /// <item>Look for and do all the work available.</item>
    /// <item><see langword="await"/> on the task returned by <see cref="GetAwaitReleaseTask(cTrace.cContext)"/>.</item>
    /// </list>
    /// </para>
    /// <para>The work creating tasks should;
    /// <list type="number">
    /// <item>Create item(s) of work.</item>
    /// <item>Call <see cref="Release(cTrace.cContext)"/>.</item>
    /// </list>
    /// </para>
    /// <para>This class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// <note type="note">Before disposing an instance the <see cref="System.Threading.CancellationToken"/> provided to its constructor must be cancelled, otherwise the dispose may never complete.</note>
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
        /// Initialises a new instance with the specified name and cancellation token.
        /// </summary>
        /// <param name="pName">A name to use when tracing.</param>
        /// <param name="pCancellationToken">A cancellation token to use on the coordinating tasks, may not be <see cref="System.Threading.CancellationToken.None"/>, must be capable of being cancelled.</param>
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
        /// Indicates whether the current coordinating task is complete.
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
        /// Disposes the current coordinating task if it is complete, allowing a new coordinating task to be started.
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

        /// <summary>
        /// Releases all resources used by the instance.
        /// </summary>
        /// <remarks>
        /// <note type="note">Before disposing an instance the <see cref="System.Threading.CancellationToken"/> provided to its constructor must be cancelled, otherwise the dispose may never complete.</note>
        /// </remarks>
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
