﻿using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Generates a sequence of 'countdown' tasks.
    /// </summary>
    /// <remarks>
    /// Each task runs for the same length of time (set when the instance is created). Only one task can be running at a time.
    /// Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.
    /// </remarks>
    public sealed class cCountdownTimer : IDisposable
    {
        private bool mDisposed = false;
        private readonly int mTimeout;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private Task mTask;

        /// <summary>
        /// Initialises a new instance. The first 'countdown' starts immediately.
        /// </summary>
        /// <param name="pTimeout">The duration of each successive 'countdown' task, in milliseconds.</param>
        /// <param name="pParentContext">Context for trace messages.</param>
        public cCountdownTimer(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewObject(nameof(cCountdownTimer), pTimeout);
            mTimeout = pTimeout;
            mTask = Task.Delay(mTimeout, mCancellationTokenSource.Token);
        }

        /// <summary>
        /// Gets the currently running 'countdown' task.
        /// </summary>
        /// <returns></returns>
        public Task GetAwaitCountdownTask()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cCountdownTimer));
            return mTask;
        }

        /// <summary>
        /// Starts a new 'countdown' task. 
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <remarks>
        /// If the current 'countdown' is still running, this method will throw.
        /// </remarks>
        public void Restart(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCountdownTimer), nameof(Restart));
            if (mDisposed) throw new ObjectDisposedException(nameof(cCountdownTimer));
            if (mTask == null || !mTask.IsCompleted) throw new InvalidOperationException();
            mTask.Dispose();
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