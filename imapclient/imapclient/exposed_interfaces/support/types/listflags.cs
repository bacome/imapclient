using System;
using System.Threading;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fListFlags
    {
        // rfc 3501
        noinferiors = 1 << 0, // hasnochildren will be set if this is set
        noselect = 1 << 1, // \ 
        marked = 1 << 2, //    > only one of these may be true
        unmarked = 1 << 3, // /

        // rfc 5258
        nonexistent = 1 << 4, // noselect will be set if this is set
        subscribed = 1 << 5,
        remote = 1 << 6,
        haschildren = 1 << 7, // rfc 3348
        hasnochildren = 1 << 8, // rfc 3348

        hassubscribedchildren = 1 << 9, // derived from the CHILDINFO response

        // next 7 rfc 6154 (specialuse)
        all = 1 << 10,
        archive = 1 << 11,
        drafts = 1 << 12,
        flagged = 1 << 13,
        junk = 1 << 14,
        sent = 1 << 15,
        trash = 1 << 16
    }

    public class cListFlags
    {
        public enum fProperties
        {
            canhavechildren = 1 << 0,
            canselect = 1 << 1,
            ismarked = 1 << 2,
            exists = 1 << 3,
            issubscribed = 1 << 4,
            isremote = 1 << 5,
            haschildren = 1 << 6,
            hassubscribedchildren = 1 << 7,
            containsall = 1 << 8,
            isarchive = 1 << 9,
            containsdrafts = 1 << 10,
            containsflagged = 1 << 11,
            containsjunk = 1 << 12,
            containssent = 1 << 13,
            containstrash = 1 << 14
        }

        public static readonly cListFlags NonExistent = new cListFlags(fListFlags.noinferiors | fListFlags.noselect | fListFlags.nonexistent);

        private static int mLastSequence = 0;

        public readonly int Sequence;
        private readonly fListFlags mFlags;

        public cListFlags(fListFlags pFlags)
        {
            Sequence = Interlocked.Increment(ref mLastSequence);
            mFlags = pFlags;
        }

        public bool CanHaveChildren => (mFlags & fListFlags.noinferiors) == 0;
        public bool CanSelect => (mFlags & fListFlags.noselect) == 0;

        public bool? IsMarked
        {
            get
            {
                fListFlags lFlags = mFlags & (fListFlags.marked | fListFlags.unmarked);
                if (lFlags == fListFlags.marked) return true;
                if (lFlags == fListFlags.unmarked) return false;
                return null;
            }
        }

        public bool Exists => (mFlags & fListFlags.nonexistent) == 0;
        public bool IsSubscribed => (mFlags & fListFlags.subscribed) != 0;
        public bool IsRemote => (mFlags & fListFlags.remote) != 0;

        public bool? HasChildren
        {
            get
            {
                fListFlags lFlags = mFlags & (fListFlags.haschildren | fListFlags.hasnochildren);
                if (lFlags == fListFlags.haschildren) return true;
                if (lFlags == fListFlags.hasnochildren) return false;
                return null;
            }
        }

        public bool HasSubscribedChildren => (mFlags & fListFlags.hassubscribedchildren) != 0;

        public bool ContainsAll => (mFlags & fListFlags.all) != 0;
        public bool IsArchive => (mFlags & fListFlags.archive) != 0;
        public bool ContainsDrafts => (mFlags & fListFlags.drafts) != 0;
        public bool ContainsFlagged => (mFlags & fListFlags.flagged) != 0;
        public bool ContainsJunk => (mFlags & fListFlags.junk) != 0;
        public bool ContainsSent => (mFlags & fListFlags.sent) != 0;
        public bool ContainsTrash => (mFlags & fListFlags.trash) != 0;

        public override string ToString() => $"{nameof(cListFlags)}({Sequence},{mFlags})";

        public static int LastSequence = mLastSequence;

        public static fProperties Differences(cListFlags pOld, cListFlags pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (ReferenceEquals(pOld, NonExistent)) return 0;
            if (pOld.mFlags == pNew.mFlags) return 0;

            fProperties lProperties = 0;

            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.noinferiors, fProperties.canhavechildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.noselect, fProperties.canselect);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.marked | fListFlags.unmarked, fProperties.ismarked);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.nonexistent, fProperties.exists);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.subscribed, fProperties.issubscribed);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.remote, fProperties.isremote);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.haschildren | fListFlags.hasnochildren, fProperties.haschildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.hassubscribedchildren, fProperties.hassubscribedchildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.all, fProperties.containsall);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.archive, fProperties.isarchive);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.drafts, fProperties.containsdrafts);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.flagged, fProperties.containsflagged);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.junk, fProperties.containsjunk);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.sent, fProperties.containssent);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.trash, fProperties.containstrash);

            return lProperties;
        }

        private static fProperties ZPropertyIfDifferent(cListFlags pA, cListFlags pB, fListFlags pFlags, fProperties pProperty)
        {
            if ((pA.mFlags & pFlags) == (pB.mFlags & pFlags)) return 0;
            return pProperty;
        }
    } 
}