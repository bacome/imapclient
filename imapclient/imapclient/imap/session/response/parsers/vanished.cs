using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataParserVanished : iResponseDataParser
            {
                private static readonly cBytes kVANISHEDSpace = new cBytes("VANISHED ");
                private static readonly cBytes kEARLIERSpace = new cBytes("(EARLIER) ");

                public cResponseDataParserVanished() { }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserVanished), nameof(Process));

                    if (!pCursor.SkipBytes(kVANISHEDSpace)) { rResponseData = null; return false; }

                    bool lEarlier = pCursor.SkipBytes(kEARLIERSpace);

                    if (pCursor.GetSequenceSet(false, out var lKnownUIDs) && pCursor.Position.AtEnd && cUIntList.TryConstruct(lKnownUIDs, 0, true, out var lUIDs))
                    {
                        rResponseData = new cResponseDataVanished(lEarlier, lUIDs);
                        return true;
                    }

                    rResponseData = null;
                    return false;
                }
            }
        }
    }
}