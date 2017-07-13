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

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName);
                    var lProperties = lItem.SetMailboxFlags(pMailboxFlags);
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

                public void StoreStatus(string pEncodedMailboxName, cStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(StoreStatus), pEncodedMailboxName, pStatus);

                    var lItem = ZItem(pEncodedMailboxName, null);
                    cStatus lStatus = cStatus.Combine(lItem.Status, pStatus);
                    lItem.Status = lStatus;

                    if (lItem.MailboxSelected.IsSelected) return;

                    cMailboxStatus lMailboxStatus = new cMailboxStatus(lStatus.Messages ?? 0, lStatus.Recent ?? 0, lStatus.UIDNext ?? 0, 0, lStatus.UIDValidity ?? 0, lStatus.Unseen ?? 0, 0, lStatus.HighestModSeq ?? 0);

                    var lProperties = lItem.SetMailboxStatus(lMailboxStatus);
                    if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                }

                public void Select(string pEncodedMailboxName, cMailboxName pMailboxName, bool pForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags, cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Select), pEncodedMailboxName, pMailboxName, pForUpdate, pAccessReadOnly, pMessageFlags, pPermanentFlags, pMailboxStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName);
                    if (lItem.MailboxSelected.IsSelected) throw new InvalidOperationException();

                    fMailboxProperties lProperties = 0;
                    lProperties |= lItem.Select(pForUpdate, pAccessReadOnly, pMessageFlags, pPermanentFlags);
                    lProperties |= lItem.SetMailboxStatus(pMailboxStatus);
                    if (lProperties != 0) ZMailboxPropertiesChanged(pMailboxName, lProperties, lContext);
                }

                public void SetAccessReadOnly(string pEncodedMailboxName, bool pAccessReadOnly, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetAccessReadOnly), pAccessReadOnly);

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName);
                    if (!lItem.MailboxSelected.IsSelected) throw new InvalidOperationException();
                }

                public void SetMessageFlags()
                {

                }

                public void SetPermanentFlags()
                {

                }

                public void SetMailboxStatus(string pEncodedMailboxName, cMailboxName pMailboxName, cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMailboxStatus), pEncodedMailboxName, pMailboxName, pMailboxStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName);

                    if (!lItem.MailboxSelected.IsSelected) throw new InvalidOperationException();

                    var lProperties = lItem.SetMailboxStatus(pMailboxStatus);
                    if (lProperties != 0) ZMailboxPropertiesChanged(pMailboxName, lProperties, lContext);
                }

                public void Deselect(string pEncodedMailboxName, cMailboxName pMailboxName, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Deselect), pEncodedMailboxName, pMailboxName);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName);
                    if (!lItem.MailboxSelected.IsSelected) throw new InvalidOperationException();

                    fMailboxProperties lProperties = lItem.Deselect();
                    if (lProperties != 0) ZMailboxPropertiesChanged(pMailboxName, lProperties, lContext);
                }

                ;?; // api name?
                public long StatusAge(cMailboxName pMailboxName) => ZItem(pMailboxId.MailboxName, false)?.StatusAge ?? long.MaxValue;

                private cItem ZItem(string pEncodedMailboxName, cMailboxName pMailboxName)
                {
                    cItem lItem = mDictionary.GetOrAdd(pEncodedMailboxName, new cItem());
                    if (lItem.MailboxName == null && pMailboxName != null) lItem.MailboxName = pMailboxName;
                    return lItem;
                }

                private void ZMailboxPropertiesChanged(cMailboxName pMailboxName, fMailboxProperties pProperties, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZMailboxPropertiesChanged), pMailboxName, pProperties);

                    if (pMailboxName == null || pProperties == 0 || !mEventSynchroniser.AreMailboxPropertyChangedSubscriptions) return;

                    cMailboxId lMailboxId = new cMailboxId(mConnectedAccountId, pMailboxName);

                    if ((pProperties & fMailboxProperties.exists) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.Exists), lContext);

                    if ((pProperties & fMailboxProperties.mailboxflags) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.MailboxFlags), lContext);
                    if ((pProperties & fMailboxProperties.canhavechildren) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.CanHaveChildren), lContext);
                    if ((pProperties & fMailboxProperties.canselect) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.CanSelect), lContext);
                    if ((pProperties & fMailboxProperties.ismarked) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.IsMarked), lContext);
                    if ((pProperties & fMailboxProperties.nonexistent) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.NonExistent), lContext);
                    if ((pProperties & fMailboxProperties.issubscribed) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.IsSubscribed), lContext);
                    if ((pProperties & fMailboxProperties.isremote) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.IsRemote), lContext);
                    if ((pProperties & fMailboxProperties.haschildren) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.HasChildren), lContext);
                    if ((pProperties & fMailboxProperties.hassubscribedchildren) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.HasSubscribedChildren), lContext);
                    if ((pProperties & fMailboxProperties.containsall) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ContainsAll), lContext);
                    if ((pProperties & fMailboxProperties.isarchive) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.IsArchive), lContext);
                    if ((pProperties & fMailboxProperties.containsdrafts) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ContainsDrafts), lContext);
                    if ((pProperties & fMailboxProperties.containsflagged) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ContainsFlagged), lContext);
                    if ((pProperties & fMailboxProperties.containsjunk) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ContainsJunk), lContext);
                    if ((pProperties & fMailboxProperties.containssent) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ContainsSent), lContext);
                    if ((pProperties & fMailboxProperties.containstrash) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ContainsTrash), lContext);

                    if ((pProperties & fMailboxProperties.mailboxstatus) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.MailboxStatus), lContext);
                    if ((pProperties & fMailboxProperties.messagecount) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.MessageCount), lContext);
                    if ((pProperties & fMailboxProperties.recentcount) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.RecentCount), lContext);
                    if ((pProperties & fMailboxProperties.uidnext) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.UIDNext), lContext);
                    if ((pProperties & fMailboxProperties.newunknownuidcount) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.NewUnknownUIDCount), lContext);
                    if ((pProperties & fMailboxProperties.uidvalidity) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.UIDValidity), lContext);
                    if ((pProperties & fMailboxProperties.unseencount) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.UnseenCount), lContext);
                    if ((pProperties & fMailboxProperties.unseenunknowncount) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.UnseenUnknownCount), lContext);
                    if ((pProperties & fMailboxProperties.highestmodseq) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.HighestModSeq), lContext);

                    if ((pProperties & fMailboxProperties.mailboxselected) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.MailboxSelected), lContext);
                    if ((pProperties & fMailboxProperties.isselected) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.IsSelected), lContext);
                    if ((pProperties & fMailboxProperties.isselectedforupdate) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.IsSelectedForUpdate), lContext);
                    if ((pProperties & fMailboxProperties.isaccessreadonly) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.IsAccessReadOnly), lContext);
                    if ((pProperties & fMailboxProperties.hasbeenselected) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.HasBeenSelected), lContext);
                    if ((pProperties & fMailboxProperties.hasbeenselectedforupdate) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.HasBeenSelectedForUpdate), lContext);
                    if ((pProperties & fMailboxProperties.hasbeenselectedreadonly) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.HasBeenSelectedReadOnly), lContext);
                    if ((pProperties & fMailboxProperties.messageflags) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.MessageFlags), lContext);
                    if ((pProperties & fMailboxProperties.forupdatepermanentflags) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ForUpdatePermanentFlags), lContext);
                    if ((pProperties & fMailboxProperties.readonlypermanentflags) != 0) mEventSynchroniser.MailboxPropertyChanged(lMailboxId, nameof(cMailbox.ReadOnlyPermanentFlags), lContext);
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
                    private cMailboxSelected mMailboxSelected = cMailboxSelected.New;

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
                        if (mMailboxSelected.IsSelected) return 0; // never expect a status reply for the selected mailbox
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

                        if (!mMailboxSelected.IsSelected)
                        {
                            if (mMailboxStatusStopwatch == null) mMailboxStatusStopwatch = Stopwatch.StartNew();
                            else mMailboxStatusStopwatch.Restart();
                        }

                        return lDifferences;
                    }

                    public long MailboxStatusAge => mMailboxStatusStopwatch?.ElapsedMilliseconds ?? long.MaxValue;

                    public cMailboxSelected MailboxSelected => mMailboxSelected;

                    public fMailboxProperties Select(bool pForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags)
                    {
                        ;?; // mailbox status as well?
                        if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                        var lSelected = mMailboxSelected.Select(pForUpdate, pAccessReadOnly, pMessageFlags, pPermanentFlags);
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxSelected.Differences(mMailboxSelected, lSelected);
                        mMailboxSelected = lSelected;
                        mMailboxStatusStopwatch = null;
                        return lDifferences;
                    }

                    public fMailboxProperties Deselect()
                    {
                        var lSelected = mMailboxSelected.Deselect();
                        fMailboxProperties lDifferences = cMailboxSelected.Differences(mMailboxSelected, lSelected);
                        mMailboxSelected = lSelected;
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
                        mMailboxSelected = cMailboxSelected.New;
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