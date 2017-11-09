using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// <para>Manages tasks that complete after a specified length of time.</para>
    /// <para>Use <see cref="GetAwaitCountdownTask"/> to get the currently running countdown task.</para>
    /// <para>Use <see cref="Restart(cTrace.cContext)"/> to start a new countdown task (can only be used after the current countdown task completes).</para>
    /// <para>Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// </summary>
    public sealed class cCountdownTimer : IDisposable
    {
        private bool mDisposed = false;
        private readonly int mTimeout;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private Task mTask;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pTimeout">The duration of each countdown task.</param>
        /// <param name="pParentContext">Context for trace messages.</param>
        public cCountdownTimer(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewObject(nameof(cCountdownTimer), pTimeout);
            mTimeout = pTimeout;
            mTask = Task.Delay(mTimeout, mCancellationTokenSource.Token);
        }

        /// <summary>
        /// Returns the currently running countdown task.
        /// </summary>
        /// <returns>The currently running countdown task.</returns>
        public Task GetAwaitCountdownTask()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cCountdownTimer));
            return mTask;
        }

        /// <summary>
        /// <para>Starts a new countdown task.</para>
        /// <para>Can only be used after the current countdown task completes.</para>
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
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