using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cFetchConfiguration
    {
        public readonly int Min;
        public readonly int Max;
        public readonly int MaxTime;
        private readonly cSamples[] mSamples = new cSamples[4];
        private int _Current;

        private readonly double mMaxTime;

        public cFetchConfiguration(int pMin, int pMax, int pMaxTime, int pInitial)
        {
            if (pMin < 1) throw new ArgumentNullException(nameof(pMin));
            if (pMax < pMin) throw new ArgumentOutOfRangeException(nameof(pMax));
            if (pMaxTime < 1) throw new ArgumentOutOfRangeException(nameof(pMaxTime));

            Min = pMin;
            Max = pMax;
            MaxTime = pMaxTime;
            mSamples[0] = new cSamples(1);
            mSamples[1] = new cSamples(5);
            mSamples[2] = new cSamples(10);
            mSamples[3] = new cSamples(50);
            Current = pInitial;

            mMaxTime = pMaxTime;
        }

        public int Current
        {
            get => _Current;

            private set
            {
                if (value < Min) _Current = Min;
                else if (value > Max) _Current = Max;
                else _Current = value;
            }
        }

        public void AddSample(int pN, long pTime)
        {
            if (pN < 1) throw new ArgumentOutOfRangeException(nameof(pN));
            if (pTime < 0) throw new ArgumentOutOfRangeException(nameof(pTime));

            lock (mSamples)
            {
                int lNewCurrent = 0;

                for (int i = 0; i < mSamples.Length; i++)
                {
                    mSamples[i].AddSample(pN, pTime);

                    double lTotalTime = mSamples[i].TotalTime;

                    int lThisNewCurrent;
                    if (lTotalTime == 0) lThisNewCurrent = Max;
                    else lThisNewCurrent = (int)(mSamples[i].TotalN * (mMaxTime / lTotalTime));

                    if (i == 0) lNewCurrent = lThisNewCurrent;
                    else if (lThisNewCurrent < lNewCurrent) lNewCurrent = lThisNewCurrent;
                }

                if (lNewCurrent > _Current * 2) Current = _Current * 2;
                else Current = lNewCurrent;
            }
        }

        public override string ToString() => $"{nameof(cFetchConfiguration)}({Min},{Max},{MaxTime},{mSamples},{_Current})";

        private class cSamples
        {
            private readonly int mMaxSamples;
            private readonly Queue<sSample> mSamples;

            public cSamples(int pMaxSamples)
            {
                mMaxSamples = pMaxSamples;
                mSamples = new Queue<sSample>(pMaxSamples);
            }

            public int MaxSamples => mMaxSamples;

            public void AddSample(int pN, long pTime)
            {
                if (mSamples.Count == mMaxSamples)
                {
                    var lSample = mSamples.Dequeue();
                    TotalN -= lSample.N;
                    TotalTime -= lSample.Time;
                }

                mSamples.Enqueue(new sSample(pN, pTime));

                TotalN += pN;
                TotalTime += pTime;
            }

            public long TotalN { get; private set; } = 0;
            public long TotalTime { get; private set; } = 0;

            public override string ToString()
            {
                cListBuilder lBuilder = new cListBuilder(nameof(cSamples));
                lBuilder.Append(mMaxSamples);
                cListBuilder lSamples = new cListBuilder(nameof(mSamples));
                foreach (var lSample in mSamples) lSamples.Append(lSample);
                lBuilder.Append(lSamples.ToString());
                return lBuilder.ToString();
            }

            private struct sSample
            {
                public long N;
                public long Time;

                public sSample(long pN, long pTime)
                {
                    N = pN;
                    Time = pTime;
                }

                public override string ToString() => $"{nameof(sSample)}({N},{Time})";
            }
        }

        public static class cTests
        {
            [Conditional("DEBUG")]
            public static void Tests(cTrace.cContext pParentContext)
            {
                cFetchConfiguration lConfig = new cFetchConfiguration(1, 100, 1000, 10);

                if (lConfig.Current != 10) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.1");
                lConfig.AddSample(10, 1000);
                if (lConfig.Current != 10) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.2");
                lConfig.AddSample(5, 500);
                if (lConfig.Current != 10) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.3");
                lConfig.AddSample(150, 1500); // 10 times faster
                if (lConfig.Current != 20) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.4");
                lConfig.AddSample(235, 1000);
                if (lConfig.Current != 40) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.5");
                lConfig.AddSample(10, 100);
                if (lConfig.Current != 80) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.6");
                lConfig.AddSample(10, 100);
                if (lConfig.Current != 100) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.7");
                lConfig.AddSample(10, 100);
                if (lConfig.Current != 100) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.8");
                lConfig.AddSample(1, 1000);
                if (lConfig.Current != 1) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.9");
                lConfig.AddSample(10, 1000);
                if (lConfig.Current != 2) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.10");
                lConfig.AddSample(10, 1000);
                if (lConfig.Current != 4) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.11");
                lConfig.AddSample(10, 1000);
                if (lConfig.Current != 8) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.12");
                lConfig.AddSample(10, 1000);
                if (lConfig.Current != 8) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.13");
                lConfig.AddSample(10, 1000);
                if (lConfig.Current != 10) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.14");
                lConfig.AddSample(100, 1000);
                if (lConfig.Current != 20) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.15");
                lConfig.AddSample(100, 1000);
                if (lConfig.Current != 33) throw new cTestsException($"{nameof(cFetchConfiguration)}.1.16");




            }
        }
    }
}