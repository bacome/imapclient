﻿using System;
using System.Threading;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress-increment callback.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cIncrementConfiguration
    {
        internal readonly cMethodControl MC;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. Invoked once for each batch of work in the operation, the argument specifies the size of the batch.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        internal cIncrementConfiguration(cMethodControl pMC)
        {
            MC = pMC;
            Increment = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified timeout. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        public cIncrementConfiguration(int pTimeout)
        {
            MC = new cMethodControl(pTimeout);
            Increment = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token and progress-increment callback. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        public cIncrementConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement = null)
        {
            MC = new cMethodControl(pCancellationToken);
            Increment = pIncrement;
        }

        public cIncrementConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement = null)
        {
            MC = new cMethodControl(pTimeout, pCancellationToken);
            Increment = pIncrement;
            Increment = pIncrement;
        }

        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public int Timeout => MC.Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public CancellationToken CancellationToken => MC.CancellationToken;

        public override string ToString() => $"{nameof(cIncrementConfiguration)}({MC},{Increment != null})";

        public static implicit operator cIncrementConfiguration(int pTimeout) => new cIncrementConfiguration(pTimeout);
        public static implicit operator cIncrementConfiguration(CancellationToken pCancellationToken) => new cIncrementConfiguration(pCancellationToken);
    }
}