using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cProgress
        {
            private readonly cCallbackSynchroniser mSynchroniser;
            private readonly Action<int> mSetCount;
            private readonly Action<int> mIncrement;

            public cProgress()
            {
                mSynchroniser = null;
                mSetCount = null;
                mIncrement = null;
            }

            public cProgress(cCallbackSynchroniser pSynchroniser, Action<int> pIncrement)
            {
                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mSetCount = null;
                mIncrement = pIncrement;
            }

            public cProgress(cCallbackSynchroniser pSynchroniser, Action<int> pSetCount, Action<int> pIncrement)
            {
                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mSetCount = pSetCount;
                mIncrement = pIncrement;
            }

            public void SetCount(int pValue, cTrace.cContext pParentContext) => mSynchroniser?.InvokeActionInt(mSetCount, pValue, pParentContext);
            public void Increment(int pValue, cTrace.cContext pParentContext) => mSynchroniser?.InvokeActionInt(mIncrement, pValue, pParentContext);
        }
    }
}
