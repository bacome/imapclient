using System;
using System.Threading;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-setmaximum and progress-increment callbacks.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cSetMaximumConfiguration : cIncrementConfiguration
    {
        /// <summary>
        /// The progress-setmaximum callback for the operation. May be <see langword="null"/>. Invoked once before any progress-increment invokes, the argument specifies the size of the operation.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<long> SetMaximum;

        /// <inheritdoc cref="cFetchCacheItemConfiguration(int)"/>
        public cSetMaximumConfiguration(int pTimeout) : base(pTimeout)
        {
            SetMaximum = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-setmaximum and progress-increment callbacks. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum">May be <see langword="null"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        public cSetMaximumConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum = null, Action<int> pIncrement = null) : base(pCancellationToken, pIncrement)
        {
            SetMaximum = pSetMaximum;
        }

        public cSetMaximumConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<long> pSetMaximum = null, Action<int> pIncrement = null) : base(pTimeout, pCancellationToken, pIncrement)
        {
            SetMaximum = pSetMaximum;
        }

        public override string ToString() => $"{nameof(cSetMaximumConfiguration)}({Timeout},{CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled},{SetMaximum != null},{Increment != null})";

        public static implicit operator cSetMaximumConfiguration(int pTimeout) => new cSetMaximumConfiguration(pTimeout);
        public static implicit operator cSetMaximumConfiguration(CancellationToken pCancellationToken) => new cSetMaximumConfiguration(pCancellationToken);
    }
}