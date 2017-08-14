﻿using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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

        /*
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
        } */

        public async Task<Task> AwaitAny(Task pTask, params Task[] pTasks)
        {
            if (pTask == null) throw new ArgumentNullException(nameof(pTask));

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

            Task lTask = await Task.WhenAny(lTasks).ConfigureAwait(false);

            if (lTask.Exception != null) ExceptionDispatchInfo.Capture(lTask.Exception).Throw();
            if (lTask.IsCanceled) throw new OperationCanceledException();
            if (ReferenceEquals(lTask, mTask)) throw new TimeoutException();

            return lTask;
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
                Task lCompleted = await Task.WhenAny(lTerminator.mTask, Task.WhenAll(lTasks)).ConfigureAwait(false);

                if (ReferenceEquals(lCompleted, lTerminator.mTask))
                {
                    if (lTerminator.mTask.IsCanceled) throw new OperationCanceledException();
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