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

                public void SetSelected(cMailboxId pMailboxId, bool pSelectedForUpdate, bool pAccessReadOnly, cMessageFlags pFlags, cMessageFlags pPermanentFlags, cMailboxStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetSelected), pMailboxId, pSelectedForUpdate, pAccessReadOnly, pFlags, pPermanentFlags, pStatus);

                    var lItem = ZItem(pMailboxId.MailboxName, true);

                    fMailboxCacheItemDifferences lDifferences = 0;

                    if (!lItem.Selected)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.selected;
                        lItem.Selected = true;
                    }

                    if (lItem.SelectedForUpdate != pSelectedForUpdate)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.selectedforupdate;
                        lItem.SelectedForUpdate = pSelectedForUpdate;
                    }

                    if (lItem.AccessReadOnly != pAccessReadOnly)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.accessreadonly;
                        lItem.AccessReadOnly = pAccessReadOnly;
                    }

                    if (lItem.Flags != pFlags)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.flags;
                        lItem.Flags = pFlags;
                    }

                    if (lItem.PermanentFlags != pPermanentFlags)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.permanentflags;
                        lItem.PermanentFlags = pPermanentFlags;
                    }

                    fMailboxCacheItemDifferences lStatusDifferences = cMailboxStatus.Differences(lItem.Status, pStatus);

                    if (lStatusDifferences != 0)
                    {
                        lDifferences |= fMailboxCacheItemDifferences.status | lStatusDifferences;
                        lItem.Status = pStatus;
                    }

                    ZPropertyChanges(lDifferences);
                }

                public void SetUnselected(cMailboxId pMailboxId, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetUnselected), pMailboxId);

                    var lItem = ZItem(pMailboxId.MailboxName, true);

                    if (!lItem.Selected) return;

                    fChanged lChanged = fChanged.selected;

                    if (lItem.SelectedForUpdate != pSelectedForUpdate)
                    {
                        lChanged |= fChanged.selectedforupdate;
                        lItem.SelectedForUpdate = pSelectedForUpdate;
                    }

                    if (lItem.AccessReadOnly != pAccessReadOnly)
                    {
                        lChanged |= fChanged.accessreadonly;
                        lItem.AccessReadOnly = pAccessReadOnly;
                    }





                    ;?;


                }






                public void SetFlags(cMailboxId pMailboxId, cMessageFlags pFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetFlags), pMailboxId, pFlags);
                    if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
                    var lItem = ZItem(pMailboxId.MailboxName, true);
                    if (pFlags == lItem.Flags) return;
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







                public bool SelectedForUpdate(cMailboxId pMailboxId) => ZItem(pMailboxId.MailboxName, false)?.SelectedForUpdate ?? false;






                public bool SelectedAccessReadOnly(cMailboxId pMailboxId) => ZItem(pMailboxId.MailboxName, false)?.SelectedForUpdate ?? false;





                public void Status(cMailboxId pMailboxId, cMailboxStatus)
                {
                    var lItem = ZItem(pMailboxId.MailboxName, false);
                    


                    => ZItem(pMailboxId.MailboxName, false)?.Status;
                }







                public void SetStatus(cMailboxName pMailboxName, cMailboxStatus pStatus)
                {
                    ;?; // must analyse which properties have changed for the events


                    ;?;
                }







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

                private void ZPropertyChanges(cMailboxId pMailboxId, fMailboxCacheItemDifferences pDifferences, )
                {
                    if ((pDifferences & fMailboxCacheItemDifferences.selected) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Selected), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.selectedforupdate) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.SelectedForUpdate), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.accessreadonly) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.AccessReadOnly), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.flags) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Flags), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.permanentflags) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.PermanentFlags), lContext);
                    if ((pDifferences & fMailboxCacheItemDifferences.status) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Status), lContext);

                    ;?;
                    if ((lChanged & fChanged.selected) != 0) mMailboxPropertyChanged(pMailboxId, nameof(cMailbox.Selected), lContext);


                }

                private class cItem : iMailboxCacheItem
                {
                    private cMessageFlags mPermanentFlags = null;
                    private cMailboxStatus mStatus = null;
                    private Stopwatch mStatusStopwatch = null;

                    public bool Selected { get; set; } = false;
                    public bool SelectedForUpdate { get; set; } = false;
                    public bool AccessReadOnly { get; set; } = false;
                    public cMessageFlags Flags { get; set; } = null;

                    public cItem() { }

                    public cMessageFlags PermanentFlags
                    {
                        get => mPermanentFlags ?? Flags;
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