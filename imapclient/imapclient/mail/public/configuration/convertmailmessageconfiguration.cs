using System;
using System.Threading;

namespace work.bacome.mailclient
{
    public class cConvertMailMessageConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        public readonly Action<long> SetMaximum;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. 
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        public cConvertMailMessageConfiguration(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            SetMaximum = null;
            Increment = null;
        }

        public cConvertMailMessageConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            SetMaximum = pSetMaximum;
            Increment = pIncrement;
        }

        public cConvertMailMessageConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            SetMaximum = pSetMaximum;
            Increment = pIncrement;
        }
    }
}