using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public interface iIncrementer : IDisposable
    {
        void Increment(int pValue);
    }

    public abstract partial class cMailClient
    {
        protected sealed class cIncrementer : iIncrementer
        {
            private bool mDisposed = false;

            private readonly cCallbackSynchroniser mSynchroniser;
            private readonly Action<int> mIncrement;
            private readonly int mIncrementInvokeMillisecondsDelay;
            private readonly cTrace.cContext mContextForInvoke;
            private readonly CancellationTokenSource mCancellationTokenSource;
            private long mValue = 0;

            private Task mBackgroundTask = null;

            public cIncrementer(cCallbackSynchroniser pSynchroniser, Action<int> pIncrement, int pIncrementInvokeMillisecondsDelay, cTrace.cContext pContextForInvoke)
            {
                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mIncrement = pIncrement;
                if (pIncrementInvokeMillisecondsDelay < -1) throw new ArgumentOutOfRangeException(nameof(pIncrementInvokeMillisecondsDelay));
                mIncrementInvokeMillisecondsDelay = pIncrementInvokeMillisecondsDelay;
                mContextForInvoke = pContextForInvoke ?? throw new ArgumentNullException(nameof(pContextForInvoke));

                if (mIncrement == null) mCancellationTokenSource = null;
                else mCancellationTokenSource = new CancellationTokenSource();
            }

            public void Increment(int pValue)
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIncrementer));

                if (pValue < 0) throw new ArgumentOutOfRangeException(nameof(pValue));

                if (mIncrement == null || pValue == 0) return;

                var lValue = Interlocked.Add(ref mValue, pValue);

                if (lValue == pValue)
                {
                    if (mBackgroundTask != null)
                    {
                        mBackgroundTask.Wait();
                        mBackgroundTask.Dispose();
                    }

                    mBackgroundTask = ZBackgroundTask();
                }
            }

            private async Task ZBackgroundTask()
            {
                try { await Task.Delay(mIncrementInvokeMillisecondsDelay, mCancellationTokenSource.Token).ConfigureAwait(false); }
                catch { }

                var lValue = Interlocked.Exchange(ref mValue, 0);

                while (true)
                {
                    if (lValue > int.MaxValue)
                    {
                        mSynchroniser.InvokeActionIntx(mIncrement, int.MaxValue, mContextForInvoke);
                        lValue -= int.MaxValue;
                    }
                    else
                    {
                        mSynchroniser.InvokeActionIntx(mIncrement, (int)lValue, mContextForInvoke);
                        return;
                    }
                }
            }

            public void Dispose()
            {
                if (mCancellationTokenSource != null && !mCancellationTokenSource.IsCancellationRequested) mCancellationTokenSource.Cancel();

                if (mBackgroundTask != null)
                {
                    try { mBackgroundTask.Wait(); }
                    catch { }

                    try { mBackgroundTask.Dispose(); }
                    catch { }
                }

                if (mCancellationTokenSource != null)
                {
                    try { mCancellationTokenSource.Dispose(); }
                    catch { }
                }
            }
        }
    }
}