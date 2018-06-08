﻿using System;
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

                protected cUIntList mUInts = null;

                public cCommandHookBaseSearch() { }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookBaseSearch), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kSearch)) return eProcessDataResult.notprocessed;

                    cUIntList lUInts = new cUIntList();

                    while (true)
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;

                        if (!pCursor.GetNZNumber(out _, out var lUInt))
                        {
                            lContext.TraceWarning("likely malformed search: not an nz-number list?");
                            return eProcessDataResult.notprocessed;
                        }

                        lUInts.Add(lUInt);
                    }

                    if (!pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed search: not at end?");
                        return eProcessDataResult.notprocessed;
                    }

                    if (mUInts == null) mUInts = lUInts;
                    else mUInts.AddRange(lUInts);

                    return eProcessDataResult.processed;
                }
            }
        }
    }
}