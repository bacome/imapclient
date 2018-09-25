using System;
using System.Linq;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailbox
            {
                public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) => mCache.ProcessData(pData, pParentContext);
                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => mCache.ProcessData(pCursor, pParentContext);

                public void ProcessTextCode(eIMAPResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessTextCode), pTextContext, pData);

                    if (pData is cResponseDataUIDValidity lUIDValidity)
                    { 
                        mPersistentCache.MessageCacheDeactivated(mCache, lContext);
                        mCache = new cSelectedMailboxCache(mCache, lUIDValidity.UIDValidity, lContext);
                        return;
                    }
                    else mCache.ProcessTextCode(pTextContext, pData, lContext);
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

                        case cResponseDataFlags lFlags:

                            mMailboxCacheItem.SetMessageFlags(lFlags.Flags, lContext);
                            return eProcessDataResult.processed;

                        case cResponseDataVanished lVanished:

                            if (lVanished.Earlier)
                            {
                                if (mPersistentCache.Vanishedealier(mMailboxUID, lVanished.KnownUIDs, lContext)) return eProcessDataResult.processed;
                                return eProcessDataResult.notprocessed;
                            }
                            else
                            {

                                ;?;
                                // expunge
                            }

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

                public void ProcessTextCode(eIMAPResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ProcessTextCode), pTextContext, pData);

                    switch (pData)
                    {
                        case cResponseDataHighestModSeq lHighestModSeq when pTextContext == eIMAPResponseTextContext.success:

                            ZHighestModSeq(lHighestModSeq.HighestModSeq, lContext);
                            return;

                        case cResponseDataUIDNext lUIDNext:

                            ZUIDNext(lUIDNext.UIDNext, lContext);
                            return;

                        case cResponseDataPermanentFlags lPermanentFlags:

                            mMailboxCacheItem.SetPermanentFlags(mSelectedForUpdate, lPermanentFlags.Flags, lContext);
                            return;


                        case cResponseDataAccess lAccess:

                            if (lAccess.ReadOnly != mAccessReadOnly)
                            {
                                mAccessReadOnly = lAccess.ReadOnly;
                                mSynchroniser.InvokeMailboxPropertiesChanged(mMailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                            }

                            return;
                    }
                }
            }
        }
    }
}
