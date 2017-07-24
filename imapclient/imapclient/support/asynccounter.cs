using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cAsyncCounter
        {
            private readonly cEventSynchroniser mEventSynchroniser;
            private int mCount = 0;

            public cAsyncCounter(cEventSynchroniser pEventSynchroniser)
            {
                mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
            }

            public void Increment(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAsyncCounter), nameof(Increment));
                Interlocked.Increment(ref mCount);
                mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.AsyncCount), lContext);
            }

            public void Decrement(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAsyncCounter), nameof(Decrement));
                Interlocked.Decrement(ref mCount);
                mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.AsyncCount), lContext);
            }

            public int Count => mCount;
        }
    }
}