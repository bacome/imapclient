using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cLSubFlags
    {
        public readonly int Sequence;
        public readonly fLSubFlags Flags;

        public cLSubFlags(int pSequence, fLSubFlags pFlags)
        {
            Sequence = pSequence;
            Flags = pFlags;
        }

        public bool IsSubscribed => (Flags & fLSubFlags.subscribed) != 0;
        public bool HasSubscribedChildren => (Flags & fLSubFlags.hassubscribedchildren) != 0;

        public override string ToString() => $"{nameof(cLSubFlags)}({Sequence},{Flags})";

        public static fMailboxProperties Differences(cLSubFlags pOld, cLSubFlags pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (pOld == null) return 0;
            if (pOld.Flags == pNew.Flags) return 0;

            fMailboxProperties lProperties = fMailboxProperties.lsubflags;

            lProperties |= ZPropertyIfDifferent(pOld, pNew, fLSubFlags.subscribed, fMailboxProperties.issubscribed);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fLSubFlags.hassubscribedchildren, fMailboxProperties.hassubscribedchildren);

            return lProperties;
        }

        private static fMailboxProperties ZPropertyIfDifferent(cLSubFlags pA, cLSubFlags pB, fLSubFlags pFlags, fMailboxProperties pProperty)
        {
            if ((pA.Flags & pFlags) == (pB.Flags & pFlags)) return 0;
            return pProperty;
        }
    }
}