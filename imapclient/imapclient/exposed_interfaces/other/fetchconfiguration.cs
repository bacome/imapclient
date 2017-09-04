using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cPropertyFetchConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<int> Increment;

        public cPropertyFetchConfiguration(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
        }

        public cPropertyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }
    }

    public class cBodyFetchConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<int> Increment;
        public readonly cBatchSizerConfiguration Write;

        public cBodyFetchConfiguration(int pTimeout, cBatchSizerConfiguration pWrite = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            Write = pWrite;
        }

        public cBodyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cBatchSizerConfiguration pWrite = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            Write = pWrite;
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