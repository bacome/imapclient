using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailbox : iSelectedMailboxDetails
            {
                private readonly cIMAPCallbackSynchroniser mSynchroniser;
                private readonly cMailboxCacheItem mMailboxCacheItem;
                private readonly bool mSelectedForUpdate;

                private bool mAccessReadOnly;
                private cSelectedMailboxCache mCache;

                public cSelectedMailbox(cIMAPCallbackSynchroniser pSynchroniser, Action<cMessageUID, cTrace.cContext> pMessageExpunged, cMailboxCacheItem pMailboxCacheItem, bool pSelectedForUpdate, bool pAccessReadOnly, int pExists, int pRecent, uint pUIDNext, uint pUIDValidity, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailbox), pMailboxCacheItem, pSelectedForUpdate, pAccessReadOnly, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq);
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mMailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mSelectedForUpdate = pSelectedForUpdate;
                    mAccessReadOnly = pAccessReadOnly;
                    mCache = new cSelectedMailboxCache(pSynchroniser, pMessageExpunged, pMailboxCacheItem, pUIDValidity, pExists, pRecent, pUIDNext, pHighestModSeq, lContext);
                }

                public iMailboxHandle MailboxHandle => mMailboxCacheItem;
                public bool SelectedForUpdate => mSelectedForUpdate;
                public bool AccessReadOnly => mAccessReadOnly;
                public iMessageCache MessageCache => mCache;

                public bool HasPendingHighestModSeq() => mCache.HasPendingHighestModSeq();
                public void UpdateHighestModSeq(cTrace.cContext pParentContext) => mCache.UpdateHighestModSeq(pParentContext);

                public iMessageHandle GetHandle(uint pMSN) => mCache.GetHandle(pMSN); // this should only be called from a commandcompletion
                public iMessageHandle GetHandle(cUID pUID) => mCache.GetHandle(pUID);
                public uint GetMSN(iMessageHandle pMessageHandle) => mCache.GetMSN(pMessageHandle); // this should only be called when no msnunsafe commands are running

                public cMessageHandleList SetUnseenCount(int pMessageCount, cUIntList pMSNs, cTrace.cContext pParentContext) => mCache.SetUnseenCount(pMessageCount, pMSNs, pParentContext); // this should only be called from a commandcompletion

                // this should only be called when messages aren't being processed (e.g. from command start)
                public cMessageHandleList GetMessagesToBeExpunged(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(GetMessagesToBeExpunged));
                    if (!mSelectedForUpdate || mAccessReadOnly) return null;
                    return mCache.GetDeletedMessages(lContext);
                }

                public override string ToString() => $"{nameof(cSelectedMailbox)}({mMailboxCacheItem},{mSelectedForUpdate},{mAccessReadOnly})";
            }
        }
    }
}