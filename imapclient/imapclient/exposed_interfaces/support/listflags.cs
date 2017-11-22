using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Contains some cached mailbox data.
    /// </summary>
    /// <seealso cref="iMailboxHandle"/>
    public class cListFlags
    {
        internal readonly int Sequence;
        internal readonly fListFlags Flags;

        internal cListFlags(int pSequence, fListFlags pFlags)
        {
            Sequence = pSequence;
            Flags = pFlags;
        }

        internal bool CanHaveChildren => (Flags & fListFlags.noinferiors) == 0;
        internal bool CanSelect => (Flags & fListFlags.noselect) == 0;

        internal bool? IsMarked
        {
            get
            {
                fListFlags lFlags = Flags & (fListFlags.marked | fListFlags.unmarked);
                if (lFlags == fListFlags.marked) return true;
                if (lFlags == fListFlags.unmarked) return false;
                return null;
            }
        }

        internal bool IsRemote => (Flags & fListFlags.remote) != 0;

        internal bool? HasChildren
        {
            get
            {
                fListFlags lFlags = Flags & (fListFlags.haschildren | fListFlags.hasnochildren);
                if (lFlags == fListFlags.haschildren) return true;
                if (lFlags == fListFlags.hasnochildren) return false;
                return null;
            }
        }

        internal bool ContainsAll => (Flags & fListFlags.all) != 0;
        internal bool IsArchive => (Flags & fListFlags.archive) != 0;
        internal bool ContainsDrafts => (Flags & fListFlags.drafts) != 0;
        internal bool ContainsFlagged => (Flags & fListFlags.flagged) != 0;
        internal bool ContainsJunk => (Flags & fListFlags.junk) != 0;
        internal bool ContainsSent => (Flags & fListFlags.sent) != 0;
        internal bool ContainsTrash => (Flags & fListFlags.trash) != 0;

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cListFlags)}({Sequence},{Flags})";

        internal static fMailboxProperties Differences(cListFlags pOld, cListFlags pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (pOld == null) return 0;
            if (pOld.Flags == pNew.Flags) return 0;

            fMailboxProperties lProperties = 0;

            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.noinferiors, fMailboxProperties.canhavechildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.noselect, fMailboxProperties.canselect);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.marked | fListFlags.unmarked, fMailboxProperties.ismarked);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fListFlags.nonexistent, fMailboxProperties.exists);
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