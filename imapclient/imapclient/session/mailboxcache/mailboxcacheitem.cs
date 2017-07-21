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
                    private int mSequence = 0;
                    private bool? mExists = null;
                    private cMailboxFlags mMailboxFlags = null;
                    private cLSubFlags mLSubFlags = null;
                    private cMailboxFlags mMergedFlags = null; // the merge between the mailbox and the lsub flags
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

                    public int Sequence => mSequence;

                    public bool? Exists => mExists;

                    public void ResetExists(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ResetExists));

                        fMailboxProperties lProperties;

                        if (mExists == true) lProperties = -1;
                        else lProperties = 0;

                        mExists = false;
                        mMailboxFlags = null;
                        mLSubFlags = null;
                        mMergedFlags = null;
                        mStatus = null;
                        mMailboxStatus = null;
                        mSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;

                        mEventSynchroniser.FireMailboxPropertiesChanged(this, fMailboxProperties.exists, lContext);
                    }




                    /*
                    public void ResetExists(cMailboxNamePattern pPattern, int pMailboxFlagsSequence, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ResetExists), pPattern);
                        ;?;
                        if (mExists == false) return; // already done
                        if (MailboxName == null) return; // can't tell
                        if (mMailboxFlags != null && mMailboxFlags.Sequence > pMailboxFlagsSequence) return; // been refreshed recently => probably still exists
                        if (!pPattern.Matches(MailboxName.Name)) return; // don't expect that it should have been refreshed
                        mEventSynchroniser.FireMailboxPropertiesChanged(this, ZResetExists(), lContext);
                    }

                    public fMailboxProperties ResetExists(int pMailboxStatusSequence)
                    {
                        if (mExists == false) return 0; // already done
                        if (mMailboxStatus != null && mMailboxStatus.Sequence > pMailboxStatusSequence) return 0; // been refreshed recently => probably still exists
                        return ZResetExists();
                    } */

                    public cMailboxFlags MailboxFlags
                    {
                        get
                        {
                            if (mMergedFlags == null && mMailboxFlags != null) mMergedFlags = mMailboxFlags.Merge(mLSubFlags);
                            return mMergedFlags;
                        }
                    }

                    public void SetMailboxFlags(int pSequence, cMailboxFlags pMailboxFlags, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetMailboxFlags), pSequence, pMailboxFlags);
                        if (pMailboxFlags == null) throw new ArgumentNullException(nameof(pMailboxFlags));
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxFlags.Differences(mMailboxFlags, pMailboxFlags);
                        mSequence = pSequence;
                        mMailboxFlags = pMailboxFlags;
                        mMergedFlags = null;
                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lDifferences, lContext);
                    }

                    public void SetLSubFlags(int pSequence, cLSubFlags pLSubFlags, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetLSubFlags), pSequence, pLSubFlags);
                        if (pLSubFlags == null) throw new ArgumentNullException(nameof(pLSubFlags));
                        var lDifferences = ZSetExists() | cLSubFlags.Differences(mLSubFlags, pLSubFlags);
                        mSequence = pSequence;
                        mLSubFlags = pLSubFlags;
                        mMergedFlags = null;
                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lDifferences, lContext);
                    }

                    /*
                    public void ClearLSubFlags(cMailboxNamePattern pPattern, int pLSubFlagsSequence)
                    {
                        if (mLSubFlags == null) return 0;
                        if (MailboxName == null) return 0; // can't tell
                        if (mLSubFlags.Sequence > pLSubFlagsSequence) return 0; // been refreshed recently
                        if (!pPattern.Matches(MailboxName.Name)) return 0; // don't expect that it should have been refreshed

                        fMailboxProperties lDifferences = cLSubFlags.Differences(mLSubFlags, null);
                        mLSubFlags = null;
                        mMergedFlags = null;
                        return lDifferences;
                    } */

                    public cMailboxStatus MailboxStatus => mMailboxStatus;

                    public void UpdateStatus(int pSequence, cStatus pStatus, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(UpdateStatus), pSequence, pStatus);
                        if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));
                        mSequence = pSequence;
                        mStatus = cStatus.Combine(mStatus, pStatus);
                    }

                    public void UpdateMailboxStatus(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(UpdateMailboxStatus));
                        if (mStatus == null) throw new InvalidOperationException();
                        cMailboxStatus lMailboxStatus = new cMailboxStatus(mStatus.Messages ?? 0, mStatus.Recent ?? 0, mStatus.UIDNext ?? 0, mStatus.UIDValidity ?? 0, mStatus.Unseen ?? 0, mStatus.HighestModSeq ?? 0);
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxStatus.Differences(mMailboxStatus, lMailboxStatus);
                        mMailboxStatus = lMailboxStatus;
                        mEventSynchroniser.FireMailboxPropertiesChanged(this, lDifferences, lContext);
                    }

                    public cMailboxSelectedProperties SelectedProperties => mSelectedProperties;

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

                    ;?;
                    public fMailboxProperties UpdateMailboxSelectedProperties(cMailboxStatus pStatus, cMessageFlags pFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags)
                    {
                        if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));
                        if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
                        var lMailboxSelectedProperties = mMailboxSelectedProperties.Update(pFlags, pSelectedForUpdate, pPermanentFlags);
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxStatus.Differences(mMailboxStatus, pStatus) | cMailboxSelectedProperties.Differences(mMailboxSelectedProperties, lMailboxSelectedProperties);
                        mMailboxStatus = pStatus;
                        mMailboxSelectedProperties = lMailboxSelectedProperties;
                        return lDifferences;
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