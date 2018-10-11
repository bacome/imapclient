using System;
using System.Threading;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cAppendMailMessageConfiguration
    {
        internal readonly cMethodControl MC;
        public readonly Action<long> AppendSetMaximum;
        public readonly Action<int> AppendIncrement;
        public readonly Action<long> ConvertSetMaximum;
        public readonly Action<int> ConvertIncrement;

        public cAppendMailMessageConfiguration(int pTimeout)
        {
            MC = new cMethodControl(pTimeout);
            AppendSetMaximum = null;
            AppendIncrement = null;
            ConvertSetMaximum = null;
            ConvertIncrement = null;
        }

        public cAppendMailMessageConfiguration(CancellationToken pCancellationToken, Action<long> pAppendSetMaximum = null, Action<int> pAppendIncrement = null, Action<long> pConvertSetMaximum = null, Action<int> pConvertIncrement = null)
        {
            MC = new cMethodControl(pCancellationToken);
            AppendSetMaximum = pAppendSetMaximum;
            AppendIncrement = pAppendIncrement;
            ConvertSetMaximum = pConvertSetMaximum;
            ConvertIncrement = pConvertIncrement;
        }

        public cAppendMailMessageConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<long> pAppendSetMaximum = null, Action<int> pAppendIncrement = null, Action<long> pConvertSetMaximum = null, Action<int> pConvertIncrement = null)
        {
            MC = new cMethodControl(pTimeout, pCancellationToken);
            AppendSetMaximum = pAppendSetMaximum;
            AppendIncrement = pAppendIncrement;
            ConvertSetMaximum = pConvertSetMaximum;
            ConvertIncrement = pConvertIncrement;
        }

        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public int Timeout => MC.Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public CancellationToken CancellationToken => MC.CancellationToken;

        public override string ToString() => $"{nameof(cAppendMailMessageConfiguration)}({MC},{AppendSetMaximum != null},{AppendIncrement != null},{ConvertSetMaximum != null},{ConvertIncrement != null})";

        public static implicit operator cAppendMailMessageConfiguration(int pTimeout) => new cAppendMailMessageConfiguration(pTimeout);
        public static implicit operator cAppendMailMessageConfiguration(CancellationToken pCancellationToken) => new cAppendMailMessageConfiguration(pCancellationToken);
    }
}