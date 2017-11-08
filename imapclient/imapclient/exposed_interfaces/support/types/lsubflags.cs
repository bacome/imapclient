using System;

namespace work.bacome.imapclient.support
{
    public class cLSubFlags
    {
        public readonly int Sequence;
        public readonly bool Subscribed;

        public cLSubFlags(int pSequence, bool pSubscribed)
        {
            Sequence = pSequence;
            Subscribed = pSubscribed;
        }

        public override string ToString() => $"{nameof(cLSubFlags)}({Sequence},{Subscribed})";

        internal static fMailboxProperties Differences(cLSubFlags pOld, cLSubFlags pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (pOld == null) return 0;
            if (pOld.Subscribed == pNew.Subscribed) return 0;
            return fMailboxProperties.issubscribed;
        }
    }
}