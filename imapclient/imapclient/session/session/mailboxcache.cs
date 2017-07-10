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
                [Flags]
                private enum fProperties
                {
                    isselected = 1 << 0,
                    isselectedforupdate = 1 << 24,
                    isaccessreadonly = 1 << 25,

                    hasbeenselected = 1 << 26,
                    messageflags = 1 << 27,
                    hasbeenselectedforupdate = 1 << 28,
                    forupdatepermanentflags = 1 << 29,
                    hasbeenselectedreadonly = 1 << 30,
                    readonlypermanentflags = 1 << 31
                }

                private readonly Action<cMailboxId, string, cTrace.cContext> mMailboxPropertyChanged;
                private readonly Dictionary<string, cItem> mDictionary = new Dictionary<string, cItem>();

                public cMailboxCache(Action<cMailboxId, string, cTrace.cContext> pMailboxPropertyChanged)
                {
                    mMailboxPropertyChanged = pMailboxPropertyChanged ?? throw new ArgumentNullException(nameof(pMailboxPropertyChanged));
                }

                public iMailboxCacheItem Item(string pEncodedMailboxName) => ZItem(pEncodedMailboxName, null, false);

                public void UpdateMailboxFlags(string pEncodedMailboxName, cMailboxId pMailboxId, cMailboxFlags pMailboxFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxFlags), pEncodedMailboxName, pMailboxId, pMailboxFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
                    if (pMailboxFlags == null) throw new ArgumentNullException(nameof(pMailboxFlags));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxId, true);

                    cMailboxFlags lOldFlags = lItem.MailboxFlags;
                    cMailboxFlags lNewFlags = cMailboxFlags.Combine(lOldFlags, pMailboxFlags);

                    if (lNewFlags == lOldFlags) return;

                    lItem.MailboxFlags = lNewFlags;

                    var lDifferences = cMailboxFlags.Differences(lOldFlags, lNewFlags);
                    if (lDifferences != 0) ZMailboxPropertiesChanged(pMailboxId, lDifferences, lContext);
                }

                public void UpdateStatus(string pEncodedMailboxName, cStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateStatus), pEncodedMailboxName, pStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));

                    var lItem = ZItem(pEncodedMailboxName, null, true);

                    cStatus lNewStatus = cStatus.Combine(lItem.Status, pStatus);

                    lItem.Status = lNewStatus;

                    if (lItem.IsSelected) return;

                    cMailboxStatus lOldMailboxStatus = lItem.MailboxStatus;
                    cMailboxStatus lNewMailboxStatus = new cMailboxStatus(lNewStatus);

                    // set it regardless of change to reset the age
                    lItem.MailboxStatus = lNewMailboxStatus;

                    cMailboxId lMailboxId = lItem.MailboxId;
                    if (lMailboxId == null) return; // can't throw events without it

                    var lDifferences = cMailboxStatus.Differences(lOldMailboxStatus, lNewMailboxStatus);
                    if (lDifferences != 0) ZMailboxPropertiesChanged(lMailboxId, lDifferences, lContext);
                }

                public void UpdateMailboxStatus(string pEncodedMailboxName, cMailboxId pMailboxId, cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxStatus), pEncodedMailboxName, pMailboxId, pMailboxStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxId, false);

                    if (lItem == null || !lItem.IsSelected) throw new InvalidOperationException();

                    cMailboxStatus lOldMailboxStatus = lItem.MailboxStatus;

                    // set it regardless of change to reset the age
                    lItem.MailboxStatus = pMailboxStatus;

                    var lDifferences = cMailboxStatus.Differences(lOldMailboxStatus, pMailboxStatus);
                    if (lDifferences != 0) ZMailboxPropertiesChanged(pMailboxId, lDifferences, lContext);
                }

                public void SetSelected(string pEncodedMailboxName, cMailboxId pMailboxId, bool pSelectedForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags, cMailboxStatus pMailboxStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetSelected), pEncodedMailboxName, pMailboxId, pSelectedForUpdate, pAccessReadOnly, pMessageFlags, pPermanentFlags, pMailboxStatus);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
                    if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                    if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxId, true);

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
                    if (lStatusDifferences != 0) ZMailboxPropertiesChanged(pMailboxId, lStatusDifferences, lContext);
                    ZMailboxPropertiesChanged(pMailboxId, lSelectedDifferences, lContext);
                }

                public void SetUnselected(string pEncodedMailboxName, cMailboxId pMailboxId, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetUnselected), pEncodedMailboxName, pMailboxId);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

                    var lItem = ZItem(pEncodedMailboxName, pMailboxId, true);

                    fProperties lDifferences = fProperties.isselected;
                    if (lItem.IsSelectedForUpdate) lDifferences |= fProperties.isselectedforupdate;
                    if (lItem.IsAccessReadOnly) lDifferences |= fProperties.isaccessreadonly;

                    lItem.IsSelected = false;
                    lItem.IsSelectedForUpdate = false;
                    lItem.IsAccessReadOnly = false;

                    ZMailboxPropertiesChanged(pMailboxId, lDifferences, lContext);
                }

                public void SetAccessReadOnly(bool pReadOnly)
                {
                    ;?;
                }

                ;?; // set access readonly

                ;?; // HERE

                public void SetMessageFlags(cMailboxId pMailboxId, cMessageFlags pMessageFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMessageFlags), pMailboxId, pMessageFlags);
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

                public long StatusAge(cMailboxId pMailboxId) => ZItem(pMailboxId.MailboxName, false)?.StatusAge ?? long.MaxValue;

                private cItem ZItem(string pEncodedMailboxName, cMailboxId pMailboxId, bool pAddIfNotThere)
                {
                    cItem lItem;

                    lock (mDictionary)
                    {
                        if (mDictionary.TryGetValue(pEncodedMailboxName, out lItem))
                        {
                            if (lItem.MailboxId == null) lItem.MailboxId = pMailboxId;
                        }
                        else if (pAddIfNotThere)
                        {
                            lItem = new cItem(pMailboxId);
                            mDictionary.Add(pEncodedMailboxName, lItem);
                        }
                        else lItem = null;
                    }

                    return lItem;
                }

                private void ZMailboxPropertiesChanged(cMailboxId pMailboxId, fMailboxProperties pProperties, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZMailboxPropertiesChanged), pMailboxId, pProperties);

                    if ((pProperties & fMailboxProperties.canhavechildren) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.CanHaveChildren), lContext);
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
                    private cMessageFlags mMessageFlags = null;
                    private cMessageFlags mForUpdatePermanentFlags = null;
                    private cMessageFlags mReadOnlyPermanentFlags = null;
                    private cMailboxStatus mMailboxStatus = null;
                    private Stopwatch mMailboxStatusStopwatch = null;

                    public cItem(cMailboxId pMailboxId)
                    {
                        MailboxId = pMailboxId;
                    }

                    public cMailboxId MailboxId { get; set; } // may be null if the cache was populated from a status

                    public cMailboxFlags MailboxFlags { get; set; } = null;

                    public bool IsSelected { get; set; } = false;
                    public bool IsSelectedForUpdate { get; set; } = false;
                    public bool IsAccessReadOnly { get; set; } = false;

                    public bool HasBeenSelected { get; set; } = false;
                    public bool HasBeenSelectedForUpdate { get; set; } = false;
                    public bool HasBeenSelectedReadOnly { get; set; } = false;

                    public cMessageFlags MessageFlags
                    {
                        get
                        {
                            if (!HasBeenSelected) throw new cNeverBeenSelectedException();
                            return mMessageFlags;
                        }

                        set => mMessageFlags = value;
                    }

                    public cMessageFlags ForUpdatePermanentFlags
                    {
                        get
                        {
                            if (!HasBeenSelectedForUpdate) throw new cNeverBeenSelectedException();
                            return mForUpdatePermanentFlags ?? mMessageFlags;
                        }

                        set => mForUpdatePermanentFlags = value;
                    }

                    public cMessageFlags ReadOnlyPermanentFlags
                    {
                        get
                        {
                            if (!HasBeenSelectedReadOnly) throw new cNeverBeenSelectedException();
                            return mReadOnlyPermanentFlags ?? mMessageFlags;
                        }

                        set => mReadOnlyPermanentFlags = value;
                    }

                    public cStatus Status { get; set; }

                    public cMailboxStatus MailboxStatus
                    {
                        get => mMailboxStatus;

                        set
                        {
                            mMailboxStatus = value;

                            if (mMailboxStatusStopwatch == null) mMailboxStatusStopwatch = Stopwatch.StartNew();
                            else mMailboxStatusStopwatch.Restart();
                        }
                    }

                    public long MailboxStatusAge => mMailboxStatusStopwatch?.ElapsedMilliseconds ?? long.MaxValue;
                }
            }
        }
    }
}