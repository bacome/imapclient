using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataFlags : cResponseData
            {
                public readonly cMessageFlags Flags;
                public cResponseDataFlags(cFlags pFlags) { Flags = new cMessageFlags(pFlags); }
                public override string ToString() => $"{nameof(cResponseDataFlags)}({Flags})";
            }

            private class cResponseDataExists : cResponseData
            {
                public readonly int Exists;
                public cResponseDataExists(uint pExists) { Exists = (int)pExists; }
                public override string ToString() => $"{nameof(cResponseDataExists)}({Exists})";
            }

            private class cResponseDataRecent : cResponseData
            {
                public readonly int Recent;
                public cResponseDataRecent(uint pRecent) { Recent = (int)pRecent; }
                public override string ToString() => $"{nameof(cResponseDataRecent)}({Recent})";
            }

            private class cResponseDataParserSelect : iResponseDataParser
            {
                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");
                private static readonly cBytes kExists = new cBytes("EXISTS");
                private static readonly cBytes kRecent = new cBytes("RECENT");

                public cResponseDataParserSelect() { }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserSelect), nameof(Process));

                    if (pCursor.SkipBytes(kFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd) rResponseData = new cResponseDataFlags(lFlags);
                        else
                        {
                            lContext.TraceWarning("likely malformed flags response");
                            rResponseData = null;
                        }

                        return true;
                    }

                    if (pCursor.GetNumber(out _, out var lNumber) && pCursor.SkipByte(cASCII.SPACE))
                    {
                        if (pCursor.SkipBytes(kExists))
                        {
                            if (pCursor.Position.AtEnd) rResponseData = new cResponseDataExists(lNumber);
                            else
                            {
                                lContext.TraceWarning("likely malformed exists response");
                                rResponseData = null;
                            }

                            return true;
                        }

                        if (pCursor.SkipBytes(kRecent))
                        {
                            if (pCursor.Position.AtEnd) rResponseData = new cResponseDataRecent(lNumber);
                            else
                            {
                                lContext.TraceWarning("likely malformed recent response");
                                rResponseData = null;
                            }

                            return true;
                        }
                    }

                    rResponseData = null;
                    return false;
                }
            }
        }
    }
}