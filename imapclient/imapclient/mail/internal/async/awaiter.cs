using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.mailclient
{
    internal sealed class cAwaiter : IDisposable
    {
        private bool mDisposed = false;
        private readonly CancellationTokenSource mDisposeCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource mLinkedCancellationTokenSource;
        private readonly Task mTask;

        public cAwaiter(cMethodControl pMC)
        {
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(mDisposeCancellationTokenSource.Token, pMC.CancellationToken);
            mTask = Task.Delay(pMC.Timeout, mLinkedCancellationTokenSource.Token);
        }

        public cAwaiter(CancellationToken pCancellationToken)
        {
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(mDisposeCancellationTokenSource.Token, pCancellationToken);
            mTask = Task.Delay(Timeout.Infinite, mLinkedCancellationTokenSource.Token);
        }

        public async Task<Task> AwaitAny(Task pTask, params Task[] pTasks)
        {
            if (pTask == null) throw new ArgumentNullException(nameof(pTask));

            List<Task> lTasks = new List<Task>();
            lTasks.Add(pTask);
            lTasks.Add(mTask);
            if (pTasks != null) foreach (var t in pTasks) if (t != null) lTasks.Add(t);

            Task lTask = await Task.WhenAny(lTasks).ConfigureAwait(false);

            if (lTask.IsFaulted) ExceptionDispatchInfo.Capture(lTask.Exception).Throw();
            if (lTask.IsCanceled) throw new OperationCanceledException();
            if (ReferenceEquals(lTask, mTask)) throw new TimeoutException();

            return lTask;
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