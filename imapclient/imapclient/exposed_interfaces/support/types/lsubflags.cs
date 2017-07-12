using System;
using System.Threading;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fLSubFlags
    {
        subscribed = 1 << 0,
        hassubscribedchildren = 1 << 1
    }

    public class cLSubFlags
    {
        public enum fProperties
        {
            issubscribed = 1 << 0,
            hassubscribedchildren = 1 << 1,
        }

        public static readonly cLSubFlags NonExistent = new cLSubFlags(0);
        public static readonly cLSubFlags NotSubscribed = new cLSubFlags(0);

        private static int mLastSequence = 0;

        public readonly int Sequence;
        private readonly fLSubFlags mFlags;

        public cLSubFlags(fLSubFlags pFlags)
        {
            Sequence = Interlocked.Increment(ref mLastSequence);
            mFlags = pFlags;
        }

        public bool IsSubscribed => (mFlags & fLSubFlags.subscribed) != 0;
        public bool HasSubscribedChildren => (mFlags & fLSubFlags.hassubscribedchildren) != 0;

        public override string ToString() => $"{nameof(cLSubFlags)}({Sequence},{mFlags})";

        public static int LastSequence = mLastSequence;

        public static fProperties Differences(cLSubFlags pOld, cLSubFlags pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (ReferenceEquals(pOld, NonExistent)) return 0;
            if (pOld.mFlags == pNew.mFlags) return 0;

            fProperties lProperties = 0;

            lProperties |= ZPropertyIfDifferent(pOld, pNew, fLSubFlags.subscribed, fProperties.issubscribed);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fLSubFlags.hassubscribedchildren, fProperties.hassubscribedchildren);

            return lProperties;
        }

        private static fProperties ZPropertyIfDifferent(cLSubFlags pA, cLSubFlags pB, fLSubFlags pFlags, fProperties pProperty)
        {
            if ((pA.mFlags & pFlags) == (pB.mFlags & pFlags)) return 0;
            return pProperty;
        }
    }
}