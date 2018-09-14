﻿using System;
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
                public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessData));

                    switch (pData)
                    {
                        case cResponseDataFlags lFlags:
                    
                            mMailboxCacheItem.SetMessageFlags(lFlags.Flags, lContext);
                            return eProcessDataResult.processed;

                        case cResponseDataVanished lVanished:

                            // note that during select these have to be saved up

                            if (lVanished.Earlier && cUIntList.TryConstruct(lVanished.KnownUIDs, -1, true, out var lUIDs))
                            {
                                mPersistentCache.MessagesExpunged(mMailboxCacheItem.MailboxId, from lUID in lUIDs select new cUID(mCache.UIDValidity, lUID), lContext);
                                return eProcessDataResult.processed;
                            }

                            break;
                    }

                    return mCache.ProcessData(pData, lContext);
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => mCache.ProcessData(pCursor, pParentContext);

                public void ProcessTextCode(eIMAPResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessTextCode), pTextContext, pData);

                    switch (pData)
                    {
                        case cResponseDataPermanentFlags lPermanentFlags:

                            mMailboxCacheItem.SetPermanentFlags(mSelectedForUpdate, lPermanentFlags.Flags, lContext);
                            return;

                        case cResponseDataUIDValidity lUIDValidity:

                            mPersistentCache.MessageCacheDeactivated(mCache, lContext);

                            // need UIDs to be able to process VANISHED
                            if (mQResyncEnabled && !mCache.NoModSeq && lUIDValidity.UIDValidity == 0) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.QResyncWithModSeqAndNoUIDValidity, fIMAPCapabilities.qresync, lContext);

                            mCache = new cSelectedMailboxCache(mCache, lUIDValidity.UIDValidity, lContext);
                            return;

                        case cResponseDataAccess lAccess:

                            if (lAccess.ReadOnly != mAccessReadOnly)
                            {
                                mAccessReadOnly = lAccess.ReadOnly;
                                mSynchroniser.InvokeMailboxPropertiesChanged(mMailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                            }

                            return;
                    }

                    mCache.ProcessTextCode(pTextContext, pData, lContext);
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

                        case cResponseDataVanished lVanished:

                            if (lVanished.Earlier) break;

                            ;?;

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

                    ;?; // vanished <seq> (NOT earlier)

                    return eProcessDataResult.notprocessed;
                }

                public void ProcessTextCode(eIMAPResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ProcessTextCode), pTextContext, pData);
                    if (pData is cResponseDataUIDNext lUIDNext) ZUIDNext(lUIDNext.UIDNext, lContext);
                    else if (pTextContext == eIMAPResponseTextContext.success && pData is cResponseDataHighestModSeq lHighestModSeq) ZHighestModSeq(lHighestModSeq.HighestModSeq, lContext);
                }
            }
        }
    }
}
