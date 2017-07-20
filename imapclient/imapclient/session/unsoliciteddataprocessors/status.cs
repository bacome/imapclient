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
            private class cStatusDataProcessor : iUnsolicitedDataProcessor
            {


                private readonly cMailboxCache mMailboxCache;

                public cStatusDataProcessor(cMailboxCache pMailboxCache)
                {
                    mMailboxCache = pMailboxCache;
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusDataProcessor), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kStatusSpace)) return eProcessDataResult.notprocessed;

                    if (!pCursor.GetAString(out string lEncodedMailboxName) ||
                        !pCursor.SkipBytes(cBytesCursor.SpaceLParen) ||
                        !ZProcessAttributes(pCursor, out var lStatus, lContext) ||
                        !pCursor.SkipByte(cASCII.RPAREN) ||
                        !pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed status response");
                        return eProcessDataResult.notprocessed;
                    }

                    mMailboxCache.UpdateMailboxStatus(lEncodedMailboxName, lStatus, lContext);

                    return eProcessDataResult.processed;
                }
            }
        }
    }
}