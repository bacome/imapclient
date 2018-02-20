using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cConvertMailMessageConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<int> Increment;

        public cConvertMailMessageConfiguration(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
        }

        public cConvertMailMessageConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }
    }
}