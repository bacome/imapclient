using System;

namespace work.bacome.imapclient
{
    public class cFetchSizeConfiguration
    {
        public readonly int Min;
        public readonly int Max;
        public readonly int MaxTime;
        public readonly int Initial;

        public cFetchSizeConfiguration(int pMin, int pMax, int pMaxTime, int pInitial)
        {
            if (pMin < 1) throw new ArgumentOutOfRangeException(nameof(pMin));
            if (pMax < pMin) throw new ArgumentOutOfRangeException(nameof(pMax));
            if (pMaxTime < 1) throw new ArgumentOutOfRangeException(nameof(pMaxTime));
            if (pInitial < 1) throw new ArgumentOutOfRangeException(nameof(pInitial));

            Min = pMin;
            Max = pMax;
            MaxTime = pMaxTime;
            Initial = pInitial;
        }

        public override string ToString() => $"{nameof(cFetchSizeConfiguration)}({Min},{Max},{MaxTime},{Initial})";
    }
}