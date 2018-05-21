using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cAppendMailMessageConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<long> AppendSetMaximum;
        public readonly Action<int> AppendIncrement;
        public readonly Action<long> ConvertSetMaximum;
        public readonly Action<int> ConvertIncrement;

        public cAppendMailMessageConfiguration(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            AppendSetMaximum = null;
            AppendIncrement = null;
            ConvertSetMaximum = null;
            ConvertIncrement = null;
        }

        public cAppendMailMessageConfiguration(CancellationToken pCancellationToken, Action<long> pAppendSetMaximum, Action<int> pAppendIncrement, Action<long> pConvertSetMaximum = null, Action<int> pConvertIncrement = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            AppendSetMaximum = pAppendSetMaximum;
            AppendIncrement = pAppendIncrement;
            ConvertSetMaximum = pConvertSetMaximum;
            ConvertIncrement = pConvertIncrement;
        }

        public cAppendMailMessageConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<long> pAppendSetMaximum, Action<int> pAppendIncrement, Action<long> pConvertSetMaximum, Action<int> pConvertIncrement)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            AppendSetMaximum = pAppendSetMaximum;
            AppendIncrement = pAppendIncrement;
            ConvertSetMaximum = pConvertSetMaximum;
            ConvertIncrement = pConvertIncrement;
        }

        public override string ToString() => $"{nameof(cAppendMailMessageConfiguration)}({Timeout},{CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled},{AppendSetMaximum != null},{AppendIncrement != null},{ConvertSetMaximum != null},{ConvertIncrement != null})";
    }
}