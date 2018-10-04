using System;
using System.Collections.Generic;
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
                //private readonly cPersistentCache mPersistentCache;
                //private readonly cIMAPCallbackSynchroniser mSynchroniser;
                private cSelectedMailboxCache mCache;
                private readonly bool mSelectedForUpdate;
                private bool mAccessReadOnly;

                public cSelectedMailbox(cSelectedMailboxCache pCache, bool pSelectedForUpdate, bool pAccessReadOnly,  cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailbox), pMailboxCacheItem, pSelectedForUpdate, pAccessReadOnly, pCache);
                    mPersistentCache = pPersistentCache ?? throw new ArgumentNullException(nameof(pPersistentCache));
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mMailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mSelectedForUpdate = pSelectedForUpdate;
                    mAccessReadOnly = pAccessReadOnly;
                    pCache.SetMailboxCacheItem(pMailboxCacheItem, lContext);
                    mCache = pCache;
                }

                public iMailboxHandle MailboxHandle => mCache.MailboxHandle;
                public bool SelectedForUpdate => mSelectedForUpdate;
                public bool AccessReadOnly => mAccessReadOnly;
                public iMessageCache MessageCache => mCache;

                public bool HasPendingHighestModSeq() => mCache.HasPendingHighestModSeq();
                public void UpdateHighestModSeq(cTrace.cContext pParentContext) => mCache.UpdateHighestModSeq(pParentContext);

                public iMessageHandle GetHandle(uint pMSN) => mCache.GetHandle(pMSN); // this should only be called from a commandcompletion
                public iMessageHandle GetHandle(cUID pUID) => mCache.GetHandle(pUID);
                public uint GetMSN(iMessageHandle pMessageHandle) => mCache.GetMSN(pMessageHandle); // this should only be called when no msnunsafe commands are running
                public cMessageHandleList SetUnseenCount(int pMessageCount, IEnumerable<uint> pMSNs, cTrace.cContext pParentContext) => mCache.SetUnseenCount(pMessageCount, pMSNs, pParentContext); // this should only be called from a commandcompletion
                public cUIDList GetMessageUIDsWithDeletedFlag(cTrace.cContext pParentContext) => mCache.GetMessageUIDsWithDeletedFlag(pParentContext); // this should only be called from a commandcompletion

                public void SetUnselected(cTrace.cContext pParentContext) => mCache.SetInvalid(pParentContext);

                public override string ToString() => $"{nameof(cSelectedMailbox)}({mCache},{mSelectedForUpdate},{mAccessReadOnly})";
            }
        }
    }
}