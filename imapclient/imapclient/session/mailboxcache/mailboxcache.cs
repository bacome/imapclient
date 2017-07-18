﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache : iUnsolicitedDataProcessor
            {
                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly cAccountId mConnectedAccountId;
                private readonly ConcurrentDictionary<string, cItem> mDictionary = new ConcurrentDictionary<string, cItem>();

                public cMailboxCache(cEventSynchroniser pEventSynchroniser, cAccountId pConnectedAccountId)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mConnectedAccountId = pConnectedAccountId ?? throw new ArgumentNullException(nameof(pConnectedAccountId));
                }

                public iMailboxHandle GetHandle(string pEncodedMailboxName, cMailboxName pMailboxName) => ZItem(pEncodedMailboxName, pMailboxName);

                public void ResetExists(cMailboxNamePattern pPattern, int pMailboxFlagsSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pMailboxFlagsSequence);

                    foreach (var lItem in mDictionary.Values)
                    {
                        var lProperties = lItem.ResetExists(pPattern, pMailboxFlagsSequence);
                        if (lProperties != 0) mEventSynchroniser.FireMailboxPropertiesChanged(lItem, lProperties, lContext);
                    }
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
                        if (lProperties != 0) ZMailboxPropertiesChanged(lItem.MailboxName, lProperties, lContext);
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
                
                public void SetMailboxStatus(iMailboxHandle pHandle, cMailboxStatus pStatus, cTrace.cContext pParentContext)
                {
                    // should only be called for the selected mailbox

                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(SetMailboxStatus), pHandle, pStatus);

                    if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                    if (pStatus == null) throw new ArgumentNullException(nameof(pStatus));

                    var lProperties = ZItem(pHandle.EncodedMailboxName, null).SetMailboxStatus(pStatus);
                    if (lProperties != 0) mEventSynchroniser.FireMailboxPropertiesChanged(pHandle, lProperties, lContext);
                }

                public void UpdateMailboxSelectedProperties(iMailboxHandle pHandle, cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags, cTrace.cContext pParentContext)
                {
                    // should only be called for the selected mailbox

                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(UpdateMailboxBeenSelected), pEncodedMailboxName, pMailboxName, pMessageFlags, pSelectedForUpdate, pPermanentFlags);

                    if (pEncodedMailboxName == null) throw new ArgumentNullException(nameof(pEncodedMailboxName));
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));

                    // get the handle
                    var lItem = ZItem(pEncodedMailboxName, pMailboxName);

                    var lProperties = lItem.UpdateMailboxBeenSelected(pMessageFlags, pSelectedForUpdate, pPermanentFlags);
                    if (lProperties != 0) ZMailboxPropertiesChanged(pMailboxName, lProperties, lContext);
                }

                private cItem ZItem(string pEncodedMailboxName, cMailboxName pMailboxName)
                {
                    cItem lItem = mDictionary.GetOrAdd(pEncodedMailboxName, new cItem(this, pEncodedMailboxName));
                    if (lItem.MailboxName == null && pMailboxName != null) lItem.MailboxName = pMailboxName;
                    return lItem;
                }
            }
        }
    }
}