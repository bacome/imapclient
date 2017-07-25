using System;

namespace work.bacome.imapclient.support
{
    public class cListFlags
    {
        public readonly int Sequence;
        public readonly fListFlags Flags;

        public cListFlags(int pSequence, fListFlags pFlags)
        {
            Sequence = pSequence;
            Flags = pFlags;
        }

        public bool CanHaveChildren => (Flags & fListFlags.noinferiors) == 0;
        public bool CanSelect => (Flags & fListFlags.noselect) == 0;

        public bool? IsMarked
        {
            get
            {
                fListFlags lFlags = Flags & (fListFlags.marked | fListFlags.unmarked);
                if (lFlags == fListFlags.marked) return true;
                if (lFlags == fListFlags.unmarked) return false;
                return null;
            }
        }

        public bool NonExistent => (Flags & fListFlags.nonexistent) != 0;
        public bool IsRemote => (Flags & fListFlags.remote) != 0;

        public bool? HasChildren
        {
            get
            {
                fListFlags lFlags = Flags & (fListFlags.haschildren | fListFlags.hasnochildren);
                if (lFlags == fListFlags.haschildren) return true;
                if (lFlags == fListFlags.hasnochildren) return false;
                return null;
            }
        }

        public bool ContainsAll => (Flags & fListFlags.all) != 0;
        public bool IsArchive => (Flags & fListFlags.archive) != 0;
        public bool ContainsDrafts => (Flags & fListFlags.drafts) != 0;
        public bool ContainsFlagged => (Flags & fListFlags.flagged) != 0;
        public bool ContainsJunk => (Flags & fListFlags.junk) != 0;
        public bool ContainsSent => (Flags & fListFlags.sent) != 0;
        public bool ContainsTrash => (Flags & fListFlags.trash) != 0;

        public override string ToString() => $"{nameof(cListFlags)}({Sequence},{Flags})";

        public static fMailboxProperties Differences(cListFlags pOld, cListFlags pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (pOld == null) return 0;
            if (pOld.Flags == pNew.Flags) return 0;

            fMailboxProperties lProperties = fMailboxProperties.listflags;

            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.noinferiors, fMailboxProperties.canhavechildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.noselect, fMailboxProperties.canselect);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.marked | fListFlags.unmarked, fMailboxProperties.ismarked);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.nonexistent, fMailboxProperties.nonexistent);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.remote, fMailboxProperties.isremote);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.haschildren | fListFlags.hasnochildren, fMailboxProperties.haschildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.all, fMailboxProperties.containsall);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.archive, fMailboxProperties.isarchive);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.drafts, fMailboxProperties.containsdrafts);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.flagged, fMailboxProperties.containsflagged);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.junk, fMailboxProperties.containsjunk);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.sent, fMailboxProperties.containssent);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.trash, fMailboxProperties.containstrash);

            return lProperties;
        }

        private static fMailboxProperties ZPropertyIfDifferent(cListFlags pA, cListFlags pB, fListFlags pFlags, fMailboxProperties pProperty)
        {
            if ((pA.Flags & pFlags) == (pB.Flags & pFlags)) return 0;
            return pProperty;
        }
    }
}