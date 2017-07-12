using System;
using System.Collections.Generic;
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
                private xx xx;
                private readonly Dictionary<string, cItem> mDictionary = new Dictionary<string, cItem>();

                public cMailboxCache(cEventSynchroniser mEventSynchroniser, cAccountId pAccountId)
                {
                    mMailboxPropertyChanged = pMailboxPropertyChanged ?? throw new ArgumentNullException(nameof(pMailboxPropertyChanged));
                }

                public iMailboxCacheItem Item(string pEncodedMailboxName, cMailboxName pMailboxName) => ZItem(pEncodedMailboxName, pMailboxName, false);

                public void SetListFlags(string pEncodedMailboxName, cMailboxName pMailboxName, cListFlags pListFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetListFlags), pEncodedMailboxName, pMailboxName, pListFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pListFlags == null) throw new ArgumentNullException(nameof(pListFlags));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName, true);
                    ZMailboxPropertiesChanged(pMailboxName, lItem.SetListFlags(pListFlags), lContext);
                }

                public void SetNonExistent(cMailboxNamePattern pPattern, int pListFlagsLastSequence)
                {
                    lock (mDictionary)
                    {
                        foreach (var lItem in mDictionary.Values)
                        {
                            if (lItem.MailboxName != null && !ReferenceEquals(lItem.ListFlags, cListFlags.NonExistent) && lItem.ListFlags.Sequence <= pListFlagsLastSequence && pPattern.Matches(lItem.MailboxName.Name))
                            {
                                cListFlags lOldListFlags = lItem.ListFlags;
                                cLSubFlags lOldLSubFlags = lItem.lsub
                            }
                        }
                    }
                }





                public void SetListFlags(string pEncodedMailboxName, cMailboxName pMailboxName, cListFlags pListFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetListFlags), pEncodedMailboxName, pMailboxName, pListFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pListFlags == null) throw new ArgumentNullException(nameof(pListFlags));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName, true);

                    cListFlags lOldFlags = lItem.ListFlags;
                    lItem.ListFlags = pListFlags;

                    var lDifferences = cListFlags.Differences(lOldFlags, pListFlags);
                    if (lDifferences != 0) ZMailboxPropertiesChanged(pMailboxName, lDifferences, lContext);
                }


                public void UpdateStatus(string pEncodedMailboxName, cStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateStatus), pEncodedMailboxName, pStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));

                    var lItem = ZItem(pEncodedMailboxName, null, true);

                    cStatus lNewStatus = cStatus.Combine(lItem.Status, pStatus);

                    lItem.Status = lNewStatus;

                    if (lItem.SelectedMailboxProperties.IsSelected) return;

                    cMailboxStatus lOldMailboxStatus = lItem.MailboxStatus;
                    cMailboxStatus lNewMailboxStatus = new cMailboxStatus(lNewStatus.Messages ?? 0, lNewStatus.Recent ?? 0, lNewStatus.UIDNext ?? 0, 0, lNewStatus.UIDValidity ?? 0, lNewStatus.Unseen ?? 0, 0, lNewStatus.HighestModSeq ?? 0);

                    lItem.MailboxStatus = lNewMailboxStatus;

                    cMailboxName lMailboxName = lItem.MailboxName;
                    if (lMailboxName == null) return; // can't throw events without it

                    var lDifferences = cMailboxStatus.Differences(lOldMailboxStatus, lNewMailboxStatus);
                    if (lDifferences != 0) ZMailboxPropertiesChanged(lMailboxName, lDifferences, lContext);
                }

                public void UpdateMailboxStatus(string pEncodedMailboxName, cMailboxName pMailboxName, cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxStatus), pEncodedMailboxName, pMailboxName, pMailboxStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName, false);

                    if (lItem == null || !lItem.IsSelected) throw new InvalidOperationException();

                    cMailboxStatus lOldMailboxStatus = lItem.MailboxStatus;

                    // set it regardless of change to reset the age
                    lItem.MailboxStatus = pMailboxStatus;

                    var lDifferences = cMailboxStatus.Differences(lOldMailboxStatus, pMailboxStatus);
                    if (lDifferences != 0) ZMailboxPropertiesChanged(pMailboxName, lDifferences, lContext);
                }

                public void SetSelected(string pEncodedMailboxName, cMailboxName pMailboxName, bool pSelectedForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags, cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetSelected), pEncodedMailboxName, pMailboxName, pSelectedForUpdate, pAccessReadOnly, pMessageFlags, pPermanentFlags, pMailboxStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName, true);

                    // the order is incase someone is accessing it while we are changing it

                    // status set regardless of change to reset the age
                    cMailboxStatus lOldMailboxStatus = lItem.MailboxStatus;
                    lItem.MailboxStatus = pMailboxStatus;
                    var lStatusDifferences = cMailboxStatus.Differences(lOldMailboxStatus, pMailboxStatus);

                    fProperties lSelectedDifferences = fProperties.isselected;

                    if (lItem.HasBeenSelected)
                    {
                        if (lItem.MessageFlags != pMessageFlags) lSelectedDifferences |= fProperties.messageflags;
                    }
                    else lSelectedDifferences |= fProperties.hasbeenselected;

                    if (pAccessReadOnly)
                    {
                        lSelectedDifferences |= fProperties.isaccessreadonly;
                        lItem.IsAccessReadOnly = pAccessReadOnly;
                    }

                    if (pSelectedForUpdate)
                    {
                        lSelectedDifferences |= fProperties.isselectedforupdate;

                        if (lItem.HasBeenSelectedForUpdate)
                        {
                            if (lItem.ForUpdatePermanentFlags != pPermanentFlags) lSelectedDifferences |= fProperties.forupdatepermanentflags;
                        }
                        else lSelectedDifferences |= fProperties.hasbeenselectedforupdate;

                        lItem.ForUpdatePermanentFlags = pPermanentFlags;
                        lItem.IsSelectedForUpdate = true;
                    }
                    else
                    {
                        if (lItem.HasBeenSelectedReadOnly)
                        {
                            if (lItem.ReadOnlyPermanentFlags != pPermanentFlags) lSelectedDifferences |= fProperties.readonlypermanentflags;
                        }
                        else lSelectedDifferences |= fProperties.hasbeenselectedreadonly;

                        lItem.ReadOnlyPermanentFlags = pPermanentFlags;
                    }

                    // last
                    lItem.IsSelected = true;

                    // events
                    if (lStatusDifferences != 0) ZMailboxPropertiesChanged(pMailboxName, lStatusDifferences, lContext);
                    ZMailboxPropertiesChanged(pMailboxName, lSelectedDifferences, lContext);
                }

                public void SetUnselected(string pEncodedMailboxName, cMailboxName pMailboxName, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetUnselected), pEncodedMailboxName, pMailboxName);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxName, true);

                    fProperties lDifferences = fProperties.isselected;
                    if (lItem.IsSelectedForUpdate) lDifferences |= fProperties.isselectedforupdate;
                    if (lItem.IsAccessReadOnly) lDifferences |= fProperties.isaccessreadonly;

                    lItem.IsSelected = false;
                    lItem.IsSelectedForUpdate = false;
                    lItem.IsAccessReadOnly = false;

                    ZMailboxPropertiesChanged(pMailboxName, lDifferences, lContext);
                }

                public void SetAccessReadOnly(bool pReadOnly)
                {
                    ;?;
                }

                ;?; // set access readonly

                ;?; // HERE

                public void SetMessageFlags(cMailboxName pMailboxName, cMessageFlags pMessageFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMessageFlags), pMailboxName, pMessageFlags);
                    if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                    var lItem = ZItem(pMailboxId.MailboxName, true);
                    if (lItem.MessageFlags) return;
                    lItem.Flags = pFlags;
                    mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Flags), lContext);
                }

                public void SetPermanentFlags(cMailboxId pMailboxId, cMessageFlags pPermanentFlags, cTrace.cContext pParentContext)
                {

                    
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetPermanentFlags), pMailboxId, pPermanentFlags);
                    // this can be set to null (e.g. if the permanent flags weren't received on a select)
                    var lItem = ZItem(pMailboxId.MailboxName, true);
                    if (pPermanentFlags == lItem.PermanentFlags) return;
                    lItem.PermanentFlags = pPermanentFlags;
                    mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.PermanentFlags), lContext);
                }

                // set status from cStatus - check tha tthe mailbox is not selected, ignore if not

                // set status from cMailboxStatus - CHECK THAT THE MAILBOX IS SELECTED, THROW IF NOT

                public long StatusAge(cMailboxName pMailboxName) => ZItem(pMailboxId.MailboxName, false)?.StatusAge ?? long.MaxValue;




                public void Delete()
                {
                    // from list
                    // take the pattern(s) and a seq no
                    //  the ones that match, update the listflags to deleted, lsubflags to null, mailboxstatus to null, selelctprops to null
                    // set status to null
                }

                public void Delete()
                {
                    // take mailbox name (called from status)
                }

                public void ClearLSub()
                {
                    // take pattern and seqno
                    // ...
                }


                private cItem ZItem(string pEncodedMailboxName, cMailboxName pMailboxName, bool pAddIfNotThere)
                {
                    cItem lItem;

                    lock (mDictionary)
                    {
                        if (mDictionary.TryGetValue(pEncodedMailboxName, out lItem))
                        {
                            if (lItem.MailboxId == null && pMailboxName != null) lItem.MailboxId = new cMailboxId(mConnectedAccountId, pMailboxName);
                        }
                        else if (pAddIfNotThere)
                        {
                            if () ;
                            lItem = new cItem(pMailboxName);
                            mDictionary.Add(pEncodedMailboxName, lItem);
                        }
                        else lItem = null;
                    }

                    return lItem;
                }

                private void ZMailboxPropertiesChanged(cMailboxId pMailboxId, cListFlags.fProperties pProperties, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZMailboxPropertiesChanged), pMailboxId, pProperties);

                    if (!mEventSynchroniser.MailboxPropertyChangedIsSubscribedTo) return;

                    if ((pProperties & cListFlags.fProperties.canhavechildren) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.CanHaveChildren), lContext);
                    if ((pProperties & fMailboxProperties.haschildren) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasChildren), lContext);
                    if ((pProperties & fMailboxProperties.canselect) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.CanSelect), lContext);
                    if ((pProperties & fMailboxProperties.ismarked) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsMarked), lContext);
                    if ((pProperties & fMailboxProperties.issubscribed) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSubscribed), lContext);
                    if ((pProperties & fMailboxProperties.hassubscribedchildren) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasSubscribedChildren), lContext);
                    if ((pProperties & fMailboxProperties.islocal) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsLocal), lContext);
                    if ((pProperties & fMailboxProperties.containsall) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsAll), lContext);
                    if ((pProperties & fMailboxProperties.isarchive) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsArchive), lContext);
                    if ((pProperties & fMailboxProperties.containsdrafts) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsDrafts), lContext);
                    if ((pProperties & fMailboxProperties.containsflagged) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsFlagged), lContext);
                    if ((pProperties & fMailboxProperties.containsjunk) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsJunk), lContext);
                    if ((pProperties & fMailboxProperties.containssent) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsSent), lContext);
                    if ((pProperties & fMailboxProperties.containstrash) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsTrash), lContext);

                    if ((pProperties & fMailboxProperties.messagecount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.MessageCount), lContext);
                    if ((pProperties & fMailboxProperties.recentcount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.RecentCount), lContext);
                    if ((pProperties & fMailboxProperties.uidnext) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.UIDNext), lContext);
                    if ((pProperties & fMailboxProperties.newunknownuidcount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.NewUnknownUIDCount), lContext);
                    if ((pProperties & fMailboxProperties.uidvalidity) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.UIDValidity), lContext);
                    if ((pProperties & fMailboxProperties.unseencount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.UnseenCount), lContext);
                    if ((pProperties & fMailboxProperties.unseenunknowncount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.UnseenUnknownCount), lContext);
                    if ((pProperties & fMailboxProperties.highestmodseq) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HighestModSeq), lContext);
                }

                private void ZMailboxPropertiesChanged(cMailboxId pMailboxId, fProperties pProperties, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZMailboxPropertiesChanged), pMailboxId, pProperties);

                    if ((pProperties & fProperties.isselected) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSelected), lContext);
                    if ((pProperties & fProperties.isselectedforupdate) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSelectedForUpdate), lContext);
                    if ((pProperties & fProperties.isaccessreadonly) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsAccessReadOnly), lContext);

                    if ((pProperties & fProperties.hasbeenselected) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasBeenSelected), lContext);
                    if ((pProperties & fProperties.hasbeenselectedforupdate) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasBeenSelectedForUpdate), lContext);
                    if ((pProperties & fProperties.hasbeenselectedreadonly) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasBeenSelectedReadOnly), lContext);

                    if ((pProperties & fProperties.messageflags) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.MessageFlags), lContext);
                    if ((pProperties & fProperties.forupdatepermanentflags) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ForUpdatePermanentFlags), lContext);
                    if ((pProperties & fProperties.readonlypermanentflags) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ReadOnlyPermanentFlags), lContext);
                }


                private class cItem : iMailboxCacheItem
                {
                    private cMailboxId mMailboxId;
                    private cListFlags mListFlags = cListFlags.NonExistent;
                    private cLSubFlags mLSubFlags = cLSubFlags.NonExistent;
                    private cMailboxStatus mMailboxStatus = cMailboxStatus.NonExistent;
                    private Stopwatch mMailboxStatusStopwatch = null;
                    private cSelectedMailboxProperties mSelectedMailboxProperties = cSelectedMailboxProperties.NonExistent;

                    public cItem(cMailboxId pMailboxId) { mMailboxId = pMailboxId; }

                    public cMailboxId MailboxId
                    {
                        get => mMailboxId;
                        set => mMailboxId = value ?? throw new ArgumentNullException();
                    }

                    public cListFlags ListFlags => mListFlags;

                    public cListFlags.fProperties SetListFlags(cListFlags pListFlags)
                    { 
                        if (pListFlags == null) throw new ArgumentNullException(nameof(pListFlags));
                        var lDifferences = cListFlags.Differences(mListFlags, pListFlags);
                        mListFlags = pListFlags;
                        return lDifferences;
                    }

                    public cLSubFlags LSubFlags => mLSubFlags;

                    public cLSubFlags.fProperties SetLSubFlags(cLSubFlags pLSubFlags)
                    {
                        if (pLSubFlags == null) throw new ArgumentNullException(nameof(pLSubFlags));
                        var lDifferences = cLSubFlags.Differences(mLSubFlags, pLSubFlags);
                        mLSubFlags = pLSubFlags;
                        return lDifferences;
                    }

                    public cStatus Status { get; set; } = null;

                    public cMailboxStatus MailboxStatus => mMailboxStatus;

                    public cMailboxStatus.fProperties SetMailboxStatus(cMailboxStatus pMailboxStatus)
                    {
                        if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                        var lDifferences = cMailboxStatus.Differences(pMailboxStatus, mMailboxStatus);

                        mMailboxStatus = pMailboxStatus;

                        if (ReferenceEquals(pMailboxStatus, cMailboxStatus.NonExistent)) mMailboxStatusStopwatch = null;
                        else
                        {
                            if (mMailboxStatusStopwatch == null) mMailboxStatusStopwatch = Stopwatch.StartNew();
                            else mMailboxStatusStopwatch.Restart();
                        }

                        return lDifferences;
                    }

                    public long MailboxStatusAge => mMailboxStatusStopwatch?.ElapsedMilliseconds ?? long.MaxValue;

                    public cSelectedMailboxProperties SelectedMailboxProperties => mSelectedMailboxProperties;

                    public cSelectedMailboxProperties.fProperties SetSelectedMailboxProperties(cSelectedMailboxProperties pProperties)
                    {
                        if (pProperties == null) throw new ArgumentNullException(nameof(pProperties));
                        var lDifferences = cSelectedMailboxProperties.Differences(mSelectedMailboxProperties, pProperties);
                        mSelectedMailboxProperties = pProperties;
                        return lDifferences;
                    }
                }
            }
        }
    }
}