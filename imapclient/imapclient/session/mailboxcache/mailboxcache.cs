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


                private int mSequence = 7;
                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cEventSynchroniser pEventSynchroniser, cAccountId pConnectedAccountId, cCommandPartFactory pCommandPartFactory, Action<eState, cTrace.cContext> pSetState)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mConnectedAccountId = pConnectedAccountId ?? throw new ArgumentNullException(nameof(pConnectedAccountId));
                    mCommandPartFactory = pCommandPartFactory ?? throw new ArgumentNullException(nameof(pCommandPartFactory));
                    mSetState = pSetState ?? throw new ArgumentNullException(nameof(pSetState));
                }

                public iMailboxHandle GetHandle(cMailboxName pMailboxName) => ZItem(pMailboxName);

                public void CheckHandle(iMailboxHandle pHandle)
                {
                    if (!ReferenceEquals(pHandle.Cache, this)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!mDictionary.TryGetValue(pHandle.EncodedMailboxName, out var lItem)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!ReferenceEquals(lItem, pHandle)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                }

                public void CommandCompletion(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(CommandCompletion));
                    if (mSelectedMailbox == null) return;
                    if (mSelectedMailbox.NoModSeq) return;
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

                public List<iMailboxHandle> List(cMailboxNamePattern pPattern, bool pStatus, int pSequence, cTrace.cContext pParentContext)
                {
                    ;?; // shoul dnot return anything
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(List), pPattern, pSequence);

                    iMailboxHandle lSelectedMailboxHandle;

                    if (pStatus) lSelectedMailboxHandle = mSelectedMailbox?.Handle;
                    else lSelectedMailboxHandle = null;

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
                                    if (!ReferenceEquals(lSelectedMailboxHandle, lItem)) lItem.UpdateMailboxStatus(lContext);
                                }
                            }
                        }

                    return lHandles;
                }

                public List<iMailboxHandle> LSub(cMailboxNamePattern pPattern, bool pStatus, int pSequence, cTrace.cContext pParentContext)
                {
                    ;?; // shoul dnot return anything
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(LSub), pPattern, pSequence);

                    iMailboxHandle lSelectedMailboxHandle;

                    if (pStatus) lSelectedMailboxHandle = mSelectedMailbox?.Handle;
                    else lSelectedMailboxHandle = null;

                    List<iMailboxHandle> lHandles = new List<iMailboxHandle>();

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Name))
                        {
                            if (lItem.LSubFlags.Sequence < pSequence) lItem.SetFlags(null, lContext);
                            else
                            {
                                lHandles.Add(lItem);

                                if (pStatus && lItem.Status != null && lItem.Status.Sequence < pSequence)
                                {
                                    lItem.ClearStatus(lContext);
                                    if (!ReferenceEquals(lSelectedMailboxHandle, lItem)) lItem.UpdateMailboxStatus(lContext);
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

                    if (!ReferenceEquals(pHandle.Cache, this)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!mDictionary.TryGetValue(pHandle.EncodedMailboxName, out var lItem)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!ReferenceEquals(lItem, pHandle)) throw new ArgumentOutOfRangeException(nameof(pHandle));

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