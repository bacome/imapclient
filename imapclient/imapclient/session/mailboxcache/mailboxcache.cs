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
                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");
                private static readonly cBytes kStatusSpace = new cBytes("STATUS ");
                private static readonly cBytes kMessagesSpace = new cBytes("MESSAGES ");
                private static readonly cBytes kRecentSpace = new cBytes("RECENT ");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");

                private enum eProcessStatusAttributeResult { notprocessed, processed, error }

                private static int mLastSequence = 0;    

                ;?; // don't forget to call this from the dataprocessor of the pipeline
                



                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly cAccountId mConnectedAccountId;
                private readonly cCommandPart.cFactory mStringFactory;
                private readonly Action<eState, cTrace.cContext> mSetState;

                private readonly ConcurrentDictionary<string, cItem> mDictionary = new ConcurrentDictionary<string, cItem>();

                private cSelectedMailbox mSelectedMailbox = null;

                public cMailboxCache(cEventSynchroniser pEventSynchroniser, cAccountId pConnectedAccountId, cCommandPart.cFactory pStringFactory, Action<eState, cTrace.cContext> pSetState)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mConnectedAccountId = pConnectedAccountId ?? throw new ArgumentNullException(nameof(pConnectedAccountId));
                    mStringFactory = pStringFactory ?? throw new ArgumentNullException(nameof(pStringFactory));
                    mSetState = pSetState ?? throw new ArgumentNullException(nameof(pSetState));
                }

                public iMailboxHandle GetHandle(cMailboxName pMailboxName) => ZItem(pMailboxName);

                public void CheckHandle(iMailboxHandle pHandle, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(CheckHandle), pHandle);
                    if (!ReferenceEquals(pHandle.Cache, this)) throw new cInvalidMailboxHandleException(lContext);
                    if (!mDictionary.TryGetValue(pHandle.EncodedMailboxName, out var lItem)) throw new cInvalidMailboxHandleException(lContext);
                    if (!ReferenceEquals(lItem, pHandle)) throw new cInvalidMailboxHandleException(lContext);
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ProcessData));

                    if (mSelectedMailbox != null)
                    {
                        if (pCursor.SkipBytes(kFlagsSpace))
                        {
                            if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd)
                            {
                                lContext.TraceVerbose("got flags: {0}", lFlags);

                                var lItem = mSelectedMailbox.Handle as cItem;
                                lItem.SetMessageFlags(new cMessageFlags(lFlags), lContext);
                                return eProcessDataResult.processed;
                            }

                            lContext.TraceWarning("likely malformed flags response");
                            return eProcessDataResult.notprocessed;
                        }

                        var lBookmark = pCursor.Position;
                        var lResult = mSelectedMailbox.ProcessData(pCursor, lContext);
                        if (lResult != eProcessDataResult.notprocessed) return lResult;
                        pCursor.Position = lBookmark;
                    }

                    if (pCursor.SkipBytes(kStatusSpace))
                    {
                        if (!pCursor.GetAString(out string lEncodedMailboxName) ||
                            !pCursor.SkipBytes(cBytesCursor.SpaceLParen) ||
                            !ZProcessStatusAttributes(pCursor, out var lStatus, lContext) ||
                            !pCursor.SkipByte(cASCII.RPAREN) ||
                            !pCursor.Position.AtEnd)
                        {
                            lContext.TraceWarning("likely malformed status response");
                            return eProcessDataResult.notprocessed;
                        }

                        var lItem = ZItem(lEncodedMailboxName);

                        ;?; // sequence interlocked
                        Sequence = Interlocked.Increment(ref mLastSequence);


                        lItem.UpdateStatus(lStatus);

                        if (!ReferenceEquals(lItem, mSelectedMailbox?.Handle))
                        {
                            var lProperties = lItem.UpdateMailboxStatus();
                            if (lProperties != 0) mEventSynchroniser.FireMailboxPropertiesChanged(lItem, lProperties, lContext);
                        }

                        return eProcessDataResult.processed;
                    }

                    ;?;








                }


                private static bool ZProcessStatusAttributes(cBytesCursor pCursor, out cStatus rStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessStatusAttributes));

                    uint? lMessages = 0;
                    uint? lRecent = 0;
                    uint? lUIDNext = 0;
                    uint? lUIDValidity = 0;
                    uint? lUnseen = 0;
                    ulong? lHighestModSeq = 0;

                    while (true)
                    {
                        eProcessStatusAttributeResult lResult;

                        lResult = ZProcessStatusAttribute(pCursor, kMessagesSpace, ref lMessages, lContext);

                        if (lResult == eProcessStatusAttributeResult.notprocessed)
                        {
                            lResult = ZProcessStatusAttribute(pCursor, kRecentSpace, ref lRecent, lContext);

                            if (lResult == eProcessStatusAttributeResult.notprocessed)
                            {
                                lResult = ZProcessStatusAttribute(pCursor, kUIDNextSpace, ref lUIDNext, lContext);

                                if (lResult == eProcessStatusAttributeResult.notprocessed)
                                {
                                    lResult = ZProcessStatusAttribute(pCursor, kUIDValiditySpace, ref lUIDValidity, lContext);

                                    if (lResult == eProcessStatusAttributeResult.notprocessed)
                                    {
                                        lResult = ZProcessStatusAttribute(pCursor, kUnseenSpace, ref lUnseen, lContext);

                                        if (lResult == eProcessStatusAttributeResult.notprocessed) lResult = ZProcessStatusAttribute(pCursor, kHighestModSeqSpace, ref lHighestModSeq, lContext);
                                    }
                                }
                            }
                        }

                        if (lResult != eProcessStatusAttributeResult.processed)
                        {
                            rStatus = null;
                            return false;
                        }

                        if (!pCursor.SkipByte(cASCII.SPACE))
                        {
                            rStatus = new cStatus(lMessages, lRecent, lUIDNext, lUIDValidity, lUnseen, lHighestModSeq);
                            return true;
                        }
                    }
                }

                private static eProcessStatusAttributeResult ZProcessStatusAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref uint? rNumber, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessStatusAttribute), pAttributeSpace);

                    if (!pCursor.SkipBytes(pAttributeSpace)) return eProcessStatusAttributeResult.notprocessed;

                    if (pCursor.GetNumber(out _, out var lNumber))
                    {
                        lContext.TraceVerbose("got {0}", lNumber);
                        rNumber = lNumber;
                        return eProcessStatusAttributeResult.processed;
                    }

                    lContext.TraceWarning("likely malformed status-att-list-item: no number?");
                    return eProcessStatusAttributeResult.error;
                }

                private static eProcessStatusAttributeResult ZProcessStatusAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref ulong? rNumber, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessStatusAttribute));

                    if (!pCursor.SkipBytes(pAttributeSpace)) return eProcessStatusAttributeResult.notprocessed;

                    if (pCursor.GetNumber(out var lNumber))
                    {
                        lContext.TraceVerbose("got {0}", lNumber);
                        rNumber = lNumber;
                        return eProcessStatusAttributeResult.processed;
                    }

                    lContext.TraceWarning("likely malformed status-att-list-item: no number?");
                    return eProcessStatusAttributeResult.error;
                }












                public void ResetExists(cMailboxNamePattern pPattern, int pMailboxFlagsSequence, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ResetExists), pPattern, pMailboxFlagsSequence);

                    foreach (var lItem in mDictionary.Values)
                    {
                        if (lItem.Exists && lItem.MailboxName != null && lItem.MailboxFlagSequence 









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

                public iSelectedMailboxDetails SelectedMailboxDetails => mSelectedMailbox;

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

                public static int LastSequence = mLastSequence;

                private cItem ZItem(string pEncodedMailboxName) => mDictionary.GetOrAdd(pEncodedMailboxName, new cItem(this, mEventSynchroniser, pEncodedMailboxName));

                private cItem ZItem(cMailboxName pMailboxName)
                {
                    if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                    if (!mStringFactory.TryAsMailbox(pMailboxName, out var lCommandPart, out var lEncodedMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
                    var lItem = mDictionary.GetOrAdd(lEncodedMailboxName, new cItem(this, mEventSynchroniser, lEncodedMailboxName));
                    lItem.MailboxName = pMailboxName;
                    lItem.CommandPart = lCommandPart;
                    return lItem;
                }
            }
        }
    }
}