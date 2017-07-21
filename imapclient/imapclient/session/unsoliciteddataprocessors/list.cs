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
            private class cListDataProcessor : iUnsolicitedDataProcessor
            {

                private readonly fEnableableExtensions mEnabledExtensions;
                private readonly dGetCapability mGetCapability;
                private readonly cMailboxCache mMailboxCache;

                public cListDataProcessor(fEnableableExtensions pEnabledExtensions, dGetCapability pGetCapability, cMailboxCache pMailboxCache)
                {
                    mEnabledExtensions = pEnabledExtensions;
                    mGetCapability = pGetCapability;
                    mMailboxCache = pMailboxCache;
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListDataProcessor), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kListSpace)) return eProcessDataResult.notprocessed;

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                        !ZProcessExtendedItems(pCursor, out var lExtendedItems) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mEnabledExtensions, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed list response");
                        return eProcessDataResult.notprocessed;
                    }

                    var lCapability = mGetCapability();


                    // store
                    mMailboxCache.SetListFlags(cTools.UTF8BytesToString(lEncodedMailboxName), lMailboxName, lMailboxFlags, lContext);

                    // done
                    return eProcessDataResult.processed;
                }
            }
        }
    }
}