using System;

namespace work.bacome.imapclient
{
    public enum eCommandResultType { ok, no, bad }

    public class cCommandResult
    {
        /// <summary>
        /// The IMAP command completion type
        /// </summary>
        public readonly eCommandResultType ResultType;

        /// <summary>
        /// The IMAP response text associated with the command completion
        /// </summary>
        public readonly cResponseText ResponseText;

        public cCommandResult(eCommandResultType pResultType, cResponseText pResponseText)
        {
            ResultType = pResultType;
            ResponseText = pResponseText;
        }

        public override string ToString() => $"{nameof(cCommandResult)}({ResultType},{ResponseText})";
    }
}