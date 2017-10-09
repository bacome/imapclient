using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookStore : cCommandHook
            {
                private static readonly cBytes kModifiedSpace = new cBytes("MODIFIED ");

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

                public override bool ProcessTextCode(eResponseTextType pTextType, cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookStore), nameof(ProcessTextCode), pTextType);

                    if (pTextType == eResponseTextType.success || pTextType == eResponseTextType.failure)
                    {
                        if (pCursor.SkipBytes(kModifiedSpace))
                        {
                            if (pCursor.GetSequenceSet(out var lSequenceSet) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                            {
                                cUIntList lUInts = cUIntList.FromSequenceSet(lSequenceSet, mSelectedMailbox.Cache.Count, true);
                                foreach (var lUInt in lUInts) if (!mFeedbackCollector.WasNotUnchangedSince(lUInt)) lContext.TraceWarning("likely malformed modified response: message number not recognised: ", lUInt);
                                return true;
                            }

                            lContext.TraceWarning("likely malformed modified response");
                        }
                    }

                    return false;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookStore), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType == eCommandResultType.ok && mUIDStoreFeedback != null)
                    {
                        // find the handles for the UIDs, if possible
                        //  (this is to enhance the ability to tell if the store was successful or not for a UIDStore)
                        foreach (var lItem in mUIDStoreFeedback) lItem.Handle = mSelectedMailbox.GetHandle(lItem.UID);
                    }
                }
            }
        }
    }
}