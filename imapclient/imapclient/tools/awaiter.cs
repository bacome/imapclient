using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.async
{
    /// <summary>
    /// Provides services for waiting on a number of tasks with timeout and/or cancellation.
    /// </summary>
    /// <remarks>
    /// Note that this class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.
    /// </remarks>
    public sealed class cAwaiter : IDisposable
    {
        private bool mDisposed = false;
        private readonly CancellationTokenSource mDisposeCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource mLinkedCancellationTokenSource;
        private readonly Task mTask;

        /// <summary>
        /// Initialises a new instance with timeout and cancellation controls. If a timeout is specified then it runs from when the instance is created.
        /// </summary>
        /// <param name="pMC">The timeout and cancellation to use.</param>
        public cAwaiter(cMethodControl pMC)
        {
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(mDisposeCancellationTokenSource.Token, pMC.CancellationToken);
            mTask = Task.Delay(pMC.Timeout, mLinkedCancellationTokenSource.Token);
        }

        /// <summary>
        /// Initialises a new instance with cancellation control but no timeout.
        /// </summary>
        /// <param name="pCancellationToken">The cancellation token to use.</param>
        public cAwaiter(CancellationToken pCancellationToken)
        {
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(mDisposeCancellationTokenSource.Token, pCancellationToken);
            mTask = Task.Delay(Timeout.Infinite, mLinkedCancellationTokenSource.Token);
        }

        /// <summary>
        /// This method completes when any one of the passed tasks completes.
        /// </summary>
        /// <param name="pTask">A task, can't be null.</param>
        /// <param name="pTasks">A set of tasks, any or all can be null.</param>
        /// <returns>The task that completed.</returns>
        /// <remarks>
        /// If the task that completes did so because it failed (timed-out, was cancelled, or threw) then this method throws.
        /// If the instance controls indicate timeout or cancellation before a task completes, then this method throws.
        /// </remarks>
        public async Task<Task> AwaitAny(Task pTask, params Task[] pTasks)
        {
            if (pTask == null) throw new ArgumentNullException(nameof(pTask));

            List<Task> lTasks = new List<Task>();
            lTasks.Add(pTask);
            lTasks.Add(mTask);
            if (pTasks != null) foreach (var t in pTasks) if (t != null) lTasks.Add(t);

            Task lTask = await Task.WhenAny(lTasks).ConfigureAwait(false);

            if (lTask.Exception != null) ExceptionDispatchInfo.Capture(lTask.Exception).Throw();
            if (lTask.IsCanceled) throw new OperationCanceledException();
            if (ReferenceEquals(lTask, mTask)) throw new TimeoutException();

            return lTask;
        }

        /// <summary>
        /// The returned task completes when all of the passed tasks complete OR when the <see cref="cMethodControl"/> indicates timeout or cancellation.
        /// </summary>
        /// <param name="pMC">Controls the execution of the method.</param>
        /// <param name="pTasks">The set of tasks to wait for. Tasks in the set can be null.</param>
        /// <returns></returns>
        /// <remarks>
        /// If any of the passed tasks fail (timed-out, were cancelled, or threw) then this method throws.
        /// If the <see cref="cMethodControl"/> indicates timeout or cancellation before all the tasks complete then this method throws.
        /// </remarks>
        public static Task AwaitAll(cMethodControl pMC, params Task[] pTasks) => ZAwaitAll(pMC, pTasks);

        /// <summary>
        /// The returned task completes when all of the passed tasks complete OR when the <see cref="cMethodControl"/> indicates timeout or cancellation.
        /// </summary>
        /// <param name="pMC">Controls the execution of the method.</param>
        /// <param name="pTasks">The set of tasks to wait for. Tasks in the set can be null.</param>
        /// <returns></returns>
        /// <remarks>
        /// If any of the passed tasks fail (timed-out, were cancelled, or threw) then this method throws.
        /// If the <see cref="cMethodControl"/> indicates timeout or cancellation before all the tasks complete then this method throws.
        /// </remarks>
        public static Task AwaitAll(cMethodControl pMC, IEnumerable<Task> pTasks) => ZAwaitAll(pMC, pTasks);

        private static async Task ZAwaitAll(cMethodControl pMC, IEnumerable<Task> pTasks)
        {
            List<Task> lTasks = new List<Task>();
            foreach (var lTask in pTasks) if (lTask != null) lTasks.Add(lTask);

            if (lTasks.Count == 0) return;

            using (var lAwaiter = new cAwaiter(pMC))
            {
                Task lCompleted = await Task.WhenAny(lAwaiter.mTask, Task.WhenAll(lTasks)).ConfigureAwait(false);

                if (ReferenceEquals(lCompleted, lAwaiter.mTask))
                {
                    if (lAwaiter.mTask.IsCanceled) throw new OperationCanceledException();
                    throw new TimeoutException();
                }

                List<Exception> lExceptions = new List<Exception>();

                foreach (var lTask in lTasks)
                {
                    if (lTask.Exception != null) lExceptions.AddRange(lTask.Exception.Flatten().InnerExceptions);
                    else if (lTask.IsCanceled) lExceptions.Add(new OperationCanceledException());
                }

                if (lExceptions.Count == 0) return;
                if (lExceptions.Count == 1) throw lExceptions[0];
                throw new AggregateException(lExceptions);
            }
        }

        public void Dispose()
        {
            if (mDisposed) return;

            if (mDisposeCancellationTokenSource != null && !mDisposeCancellationTokenSource.IsCancellationRequested) mDisposeCancellationTokenSource.Cancel();

            if (mTask != null)
            {
                try { mTask.Wait(); }
                catch { }

                mTask.Dispose();
            }

            if (mLinkedCancellationTokenSource != null)
            {
                try { mLinkedCancellationTokenSource.Dispose(); }
                catch { }
            }

            if (mDisposeCancellationTokenSource != null)
            {
                try { mDisposeCancellationTokenSource.Dispose(); }
                catch { }
            }
        }
    }
}