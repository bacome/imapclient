using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cMailboxCacheItem : iMailboxHandle
            {
                private readonly cPersistentCache mPersistentCache;
                private readonly cIMAPCallbackSynchroniser mSynchroniser;
                private readonly cMailboxCache mMailboxCache;
                private readonly string mEncodedMailboxPath;

                private cMailboxId mMailboxId = null;
                private bool? mExists = null;
                private cListFlags mListFlags = null;
                private cLSubFlags mLSubFlags = null;
                private cStatus mStatus = null;
                private cMailboxStatus mMailboxStatus = null;
                private cMailboxSelectedProperties mSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;

                public cMailboxCacheItem(cPersistentCache pPersistentCache, cIMAPCallbackSynchroniser pSynchroniser, cMailboxCache pMailboxCache, string pEncodedMailboxPath)
                {
                    mPersistentCache = pPersistentCache ?? throw new ArgumentNullException(nameof(pPersistentCache));
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    mEncodedMailboxPath = pEncodedMailboxPath ?? throw new ArgumentNullException(nameof(pEncodedMailboxPath));
                }

                iMailboxCache iMailboxHandle.MailboxCache => mMailboxCache;
                public cMailboxCache MailboxCache => mMailboxCache;
                public string EncodedMailboxPath => mEncodedMailboxPath;

                public cMailboxName MailboxName { get; set; }
                public cCommandPart MailboxNameCommandPart { get; set; }

                public cMailboxId MailboxId
                {
                    get
                    {
                        if (mMailboxId == null && MailboxName != null) mMailboxId = new cMailboxId(mMailboxCache.AccountId, MailboxName);
                        return mMailboxId;
                    }
                }

                public bool? Exists => mExists;

                public void SetJustCreated(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(SetJustCreated));

                    fMailboxProperties lProperties;

                    if (mExists != null) lProperties = fMailboxProperties.exists;
                    else lProperties = 0;

                    mExists = true;
                    mListFlags = null;
                    mStatus = null;
                    mMailboxStatus = null;
                    mSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;

                    mSynchroniser.InvokeMailboxPropertiesChanged(this, lProperties, lContext);
                }

                public void ResetExists(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(ResetExists));

                    fMailboxProperties lProperties;

                    if (mExists == true) lProperties = fMailboxProperties.exists;
                    else lProperties = 0;

                    mExists = false;
                    mListFlags = null;
                    mStatus = null;
                    mMailboxStatus = null;
                    mSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;

                    mSynchroniser.InvokeMailboxPropertiesChanged(this, lProperties, lContext);
                }

                public cListFlags ListFlags => mListFlags;
                public cLSubFlags LSubFlags => mLSubFlags;

                public void SetListFlags(cListFlags pListFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(SetListFlags), pListFlags);

                    if (pListFlags == null) throw new ArgumentNullException(nameof(pListFlags));

                    fMailboxProperties lDifferences = ZSetExists((pListFlags.Flags & fListFlags.nonexistent) == 0) | cListFlags.Differences(mListFlags, pListFlags);

                    mListFlags = pListFlags;

                    mSynchroniser.InvokeMailboxPropertiesChanged(this, lDifferences, lContext);
                }

                public void SetLSubFlags(cLSubFlags pLSubFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(SetLSubFlags), pLSubFlags);

                    if (pLSubFlags == null) throw new ArgumentNullException(nameof(pLSubFlags));

                    fMailboxProperties lDifferences = cLSubFlags.Differences(mLSubFlags, pLSubFlags);

                    mLSubFlags = pLSubFlags;

                    mSynchroniser.InvokeMailboxPropertiesChanged(this, lDifferences, lContext);
                }

                public cStatus Status => mStatus;

                public void ClearStatus(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(ClearStatus));
                    mStatus = null;
                }

                public void UpdateStatus(cStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(UpdateStatus), pStatus);
                    if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));
                    mStatus = cStatus.Combine(mStatus, pStatus);
                }

                public cMailboxStatus MailboxStatus => mMailboxStatus;

                public void UpdateMailboxStatus(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(UpdateMailboxStatus));

                    if (mStatus == null && mMailboxStatus == null) return;

                    fMailboxProperties lDifferences;

                    uint lUIDValidity;
                    if (mMailboxStatus == null) lUIDValidity = 0;
                    else lUIDValidity = mMailboxStatus.UIDValidity;

                    if (mStatus == null)
                    {
                        lDifferences = fMailboxProperties.messagecount | fMailboxProperties.recentcount | fMailboxProperties.uidnext | fMailboxProperties.unseencount | fMailboxProperties.highestmodseq; // not uidvalidity because it is likely to have some heavy processing attached to it if it is monitored
                        mMailboxStatus = null;
                    }
                    else
                    {
                        cMailboxStatus lMailboxStatus = new cMailboxStatus(mStatus.Messages ?? 0, mStatus.Recent ?? 0, mStatus.UIDNext ?? 0, mStatus.UIDValidity ?? 0, mStatus.Unseen ?? 0, mStatus.HighestModSeq ?? 0);
                        lDifferences = ZSetExists(true) | cMailboxStatus.Differences(mMailboxStatus, lMailboxStatus);
                        mMailboxStatus = lMailboxStatus;

                        if (mMailboxId != null && lMailboxStatus.UIDValidity != 0 && lMailboxStatus.UIDValidity != lUIDValidity) mPersistentCache.SetUIDValidity(mMailboxId, lMailboxStatus.UIDValidity, lContext);
                    }

                    mSynchroniser.InvokeMailboxPropertiesChanged(this, lDifferences, lContext);
                }

                public void SetMailboxStatus(cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(SetMailboxStatus), pMailboxStatus);
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    uint lUIDValidity;
                    if (mMailboxStatus == null) lUIDValidity = 0;
                    else lUIDValidity = mMailboxStatus.UIDValidity;

                    fMailboxProperties lDifferences = ZSetExists(true) | cMailboxStatus.Differences(mMailboxStatus, pMailboxStatus);
                    mMailboxStatus = pMailboxStatus;

                    if (mMailboxId != null && pMailboxStatus.UIDValidity != 0 && pMailboxStatus.UIDValidity != lUIDValidity) mPersistentCache.SetUIDValidity(mMailboxId, pMailboxStatus.UIDValidity, lContext);

                    mSynchroniser.InvokeMailboxPropertiesChanged(this, lDifferences, lContext);
                }

                public cMailboxSelectedProperties SelectedProperties => mSelectedProperties;

                public void SetSelectedProperties(cFetchableFlags pMessageFlags, bool pForUpdate, cPermanentFlags pPermanentFlags, bool pUIDNotSticky, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(SetSelectedProperties), pMessageFlags, pForUpdate, pPermanentFlags, pUIDNotSticky);
                    if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                    ZSetSelectedProperties(new cMailboxSelectedProperties(mSelectedProperties, pMessageFlags, pForUpdate, pPermanentFlags, pUIDNotSticky), lContext);
                }

                public void SetMessageFlags(cFetchableFlags pFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(SetMessageFlags), pFlags);
                    if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
                    ZSetSelectedProperties(new cMailboxSelectedProperties(mSelectedProperties, pFlags), lContext);
                }

                public void SetPermanentFlags(bool pForUpdate, cPermanentFlags pPermanentFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(SetPermanentFlags), pForUpdate, pPermanentFlags);
                    ZSetSelectedProperties(new cMailboxSelectedProperties(mSelectedProperties, pForUpdate, pPermanentFlags), lContext);
                }

                private void ZSetSelectedProperties(cMailboxSelectedProperties pSelectedProperties, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCacheItem), nameof(ZSetSelectedProperties), pSelectedProperties);
                    if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));
                    fMailboxProperties lDifferences = ZSetExists(true) | cMailboxSelectedProperties.Differences(mSelectedProperties, pSelectedProperties);
                    mSelectedProperties = pSelectedProperties;
                    mSynchroniser.InvokeMailboxPropertiesChanged(this, lDifferences, lContext);
                }

                private fMailboxProperties ZSetExists(bool pExists)
                {
                    if (mExists == pExists) return 0;
                    mExists = pExists;
                    return fMailboxProperties.exists;
                }

                public override string ToString() => $"{nameof(cMailboxCacheItem)}({mEncodedMailboxPath})";
            }
        }
    }
}