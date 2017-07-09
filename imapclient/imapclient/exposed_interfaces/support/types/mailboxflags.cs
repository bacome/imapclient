using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fMailboxFlags
    {
        noinferiors = 1, // rfc 3501, hasnochildren must be set if this is set
        haschildren = 1 << 1, // rfc 3348/ 5258
        hasnochildren = 1 << 2, // rfc 3348/ 5258
        noselect = 1 << 3, // rfc 3501 \ ((must be set if rfc 5258 nonexistent is received))
        marked = 1 << 4, // rfc 3501    > only one of these may be true
        unmarked = 1 << 5, // rfc 3501 /
        subscribed = 1 << 6, // rfc 5258 
        hassubscribedchildren = 1 << 7, // derived from the LIST/LSUB replies OR the CHILDINFO response, haschildren must be set if this is set
        local = 1 << 8, // set if the mailbox is returned by list, lsub, or list-extended and no /remote was received
                        // next 7 rfc 6154 (specialuse)
        all = 1 << 9,
        archive = 1 << 10,
        drafts = 1 << 11,
        flagged = 1 << 12,
        junk = 1 << 13,
        sent = 1 << 14,
        trash = 1 << 15
    }

    public class cMailboxFlags
    {
        public static readonly fMailboxFlagSets CanHaveChildrenFlagSets = fMailboxFlagSets.rfc3501;
        public static readonly fMailboxFlagSets HasChildrenFlagSets = fMailboxFlagSets.children;
        public static readonly fMailboxFlagSets CanSelectFlagSets = fMailboxFlagSets.rfc3501;
        public static readonly fMailboxFlagSets IsMarkedFlagSets = fMailboxFlagSets.rfc3501;
        public static readonly fMailboxFlagSets IsSubscribedFlagSets = fMailboxFlagSets.subscribed;
        public static readonly fMailboxFlagSets HasSubscribedChildrenFlagSets = fMailboxFlagSets.subscribedchildren;
        public static readonly fMailboxFlagSets IsLocalFlagSets = fMailboxFlagSets.local;
        public static readonly fMailboxFlagSets ContainsAllFlagSets = fMailboxFlagSets.specialuse;
        public static readonly fMailboxFlagSets IsArchiveFlagSets = fMailboxFlagSets.specialuse;
        public static readonly fMailboxFlagSets ContainsDraftsFlagSets = fMailboxFlagSets.specialuse;
        public static readonly fMailboxFlagSets ContainsFlaggedFlagSets = fMailboxFlagSets.specialuse;
        public static readonly fMailboxFlagSets ContainsJunkFlagSets = fMailboxFlagSets.specialuse;
        public static readonly fMailboxFlagSets ContainsSentFlagSets = fMailboxFlagSets.specialuse;
        public static readonly fMailboxFlagSets ContainsTrashFlagSets = fMailboxFlagSets.specialuse;

        private static readonly fMailboxFlags krfc3501Flags = fMailboxFlags.noinferiors | fMailboxFlags.noselect | fMailboxFlags.marked | fMailboxFlags.unmarked;
        private static readonly fMailboxFlags kChildrenFlags = fMailboxFlags.haschildren
        private static readonly fMailboxFlags kSubscribedFlags =
        private static readonly fMailboxFlags kSubscribedChildrenFlags =
        private static readonly fMailboxFlags kLocalFlags =
        private static readonly fMailboxFlags kSpecialUseFlags =

        private readonly fMailboxFlags mMask;
        private readonly fMailboxFlags mFlags;

        public cMailboxFlags(fMailboxFlags pMask, fMailboxFlags pFlags)
        {
            mMask = pMask;
            mFlags = pFlags;
        }

        public bool HasProperty(fMailboxProperties pProperty) => mMask & 


        public bool CanHaveChildren
        {
            get
            {
                ZCheckRequiredFlagSets(kCanHaveChildrenFlagSets);
                return (mFlags & fMailboxFlags.noinferiors) == 0;
            }
        }

        public bool? HasChildren
        {
            get
            {
                ZCheckRequiredFlagSets(kHasChildrenFlagSets);
                fMailboxFlags lFlags = mFlags & (fMailboxFlags.haschildren | fMailboxFlags.hasnochildren);
                if (lFlags == fMailboxFlags.haschildren) return true;
                if (lFlags == fMailboxFlags.hasnochildren) return false;
                return null;
            }
        }

        public bool CanSelect
        {
            get
            {
                ZCheckRequiredFlagSets(kCanSelectFlagSets);
                return (mFlags & fMailboxFlags.noselect) == 0;
            }
        }

        public bool? IsMarked
        {
            get
            {
                ZCheckRequiredFlagSets(IsMarkedFlagSets);
                fMailboxFlags lFlags = Flags & (fMailboxFlags.marked | fMailboxFlags.unmarked);
                if (lFlags == fMailboxFlags.marked) return true;
                if (lFlags == fMailboxFlags.unmarked) return false;
                return null;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                ZCheckRequiredFlagSets(IsSubscribedFlagSets);
                return (Flags & fMailboxFlags.subscribed) != 0;
            }
        }

        public bool HasSubscribedChildren
        {
            get
            {
                ZCheckRequiredFlagSets(HasSubscribedChildrenFlagSets);
                return (Flags & fMailboxFlags.hassubscribedchildren) != 0;
            }
        }

        public bool IsLocal
        {
            get
            {
                ZCheckRequiredFlagSets(IsLocalFlagSets);
                return (Flags & fMailboxFlags.local) != 0;
            }
        }

        public bool ContainsAll
        {
            get
            {
                ZCheckRequiredFlagSets(ContainsAllFlagSets);
                return (Flags & fMailboxFlags.all) != 0;
            }
        }

        public bool IsArchive
        {
            get
            {
                ZCheckRequiredFlagSets(IsArchiveFlagSets);
                return (Flags & fMailboxFlags.archive) != 0;
            }
        }

        public bool ContainsDrafts
        {
            get
            {
                ZCheckRequiredFlagSets(ContainsDraftsFlagSets);
                return (Flags & fMailboxFlags.drafts) != 0;
            }
        }

        public bool ContainsFlagged
        {
            get
            {
                ZCheckRequiredFlagSets(ContainsFlaggedFlagSets);
                return (Flags & fMailboxFlags.flagged) != 0;
            }
        }

        public bool ContainsJunk
        {
            get
            {
                ZCheckRequiredFlagSets(ContainsJunkFlagSets);
                return (Flags & fMailboxFlags.junk) != 0;
            }
        }

        public bool ContainsSent
        {
            get
            {
                ZCheckRequiredFlagSets(ContainsSentFlagSets);
                return (Flags & fMailboxFlags.sent) != 0;
            }
        }

        public bool ContainsTrash
        {
            get
            {
                ZCheckRequiredFlagSets(ContainsTrashFlagSets);
                return (Flags & fMailboxFlags.trash) != 0;
            }
        }








        public cMailboxFlags Combine(cMailboxFlags pOld, cMailboxFlags pNew)
        {

        }









        private void ZCheckRequiredFlagSets(fMailboxFlagSets pRequired)
        {
            if ((FlagSets & pRequired) != pRequired) throw new cMailboxFlagSetsException(pRequired);
        }

        public override bool Equals(object pObject) => this == pObject as cMailboxFlags;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + FlagSets.GetHashCode();
                lHash = lHash * 23 + Flags.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cMailboxFlags)}({FlagSets},{Flags})";

        public static fMailboxCacheItemDifferences Differences(cMailboxFlags pA, cMailboxFlags pB)
        {
            if (ReferenceEquals(pA, pB)) return 0;
            if (ReferenceEquals(pA, null)) return fMailboxCacheItemDifferences.allmailboxflags;
            if (ReferenceEquals(pB, null)) return fMailboxCacheItemDifferences.allmailboxflags;

            fMailboxCacheItemDifferences lResult = 0;

            if (ZIsDifferent(pA, pB, CanHaveChildrenFlagSets, fMailboxFlags.noinferiors)) lResult |= fMailboxCacheItemDifferences.canhavechildren;
            if (ZIsDifferent(pA, pB, HasChildrenFlagSets, fMailboxFlags.haschildren | fMailboxFlags.hasnochildren)) lResult |= fMailboxCacheItemDifferences.haschildren;
            if (ZIsDifferent(pA, pB, CanSelectFlagSets, fMailboxFlags.noselect)) lResult |= fMailboxCacheItemDifferences.canselect;
            if (ZIsDifferent(pA, pB, IsMarkedFlagSets, fMailboxFlags.marked | fMailboxFlags.unmarked)) lResult |= fMailboxCacheItemDifferences.ismarked;
            if (ZIsDifferent(pA, pB, IsSubscribedFlagSets, fMailboxFlags.subscribed)) lResult |= fMailboxCacheItemDifferences.issubscribed;
            if (ZIsDifferent(pA, pB, HasSubscribedChildrenFlagSets, fMailboxFlags.hassubscribedchildren)) lResult |= fMailboxCacheItemDifferences.hassubscribedchildren;
            if (ZIsDifferent(pA, pB, IsLocalFlagSets, fMailboxFlags.local)) lResult |= fMailboxCacheItemDifferences.islocal;
            if (ZIsDifferent(pA, pB, ContainsAllFlagSets, fMailboxFlags.all)) lResult |= fMailboxCacheItemDifferences.containsall;
            if (ZIsDifferent(pA, pB, IsArchiveFlagSets, fMailboxFlags.archive)) lResult |= fMailboxCacheItemDifferences.isarchive;
            if (ZIsDifferent(pA, pB, ContainsDraftsFlagSets, fMailboxFlags.drafts)) lResult |= fMailboxCacheItemDifferences.containsdrafts;
            if (ZIsDifferent(pA, pB, ContainsFlaggedFlagSets, fMailboxFlags.flagged)) lResult |= fMailboxCacheItemDifferences.containsflagged;
            if (ZIsDifferent(pA, pB, ContainsJunkFlagSets, fMailboxFlags.junk)) lResult |= fMailboxCacheItemDifferences.containsjunk;
            if (ZIsDifferent(pA, pB, ContainsSentFlagSets, fMailboxFlags.sent)) lResult |= fMailboxCacheItemDifferences.containssent;
            if (ZIsDifferent(pA, pB, ContainsTrashFlagSets, fMailboxFlags.trash)) lResult |= fMailboxCacheItemDifferences.containstrash;

            return lResult;
        }

        private static bool ZIsDifferent(cMailboxFlags pA, cMailboxFlags pB, fMailboxFlagSets pFlagSets, fMailboxFlags pFlags)
        {
            if ((pA.FlagSets & pFlagSets) != (pB.FlagSets & pFlagSets)) return true;
            if ((pA.FlagSets & pFlagSets) != pFlagSets) return false;
            if ((pA.Flags & pFlags) == (pB.Flags & pFlags)) return false;
            return true;
        }

        public static bool operator ==(cMailboxFlags pA, cMailboxFlags pB) => Differences(pA, pB) == 0;
        public static bool operator !=(cMailboxFlags pA, cMailboxFlags pB) => Differences(pA, pB) != 0;
    }
}