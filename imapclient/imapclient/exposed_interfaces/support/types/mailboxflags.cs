using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fMailboxFlags
    {
        noinferiors = 1 << 0, // rfc 3501, hasnochildren must be set if this is set
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
        private readonly fMailboxFlags mMask; // the flags that are correct
        private readonly fMailboxFlags mFlags; // the values of those flags

        public cMailboxFlags(fMailboxFlags pMask, fMailboxFlags pFlags)
        {
            mMask = pMask;
            mFlags = pFlags;
        }

        public bool HasFlagsFor(fMailboxProperties pProperties)
        {
            fMailboxFlags lFlags = cIMAPClient.MailboxPropertiesToMailboxFlags(pProperties);
            return (mMask & lFlags) == lFlags;
        }

        public bool CanHaveChildren
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.canhavechildren)) return false;
                return (mFlags & fMailboxFlags.noinferiors) == 0;
            }
        }

        public bool? HasChildren
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.haschildren)) return null;
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
                if (!HasFlagsFor(fMailboxProperties.canselect)) return false;
                return (mFlags & fMailboxFlags.noselect) == 0;
            }
        }

        public bool? IsMarked
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.ismarked)) return null;
                fMailboxFlags lFlags = mFlags & (fMailboxFlags.marked | fMailboxFlags.unmarked);
                if (lFlags == fMailboxFlags.marked) return true;
                if (lFlags == fMailboxFlags.unmarked) return false;
                return null;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.issubscribed)) return false;
                return (mFlags & fMailboxFlags.subscribed) != 0;
            }
        }

        public bool HasSubscribedChildren
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.hassubscribedchildren)) return false;
                return (mFlags & fMailboxFlags.hassubscribedchildren) != 0;
            }
        }

        public bool IsLocal
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.islocal)) return false;
                return (mFlags & fMailboxFlags.local) != 0;
            }
        }

        public bool ContainsAll
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.containsall)) return false;
                return (mFlags & fMailboxFlags.all) != 0;
            }
        }

        public bool IsArchive
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.isarchive)) return false;
                return (mFlags & fMailboxFlags.archive) != 0;
            }
        }

        public bool ContainsDrafts
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.containsdrafts)) return false;
                return (mFlags & fMailboxFlags.drafts) != 0;
            }
        }

        public bool ContainsFlagged
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.containsflagged)) return false;
                return (mFlags & fMailboxFlags.flagged) != 0;
            }
        }

        public bool ContainsJunk
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.containsjunk)) return false;
                return (mFlags & fMailboxFlags.junk) != 0;
            }
        }

        public bool ContainsSent
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.containssent)) return false;
                return (mFlags & fMailboxFlags.sent) != 0;
            }
        }

        public bool ContainsTrash
        {
            get
            {
                if (!HasFlagsFor(fMailboxProperties.containstrash)) return false;
                return (mFlags & fMailboxFlags.trash) != 0;
            }
        }

        public override bool Equals(object pObject) => this == pObject as cMailboxFlags;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + mMask.GetHashCode();
                lHash = lHash * 23 + mFlags.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cMailboxFlags)}({mMask},{mFlags})";

        public static cMailboxFlags Combine(cMailboxFlags pOld, cMailboxFlags pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));
            if (pOld == null) return pNew;
            fMailboxFlags lMask = pOld.mMask | pNew.mMask;
            fMailboxFlags lFlags = (pOld.mFlags & ~pNew.mMask) | pNew.mFlags;
            return new cMailboxFlags(lMask, lFlags);
        }

        public static fMailboxProperties Differences(cMailboxFlags pA, cMailboxFlags pB)
        {
            if (ReferenceEquals(pA, pB)) return 0;
            if (ReferenceEquals(pA, null)) return 0;
            if (ReferenceEquals(pB, null)) return 0;

            fMailboxFlags lMask = pA.mMask & pB.mMask;
            if (lMask == 0) return 0;

            if ((pA.mFlags & lMask) == (pB.mFlags & lMask)) return 0;

            fMailboxProperties lResult = 0;

            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.canhavechildren);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.haschildren);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.canselect);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.ismarked);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.issubscribed);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.hassubscribedchildren);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.islocal);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.containsall);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.isarchive);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.containsdrafts);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.containsflagged);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.containsjunk);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.containssent);
            lResult |= ZPropertyIfDifferent(pA, pB, lMask, fMailboxProperties.containstrash);

            return lResult;
        }

        private static fMailboxProperties ZPropertyIfDifferent(cMailboxFlags pA, cMailboxFlags pB, fMailboxFlags pMask, fMailboxProperties pProperty)
        {
            fMailboxFlags lMask = cIMAPClient.MailboxPropertiesToMailboxFlags(pProperty);
            if ((lMask & pMask) == 0) return 0;
            if ((pA.mFlags & lMask) != (pB.mFlags & lMask)) return pProperty;
            return 0;
        }

        public static bool operator ==(cMailboxFlags pA, cMailboxFlags pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.mMask == pB.mMask && pA.mFlags == pB.mFlags;
        }

        public static bool operator !=(cMailboxFlags pA, cMailboxFlags pB) => !(pA == pB);
    }
}