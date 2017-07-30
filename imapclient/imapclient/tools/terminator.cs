using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.async
{ 
    public sealed class cTerminator : IDisposable
    {
        private bool mDisposed = false;
        private readonly CancellationTokenSource mDisposeCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource mLinkedCancellationTokenSource;
        private readonly Task mTask;

        public cTerminator(cMethodControl pMC)
        {
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(mDisposeCancellationTokenSource.Token, pMC.CancellationToken);
            mTask = Task.Delay(pMC.Timeout, mLinkedCancellationTokenSource.Token);
        }

        public cTerminator(CancellationToken pCancellationToken)
        {
            mLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(mDisposeCancellationTokenSource.Token, pCancellationToken);
            mTask = Task.Delay(Timeout.Infinite, mLinkedCancellationTokenSource.Token);
        }

        public Task GetAwaitTerminationTask()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cTerminator));
            return mTask;
        }

        public bool Terminated()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cTerminator));
            return mTask.IsCompleted;
        }

        public bool Throw()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cTerminator));
            if (!mTask.IsCompleted) throw new InvalidOperationException();
            if (mTask.IsCanceled) throw new OperationCanceledException();
            throw new TimeoutException();
        }

        public void ThrowIfTerminated()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cTerminator));
            if (!mTask.IsCompleted) return;
            if (mTask.IsCanceled) throw new OperationCanceledException();
            throw new TimeoutException();
        }

        public async Task<Task> WhenAny(Task pTask, params Task[] pTasks)
        {
            Task[] lTasks;

            if (pTasks == null)
            {
                lTasks = new Task[2];
                lTasks[0] = pTask;
                lTasks[1] = mTask;
            }
            else
            {
                lTasks = new Task[pTasks.Length + 2];
                for (int i = 0; i < pTasks.Length; i++) lTasks[i] = pTasks[i];
                lTasks[pTasks.Length] = pTask;
                lTasks[pTasks.Length + 1] = mTask;
            }

            Task lResult = await Task.WhenAny(lTasks).ConfigureAwait(false);

            if (ReferenceEquals(lResult, mTask))
            {
                if (mTask.IsCanceled) throw new OperationCanceledException();
                throw new TimeoutException();
            }

            return lResult;
        }

        public static Task AwaitAll(cMethodControl pMC, params Task[] pTasks) => ZAwaitAll(pMC, pTasks);
        public static Task AwaitAll(cMethodControl pMC, IEnumerable<Task> pTasks) => ZAwaitAll(pMC, pTasks);

        private static async Task ZAwaitAll(cMethodControl pMC, IEnumerable<Task> pTasks)
        {
            List<Task> lTasks = new List<Task>();
            foreach (var lTask in pTasks) if (lTask != null) lTasks.Add(lTask);

            if (lTasks.Count == 0) return;

            using (var lTerminator = new cTerminator(pMC))
            {
                Task lAwaitTerminationTask = lTerminator.GetAwaitTerminationTask();

                Task lCompleted = await Task.WhenAny(lAwaitTerminationTask, Task.WhenAll(lTasks)).ConfigureAwait(false);

                if (ReferenceEquals(lCompleted, lAwaitTerminationTask))
                {
                    if (lAwaitTerminationTask.IsCanceled) throw new OperationCanceledException();
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