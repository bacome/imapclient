using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Manages tasks that complete after a specified length of time.
    /// Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.
    /// </summary>
    public sealed class cCountdownTimer : IDisposable
    {
        private bool mDisposed = false;
        private readonly int mTimeout;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private Task mTask;

        /// <summary>
        /// Constructs a new instance. The first countdown commences immediately.
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
        /// Gets the currently running countdown task.
        /// </summary>
        /// <returns>The currently running countdown task.</returns>
        public Task GetAwaitCountdownTask()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cCountdownTimer));
            return mTask;
        }

        /// <summary>
        /// Starts a new countdown task.
        /// Cannot be called if there is a countdown running.
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