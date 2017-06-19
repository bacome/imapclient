using System;

namespace work.bacome.imapclient
{
    public class cCommandResult
    {
        public enum eResult { ok, no, bad }

        public readonly eResult Result;
        public readonly cResponseText ResponseText;

        public cCommandResult(eResult pResult, cResponseText pResponseText)
        {
            Result = pResult;
            ResponseText = pResponseText;
        }

        public override string ToString() => $"{nameof(cCommandResult)}({Result},{ResponseText})";
    }
}