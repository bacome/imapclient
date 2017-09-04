using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cBatchSizer
        {
            public readonly cBatchSizerConfiguration Configuration;
            private readonly cSamples[] mSamples = new cSamples[4];
            private int _Current;
            private readonly double mMaxTime;

            public cBatchSizer(cBatchSizerConfiguration pConfiguration)
            {
                Configuration = pConfiguration ?? throw new ArgumentNullException(nameof(pConfiguration));
                mSamples[0] = new cSamples(1);
                mSamples[1] = new cSamples(5);
                mSamples[2] = new cSamples(10);
                mSamples[3] = new cSamples(50);
                Current = pConfiguration.Initial;
                mMaxTime = pConfiguration.MaxTime;
            }

            public int Current
            {
                get => _Current;

                private set
                {
                    if (value < Configuration.Min) _Current = Configuration.Min;
                    else if (value > Configuration.Max) _Current = Configuration.Max;
                    else _Current = value;
                }
            }

            public void AddSample(int pN, long pTime, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cBatchSizer), nameof(AddSample), pN, pTime);

                if (pN < 0) throw new ArgumentOutOfRangeException(nameof(pN));
                if (pTime < 0) throw new ArgumentOutOfRangeException(nameof(pTime));

                if (pN == 0) return;

                lock (mSamples)
                {
                    int lNewCurrent = 0;

                    for (int i = 0; i < mSamples.Length; i++)
                    {
                        mSamples[i].AddSample(pN, pTime);

                        double lTotalTime = mSamples[i].TotalTime;

                        int lThisNewCurrent;
                        if (lTotalTime == 0) lThisNewCurrent = Configuration.Max;
                        else
                        {
                            var lThisNewCurrentd = mSamples[i].TotalN * (mMaxTime / lTotalTime);
                            if (lThisNewCurrentd > int.MaxValue) lThisNewCurrent = int.MaxValue;
                            else lThisNewCurrent = (int)(mSamples[i].TotalN * (mMaxTime / lTotalTime));
                        }

                        if (i == 0) lNewCurrent = lThisNewCurrent;
                        else if (lThisNewCurrent < lNewCurrent) lNewCurrent = lThisNewCurrent;
                    }

                    if (lNewCurrent > _Current * 2) Current = _Current * 2;
                    else Current = lNewCurrent;
                }
            }

            public override string ToString() => $"{nameof(cBatchSizer)}({Configuration},{_Current})";

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

            [Conditional("DEBUG")]
            public static void _Tests(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cBatchSizer), nameof(_Tests));

                cBatchSizer lSizer = new cBatchSizer(new cBatchSizerConfiguration(1, 100, 1000, 10));

                if (lSizer.Current != 10) throw new cTestsException($"{nameof(cBatchSizer)}.1.1");
                lSizer.AddSample(10, 1000, lContext);
                if (lSizer.Current != 10) throw new cTestsException($"{nameof(cBatchSizer)}.1.2");
                lSizer.AddSample(5, 500, lContext);
                if (lSizer.Current != 10) throw new cTestsException($"{nameof(cBatchSizer)}.1.3");
                lSizer.AddSample(150, 1500, lContext); // 10 times faster
                if (lSizer.Current != 20) throw new cTestsException($"{nameof(cBatchSizer)}.1.4");
                lSizer.AddSample(235, 1000, lContext);
                if (lSizer.Current != 40) throw new cTestsException($"{nameof(cBatchSizer)}.1.5");
                lSizer.AddSample(10, 100, lContext);
                if (lSizer.Current != 80) throw new cTestsException($"{nameof(cBatchSizer)}.1.6");
                lSizer.AddSample(10, 100, lContext);
                if (lSizer.Current != 100) throw new cTestsException($"{nameof(cBatchSizer)}.1.7");
                lSizer.AddSample(10, 100, lContext);
                if (lSizer.Current != 100) throw new cTestsException($"{nameof(cBatchSizer)}.1.8");
                lSizer.AddSample(1, 1000, lContext);
                if (lSizer.Current != 1) throw new cTestsException($"{nameof(cBatchSizer)}.1.9");
                lSizer.AddSample(10, 1000, lContext);
                if (lSizer.Current != 2) throw new cTestsException($"{nameof(cBatchSizer)}.1.10");
                lSizer.AddSample(10, 1000, lContext);
                if (lSizer.Current != 4) throw new cTestsException($"{nameof(cBatchSizer)}.1.11");
                lSizer.AddSample(10, 1000, lContext);
                if (lSizer.Current != 8) throw new cTestsException($"{nameof(cBatchSizer)}.1.12");
                lSizer.AddSample(10, 1000, lContext);
                if (lSizer.Current != 8) throw new cTestsException($"{nameof(cBatchSizer)}.1.13");
                lSizer.AddSample(10, 1000, lContext);
                if (lSizer.Current != 10) throw new cTestsException($"{nameof(cBatchSizer)}.1.14");
                lSizer.AddSample(100, 1000, lContext);
                if (lSizer.Current != 20) throw new cTestsException($"{nameof(cBatchSizer)}.1.15");
                lSizer.AddSample(100, 1000, lContext);
                if (lSizer.Current != 33) throw new cTestsException($"{nameof(cBatchSizer)}.1.16");





                int lCurrent;

                lSizer = new cBatchSizer(new cBatchSizerConfiguration(1000, 1000000, 10000, 1000));
                lSizer.AddSample(1000, 0, lContext);
                lSizer.AddSample(2000, 0, lContext);
                lSizer.AddSample(4000, 1, lContext);
                lSizer.AddSample(8000, 0, lContext);
                lSizer.AddSample(16000, 0, lContext);
                lSizer.AddSample(32000, 0, lContext);
                lSizer.AddSample(64000, 0, lContext);
                lSizer.AddSample(128000, 0, lContext);
                lCurrent = lSizer.Current;
                lSizer.AddSample(256000, 0, lContext);
                lCurrent = lSizer.Current;
                lSizer.AddSample(512000, 1, lContext);
                lCurrent = lSizer.Current;
                lSizer.AddSample(1000000, 0, lContext);
                lCurrent = lSizer.Current;
                lSizer.AddSample(1000000, 0, lContext);
                lCurrent = lSizer.Current;






            }
        }
    }
}