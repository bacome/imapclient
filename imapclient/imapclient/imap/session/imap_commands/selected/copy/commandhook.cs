using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookCopy : cCommandHook
            {
                private static readonly cBytes kCopyUID = new cBytes("COPYUID");

                private uint mSourceUIDValidity;

                public cCommandHookCopy(uint pSourceUIDValidity) { mSourceUIDValidity = pSourceUIDValidity; }

                public cCopyFeedback Feedback { get; private set; } = null;

                public override void ProcessTextCode(eIMAPResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookCopy), nameof(ProcessTextCode), pTextContext, pCode, pArguments);

                    if (pTextContext == eIMAPResponseTextContext.success && pCode.Equals(kCopyUID))
                    {
                        if (pArguments != null)
                        {
                            cBytesCursor lCursor = new cBytesCursor(pArguments);

                            if (lCursor.GetNZNumber(out _, out var lDestinationUIDValidity) &&
                                lCursor.SkipByte(cASCII.SPACE) &&
                                lCursor.GetSequenceSet(out var lSourceUIDSet) &&
                                lCursor.SkipByte(cASCII.SPACE) &&
                                lCursor.GetSequenceSet(out var lCreatedUIDSet) &&
                                lCursor.Position.AtEnd &&
                                cUIntList.TryConstruct(lSourceUIDSet, -1, false, out var lSourceUIDs) &&
                                cUIntList.TryConstruct(lCreatedUIDSet, -1, false, out var lCreatedUIDs) &&
                                lSourceUIDs.Count == lCreatedUIDs.Count)
                            {
                                Feedback = new cCopyFeedback(mSourceUIDValidity, lSourceUIDs, lDestinationUIDValidity, lCreatedUIDs);
                                return;
                            }
                        }

                        lContext.TraceWarning("likely malformed copyuid response");
                    }
                }
            }
        }
    }
}