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
                public readonly cPermanentFlags Flags;
                public cResponseDataPermanentFlags(cPermanentFlags pFlags) { Flags = pFlags; }
                public override string ToString() => $"{nameof(cResponseDataPermanentFlags)}({Flags})";
            }

            private class cResponseDataUIDNext : cResponseData
            {
                public readonly uint UIDNext;
                public cResponseDataUIDNext(uint pUIDNext) { UIDNext = pUIDNext; }
                public override string ToString() => $"{nameof(cResponseDataUIDNext)}({UIDNext})";
            }

            private class cResponseDataUIDValidity : cResponseData
            {
                public readonly uint UIDValidity;
                public cResponseDataUIDValidity(uint pUIDValidity) { UIDValidity = pUIDValidity; }
                public override string ToString() => $"{nameof(cResponseDataUIDValidity)}({UIDValidity})";
            }

            private class cResponseDataHighestModSeq : cResponseData
            {
                public readonly uint HighestModSeq;
                public cResponseDataHighestModSeq(uint pHighestModSeq) { HighestModSeq = pHighestModSeq; }
                public override string ToString() => $"{nameof(cResponseDataHighestModSeq)}({HighestModSeq})";
            }

            private class cResponseDataAccess : cResponseData
            {
                public readonly bool ReadOnly;
                public cResponseDataAccess(bool pReadOnly) { ReadOnly = pReadOnly; }
                public override string ToString() => $"{nameof(cResponseDataAccess)}({ReadOnly})";
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
                        if (pCursor.GetFlags(out var lRawFlags) && pCursor.SkipBytes(cBytesCursor.RBracketSpace) && cPermanentFlags.TryConstruct(lRawFlags, out var lFlags))
                        {
                            rResponseData = new cResponseDataPermanentFlags(lFlags);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed permanentflags");

                        rResponseData = null;
                        return false;
                    }

                    if (pCursor.SkipBytes(kUIDNextSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            rResponseData = new cResponseDataUIDNext(lNumber);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidnext");

                        rResponseData = null;
                        return false;
                    }

                    if (pCursor.SkipBytes(kUIDValiditySpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            rResponseData = new cResponseDataUIDValidity(lNumber);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidvalidity");

                        rResponseData = null;
                        return false;
                    }

                    if (mCapabilities.CondStore)
                    {
                        if (pCursor.SkipBytes(kHighestModSeqSpace))
                        {
                            if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                            {
                                rResponseData = new cResponseDataHighestModSeq(lNumber);
                                return true;
                            }

                            lContext.TraceWarning("likely malformed highestmodseq");

                            rResponseData = null;
                            return false;
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