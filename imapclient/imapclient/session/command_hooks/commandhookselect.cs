using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookSelect : cCommandHook
            {
                private static readonly cBytes kClosedRBracketSpace = new cBytes("CLOSED] ");

                private bool mDeselectRequired;
                private readonly cCapability mCapability;
                private readonly cSelectedMailbox mPendingSelectedMailbox;
                private readonly Action<cSelectedMailbox, cTrace.cContext> mSetSelectedMailbox;

                public cCommandHookSelect(bool pDeselectRequired, cCapability pCapability, cSelectedMailbox pPendingSelectedMailbox, Action<cSelectedMailbox, cTrace.cContext> pSetSelectedMailbox)
                {
                    mDeselectRequired = pDeselectRequired;
                    mCapability = pCapability ?? throw new ArgumentNullException(nameof(pCapability));
                    mPendingSelectedMailbox = pPendingSelectedMailbox ?? throw new ArgumentNullException(nameof(pPendingSelectedMailbox));
                    mSetSelectedMailbox = pSetSelectedMailbox ?? throw new ArgumentNullException(nameof(pSetSelectedMailbox));
                }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    if (mDeselectRequired && !mCapability.QResync)
                    {
                        mDeselectRequired = false;
                        mSetSelectedMailbox(null, pParentContext);
                    }
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    if (mDeselectRequired) return eProcessDataResult.notprocessed;
                    return mPendingSelectedMailbox.ProcessData(pCursor, pParentContext);
                }

                public override bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessTextCode));

                    if (mDeselectRequired)
                    {
                        if (pCursor.SkipBytes(kClosedRBracketSpace))
                        {
                            lContext.TraceVerbose("got closed");
                            mDeselectRequired = false;
                            mSetSelectedMailbox(null, lContext);
                            return true;
                        }

                        return false;
                    }
                    else return mPendingSelectedMailbox.ProcessTextCode(pCursor, pParentContext);
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandCompleted), pResult);

                    if (mDeselectRequired)
                    {
                        mDeselectRequired = false;
                        mSetSelectedMailbox(null, pParentContext);
                    }

                    if (pResult != null && pResult.Result == cCommandResult.eResult.ok) mSetSelectedMailbox(mPendingSelectedMailbox, lContext);
                }
            }
        }
    }
}