using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailbox : iSelectedMailboxDetails
            {
                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly cMailboxCacheItem mMailboxCacheItem;
                private readonly bool mSelectedForUpdate;

                private bool mAccessReadOnly;
                private cSelectedMailboxCache mCache;

                public cSelectedMailbox(cCallbackSynchroniser pSynchroniser, cMailboxCacheItem pMailboxCacheItem, bool pSelectedForUpdate, bool pAccessReadOnly, int pExists, int pRecent, uint pUIDNext, uint pUIDValidity, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailbox), pMailboxCacheItem, pSelectedForUpdate, pAccessReadOnly, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq);
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mMailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mSelectedForUpdate = pSelectedForUpdate;
                    mAccessReadOnly = pAccessReadOnly;
                    mCache = new cSelectedMailboxCache(pSynchroniser, pMailboxCacheItem, pUIDValidity, pExists, pRecent, pUIDNext, pHighestModSeq, lContext);
                }

                public iMailboxHandle Handle => mMailboxCacheItem;
                public bool SelectedForUpdate => mSelectedForUpdate;
                public bool AccessReadOnly => mAccessReadOnly;
                public iMessageCache Cache => mCache;

                public void UpdateHighestModSeq(cTrace.cContext pParentContext) => mCache.UpdateHighestModSeq(pParentContext);

                public iMessageHandle GetHandle(uint pMSN) => mCache.GetHandle(pMSN); // this should only be called from a commandcompletion
                public iMessageHandle GetHandle(cUID pUID) => mCache.GetHandle(pUID);
                public uint GetMSN(iMessageHandle pHandle) => mCache.GetMSN(pHandle); // this should only be called when no msnunsafe commands are running

                public cMessageHandleList SetUnseen(int pMessageCount, cUIntList pMSNs, cTrace.cContext pParentContext) => mCache.SetUnseen(pMessageCount, pMSNs, pParentContext); // this should only be called from a commandcompletion

                public override string ToString() => $"{nameof(cSelectedMailbox)}({mMailboxCacheItem},{mSelectedForUpdate},{mAccessReadOnly})";
            }
        }
    }
}