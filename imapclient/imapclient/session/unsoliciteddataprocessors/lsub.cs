using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cLSubDataProcessor : iUnsolicitedDataProcessor
            {
                private static readonly cBytes kLSubSpace = new cBytes("LSUB ");

                private readonly fEnableableExtensions mEnabledExtensions;
                private readonly cMailboxCache mMailboxCache;

                public cLSubDataProcessor(fEnableableExtensions pEnabledExtensions, cMailboxCache pMailboxCache)
                {
                    mEnabledExtensions = pEnabledExtensions;
                    mMailboxCache = pMailboxCache;
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cLSubDataProcessor), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kLSubSpace)) return eProcessDataResult.notprocessed;

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mEnabledExtensions, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed lsub response");
                        return eProcessDataResult.notprocessed;
                    }

                    fLSubFlags lLSubFlags = 0;

                    if (lFlags.Has(@"\Noselect")) lLSubFlags = fLSubFlags.hassubscribedchildren;
                    else lLSubFlags = fLSubFlags.subscribed;

                    mMailboxCache.SetLSubFlags(cTools.UTF8BytesToString(lEncodedMailboxName), lMailboxName, lLSubFlags, lContext);

                    return eProcessDataResult.processed;
                }
            }
        }
    }
}