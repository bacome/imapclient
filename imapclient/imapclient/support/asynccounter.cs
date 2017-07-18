using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cAsyncCounter
        {
            private readonly Action<string, cTrace.cContext> mFirePropertyChanged;
            private int mCount = 0;

            public cAsyncCounter(Action<string, cTrace.cContext> pFirePropertyChanged)
            {
                mFirePropertyChanged = pFirePropertyChanged ?? throw new ArgumentNullException(nameof(pFirePropertyChanged));
            }

            public void Increment(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAsyncCounter), nameof(Increment));
                Interlocked.Increment(ref mCount);
                mFirePropertyChanged(nameof(cIMAPClient.AsyncCount), lContext);
            }

            public void Decrement(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAsyncCounter), nameof(Decrement));
                Interlocked.Decrement(ref mCount);
                mFirePropertyChanged(nameof(cIMAPClient.AsyncCount), lContext);
            }

            public int Count => mCount;
        }
    }
}