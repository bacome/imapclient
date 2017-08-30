using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cPropertyFetchConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<int> Increment;
        public readonly cFetchSizer ReadSizer;

        public cPropertyFetchConfiguration(int pTimeout, cFetchSizer pReadSizer = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            ReadSizer = pReadSizer;
        }

        public cPropertyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cFetchSizer pReadSizer = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            ReadSizer = pReadSizer;
        }
    }

    public class cBodyFetchConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<int> Increment;
        public readonly cFetchSizeConfiguration Write;
        public readonly cFetchSizer ReadSizer;

        public cBodyFetchConfiguration(int pTimeout, cFetchSizeConfiguration pWrite = null, cFetchSizer pReadSizer = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            Write = pWrite;
            ReadSizer = pReadSizer;
        }

        public cBodyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cFetchSizeConfiguration pWrite = null, cFetchSizer pReadSizer = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            Write = pWrite;
            ReadSizer = pReadSizer;
        }
    }

    public class cMessageFetchConfiguration : cPropertyFetchConfiguration
    {
        public readonly Action<int> SetCount;

        public cMessageFetchConfiguration(int pTimeout) : base(pTimeout)
        {
            SetCount = null;
        }

        public cMessageFetchConfiguration(CancellationToken pCancellationToken, Action<int> pSetCount, Action<int> pIncrement) : base(pCancellationToken, pIncrement)
        {
            SetCount = pSetCount;
        }
    }
}