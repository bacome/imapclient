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
                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly fMailboxCacheData mMailboxCacheData;
                private readonly cCommandPartFactory mCommandPartFactory;
                private readonly cCapabilities mCapabilities;
                private readonly Action<eConnectionState, cTrace.cContext> mSetState;
                private readonly ConcurrentDictionary<string, cMailboxCacheItem> mDictionary = new ConcurrentDictionary<string, cMailboxCacheItem>();

                private int mSequence = 7;
                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cCallbackSynchroniser pSynchroniser, fMailboxCacheData pMailboxCacheData, cCommandPartFactory pCommandPartFactory, cCapabilities pCapabilities, Action<eConnectionState, cTrace.cContext> pSetState)
                {
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mMailboxCacheData = pMailboxCacheData;
                    mCommandPartFactory = pCommandPartFactory ?? throw new ArgumentNullException(nameof(pCommandPartFactory));
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
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

                public cSelectedMailbox CheckIsSelectedMailbox(iMailboxHandle pHandle, uint? pUIDValidity)
                {
                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandle, mSelectedMailbox.Handle)) throw new InvalidOperationException();
                    if (pUIDValidity != null && pUIDValidity != mSelectedMailbox.Cache.UIDValidity) throw new cUIDValidityChangedException();
                    return mSelectedMailbox;
                }

                public cSelectedMailbox CheckInSelectedMailbox(iMessageHandle pHandle)
                {
                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandle.Cache, mSelectedMailbox.Cache)) throw new InvalidOperationException();
                    return mSelectedMailbox;
                }

                public cSelectedMailbox CheckInSelectedMailbox(cMessageHandleList pHandles)
                {
                    if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
                    if (pHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pHandles));
                    if (mSelectedMailbox == null || !ReferenceEquals(pHandles[0].Cache, mSelectedMailbox.Cache)) throw new InvalidOperationException();
                    return mSelectedMailbox;
                }

                public int Sequence => mSequence++;

                public iMailboxHandle Create(cMailboxName pMailboxName, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Create), pMailboxName);
                    var lItem = ZItem(pMailboxName);
                    lItem.SetJustCreated(lContext);
                    return lItem;
                }

                public void ResetExists(cMailboxPathPattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pSequence);

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists != false && lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Path))
                            if (lItem.ListFlags == null || lItem.ListFlags.Sequence < pSequence)
                                lItem.ResetExists(lContext);
                }

                public void ResetLSubFlags(cMailboxPathPattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    // called after an LSub with subscribed = true
                    //  called after a list-extended/subscribed with subscribed = true

                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetLSubFlags), pPattern, pSequence);

                    cLSubFlags lNotSubscribed = new cLSubFlags(mSequence++, false);

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Path))
                            if (lItem.LSubFlags == null || lItem.LSubFlags.Sequence < pSequence)
                                lItem.SetLSubFlags(lNotSubscribed, lContext);
                }

                public void ResetStatus(cMailboxPathPattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetStatus), pPattern, pSequence);

                    iMailboxHandle lSelectedMailboxHandle = mSelectedMailbox?.Handle;

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists != false && lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Path))
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

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (mSelectedMailbox.SelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (mSelectedMailbox.AccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mSelectedMailbox = null;

                    mSetState(eConnectionState.notselected, lContext);
                    mSynchroniser.InvokeMailboxPropertiesChanged(lHandle, lProperties, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.SelectedMailbox), lContext);
                }

                public void Select(iMailboxHandle pHandle, bool pForUpdate, bool pAccessReadOnly, cFetchableFlags pFlags, cPermanentFlags pPermanentFlags, int pExists, int pRecent, uint pUIDNext, uint pUIDValidity, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Select), pHandle, pForUpdate, pAccessReadOnly, pFlags, pPermanentFlags, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq);

                    if (mSelectedMailbox != null) throw new InvalidOperationException();

                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                    var lItem = CheckHandle(pHandle);

                    if (pExists < 0) throw new ArgumentOutOfRangeException(nameof(pExists));
                    if (pRecent < 0) throw new ArgumentOutOfRangeException(nameof(pRecent));

                    mSelectedMailbox = new cSelectedMailbox(mSynchroniser, lItem, pForUpdate, pAccessReadOnly, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq, lContext);

                    lItem.SetSelectedProperties(pFlags, pForUpdate, pPermanentFlags, lContext);

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (pForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (pAccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mSetState(eConnectionState.selected, lContext);
                    mSynchroniser.InvokeMailboxPropertiesChanged(pHandle, lProperties, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.SelectedMailbox), lContext);
                }

                public bool HasChildren(iMailboxHandle pHandle)
                {
                    CheckHandle(pHandle);

                    if (pHandle.MailboxName.Delimiter == null) return false;

                    cMailboxPathPattern lPattern = new cMailboxPathPattern(pHandle.MailboxName.Path + pHandle.MailboxName.Delimiter, "*", pHandle.MailboxName.Delimiter);

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists == true && lItem.MailboxName != null && lPattern.Matches(lItem.MailboxName.Path))
                            return true;

                    return false;
                }

                private cMailboxCacheItem ZItem(string pEncodedMailboxPath) => mDictionary.GetOrAdd(pEncodedMailboxPath, new cMailboxCacheItem(mSynchroniser, this, pEncodedMailboxPath));

                private cMailboxCacheItem ZItem(cMailboxName pMailboxName)
                {
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (!mCommandPartFactory.TryAsMailbox(pMailboxName.Path, pMailboxName.Delimiter, out var lCommandPart, out var lEncodedMailboxPath)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
                    var lItem = mDictionary.GetOrAdd(lEncodedMailboxPath, new cMailboxCacheItem(mSynchroniser, this, lEncodedMailboxPath));
                    lItem.MailboxName = pMailboxName;
                    lItem.MailboxNameCommandPart = lCommandPart;
                    return lItem;
                }
            }
        }
    }
}