using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents the type of an IMAP command result.
    /// </summary>
    public enum eIMAPCommandResultType
    {
        /**<summary>Successful completion.</summary>*/
        ok,
        /**<summary>Unsuccessful completion.</summary>*/
        no,
        /**<summary>Protocol error.</summary>*/
        bad
    }

    /// <summary>
    /// Contains data relating to the result of an IMAP command.
    /// </summary>
    /// <seealso cref="cIMAPProtocolErrorException"/>
    public class cIMAPCommandResult
    {
        /// <summary>
        /// The type of the result.
        /// </summary>
        public readonly eIMAPCommandResultType ResultType;

        /// <summary>
        /// The response text associated with the result.
        /// </summary>
        public readonly cIMAPResponseText ResponseText;

        internal cIMAPCommandResult(eIMAPCommandResultType pResultType, cIMAPResponseText pResponseText)
        {
            ResultType = pResultType;
            ResponseText = pResponseText;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cIMAPCommandResult)}({ResultType},{ResponseText})";
    }
}