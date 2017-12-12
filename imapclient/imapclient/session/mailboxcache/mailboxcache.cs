using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache
            {
                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly fMailboxCacheDataItems mMailboxCacheDataItems;
                private readonly cCommandPartFactory mCommandPartFactory;
                private readonly cCapabilities mCapabilities;
                private readonly Action<eConnectionState, cTrace.cContext> mSetState;
                private readonly ConcurrentDictionary<string, cMailboxCacheItem> mDictionary = new ConcurrentDictionary<string, cMailboxCacheItem>();

                private int mSequence = 7;
                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cCallbackSynchroniser pSynchroniser, fMailboxCacheDataItems pMailboxCacheDataItems, cCommandPartFactory pCommandPartFactory, cCapabilities pCapabilities, Action<eConnectionState, cTrace.cContext> pSetState)
                {
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mMailboxCacheDataItems = pMailboxCacheDataItems;
                    mCommandPartFactory = pCommandPartFactory ?? throw new ArgumentNullException(nameof(pCommandPartFactory));
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                    mSetState = pSetState ?? throw new ArgumentNullException(nameof(pSetState));
                }

                public iMailboxHandle GetHandle(cMailboxName pMailboxName) => ZItem(pMailboxName);

                public List<iMailboxHandle> GetHandles(List<cMailboxName> pMailboxNames)
                {
                    if (pMailboxNames == null) return null;

                    List<iMailboxHandle> lMailboxHandles = new List<iMailboxHandle>();

                    pMailboxNames.Sort();

                    cMailboxName lLastMailboxName = null;

                    foreach (var lMailboxName in pMailboxNames)
                        if (lMailboxName != lLastMailboxName)
                        {
                            lMailboxHandles.Add(ZItem(lMailboxName));
                            lLastMailboxName = lMailboxName;
                        }

                    return lMailboxHandles;
                }

                public cMailboxCacheItem CheckHandle(iMailboxHandle pMailboxHandle)
                {
                    if (!(pMailboxHandle is cMailboxCacheItem lItem)) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
                    if (!ReferenceEquals(lItem.MailboxCache, this)) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
                    return lItem;
                }

                public void CommandCompletion(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(CommandCompletion));
                    if (mSelectedMailbox == null) return;
                    if (mSelectedMailbox.MessageCache.NoModSeq) return;
                    mSelectedMailbox.UpdateHighestModSeq(lContext);
                }

                public iSelectedMailboxDetails SelectedMailboxDetails => mSelectedMailbox;

                public cSelectedMailbox CheckIsSelectedMailbox(iMailboxHandle pMailboxHandle, uint? pUIDValidity)
                {
                    if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pMailboxHandle, mSelectedMailbox.MailboxHandle)) throw new InvalidOperationException(kInvalidOperationExceptionMessage.MailboxNotSelected);
                    if (pUIDValidity != null && pUIDValidity != mSelectedMailbox.MessageCache.UIDValidity) throw new cUIDValidityException();
                    return mSelectedMailbox;
                }

                public cSelectedMailbox CheckInSelectedMailbox(iMessageHandle pMessageHandle)
                {
                    if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
                    if (mSelectedMailbox == null || !ReferenceEquals(pMessageHandle.MessageCache, mSelectedMailbox.MessageCache)) throw new InvalidOperationException(kInvalidOperationExceptionMessage.MailboxNotSelected);
                    if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                    return mSelectedMailbox;
                }

                public cSelectedMailbox CheckInSelectedMailbox(cMessageHandleList pMessageHandles)
                {
                    if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
                    if (pMessageHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pMessageHandles));
                    if (mSelectedMailbox == null || !ReferenceEquals(pMessageHandles[0].MessageCache, mSelectedMailbox.MessageCache)) throw new InvalidOperationException(kInvalidOperationExceptionMessage.MailboxNotSelected);
                    return mSelectedMailbox;
                }

                public cSelectedMailbox CheckInSelectedMailbox(cStoreFeedback pFeedback)
                {
                    if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
                    if (pFeedback.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));
                    if (mSelectedMailbox == null || !ReferenceEquals(pFeedback[0].MessageHandle.MessageCache, mSelectedMailbox.MessageCache)) throw new InvalidOperationException(kInvalidOperationExceptionMessage.MailboxNotSelected);
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

                    iMailboxHandle lSelectedMailboxHandle = mSelectedMailbox?.MailboxHandle;

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists != false && lItem.MailboxName != null && pPattern.Matches(lItem.MailboxName.Path))
                            if (lItem.Status != null && lItem.Status.Sequence < pSequence)
                            {
                                lItem.ClearStatus(lContext);
                                if (!ReferenceEquals(lSelectedMailboxHandle, lItem)) lItem.UpdateMailboxStatus(lContext);
                            }
                }

                public void Unselect(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Unselect));

                    if (mSelectedMailbox == null) return;

                    var lMailboxHandle = mSelectedMailbox.MailboxHandle;

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (mSelectedMailbox.SelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (mSelectedMailbox.AccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mSelectedMailbox = null;

                    mSetState(eConnectionState.notselected, lContext);
                    mSynchroniser.InvokeMailboxPropertiesChanged(lMailboxHandle, lProperties, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.SelectedMailbox), lContext);
                }

                public void Select(iMailboxHandle pMailboxHandle, bool pForUpdate, bool pAccessReadOnly, bool pUIDNotSticky, cFetchableFlags pFlags, cPermanentFlags pPermanentFlags, int pExists, int pRecent, uint pUIDNext, uint pUIDValidity, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Select), pMailboxHandle, pForUpdate, pAccessReadOnly, pUIDNotSticky, pFlags, pPermanentFlags, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq);

                    if (mSelectedMailbox != null) throw new InvalidOperationException();

                    if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                    if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                    var lItem = CheckHandle(pMailboxHandle);

                    if (pExists < 0) throw new ArgumentOutOfRangeException(nameof(pExists));
                    if (pRecent < 0) throw new ArgumentOutOfRangeException(nameof(pRecent));

                    mSelectedMailbox = new cSelectedMailbox(mSynchroniser, lItem, pForUpdate, pAccessReadOnly, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq, lContext);

                    lItem.SetSelectedProperties(pUIDNotSticky, pFlags, pForUpdate, pPermanentFlags, lContext);

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (pForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (pAccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mSetState(eConnectionState.selected, lContext);
                    mSynchroniser.InvokeMailboxPropertiesChanged(pMailboxHandle, lProperties, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.SelectedMailbox), lContext);
                }

                public bool HasChildren(iMailboxHandle pMailboxHandle)
                {
                    CheckHandle(pMailboxHandle);

                    if (pMailboxHandle.MailboxName.Delimiter == null) return false;

                    cMailboxPathPattern lPattern = new cMailboxPathPattern(pMailboxHandle.MailboxName.Path + pMailboxHandle.MailboxName.Delimiter, "*", pMailboxHandle.MailboxName.Delimiter);

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