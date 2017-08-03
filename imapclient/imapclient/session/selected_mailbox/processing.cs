﻿using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailbox
            {
                public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessData));

                    if (pData is cResponseDataFlags lFlags)
                    {
                        mMailboxCacheItem.SetMessageFlags(lFlags.Flags, lContext);
                        return eProcessDataResult.processed;
                    }

                    return mCache.ProcessData(pData, lContext);
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => mCache.ProcessData(pCursor, pParentContext);

                public void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessTextCode));

                    switch (pData)
                    {
                        case cResponseDataPermanentFlags lPermanentFlags:

                            mMailboxCacheItem.SetPermanentFlags(mSelectedForUpdate, lPermanentFlags.Flags, lContext);
                            return;

                        case cResponseDataUIDValidity lUIDValidity:

                            mCache = new cSelectedMailboxMessageCache(mCache, lUIDValidity.UIDValidity, lContext);
                            return;

                        case cResponseDataAccess lAccess:

                            if (lAccess.ReadOnly != mAccessReadOnly)
                            {
                                mAccessReadOnly = lAccess.ReadOnly;
                                mEventSynchroniser.FireMailboxPropertiesChanged(mMailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                            }

                            return;
                    }

                    mCache.ProcessTextCode(pData, lContext);
                }
            }

            private partial class cSelectedMailboxMessageCache
            {
                private static readonly cBytes kSpaceExpunge = new cBytes(" EXPUNGE");

                public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ProcessData));

                    switch (pData)
                    {
                        case cResponseDataFetch lFetch:

                            ZFetch(lFetch, lContext);
                            return eProcessDataResult.observed;

                        case cResponseDataExists lExists:

                            ZExists(lExists.Exists, lContext);
                            return eProcessDataResult.processed;

                        case cResponseDataRecent lRecent:

                            ZRecent(lRecent.Recent, lContext);
                            return eProcessDataResult.processed;
                    }

                    return eProcessDataResult.notprocessed;
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ProcessData));

                    if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(kSpaceExpunge) && pCursor.Position.AtEnd)
                    {
                        lContext.TraceVerbose("got expunge: {0}", lNumber);
                        ZExpunge((int)lNumber, lContext);
                        return eProcessDataResult.processed;
                    }

                    return eProcessDataResult.notprocessed;
                }

                public void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ProcessTextCode));

                    switch (pData)
                    {
                        case cResponseDataUIDNext lUIDNext:

                            ZUIDNext(lUIDNext.UIDNext, lContext);
                            return;

                        case cResponseDataHighestModSeq lHighestModSeq:

                            ZHighestModSeq(lHighestModSeq.HighestModSeq, lContext);
                            return;
                    }
                }
            }
        }
    }
}
