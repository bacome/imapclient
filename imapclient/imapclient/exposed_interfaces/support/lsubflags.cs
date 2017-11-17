using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Contains some mailbox data.
    /// </summary>
    /// <seealso cref="iMailboxHandle"/>
    public class cLSubFlags
    {
        internal readonly int Sequence;
        internal readonly bool Subscribed;

        internal cLSubFlags(int pSequence, bool pSubscribed)
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