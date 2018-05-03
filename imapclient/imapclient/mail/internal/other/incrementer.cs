using System;
using System.Diagnostics;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract partial class cMailClient
    {
        internal class cIncrementer
        {
            private readonly cCallbackSynchroniser mSynchroniser;
            private readonly Action<int> mIncrement;
            private readonly int mMaxCallbackFrequency;
            private readonly Stopwatch mStopwatch = new Stopwatch();

            private int mAccumulator = 0;

            public cIncrementer(cCallbackSynchroniser pSynchroniser, Action<int> pIncrement, int pMaxCallbackFrequency)
            {
                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mIncrement = pIncrement ?? throw new ArgumentNullException(nameof(pIncrement));
                mMaxCallbackFrequency = pMaxCallbackFrequency;
                mStopwatch.Start();
            }

            public void Increment(int pInt, cTrace.cContext pContext)
            {
                mAccumulator += pInt;
                if (mStopwatch.ElapsedMilliseconds < mMaxCallbackFrequency) return;
                Increment(pContext);
            }

            public void Increment(cTrace.cContext pContext)
            {
                if (mAccumulator == 0) return;
                mSynchroniser.InvokeActionInt(mIncrement, mAccumulator, pContext);
                mStopwatch.Restart();
                mAccumulator = 0;
            }
        }
    }
}