using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cMailboxCache
            {
                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly cAccountId mConnectedAccountId;
                private readonly ConcurrentDictionary<string, cItem> mDictionary = new ConcurrentDictionary<string, cItem>();

                public cMailboxCache(cEventSynchroniser pEventSynchroniser, cAccountId pConnectedAccountId)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mConnectedAccountId = pConnectedAccountId ?? throw new ArgumentNullException(nameof(pConnectedAccountId));
                }

                public cSelectedMailbox SelectedMailbox { get; set; } = null;

                public iMailboxCacheItem Item(string pEncodedMailboxName, cMailboxName pMailboxName) => ZItem(pEncodedMailboxName, pMailboxName);

                public void ResetExists(cMailboxNamePattern pPattern, int pMailboxFlagsSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pMailboxFlagsSequence);

                    foreach (var lItem in mDictionary.Values)
                    {
                        var lProperties = lItem.ResetExists(pPattern, pMailboxFlagsSequence);
                        if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                    }
                }

                public void ResetExists(string pEncodedMailboxName, int pMailboxStatusSequence, cTrace.cContext pParentContext)
                {
                    // this must not be called for the selected mailbox

                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pEncodedMailboxName, pMailboxStatusSequence);

                    if (mDictionary.TryGetValue(pEncodedMailboxName, out var lItem))
                    {
                        var lProperties = lItem.ResetExists(pMailboxStatusSequence);
                        if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                    }
                }

                public void SetMailboxFlags(string pEncodedMailboxName, cMailboxName pMailboxName, cMailboxFlags pMailboxFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMailboxFlags), pEncodedMailboxName, pMailboxName, pMailboxFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMailboxFlags == null) throw new ArgumentNullException(nameof(pMailboxFlags));

                    var lProperties = ZItem(pEncodedMailboxName, pMailboxName).SetMailboxFlags(pMailboxFlags);
                    if (lProperties != 0) ZMailboxPropertiesChanged(pMailboxName, lProperties, lContext);
                }

                public void SetLSubFlags(string pEncodedMailboxName, cLSubFlags pLSubFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetLSubFlags), pEncodedMailboxName, pLSubFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pLSubFlags == null) throw new ArgumentNullException(nameof(pLSubFlags));

                    var lItem = ZItem(pEncodedMailboxName, null);
                    var lProperties = lItem.SetLSubFlags(pLSubFlags);
                    if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                }

                public void ClearLSubFlags(cMailboxNamePattern pPattern, int pLSubFlagsSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pLSubFlagsSequence);

                    foreach (var lItem in mDictionary.Values)
                    {
                        var lProperties = lItem.ClearLSubFlags(pPattern, pLSubFlagsSequence);
                        if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                    }
                }

                public void UpdateMailboxStatus(string pEncodedMailboxName, cStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxStatus), pEncodedMailboxName, pStatus);

                    var lItem = ZItem(pEncodedMailboxName, null);
                    cStatus lStatus = cStatus.Combine(lItem.Status, pStatus);
                    lItem.Status = lStatus;

                    if (SelectedMailbox?.EncodedMailboxName == pEncodedMailboxName) return; // the status is currently coming from the selected mailbox

                    cMailboxStatus lMailboxStatus = new cMailboxStatus(lStatus.Messages ?? 0, lStatus.Recent ?? 0, lStatus.UIDNext ?? 0, 0, lStatus.UIDValidity ?? 0, lStatus.Unseen ?? 0, 0, lStatus.HighestModSeq ?? 0);

                    var lProperties = lItem.SetMailboxStatus(lMailboxStatus);
                    if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                }

                public void SetMailboxStatus(cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMailboxStatus), pMailboxStatus);

                    if (SelectedMailbox == null) throw new InvalidOperationException();
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    var lProperties = ZItem(SelectedMailbox.EncodedMailboxName, SelectedMailbox.MailboxId.MailboxName).SetMailboxStatus(pMailboxStatus);
                    if (lProperties != 0) cSession.ZMailboxPropertiesChanged(mEventSynchroniser, SelectedMailbox.MailboxId, lProperties, lContext);
                }

                public void UpdateMailboxBeenSelected(string pEncodedMailboxName, cMailboxName pMailboxName, cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxBeenSelected), pEncodedMailboxName, pMailboxName, pMessageFlags, pSelectedForUpdate, pPermanentFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));

                    var lProperties = ZItem(pEncodedMailboxName, pMailboxName).UpdateMailboxBeenSelected(pMessageFlags, pSelectedForUpdate, pPermanentFlags);
                    if (lProperties != 0) ZMailboxPropertiesChanged(pMailboxName, lProperties, lContext);
                }

                private cItem ZItem(string pEncodedMailboxName, cMailboxName pMailboxName)
                {
                    cItem lItem = mDictionary.GetOrAdd(pEncodedMailboxName, new cItem());
                    if (lItem.MailboxName == null && pMailboxName != null) lItem.MailboxName = pMailboxName;
                    return lItem;
                }

                private void ZMailboxPropertiesChanged(cMailboxName pMailboxName, fMailboxProperties pProperties, cTrace.cContext pContext)
                {
                    if (pMailboxName == null || pProperties == 0 || !mEventSynchroniser.AreMailboxPropertyChangedSubscriptions) return;
                    cSession.ZMailboxPropertiesChanged(mEventSynchroniser, new cMailboxId(mConnectedAccountId, pMailboxName), pProperties, pContext);
                }

                private class cItem : iMailboxCacheItem
                {
                    private cMailboxName mMailboxName = null;
                    private bool? mExists = null;
                    private cMailboxFlags mMailboxFlags = null;
                    private cLSubFlags mLSubFlags = null;
                    private cMailboxFlags mMergedFlags = null; // the merge between the mailbox and the lsub flags
                    private cStatus mStatus = null;
                    private cMailboxStatus mMailboxStatus = null;
                    private Stopwatch mMailboxStatusStopwatch = null;
                    private cMailboxBeenSelected mMailboxBeenSelected = cMailboxBeenSelected.No;

                    public cItem() { }

                    public cMailboxName MailboxName
                    {
                        get => mMailboxName;
                        set => mMailboxName = value ?? throw new ArgumentNullException();
                    }

                    public bool? Exists => mExists;

                    public fMailboxProperties ResetExists(cMailboxNamePattern pPattern, int pMailboxFlagsSequence)
                    {
                        if (mExists == false) return 0; // already done
                        if (mMailboxName == null) return 0; // can't tell
                        if (mMailboxFlags != null && mMailboxFlags.Sequence > pMailboxFlagsSequence) return 0; // been refreshed recently => probably still exists
                        if (!pPattern.Matches(mMailboxName.Name)) return 0; // don't expect that it should have been refreshed
                        return ZResetExists();
                    }

                    public fMailboxProperties ResetExists(int pMailboxStatusSequence)
                    {
                        if (mExists == false) return 0; // already done
                        if (mMailboxStatus != null && mMailboxStatus.Sequence > pMailboxStatusSequence) return 0; // been refreshed recently => probably still exists
                        return ZResetExists();
                    }

                    public cMailboxFlags MailboxFlags
                    {
                        get
                        {
                            if (mMergedFlags == null && mMailboxFlags != null) mMergedFlags = mMailboxFlags.Merge(mLSubFlags);
                            return mMergedFlags;
                        }
                    }

                    public fMailboxProperties SetMailboxFlags(cMailboxFlags pMailboxFlags)
                    { 
                        if (pMailboxFlags == null) throw new ArgumentNullException(nameof(pMailboxFlags));
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxFlags.Differences(mMailboxFlags, pMailboxFlags);
                        mMailboxFlags = pMailboxFlags;
                        mMergedFlags = null;
                        return lDifferences;
                    }

                    public fMailboxProperties SetLSubFlags(cLSubFlags pLSubFlags)
                    {
                        if (pLSubFlags == null) throw new ArgumentNullException(nameof(pLSubFlags));
                        var lDifferences = cLSubFlags.Differences(mLSubFlags, pLSubFlags);
                        mLSubFlags = pLSubFlags;
                        mMergedFlags = null;
                        return lDifferences;
                    }

                    public fMailboxProperties ClearLSubFlags(cMailboxNamePattern pPattern, int pLSubFlagsSequence)
                    {
                        if (mLSubFlags == null) return 0;
                        if (mMailboxName == null) return 0; // can't tell
                        if (mLSubFlags.Sequence > pLSubFlagsSequence) return 0; // been refreshed recently
                        if (!pPattern.Matches(mMailboxName.Name)) return 0; // don't expect that it should have been refreshed

                        fMailboxProperties lDifferences = cLSubFlags.Differences(mLSubFlags, null);
                        mLSubFlags = null;
                        mMergedFlags = null;
                        return lDifferences;
                    }

                    public cStatus Status
                    {
                        get => mStatus;
                        set => mStatus = value ?? throw new ArgumentNullException();
                    }

                    public cMailboxStatus MailboxStatus => mMailboxStatus;

                    public fMailboxProperties SetMailboxStatus(cMailboxStatus pMailboxStatus)
                    {
                        if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxStatus.Differences(mMailboxStatus, pMailboxStatus);
                        mMailboxStatus = pMailboxStatus;
                        if (mMailboxStatusStopwatch == null) mMailboxStatusStopwatch = Stopwatch.StartNew();
                        else mMailboxStatusStopwatch.Restart();
                        return lDifferences;
                    }

                    public long MailboxStatusAge => mMailboxStatusStopwatch?.ElapsedMilliseconds ?? long.MaxValue;

                    public cMailboxBeenSelected MailboxBeenSelected => mMailboxBeenSelected;

                    public fMailboxProperties UpdateMailboxBeenSelected(cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags)
                    {
                        if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                        if (pPermanentFlags == null) throw new ArgumentNullException(nameof(pPermanentFlags));
                        var lMailboxBeenSelected = mMailboxBeenSelected.Update(pMessageFlags, pSelectedForUpdate, pPermanentFlags);
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxBeenSelected.Differences(mMailboxBeenSelected, lMailboxBeenSelected);
                        mMailboxBeenSelected = lMailboxBeenSelected;
                        return lDifferences;
                    }

                    private fMailboxProperties ZResetExists()
                    {
                        mExists = false;
                        mMailboxFlags = null;
                        mLSubFlags = null;
                        mMergedFlags = null;
                        mStatus = null;
                        mMailboxStatus = null;
                        mMailboxStatusStopwatch = null;
                        mMailboxBeenSelected = cMailboxBeenSelected.No;
                        return fMailboxProperties.exists;
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