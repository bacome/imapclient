using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cProgress
        {
            private readonly cCallbackSynchroniser mSynchroniser;
            private readonly Action<int> mIncrement;

            public cProgress()
            {
                mSynchroniser = null;
                mIncrement = null;
            }

            public cProgress(cCallbackSynchroniser pSynchroniser, Action<int> pIncrement)
            {
                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mIncrement = pIncrement;
            }

            public void Increment(int pValue, cTrace.cContext pParentContext) => mSynchroniser?.InvokeActionInt(mIncrement, pValue, pParentContext);
        }
    }
}
