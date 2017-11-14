using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The type of IMAP command result.
    /// </summary>
    /// <seealso cref="cCommandResult"/>
    public enum eCommandResultType
    {
        /**<summary>OK</summary>*/
        ok,
        /**<summary>NO</summary>*/
        no,
        /**<summary>BAD</summary>*/
        bad
    }

    /// <summary>
    /// Represents the result of an IMAP command.
    /// </summary>
    /// <seealso cref="cProtocolErrorException"/>
    public class cCommandResult
    {
        /// <summary>
        /// The IMAP command result type.
        /// </summary>
        public readonly eCommandResultType ResultType;

        /// <summary>
        /// The IMAP response text associated with the command result.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cCommandResult(eCommandResultType pResultType, cResponseText pResponseText)
        {
            ResultType = pResultType;
            ResponseText = pResponseText;
        }

        /**<summary>Returns a string that represents the result.</summary>*/
        public override string ToString() => $"{nameof(cCommandResult)}({ResultType},{ResponseText})";
    }
}