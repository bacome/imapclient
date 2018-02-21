using System;
using System.Threading;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress-increment callback, and quoted-printable-encode batch-size configurations.
    /// </summary>
    /// <remarks>
    /// If the operation requires data to be quoted-printable-encoded client-side, <see cref="Increment"/> is invoked once for each batch of bytes converted, the argument specifies how many input bytes were converted.
    /// If the operation sends data to the server, <see cref="Increment"/> is invoked once for each batch of bytes sent to the server, the argument specifies how many bytes were sent in the batch.
    /// </remarks>
    /// <seealso cref="cIMAPClient.QuotedPrintableEncode(System.IO.Stream, System.IO.Stream, cQuotedPrintableEncodeConfiguration)"/>
    /// <seealso cref="cIMAPClient.QuotedPrintableEncode(System.IO.Stream, eQuotedPrintableEncodeSourceType, eQuotedPrintableEncodeQuotingRule, System.IO.Stream, cQuotedPrintableEncodeConfiguration)"/>
    /// <seealso cref="cIMAPClient.ConvertMailMessage(System.CodeDom.Compiler.TempFileCollection, System.Net.Mail.MailMessage, cStorableFlags, DateTime?, System.Text.Encoding, cQuotedPrintableEncodeConfiguration)"/>
    public class cQuotedPrintableEncodeConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. 
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        public readonly cBatchSizerConfiguration ReadConfiguration;
        public readonly cBatchSizerConfiguration WriteConfiguration;

        /// <summary>
        /// Initialises a new instance with the specified timeout and quoted-printable-encode batch-size configurations. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        /// <param name="pReadConfiguration">May be <see langword="null"/>.</param>
        /// <param name="pWriteConfiguration">May be <see langword="null"/>.</param>
        public cQuotedPrintableEncodeConfiguration(int pTimeout, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            ReadConfiguration = pReadConfiguration;
            WriteConfiguration = pWriteConfiguration;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-increment callback and quoted-printable-encode batch-size configurations. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        /// <param name="pReadConfiguration">May be <see langword="null"/>.</param>
        /// <param name="pWriteConfiguration">May be <see langword="null"/>.</param>
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