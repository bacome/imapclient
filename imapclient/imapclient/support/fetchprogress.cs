using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cFetchProgress
        {
            private readonly cEventSynchroniser mEventSynchroniser;
            private readonly Action<int> mIncrementProgress;

            public cFetchProgress()
            {
                mEventSynchroniser = null;
                mIncrementProgress = null;
            }

            public cFetchProgress(cEventSynchroniser pEventSynchroniser, Action<int> pIncrementProgress)
            {
                mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                mIncrementProgress = pIncrementProgress;
            }

            public void Increment(int pValue, cTrace.cContext pParentContext) => mEventSynchroniser?.FireIncrementProgress(mIncrementProgress, pValue, pParentContext);
        }
    }
}
