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
                private readonly Action<cMailboxId, string, cTrace.cContext> mMailboxPropertyChanged;
                private readonly Dictionary<cMailboxName, cItem> mDictionary = new Dictionary<cMailboxName, cItem>();

                public cMailboxCache(Action<cMailboxId, string, cTrace.cContext> pMailboxPropertyChanged)
                {
                    mMailboxPropertyChanged = pMailboxPropertyChanged ?? throw new ArgumentNullException(nameof(pMailboxPropertyChanged));
                }

                public iMailboxCacheItem Item(cMailboxId pMailboxId) => ZItem(pMailboxId.MailboxName, false);

                public void SetMailboxFlags(cMailboxId pMailboxId, cMailboxFlags pMailboxFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMailboxFlags), pMailboxId, pMailboxFlags);
                    if (pMailboxFlags == null) throw new ArgumentNullException(nameof(pMailboxFlags));
                    var lItem = ZItem(pMailboxId.MailboxName, true);
                    fMailboxCacheItemDifferences lDifferences = cMailboxFlags.Differences(lItem.MailboxFlags, pMailboxFlags);
                    if (lDifferences == 0) return;
                    lItem.MailboxFlags = pMailboxFlags;
                    ZMailboxPropertyChanged(pMailboxId, lDifferences, lContext);
                }

                public void SetSelected(cMailboxId pMailboxId, bool pSelectedForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags, cMailboxStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetSelected), pMailboxId, pSelectedForUpdate, pAccessReadOnly, pMessageFlags, pPermanentFlags, pStatus);

                    var lItem = ZItem(pMailboxId.MailboxName, true);

                    fMailboxCacheItemDifferences lDifferences = 0;

                    if (!lItem.IsSelected)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.isselected;
                        lItem.IsSelected = true;
                    }

                    if (lItem.IsSelectedForUpdate != pSelectedForUpdate)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.isselectedforupdate;
                        lItem.IsSelectedForUpdate = pSelectedForUpdate;
                    }

                    if (lItem.IsAccessReadOnly != pAccessReadOnly)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.isaccessreadonly;
                        lItem.IsAccessReadOnly = pAccessReadOnly;
                    }

                    if (lItem.MessageFlags != pMessageFlags)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.messageflags;
                        lItem.MessageFlags = pMessageFlags;
                    }

                    if (lItem.PermanentFlags != pPermanentFlags)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.permanentflags;
                        lItem.PermanentFlags = pPermanentFlags;
                    }

                    fMailboxCacheItemDifferences lStatusDifferences = cMailboxStatus.Differences(lItem.Status, pStatus);

                    if (lStatusDifferences != 0)
                    {
                        lDifferences |= lStatusDifferences;
                        lItem.Status = pStatus;
                    }

                    ZMailboxPropertyChanged(pMailboxId, lDifferences, lContext);
                }

                public void SetUnselected(cMailboxId pMailboxId, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetUnselected), pMailboxId);

                    var lItem = ZItem(pMailboxId.MailboxName, true);

                    if (!lItem.IsSelected) return;

                    fMailboxCacheItemDifferences lDifferences = fMailboxCacheItemDifferences.isselected;

                    if (lItem.IsSelectedForUpdate != false)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.isselectedforupdate;
                        lItem.IsSelectedForUpdate = false;
                    }

                    if (lItem.IsAccessReadOnly != false)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.isaccessreadonly;
                        lItem.IsAccessReadOnly = false;
                    }

                    ZMailboxPropertyChanged(pMailboxId, lDifferences, lContext);
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

                private cItem ZItem(cMailboxName pMailboxName, bool pAddIfNotThere)
                {
                    cItem lItem;

                    lock (mDictionary)
                    {
                        if (!mDictionary.TryGetValue(pMailboxName, out lItem))
                        {
                            if (pAddIfNotThere)
                            {
                                lItem = new cItem();
                                mDictionary.Add(pMailboxName, lItem);
                            }
                            else lItem = null;
                        }
                    }

                    return lItem;
                }

                private void ZMailboxPropertyChanged(cMailboxId pMailboxId, fMailboxCacheItemDifferences pDifferences, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZMailboxPropertyChanged), pMailboxId, pDifferences);

                    if ((pDifferences & fMailboxCacheItemDifferences.canhavechildren) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.CanHaveChildren), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.haschildren) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasChildren), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.canselect) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.CanSelect), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.ismarked) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsMarked), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.issubscribed) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSubscribed), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.hassubscribedchildren) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasSubscribedChildren), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.islocal) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsLocal), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.containsall) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsAll), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.isarchive) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsArchive), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.containsdrafts) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsDrafts), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.containsflagged) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsFlagged), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.containsjunk) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsJunk), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.containssent) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsSent), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.containstrash) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsTrash), lContext);

                    if ((pDifferences & fMailboxCacheItemDifferences.isselected) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSelected), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.isselectedforupdate) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSelectedForUpdate), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.isaccessreadonly) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsAccessReadOnly), lContext);

                    if ((pDifferences & fMailboxCacheItemDifferences.messageflags) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.MessageFlags), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.permanentflags) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.PermanentFlags), lContext);

                    if ((pDifferences & fMailboxCacheItemDifferences.messagecount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.recentcount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.uidnext) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.newunknownuidcount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.uidvalidity) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.unseencount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.unseenunknowncount) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.highestmodseq) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);
                }

                private class cItem : iMailboxCacheItem
                {
                    private cMessageFlags mPermanentFlags = null;
                    private cMailboxStatus mStatus = null;
                    private Stopwatch mStatusStopwatch = null;

                    public cMailboxFlags MailboxFlags { get; set; } = null;
                    public bool IsSelected { get; set; } = false;
                    public bool IsSelectedForUpdate { get; set; } = false;
                    public bool IsAccessReadOnly { get; set; } = false;
                    public cMessageFlags MessageFlags { get; set; } = null;

                    public cItem() { }

                    public cMessageFlags PermanentFlags
                    {
                        get => mPermanentFlags ?? MessageFlags;
                        set => mPermanentFlags = value;
                    }

                    public cMailboxStatus Status
                    {
                        get => mStatus;

                        set
                        {
                            mStatus = value;

                            if (mStatusStopwatch == null) mStatusStopwatch = Stopwatch.StartNew();
                            else mStatusStopwatch.Restart();
                        }
                    }

                    public long StatusAge => mStatusStopwatch?.ElapsedMilliseconds ?? long.MaxValue;
                }
            }
        }
    }
}