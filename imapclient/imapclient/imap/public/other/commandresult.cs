using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents the type of an IMAP command result.
    /// </summary>
    public enum eCommandResultType
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
    /// <seealso cref="cProtocolErrorException"/>
    public class cCommandResult
    {
        /// <summary>
        /// The type of the result.
        /// </summary>
        public readonly eCommandResultType ResultType;

        /// <summary>
        /// The response text associated with the result.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cCommandResult(eCommandResultType pResultType, cResponseText pResponseText)
        {
            ResultType = pResultType;
            ResponseText = pResponseText;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cCommandResult)}({ResultType},{ResponseText})";
    }
}