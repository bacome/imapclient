using System;
using System.Collections.ObjectModel;
using System.Threading;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandTag : ReadOnlyCollection<byte>
            {
                private static int mTagSource = 7;
                public cCommandTag() : base(cTools.IntToBytesReverse(Interlocked.Increment(ref mTagSource))) { }
                public override string ToString() => cTools.BytesToLoggableString(nameof(cCommandTag), this, 10);
            }
        }
    }
}
