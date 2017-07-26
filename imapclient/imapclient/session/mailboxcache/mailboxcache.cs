using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly cAccountId mConnectedAccountId;
                private readonly cCommandPartFactory mCommandPartFactory;
                private readonly Action<eState, cTrace.cContext> mSetState;
                private readonly ConcurrentDictionary<string, cMailboxCacheItem> mDictionary = new ConcurrentDictionary<string, cMailboxCacheItem>();

                private cCapability mCapability;

                private int mSequence = 7;
                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cEventSynchroniser pEventSynchroniser, cAccountId pConnectedAccountId, cCommandPartFactory pCommandPartFactory, Action<eState, cTrace.cContext> pSetState, cCapability pCapability)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mConnectedAccountId = pConnectedAccountId ?? throw new ArgumentNullException(nameof(pConnectedAccountId));
                    mCommandPartFactory = pCommandPartFactory ?? throw new ArgumentNullException(nameof(pCommandPartFactory));
                    mSetState = pSetState ?? throw new ArgumentNullException(nameof(pSetState));
                    mCapability = pCapability ?? throw new ArgumentNullException(nameof(pCapability));
                }

                public void SetCapability(cCapability pCapability, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetCapability), pCapability);
                    mCapability = pCapability;
                }

                public iMailboxHandle GetHandle(cMailboxName pMailboxName) => ZItem(pMailboxName);

                public cMailboxCacheItem CheckHandle(iMailboxHandle pHandle)
                {
                    if (!ReferenceEquals(pHandle.Cache, this)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!mDictionary.TryGetValue(pHandle.EncodedMailboxName, out var lItem)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!ReferenceEquals(lItem, pHandle)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    return lItem;
                }

                public cSelectedMailbox SelectedMailbox => mSelectedMailbox;

                public cSelectedMailbox CheckIsSelectedMailbox(iMailboxHandle pHandle)
                {
                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandle, mSelectedMailbox.Handle)) throw new InvalidOperationException();
                    return mSelectedMailbox;
                }

                public cSelectedMailbox CheckInSelectedMailbox(iMessageHandle pHandle)
                {
                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandle.Cache, mSelectedMailbox.MessageCache)) throw new InvalidOperationException();
                    return mSelectedMailbox;
                }

                public cSelectedMailbox CheckInSelectedMailbox(cMessageHandleList pHandles)
                {
                    if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
                    if (pHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pHandles));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandles[0].Cache, mSelectedMailbox.MessageCache)) throw new InvalidOperationException();
                    return mSelectedMailbox;
                }

                public int Sequence => mSequence;

                public List<iMailboxHandle> List(cMailboxNamePattern pPattern, bool pStatus, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(List), pPattern, pSequence);

                    cMailboxCacheItem lSelectedMailboxCacheItem;

                    if (pStatus) lSelectedMailboxCacheItem = mSelectedMailbox?.MailboxCacheItem;
                    else lSelectedMailboxCacheItem = null;

                    List<iMailboxHandle> lHandles = new List<iMailboxHandle>();

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists != false && lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Name))
                        {
                            if (lItem.ListFlags == null || lItem.ListFlags.Sequence < pSequence) lItem.ResetExists(lContext);
                            else
                            {
                                lHandles.Add(lItem);

                                if (pStatus && lItem.Status != null && lItem.Status.Sequence < pSequence)
                                {
                                    lItem.ClearStatus(lContext);
                                    if (!ReferenceEquals(lSelectedMailboxCacheItem, lItem)) lItem.UpdateMailboxStatus(lContext);
                                }
                            }
                        }

                    return lHandles;
                }

                public List<iMailboxHandle> LSub(cMailboxNamePattern pPattern, bool pStatus, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(LSub), pPattern, pSequence);

                    cMailboxCacheItem lSelectedMailboxCacheItem;

                    if (pStatus) lSelectedMailboxCacheItem = mSelectedMailbox?.MailboxCacheItem;
                    else lSelectedMailboxCacheItem = null;

                    List<iMailboxHandle> lHandles = new List<iMailboxHandle>();

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.LSubFlags != null && lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Name))
                        {
                            if (lItem.LSubFlags.Sequence < pSequence) lItem.SetFlags(null, lContext);
                            else
                            {
                                lHandles.Add(lItem);

                                if (pStatus && lItem.Status != null && lItem.Status.Sequence < pSequence)
                                {
                                    lItem.ClearStatus(lContext);
                                    if (!ReferenceEquals(lSelectedMailboxCacheItem, lItem)) lItem.UpdateMailboxStatus(lContext);
                                }
                            }
                        }

                    return lHandles;
                }

                public void Deselect(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Deselect));

                    if (mSelectedMailbox == null) return;

                    var lHandle = mSelectedMailbox.Handle;

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (mSelectedMailbox.SelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (mSelectedMailbox.AccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mSelectedMailbox = null;

                    mEventSynchroniser.FireMailboxPropertiesChanged(lHandle, lProperties, lContext);
                    mSetState(eState.notselected, lContext);
                }

                public void Select(cMailboxCacheItem pItem, bool pSelectedForUpdate, bool pAccessReadOnly, cMessageFlags pFlags, cMessageFlags pPermanentFlags, int pExists, int pRecent, uint pUIDNext, uint pUIDValidity, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Select), pItem, pSelectedForUpdate, pAccessReadOnly, pFlags, pPermanentFlags, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq);

                    if (mSelectedMailbox != null) throw new InvalidOperationException();

                    if (pItem == null) throw new ArgumentNullException(nameof(pItem));
                    if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                    pItem.SetSelectedProperties(pFlags, pSelectedForUpdate, pPermanentFlags, lContext);

                    mSelectedMailbox = new cSelectedMailbox(mEventSynchroniser, pItem, pSelectedForUpdate, pAccessReadOnly, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq, lContext);

                    mSetState(eState.selected, lContext);

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (pSelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (pAccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mEventSynchroniser.FireMailboxPropertiesChanged(pItem, lProperties, lContext);
                }

                private cMailboxCacheItem ZItem(string pEncodedMailboxName) => mDictionary.GetOrAdd(pEncodedMailboxName, new cMailboxCacheItem(mEventSynchroniser, this, pEncodedMailboxName));

                private cMailboxCacheItem ZItem(cMailboxName pMailboxName)
                {
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (!mCommandPartFactory.TryAsMailbox(pMailboxName, out var lCommandPart, out var lEncodedMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
                    var lItem = mDictionary.GetOrAdd(lEncodedMailboxName, new cMailboxCacheItem(mEventSynchroniser, this, lEncodedMailboxName));
                    lItem.MailboxName = pMailboxName;
                    lItem.CommandPart = lCommandPart;
                    return lItem;
                }
            }
        }
    }
}