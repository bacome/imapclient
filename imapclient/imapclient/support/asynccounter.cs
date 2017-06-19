using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cAsyncCounter
        {
            private readonly Action<string, cTrace.cContext> mPropertyChanged;
            private int mCount = 0;

            public cAsyncCounter(Action<string, cTrace.cContext> pPropertyChanged)
            {
                mPropertyChanged = pPropertyChanged ?? throw new ArgumentNullException(nameof(pPropertyChanged));
            }

            public void Increment(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAsyncCounter), nameof(Increment));
                Interlocked.Increment(ref mCount);
                mPropertyChanged(nameof(cIMAPClient.AsyncCount), lContext);
            }

            public void Decrement(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAsyncCounter), nameof(Decrement));
                Interlocked.Decrement(ref mCount);
                mPropertyChanged(nameof(cIMAPClient.AsyncCount), lContext);
            }

            public int Count => mCount;
        }
    }
}