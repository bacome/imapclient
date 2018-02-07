using System;
using System.IO;

namespace work.bacome.imapclient
{
    public enum eQuotedPrintableSourceType { CRLFTerminatedLines, LFTerminatedLines, Binary }

    public partial class cIMAPClient
    {
        public static int QuotedPrintable(Stream pSource, eQuotedPrintableSourceType pSourceType, Stream pTarget = null)
        {
            // returns the number of bytes written to the target

            if (pSource == null) throw new ArgumentNullException(nameof(pSource));
            if (!pSource.CanRead) throw new ArgumentOutOfRangeException(nameof(pSource));

            Stream lTarget;
            if (pTarget == null) lTarget = Stream.Null;
            else lTarget = pTarget;

            ;?;
        }
    }
}