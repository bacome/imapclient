using System;
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

                public bool ProcessTextCode(eResponseTextType pTextType, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessTextCode), pTextType, pData);

                    switch (pData)
                    {
                        case cResponseDataPermanentFlags lPermanentFlags:

                            mMailboxCacheItem.SetPermanentFlags(mSelectedForUpdate, lPermanentFlags.Flags, lContext);
                            return true;

                        case cResponseDataUIDValidity lUIDValidity:

                            mCache = new cSelectedMailboxCache(mCache, lUIDValidity.UIDValidity, lContext);
                            return true;

                        case cResponseDataAccess lAccess:

                            if (lAccess.ReadOnly != mAccessReadOnly)
                            {
                                mAccessReadOnly = lAccess.ReadOnly;
                                mSynchroniser.InvokeMailboxPropertiesChanged(mMailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                            }

                            return true;
                    }

                    return mCache.ProcessTextCode(pTextType, pData, lContext);
                }
            }

            private partial class cSelectedMailboxCache
            {
                private static readonly cBytes kSpaceExpunge = new cBytes(" EXPUNGE");

                public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ProcessData));

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
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ProcessData));

                    if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(kSpaceExpunge) && pCursor.Position.AtEnd)
                    {
                        lContext.TraceVerbose("got expunge: {0}", lNumber);
                        ZExpunge((int)lNumber, lContext);
                        return eProcessDataResult.processed;
                    }

                    return eProcessDataResult.notprocessed;
                }

                public bool ProcessTextCode(eResponseTextType pTextType, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ProcessTextCode), pTextType, pData);

                    if (pData is cResponseDataUIDNext lUIDNext)
                    {
                        ZUIDNext(lUIDNext.UIDNext, lContext);
                        return true;
                    }

                    if (pTextType == eResponseTextType.success)
                    {
                        if (pData is cResponseDataHighestModSeq lHighestModSeq)
                        {
                            ZHighestModSeq(lHighestModSeq.HighestModSeq, lContext);
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}
