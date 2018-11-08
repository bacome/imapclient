﻿using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookStore : cCommandHook
            {
                private static readonly cBytes kModified = new cBytes("MODIFIED");

                private readonly cStoreFeedbackCollector mFeedbackCollector;
                private readonly cUIDStoreFeedback mUIDStoreFeedback;
                private readonly cSelectedMailbox mSelectedMailbox;

                public cCommandHookStore(cStoreFeedbackCollector pFeedbackCollector, cUIDStoreFeedback pUIDStoreFeedback, cSelectedMailbox pSelectedMailbox)
                {
                    mFeedbackCollector = pFeedbackCollector ?? throw new ArgumentNullException(nameof(pFeedbackCollector));
                    mUIDStoreFeedback = pUIDStoreFeedback;
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                }

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookFetchBody), nameof(ProcessData));

                    if (!(pData is cResponseDataFetch lFetch)) return eProcessDataResult.notprocessed;
                    if (lFetch.Flags == null) return eProcessDataResult.notprocessed;

                    uint lUInt;

                    if (mFeedbackCollector.KeyType == cStoreFeedbackCollector.eKeyType.uid)
                    {
                        if (lFetch.UID == null) return eProcessDataResult.notprocessed;
                        lUInt = lFetch.UID.Value;
                    }
                    else lUInt = lFetch.MSN;

                    if (mFeedbackCollector.ReceivedFlagsUpdate(lUInt)) return eProcessDataResult.observed;
                    return eProcessDataResult.notprocessed;
                }

                public override void ProcessTextCode(eIMAPResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookStore), nameof(ProcessTextCode), pTextContext, pCode, pArguments);

                    if (pTextContext == eIMAPResponseTextContext.success || pTextContext == eIMAPResponseTextContext.failure)
                    {
                        if (pCode.Equals(kModified))
                        {
                            if (pArguments != null)
                            {
                                cBytesCursor lCursor = new cBytesCursor(pArguments);

                                if (lCursor.GetSequenceSet(false, out var lSequenceSet) && lCursor.Position.AtEnd && cUIntList.TryConstruct(lSequenceSet, 0, true, out var lUInts))
                                {
                                    foreach (var lUInt in lUInts)
                                    {
                                        if (!mFeedbackCollector.WasNotUnchangedSince(lUInt))
                                        {
                                            lContext.TraceWarning("likely malformed modified response: message number not recognised: ", lUInt);
                                            return;
                                        }
                                    }

                                    return;
                                }
                            }

                            lContext.TraceWarning("likely malformed modified response");
                        }
                    }
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookStore), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType == eIMAPCommandResultType.ok && mUIDStoreFeedback != null)
                    {
                        // find the handles for the UIDs, if possible
                        //  (this is to enhance the ability to tell if the store was successful or not for a UIDStore)
                        foreach (var lItem in mUIDStoreFeedback.Items) lItem.MessageHandle = mSelectedMailbox.GetHandle(lItem.UID);
                    }
                }
            }
        }
    }
}