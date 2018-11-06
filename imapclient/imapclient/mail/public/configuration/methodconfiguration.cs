using System;
using System.Threading;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress callbacks.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cMethodConfiguration
    {
        internal readonly cMethodControl MC;

        /// <summary>
        /// A progress-setmaximum callback for the operation. May be <see langword="null"/>. Invoked once before invokes of the associated progress-increment, the argument specifies the size of the operation.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<long> SetMaximum1;

        /// <summary>
        /// A progress-increment callback for the operation. May be <see langword="null"/>. Invoked for a batch of work in the operation, the argument specifies the size of the batch.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment1;

        /// <inheritdoc cref="SetMaximum1" select="summary|remarks"/>
        public readonly Action<long> SetMaximum2;

        /// <inheritdoc cref="Increment1" select="summary|remarks"/>
        public readonly Action<int> Increment2;

        internal cMethodConfiguration(cMethodControl pMC)
        {
            MC = pMC;
            SetMaximum1 = null;
            Increment1 = null;
            SetMaximum2 = null;
            Increment2 = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified timeout. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        public cMethodConfiguration(int pTimeout)
        {
            MC = new cMethodControl(pTimeout);
            SetMaximum1 = null;
            Increment1 = null;
            SetMaximum2 = null;
            Increment2 = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        public cMethodConfiguration(CancellationToken pCancellationToken)
        {
            MC = new cMethodControl(pCancellationToken);
            SetMaximum1 = null;
            Increment1 = null;
            SetMaximum2 = null;
            Increment2 = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token and progress-increment callback. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement1">May be <see langword="null"/>.</param>
        public cMethodConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement1)
        {
            MC = new cMethodControl(pCancellationToken);
            SetMaximum1 = null;
            Increment1 = pIncrement1;
            SetMaximum2 = null;
            Increment2 = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token and progress callbacks. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum1">May be <see langword="null"/>.</param>
        /// <param name="pIncrement1">May be <see langword="null"/>.</param>
        public cMethodConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum1, Action<int> pIncrement1)
        {
            MC = new cMethodControl(pCancellationToken);
            SetMaximum1 = pSetMaximum1;
            Increment1 = pIncrement1;
            SetMaximum2 = null;
            Increment2 = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token and progress callbacks. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement1">May be <see langword="null"/>.</param>
        /// <param name="pSetMaximum2">May be <see langword="null"/>.</param>
        /// <param name="pIncrement2">May be <see langword="null"/>.</param>
        public cMethodConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement1, Action<long> pSetMaximum2, Action<int> pIncrement2)
        {
            MC = new cMethodControl(pCancellationToken);
            SetMaximum1 = null;
            Increment1 = pIncrement1;
            SetMaximum2 = pSetMaximum2;
            Increment2 = pIncrement2;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token and progress callbacks. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum1">May be <see langword="null"/>.</param>
        /// <param name="pIncrement1">May be <see langword="null"/>.</param>
        /// <param name="pSetMaximum2">May be <see langword="null"/>.</param>
        /// <param name="pIncrement2">May be <see langword="null"/>.</param>
        public cMethodConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum1, Action<int> pIncrement1, Action<long> pSetMaximum2, Action<int> pIncrement2)
        {
            MC = new cMethodControl(pCancellationToken);
            SetMaximum1 = pSetMaximum1;
            Increment1 = pIncrement1;
            SetMaximum2 = pSetMaximum2;
            Increment2 = pIncrement2;
        }

        /// <summary>
        /// Initialises a new instance with the specified timeout, cancellation token and progress callbacks.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum1">May be <see langword="null"/>.</param>
        /// <param name="pIncrement1">May be <see langword="null"/>.</param>
        /// <param name="pSetMaximum2">May be <see langword="null"/>.</param>
        /// <param name="pIncrement2">May be <see langword="null"/>.</param>
        public cMethodConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<long> pSetMaximum1, Action<int> pIncrement1, Action<long> pSetMaximum2, Action<int> pIncrement2)
        {
            MC = new cMethodControl(pTimeout, pCancellationToken);
            SetMaximum1 = pSetMaximum1;
            Increment1 = pIncrement1;
            SetMaximum2 = pSetMaximum2;
            Increment2 = pIncrement2;
        }

        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public int Timeout => MC.Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public CancellationToken CancellationToken => MC.CancellationToken;

        public override string ToString() => $"{nameof(cMethodConfiguration)}({MC},{SetMaximum1 != null},{Increment1 != null},{SetMaximum2 != null},{Increment2 != null})";

        public static implicit operator cMethodConfiguration(int pTimeout) => new cMethodConfiguration(pTimeout);
        public static implicit operator cMethodConfiguration(CancellationToken pCancellationToken) => new cMethodConfiguration(pCancellationToken);
    }
}