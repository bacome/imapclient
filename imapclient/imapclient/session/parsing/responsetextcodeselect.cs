using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataPermanentFlags : cResponseData
            {
                public readonly cMessageFlags Flags;
                public cResponseDataPermanentFlags(cFlags pFlags) { Flags = new cMessageFlags(pFlags); }
            }

            private class cResponseDataUIDNext : cResponseData
            {
                public readonly uint UIDNext;
                public cResponseDataUIDNext(uint pUIDNext) { UIDNext = pUIDNext; }
            }

            private class cResponseDataUIDValidity : cResponseData
            {
                public readonly uint UIDValidity;
                public cResponseDataUIDValidity(uint pUIDValidity) { UIDValidity = pUIDValidity; }
            }

            private class cResponseDataHighestModSeq : cResponseData
            {
                public readonly uint HighestModSeq;
                public cResponseDataHighestModSeq(uint pHighestModSeq) { HighestModSeq = pHighestModSeq; }
            }

            private class cResponseDataAccess : cResponseData
            {
                public readonly bool ReadOnly;
                public cResponseDataAccess(bool pReadOnly) { ReadOnly = pReadOnly; }
            }

            private class cResponseTextCodeParserSelect : iResponseTextCodeParser
            {
                private static readonly cBytes kPermanentFlagsSpace = new cBytes("PERMANENTFLAGS ");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");
                private static readonly cBytes kReadWriteRBracketSpace = new cBytes("READ-WRITE] ");
                private static readonly cBytes kReadOnlyRBracketSpace = new cBytes("READ-ONLY] ");

                private cCapabilities mCapabilities;

                public cResponseTextCodeParserSelect(cCapabilities pCapabilities)
                {
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseTextCodeParserSelect), nameof(Process));

                    if (pCursor.SkipBytes(kPermanentFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.SkipBytes(cBytesCursor.RBracketSpace)) rResponseData = new cResponseDataPermanentFlags(lFlags);
                        else
                        {
                            lContext.TraceWarning("likely malformed permanentflags");
                            rResponseData = null;
                        }

                        return true;
                    }

                    if (pCursor.SkipBytes(kUIDNextSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace)) rResponseData = new cResponseDataUIDNext(lNumber);
                        {
                            lContext.TraceWarning("likely malformed uidnext");
                            rResponseData = null;
                        }

                        return true;
                    }

                    if (pCursor.SkipBytes(kUIDValiditySpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace)) rResponseData = new cResponseDataUIDValidity(lNumber);
                        {
                            lContext.TraceWarning("likely malformed uidvalidity");
                            rResponseData = null;
                        }

                        return true;
                    }

                    if (mCapabilities.CondStore)
                    {
                        if (pCursor.SkipBytes(kHighestModSeqSpace))
                        {
                            if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace)) rResponseData = new cResponseDataHighestModSeq(lNumber);
                            {
                                lContext.TraceWarning("likely malformed highestmodseq");
                                rResponseData = null;
                            }

                            return true;
                        }
                    }

                    if (pCursor.SkipBytes(kReadWriteRBracketSpace))
                    {
                        rResponseData = new cResponseDataAccess(false);
                        return true;
                    }

                    if (pCursor.SkipBytes(kReadOnlyRBracketSpace))
                    {
                        rResponseData = new cResponseDataAccess(true);
                        return true;
                    }

                    rResponseData = null;
                    return false;
                }
            }
        }
    }
}