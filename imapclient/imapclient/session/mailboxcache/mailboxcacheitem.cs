using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache
            {
                private class cItem : iMailboxHandle
                {
                    private readonly object mCache;
                    private readonly cEventSynchroniser mEventSynchroniser;
                    private readonly string mEncodedMailboxName;
                    private bool? mExists = null;
                    private cListFlags mListFlags = null;
                    private cLSubFlags mLSubFlags = null;
                    private cStatus mStatus = null;
                    private cMailboxStatus mMailboxStatus = null;
                    private cMailboxSelectedProperties mSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;

                    public cItem(object pCache, cEventSynchroniser pEventSynchroniser, string pEncodedMailboxName)
                    {
                        mCache = pCache;
                        mEventSynchroniser = pEventSynchroniser;
                        mEncodedMailboxName = pEncodedMailboxName;
                    }

                    public object Cache => mCache;
                    public string EncodedMailboxName => mEncodedMailboxName;

                    public cMailboxName MailboxName { get; set; }
                    public cCommandPart CommandPart { get; set; }

                    public bool? Exists => mExists;

                    public void ResetExists(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ResetExists));

                        fMailboxProperties lProperties;

                        if (mExists == true) lProperties = fMailboxProperties.exists;
                        else lProperties = 0;

                        mExists = false;
                        mListFlags = null;
                        mLSubFlags = null;
                        mStatus = null;
                        mMailboxStatus = null;
                        mSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;

                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lProperties, lContext);
                    }

                    public cListFlags ListFlags => mListFlags;
                    public cLSubFlags LSubFlags => mLSubFlags;

                    public void SetFlags(cListFlags pListFlags, cLSubFlags pLSubFlags, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetFlags), pListFlags, pLSubFlags);

                        fMailboxProperties lDifferences = ZSetExists();

                        if (pListFlags != null)
                        {
                            lDifferences |= cListFlags.Differences(mListFlags, pListFlags);
                            mListFlags = pListFlags;
                        }

                        if (pLSubFlags != null)
                        {
                            lDifferences |= cLSubFlags.Differences(mLSubFlags, pLSubFlags);
                            mLSubFlags = pLSubFlags;
                        }

                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lDifferences, lContext);
                    }

                    public void UpdateStatus(cStatus pStatus, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(UpdateStatus), pStatus);
                        if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));
                        mStatus = cStatus.Combine(mStatus, pStatus);
                    }

                    public cMailboxStatus MailboxStatus => mMailboxStatus;

                    public void UpdateMailboxStatus(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(UpdateMailboxStatus));
                        if (mStatus == null) throw new InvalidOperationException();
                        cMailboxStatus lMailboxStatus = new cMailboxStatus(mStatus.Messages ?? 0, mStatus.Recent ?? 0, mStatus.UIDNext ?? 0, mStatus.UIDValidity ?? 0, mStatus.Unseen ?? 0, mStatus.HighestModSeq ?? 0);
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxStatus.Differences(mMailboxStatus, lMailboxStatus);
                        mMailboxStatus = lMailboxStatus;
                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lDifferences, lContext);
                    }

                    public void SetMailboxStatus(cMailboxStatus pStatus, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetMailboxStatus), pStatus);
                        if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxStatus.Differences(mMailboxStatus, pStatus);
                        mMailboxStatus = pStatus;
                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lDifferences, lContext);
                    }

                    public cMailboxSelectedProperties SelectedProperties => mSelectedProperties;

                    public void SetSelectedProperties(cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetSelectedProperties), pMessageFlags, pSelectedForUpdate, pPermanentFlags);
                        if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                        ZSetSelectedProperties(new cMailboxSelectedProperties(mSelectedProperties, pMessageFlags, pSelectedForUpdate, pPermanentFlags), lContext);
                    }

                    public void SetMessageFlags(cMessageFlags pMessageFlags, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetMessageFlags), pMessageFlags);
                        if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                        ZSetSelectedProperties(new cMailboxSelectedProperties(mSelectedProperties, pMessageFlags), lContext);
                    }

                    public void SetPermanentFlags(bool pSelectedForUpdate, cMessageFlags pPermanentFlags, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetPermanentFlags), pSelectedForUpdate, pPermanentFlags);
                        ZSetSelectedProperties(new cMailboxSelectedProperties(mSelectedProperties, pSelectedForUpdate, pPermanentFlags), lContext);
                    }

                    private void ZSetSelectedProperties(cMailboxSelectedProperties pSelectedProperties, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZSetSelectedProperties), pSelectedProperties);
                        if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxSelectedProperties.Differences(mSelectedProperties, pSelectedProperties);
                        mSelectedProperties = pSelectedProperties;
                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lDifferences, lContext);
                    }

                    private fMailboxProperties ZSetExists()
                    {
                        if (mExists == true) return 0;
                        mExists = true;
                        return fMailboxProperties.exists;
                    }
                }
            }
        }
    }
}