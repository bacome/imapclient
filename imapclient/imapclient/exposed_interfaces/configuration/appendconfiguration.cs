using System;
using System.Threading;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-setmaximum and progress-increment callbacks, and quoted-printable-encode batch-size configurations.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    /// <seealso cref="cMailbox.Append(cAppendData, cAppendConfiguration)"/>
    /// <seealso cref="cMailbox.Append(System.Collections.Generic.IEnumerable{cAppendData}, cAppendConfiguration)"/>
    /// <seealso cref="cMailbox.Append(System.Net.Mail.MailMessage, cStorableFlags, DateTime?, System.Text.Encoding, cAppendConfiguration)"/>
    /// <seealso cref="cMailbox.Append(System.Collections.Generic.IEnumerable{System.Net.Mail.MailMessage}, cStorableFlags, DateTime?, System.Text.Encoding, cAppendConfiguration)"/>
    public class cAppendConfiguration : cQuotedPrintableEncodeConfiguration
    {
        /// <summary>
        /// The progress-setmaximum callback for the operation. May be <see langword="null"/>. 
        /// Invoked once after any required quoted-printable-encoding and before any sending to the server, the argument specifies how many bytes are going to be sent to the server.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<long> SetMaximum;

        /// <summary>
        /// Initialises a new instance with the specified timeout and quoted-printable-encode batch-size configurations. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        /// <param name="pReadConfiguration">May be <see langword="null"/>.</param>
        /// <param name="pWriteConfiguration">May be <see langword="null"/>.</param>
        public cAppendConfiguration(int pTimeout, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null) : base(pTimeout, pReadConfiguration, pWriteConfiguration)
        {
            SetMaximum = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-setmaximum and progress-increment callbacks and quoted-printable-encode batch-size configurations. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum">May be <see langword="null"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        /// <param name="pReadConfiguration">May be <see langword="null"/>.</param>
        /// <param name="pWriteConfiguration">May be <see langword="null"/>.</param>
        public cAppendConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null) : base(pCancellationToken, pIncrement, pReadConfiguration, pWriteConfiguration)
        {
            SetMaximum = pSetMaximum;
        }
    }
}