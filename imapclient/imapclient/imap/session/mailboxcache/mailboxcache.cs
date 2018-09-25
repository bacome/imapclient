using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache : iMailboxCache
            {
                private readonly cPersistentCache mPersistentCache;
                private readonly cIMAPCallbackSynchroniser mSynchroniser;
                private readonly fMailboxCacheDataItems mMailboxCacheDataItems;
                private readonly cCommandPartFactory mCommandPartFactory;
                private readonly cIMAPCapabilities mCapabilities;
                private readonly cAccountId mAccountId;
                private readonly Action<eIMAPConnectionState, cTrace.cContext> mSetState;
                private readonly ConcurrentDictionary<string, cMailboxCacheItem> mDictionary = new ConcurrentDictionary<string, cMailboxCacheItem>();

                private int mSequence = 7;
                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cPersistentCache pPersistentCache, cIMAPCallbackSynchroniser pSynchroniser, fMailboxCacheDataItems pMailboxCacheDataItems, cCommandPartFactory pCommandPartFactory, cIMAPCapabilities pCapabilities, cAccountId pAccountId, Action<eIMAPConnectionState, cTrace.cContext> pSetState)
                {
                    mPersistentCache = pPersistentCache ?? throw new ArgumentNullException(nameof(pPersistentCache));
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mMailboxCacheDataItems = pMailboxCacheDataItems;
                    mCommandPartFactory = pCommandPartFactory ?? throw new ArgumentNullException(nameof(pCommandPartFactory));
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                    mAccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
                    mSetState = pSetState ?? throw new ArgumentNullException(nameof(pSetState));
                }

                public cAccountId AccountId => mAccountId;

                public iMailboxHandle GetHandle(cMailboxName pMailboxName) => ZItem(pMailboxName);

                public IEnumerable<iMailboxHandle> GetHandles(IEnumerable<cMailboxName> pMailboxNames)
                {
                    if (pMailboxNames == null) return null;
                    return from lMailboxName in pMailboxNames.Distinct() select ZItem(lMailboxName);
                }

                public cMailboxCacheItem CheckHandle(iMailboxHandle pMailboxHandle)
                {
                    if (!(pMailboxHandle is cMailboxCacheItem lItem)) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
                    if (!ReferenceEquals(lItem.MailboxCache, this)) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
                    return lItem;
                }

                public bool HasPendingHighestModSeq()
                {
                    if (mSelectedMailbox == null) return false;
                    if (mSelectedMailbox.MessageCache.NoModSeq) return false;
                    return mSelectedMailbox.HasPendingHighestModSeq();
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
                    if (pFeedback.Items.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));
                    if (mSelectedMailbox == null || !ReferenceEquals(pFeedback.Items[0].MessageHandle.MessageCache, mSelectedMailbox.MessageCache)) throw new InvalidOperationException(kInvalidOperationExceptionMessage.MailboxNotSelected);
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

                public void ResetExists(cMailboxName pMailboxName, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pMailboxName, pSequence);

                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists != false && lItem.MailboxName != null && lItem.MailboxName.IsDescendantOf(pMailboxName))
                            if (lItem.ListFlags == null || lItem.ListFlags.Sequence < pSequence)
                                lItem.ResetExists(lContext);
                }

                public void ResetExists(cMailboxPathPattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pSequence);

                    if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

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

                    if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

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

                    mPersistentCache.MessageCacheDeactivated(mSelectedMailbox.MessageCache, lContext);

                    var lMailboxHandle = mSelectedMailbox.MailboxHandle;

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (mSelectedMailbox.SelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (mSelectedMailbox.AccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mSelectedMailbox = null;

                    mSetState(eIMAPConnectionState.notselected, lContext);
                    mSynchroniser.InvokeMailboxPropertiesChanged(lMailboxHandle, lProperties, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.SelectedMailbox), lContext);

                    mPersistentCache.Close(lMailboxHandle.MailboxId, lContext);
                }

                public void Select(cSelectedMailboxCache pCache, bool pForUpdate, bool pAccessReadOnly, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(Select), pCache, pForUpdate, pAccessReadOnly);

                    if (mSelectedMailbox != null) throw new InvalidOperationException();

                    if (pCache == null) throw new ArgumentNullException(nameof(pCache));

                    mSelectedMailbox = new cSelectedMailbox(pCache, pForUpdate, pAccessReadOnly, lContext);

                    fMailboxProperties lProperties = fMailboxProperties.isselected;
                    if (pForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
                    if (pAccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

                    mSetState(eIMAPConnectionState.selected, lContext);
                    mSynchroniser.InvokeMailboxPropertiesChanged(pCache.MailboxHandle, lProperties, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.SelectedMailbox), lContext);
                }

                public bool HasChildren(iMailboxHandle pMailboxHandle)
                {
                    CheckHandle(pMailboxHandle);

                    if (pMailboxHandle.MailboxName.Delimiter == null) return false;

                    cMailboxPathPattern lPattern = new cMailboxPathPattern(pMailboxHandle.MailboxName.GetDescendantPathPrefix(), cStrings.Empty, "*", pMailboxHandle.MailboxName.Delimiter);

                    foreach (var lItem in mDictionary.Values)
                        if (lItem.Exists == true && lItem.MailboxName != null && lItem.MailboxName.Delimiter == pMailboxHandle.MailboxName.Delimiter && lPattern.Matches(lItem.MailboxName.Path))
                            return true;

                    return false;
                }

                private cMailboxCacheItem ZItem(string pEncodedMailboxPath) => mDictionary.GetOrAdd(pEncodedMailboxPath, new cMailboxCacheItem(mPersistentCache, mSynchroniser, this, pEncodedMailboxPath));

                private cMailboxCacheItem ZItem(cMailboxName pMailboxName)
                {
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (!mCommandPartFactory.TryAsMailbox(pMailboxName.Path, pMailboxName.Delimiter, out var lCommandPart, out var lEncodedMailboxPath)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
                    var lItem = mDictionary.GetOrAdd(lEncodedMailboxPath, new cMailboxCacheItem(mPersistentCache, mSynchroniser, this, lEncodedMailboxPath));
                    lItem.MailboxName = pMailboxName;
                    lItem.MailboxNameCommandPart = lCommandPart;
                    return lItem;
                }
            }
        }
    }
}