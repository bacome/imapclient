﻿using System;
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
                private readonly fMailboxCacheData mMailboxCacheData;
                private readonly cAccountId mConnectedAccountId;
                private readonly cCommandPartFactory mCommandPartFactory;
                private readonly cCapability mCapability;
                private readonly Action<eState, cTrace.cContext> mSetState;
                private readonly ConcurrentDictionary<string, cMailboxCacheItem> mDictionary = new ConcurrentDictionary<string, cMailboxCacheItem>();

                private int mSequence = 7;
                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cEventSynchroniser pEventSynchroniser, fMailboxCacheData pMailboxCacheData, cAccountId pConnectedAccountId, cCommandPartFactory pCommandPartFactory, cCapability pCapability, Action<eState, cTrace.cContext> pSetState)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mMailboxCacheData = pMailboxCacheData;
                    mConnectedAccountId = pConnectedAccountId ?? throw new ArgumentNullException(nameof(pConnectedAccountId));
                    mCommandPartFactory = pCommandPartFactory ?? throw new ArgumentNullException(nameof(pCommandPartFactory));
                    mCapability = pCapability ?? throw new ArgumentNullException(nameof(pCapability));
                    mSetState = pSetState ?? throw new ArgumentNullException(nameof(pSetState));
                }

                public iMailboxHandle GetHandle(cMailboxName pMailboxName) => ZItem(pMailboxName);

                public List<iMailboxHandle> GetHandles(List<cMailboxName> pMailboxNames)
                {
                    if (pMailboxNames == null) return null;

                    List<iMailboxHandle> lHandles = new List<iMailboxHandle>();

                    pMailboxNames.Sort();

                    cMailboxName lLastMailboxName = null;

                    foreach (var lMailboxName in pMailboxNames)
                        if (lMailboxName != lLastMailboxName)
                        {
                            lHandles.Add(ZItem(lMailboxName));
                            lLastMailboxName = lMailboxName;
                        }

                    return lHandles;
                }

                public cMailboxCacheItem CheckHandle(iMailboxHandle pHandle)
                {
                    if (!(pHandle is cMailboxCacheItem lItem)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!ReferenceEquals(lItem.MailboxCache, this)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    return lItem;
                }

                public void CommandCompletion(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(CommandCompletion));
                    if (mSelectedMailbox == null) return;
                    if (mSelectedMailbox.Cache.NoModSeq) return;
                    mSelectedMailbox.UpdateHighestModSeq(lContext);
                }

                public iSelectedMailboxDetails SelectedMailboxDetails => mSelectedMailbox;

                public void CheckIsSelectedMailbox(iMailboxHandle pHandle)
                {
                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandle, mSelectedMailbox.Handle)) throw new InvalidOperationException();
                }

                public void CheckInSelectedMailbox(iMessageHandle pHandle)
                {
                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandle.Cache, mSelectedMailbox.Cache)) throw new InvalidOperationException();
                }

                public void CheckInSelectedMailbox(cMessageHandleList pHandles)
                {
                    if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
                    if (pHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pHandles));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandles[0].Cache, mSelectedMailbox.Cache)) throw new InvalidOperationException();
                }

                public int Sequence => mSequence;

                public void ResetListFlags(cMailboxNamePattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetListFlags), pPattern, pSequence);

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists != false && lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Name))
                            if (lItem.ListFlags == null || lItem.ListFlags.Sequence < pSequence)
                                lItem.ResetExists(lContext);
                }

                public void ResetLSubFlags(cMailboxNamePattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    // called after an LSub with subscribed = true
                    //  called after a list-extended/subscribed with subscribed = true

                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetLSubFlags), pPattern, pSequence);

                    cLSubFlags lNotSubscribed = new cLSubFlags(mSequence++, false);

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Name))
                            if (lItem.LSubFlags == null || lItem.LSubFlags.Sequence < pSequence)
                                lItem.SetLSubFlags(lNotSubscribed, lContext);
                }

                public void ResetStatus(cMailboxNamePattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetStatus), pPattern, pSequence);

                    iMailboxHandle lSelectedMailboxHandle = mSelectedMailbox?.Handle;

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists != false && lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Name))
                            if (lItem.Status != null && lItem.Status.Sequence < pSequence)
                            {
                                lItem.ClearStatus(lContext);
                                if (!ReferenceEquals(lSelectedMailboxHandle, lItem)) lItem.UpdateMailboxStatus(lContext);
                            }
                }

                public void Deselect(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Deselect));

                    if (mSelectedMailbox == null) return;

                    var lHandle = mSelectedMailbox.Handle;

                    mSelectedMailbox = null;

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (mSelectedMailbox.SelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (mSelectedMailbox.AccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mEventSynchroniser.FireMailboxPropertiesChanged(lHandle, lProperties, lContext);

                    mSetState(eState.notselected, lContext);
                }

                public void Select(iMailboxHandle pHandle, bool pForUpdate, bool pAccessReadOnly, cMessageFlags pFlags, cMessageFlags pPermanentFlags, int pExists, int pRecent, uint pUIDNext, uint pUIDValidity, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Select), pHandle, pForUpdate, pAccessReadOnly, pFlags, pPermanentFlags, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq);

                    if (mSelectedMailbox != null) throw new InvalidOperationException();

                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                    var lItem = CheckHandle(pHandle);

                    if (pExists < 0) throw new ArgumentOutOfRangeException(nameof(pExists));
                    if (pRecent < 0) throw new ArgumentOutOfRangeException(nameof(pRecent));

                    mSelectedMailbox = new cSelectedMailbox(mEventSynchroniser, lItem, pForUpdate, pAccessReadOnly, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq, lContext);

                    lItem.SetSelectedProperties(pFlags, pForUpdate, pPermanentFlags, lContext);

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (pForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (pAccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mEventSynchroniser.FireMailboxPropertiesChanged(pHandle, lProperties, lContext);

                    mSetState(eState.selected, lContext);
                }

                public bool HasChildren(iMailboxHandle pHandle)
                {
                    CheckHandle(pHandle);

                    if (pHandle.MailboxName.Delimiter == null) return false;

                    cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name + pHandle.MailboxName.Delimiter, "*", pHandle.MailboxName.Delimiter);

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists == true && lItem.MailboxName != null && lPattern.Matches(lItem.MailboxName.Name))
                            return true;

                    return false;
                }

                private cMailboxCacheItem ZItem(string pEncodedMailboxName) => mDictionary.GetOrAdd(pEncodedMailboxName, new cMailboxCacheItem(mEventSynchroniser, this, pEncodedMailboxName));

                private cMailboxCacheItem ZItem(cMailboxName pMailboxName)
                {
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (!mCommandPartFactory.TryAsMailbox(pMailboxName, out var lCommandPart, out var lEncodedMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
                    var lItem = mDictionary.GetOrAdd(lEncodedMailboxName, new cMailboxCacheItem(mEventSynchroniser, this, lEncodedMailboxName));
                    lItem.MailboxName = pMailboxName;
                    lItem.MailboxNameCommandPart = lCommandPart;
                    return lItem;
                }
            }
        }
    }
}