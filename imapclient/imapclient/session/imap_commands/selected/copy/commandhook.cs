using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookCopy : cCommandHook
            {
                private static readonly cBytes kCopyUIDSpace = new cBytes("COPYUID ");

                private uint mSourceUIDValidity;

                public cCommandHookCopy(uint pSourceUIDValidity) { mSourceUIDValidity = pSourceUIDValidity; }

                public cCopyFeedback Feedback { get; private set; } = null;

                public override bool ProcessTextCode(eResponseTextType pTextType, cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookCopy), nameof(ProcessTextCode), pTextType);

                    if (pTextType == eResponseTextType.success && pCursor.SkipBytes(kCopyUIDSpace))
                    {
                        if (!pCursor.GetNZNumber(out _, out var lDestinationUIDValidity) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetSequenceSet(out var lSourceUIDSet) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetSequenceSet(out var lDestinationUIDSet) ||
                            !pCursor.SkipBytes(cBytesCursor.RBracketSpace) ||
                            !cUIntList.TryConstruct(lSourceUIDSet, -1, false, out var lSourceUIDs) ||
                            !cUIntList.TryConstruct(lDestinationUIDSet, -1, false, out var lDestinationUIDs) ||
                            lSourceUIDs.Count != lDestinationUIDs.Count)
                        {
                            lContext.TraceWarning("likely malformed copyuid response");
                            return false;
                        }

                        Feedback = new cCopyFeedback(mSourceUIDValidity, lSourceUIDs, lDestinationUIDValidity, lDestinationUIDs);
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}