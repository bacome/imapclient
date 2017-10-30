using System;

namespace work.bacome.imapclient
{
    public enum eCommandResultType { ok, no, bad }

    public class cCommandResult
    {
        public readonly eCommandResultType ResultType;
        public readonly cResponseText ResponseText;

        public cCommandResult(eCommandResultType pResultType, cResponseText pResponseText)
        {
            ResultType = pResultType;
            ResponseText = pResponseText;
        }

        public override string ToString() => $"{nameof(cCommandResult)}({ResultType},{ResponseText})";
    }
}