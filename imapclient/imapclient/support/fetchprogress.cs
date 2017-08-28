using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cFetchProgress
        {
            private readonly cInvokeSynchroniser mSynchroniser;
            private readonly Action<int> mIncrement;

            public cFetchProgress()
            {
                mSynchroniser = null;
                mIncrement = null;
            }

            public cFetchProgress(cInvokeSynchroniser pSynchroniser, Action<int> pIncrement)
            {
                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mIncrement = pIncrement;
            }

            public cFetchProgress(cInvokeSynchroniser pSynchroniser, Action<int> pSetCount, Action<int> pIncrement)
            {
                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mIncrement = pIncrement;
            }

            public void Increment(int pValue, cTrace.cContext pParentContext) => mSynchroniser?.InvokeActionInt(mIncrement, pValue, pParentContext);
        }
    }
}
