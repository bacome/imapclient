using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cCommandHookBaseSearch : cCommandHook
            {
                private static readonly cBytes kSearch = new cBytes("SEARCH");

                protected readonly cSelectedMailbox mSelectedMailbox;
                protected cUIntList mMSNs = null;

                public cCommandHookBaseSearch(cSelectedMailbox pSelectedMailbox)
                {
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookBaseSearch), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kSearch)) return eProcessDataResult.notprocessed;

                    cUIntList lMSNs = new cUIntList();

                    while (true)
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;

                        if (!pCursor.GetNZNumber(out _, out var lMSN))
                        {
                            lContext.TraceWarning("likely malformed search: not an nz-number list?");
                            return eProcessDataResult.notprocessed;
                        }

                        lMSNs.Add(lMSN);
                    }

                    if (!pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed search: not at end?");
                        return eProcessDataResult.notprocessed;
                    }

                    if (mMSNs == null) mMSNs = lMSNs;
                    else mMSNs.AddRange(lMSNs);

                    return eProcessDataResult.processed;
                }
            }
        }
    }
}