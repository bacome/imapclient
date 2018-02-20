using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cQuotedPrintableEncodeConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<int> Increment;
        public readonly cBatchSizerConfiguration ReadConfiguration;
        public readonly cBatchSizerConfiguration WriteConfiguration;

        public cQuotedPrintableEncodeConfiguration(int pTimeout, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            ReadConfiguration = pReadConfiguration;
            WriteConfiguration = pWriteConfiguration;
        }

        public cQuotedPrintableEncodeConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement = null, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            ReadConfiguration = pReadConfiguration;
            WriteConfiguration = pWriteConfiguration;
        }
    }
}
