using System;
using System.Threading;

namespace work.bacome.imapclient.support
{
    public class cLSubFlags
    {
        public static readonly fMailboxFlags ClearFlagsMask = ~(fMailboxFlags.subscribed | fMailboxFlags.hassubscribedchildren);

        private static readonly cLSubFlags kNullFlags = new cLSubFlags(0);

        private static int mLastSequence = 0;

        public readonly int Sequence;
        public readonly fMailboxFlags Flags;

        public cLSubFlags(fMailboxFlags pFlags)
        {
            Sequence = Interlocked.Increment(ref mLastSequence);
            Flags = pFlags;
        }

        public bool IsSubscribed => (Flags & fMailboxFlags.subscribed) != 0;
        public bool HasSubscribedChildren => (Flags & fMailboxFlags.hassubscribedchildren) != 0;

        public override string ToString() => $"{nameof(cLSubFlags)}({Sequence},{Flags})";

        public static int LastSequence = mLastSequence;

        public static fMailboxProperties Differences(cLSubFlags pOld, cLSubFlags pNew)
        {
            cLSubFlags lOld;
            if (pOld == null) lOld = kNullFlags;
            else lOld = pOld;

            cLSubFlags lNew;
            if (pNew == null) lNew = kNullFlags;
            else lNew = pNew;

            if (lOld.Flags == lNew.Flags) return 0;

            fMailboxProperties lProperties = 0;

            lProperties |= ZPropertyIfDifferent(lOld, lNew, fMailboxFlags.subscribed, fMailboxProperties.issubscribed);
            lProperties |= ZPropertyIfDifferent(lOld, lNew, fMailboxFlags.hassubscribedchildren, fMailboxProperties.hassubscribedchildren);

            if (lProperties != 0) lProperties |= fMailboxProperties.mailboxflags;

            return lProperties;
        }

        private static fMailboxProperties ZPropertyIfDifferent(cLSubFlags pA, cLSubFlags pB, fMailboxFlags pFlags, fMailboxProperties pProperty)
        {
            if ((pA.Flags & pFlags) == (pB.Flags & pFlags)) return 0;
            return pProperty;
        }
    }
}