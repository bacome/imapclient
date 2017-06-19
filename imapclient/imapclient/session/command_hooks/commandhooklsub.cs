using System;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookLSub : cCommandHook
            {
                private static readonly cBytes kLSubSpace = new cBytes("LSUB ");

                private readonly cMailboxNamePattern mMailboxNamePattern;
                private readonly fEnableableExtensions mEnabledExtensions;

                public readonly cMailboxList MailboxList = new cMailboxList();

                public cCommandHookLSub(cMailboxNamePattern pMailboxNamePattern, fEnableableExtensions pEnabledExtensions)
                {
                    mMailboxNamePattern = pMailboxNamePattern;
                    mEnabledExtensions = pEnabledExtensions;
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(ProcessData));

                    cResponseDataLSub lLSub;

                    if (pCursor.Parsed)
                    {
                        lLSub = pCursor.ParsedAs as cResponseDataLSub;
                        if (lLSub == null) return eProcessDataResult.notprocessed;
                    }
                    else
                    {
                        if (!pCursor.SkipBytes(kLSubSpace)) return eProcessDataResult.notprocessed;

                        if (!cResponseDataLSub.Process(pCursor, mEnabledExtensions, out lLSub, lContext))
                        {
                            lContext.TraceWarning("likely malformed lsub response");
                            return eProcessDataResult.notprocessed;
                        }
                    }

                    if (!mMailboxNamePattern.Matches(lLSub.MailboxName.Name)) return eProcessDataResult.notprocessed;

                    MailboxList.Store(lLSub.EncodedMailboxName, lLSub.MailboxName, lLSub.MailboxFlags);

                    return eProcessDataResult.observed;
                }

                public static class cTests
                {
                    [Conditional("DEBUG")]
                    public static void Tests(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewGeneric($"{nameof(cCommandHookLSub)}.{nameof(cTests)}.{nameof(Tests)}");
                        // TODO!
                    }
                }
            }
        }
    }
}