using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The type of IMAP command result.
    /// </summary>
    /// <seealso cref="cCommandResult"/>
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
        /// The result type.
        /// </summary>
        public readonly eCommandResultType ResultType;

        /// <summary>
        /// The response text.
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