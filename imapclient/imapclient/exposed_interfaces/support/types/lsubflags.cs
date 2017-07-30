using System;

namespace work.bacome.imapclient.support
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
            ;?;
            cLSubFlags lOld = pOld ?? kNull;
            cLSubFlags lNew = pNew ?? kNull;

            if (lOld.Flags == lNew.Flags) return 0;

            fMailboxProperties lProperties = 0;

            lProperties |= ZPropertyIfDifferent(lOld, lNew, fLSubFlags.subscribed, fMailboxProperties.issubscribed);
            lProperties |= ZPropertyIfDifferent(lOld, lNew, fLSubFlags.hassubscribedchildren, fMailboxProperties.hassubscribedchildren);

            return lProperties;
        }

        private static fMailboxProperties ZPropertyIfDifferent(cLSubFlags pA, cLSubFlags pB, fLSubFlags pFlags, fMailboxProperties pProperty)
        {
            if ((pA.Flags & pFlags) == (pB.Flags & pFlags)) return 0;
            return pProperty;
        }
    }
}