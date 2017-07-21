using System;
using System.Collections.Concurrent;
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


                ;?; // don't forget to call this from the dataprocessor of the pipeline
                



                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly cAccountId mConnectedAccountId;
                private readonly bool mUTF8Enabled;
                private readonly cCommandPart.cFactory mFactory;
                private readonly Action<eState, cTrace.cContext> mSetState;
                private cCapability mCapability;

                private readonly ConcurrentDictionary<string, cItem> mDictionary = new ConcurrentDictionary<string, cItem>();

                private int mSequence = 7;
                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cEventSynchroniser pEventSynchroniser, cAccountId pConnectedAccountId, bool pUTF8Enabled, Action<eState, cTrace.cContext> pSetState, cCapability pCapability)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mConnectedAccountId = pConnectedAccountId ?? throw new ArgumentNullException(nameof(pConnectedAccountId));
                    mUTF8Enabled = pUTF8Enabled;
                    mFactory = new cCommandPart.cFactory(mUTF8Enabled);
                    mSetState = pSetState ?? throw new ArgumentNullException(nameof(pSetState));
                    mCapability = pCapability ?? throw new ArgumentNullException(nameof(pCapability));
                }

                public void SetCapability(cCapability pCapability, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetCapability), pCapability);
                    mCapability = pCapability;
                }

                public iMailboxHandle GetHandle(cMailboxName pMailboxName) => ZItem(pMailboxName);

                public void CheckHandle(iMailboxHandle pHandle)
                {
                    if (!ReferenceEquals(pHandle.Cache, this)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!mDictionary.TryGetValue(pHandle.EncodedMailboxName, out var lItem)) throw new ArgumentOutOfRangeException(nameof(pHandle));
                    if (!ReferenceEquals(lItem, pHandle)) throw new ArgumentOutOfRangeException(nameof(pHandle));
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

                public void ResetExists(cMailboxNamePattern pPattern, int pSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pSequence);
                    foreach (var lItem in mDictionary.Values) if (lItem.Exists != false && lItem.MailboxName != null && lItem.Sequence < pSequence && pPattern.Matches(lItem.MailboxName.Name)) lItem.ResetExists(lContext);
                }

                public void ResetExists(string pEncodedMailboxName, int pMailboxStatusSequence, cTrace.cContext pParentContext)
                {


                    // this must not be called for the selected mailbox

                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pEncodedMailboxName, pMailboxStatusSequence);

                    if (mDictionary.TryGetValue(pEncodedMailboxName, out var lItem))
                    {
                        var lProperties = lItem.ResetExists(pMailboxStatusSequence);
                        if (lProperties != 0) mEventSynchroniser.FireMailboxPropertiesChanged(lItem, lProperties, lContext);
                    }
                }

                // these now done direct from the processing of the responses
                /*
                public void SetMailboxFlags(string pEncodedMailboxName, cMailboxName pMailboxName, cMailboxFlags pMailboxFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMailboxFlags), pEncodedMailboxName, pMailboxName, pMailboxFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMailboxFlags == null) throw new ArgumentNullException(nameof(pMailboxFlags));

                    var lProperties = ZItem(pEncodedMailboxName, pMailboxName).SetMailboxFlags(pMailboxFlags);
                    if (lProperties != 0) ZMailboxPropertiesChanged(pMailboxName, lProperties, lContext);
                }

                public void SetLSubFlags(string pEncodedMailboxName, cLSubFlags pLSubFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetLSubFlags), pEncodedMailboxName, pLSubFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pLSubFlags == null) throw new ArgumentNullException(nameof(pLSubFlags));

                    var lItem = ZItem(pEncodedMailboxName, null);
                    var lProperties = lItem.SetLSubFlags(pLSubFlags);
                    if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                } */

                public void ClearLSubFlags(cMailboxNamePattern pPattern, int pLSubFlagsSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pLSubFlagsSequence);

                    foreach (var lItem in mDictionary.Values)
                    {
                        var lProperties = lItem.ClearLSubFlags(pPattern, pLSubFlagsSequence);
                        if (lProperties != 0) mEventSynchroniser.FireMailboxPropertiesChanged(pHandle, lProperties, lContext);
                    }
                }

                /*
                public void UpdateMailboxStatus(string pEncodedMailboxName, cStatus pStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxStatus), pEncodedMailboxName, pStatus);

                    var lItem = ZItem(pEncodedMailboxName, null);
                    cStatus lStatus = cStatus.Combine(lItem.Status, pStatus);
                    lItem.Status = lStatus;

                    if (SelectedMailbox?.EncodedMailboxName == pEncodedMailboxName) return; // the status is currently coming from the selected mailbox

                    cMailboxStatus lMailboxStatus = new cMailboxStatus(lStatus.Messages ?? 0, lStatus.Recent ?? 0, lStatus.UIDNext ?? 0, 0, lStatus.UIDValidity ?? 0, lStatus.Unseen ?? 0, 0, lStatus.HighestModSeq ?? 0);

                    var lProperties = lItem.SetMailboxStatus(lMailboxStatus);
                    if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
                } */

                public void Select(string pEncodedMailboxName, cMailboxName pMailboxName, bool pSelectedForUpdate, cMailboxStatus pStatus, cMessageFlags pFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags, cTrace.cContext pParentContext)
                {
                    // should only be called just before the mailbox is selected

                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxSelectedProperties), pHandle, pFlags, pSelectedForUpdate, pPermanentFlags);

                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));
                    if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                    var lItem = pHandle as cItem;
                    if (lItem == null) throw new ArgumentOutOfRangeException(nameof(pHandle));

                    var lProperties = lItem.UpdateMailboxSelectedProperties(pStatus, pFlags, pSelectedForUpdate, pPermanentFlags);
                    if (lProperties != 0) mEventSynchroniser.FireMailboxPropertiesChanged(pHandle, lProperties, lContext);


                    mSetState(eState.selected, lContext);
                }

                public void Deselect()
                {
                    ;?;


                    mSetState(eState.authenticated, lContext);
                }

                private cItem ZItem(string pEncodedMailboxName) => mDictionary.GetOrAdd(pEncodedMailboxName, new cItem(this, mEventSynchroniser, pEncodedMailboxName));

                private cItem ZItem(cMailboxName pMailboxName)
                {
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (!mFactory.TryAsMailbox(pMailboxName, out var lCommandPart, out var lEncodedMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
                    var lItem = mDictionary.GetOrAdd(lEncodedMailboxName, new cItem(this, mEventSynchroniser, lEncodedMailboxName));
                    lItem.MailboxName = pMailboxName;
                    lItem.CommandPart = lCommandPart;
                    return lItem;
                }
            }
        }
    }
}