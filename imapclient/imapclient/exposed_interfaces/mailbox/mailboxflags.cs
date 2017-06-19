using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxFlags
    {
        rfc3501 = 1, // if set it means that the rfc3501 flags have been set (noinferiors, noselect) - this will be false if the mailbox flags haven't been set or if they've been set by LSUB
        noinferiors = 1 << 1, // rfc 3501, hasnochildren must be set if this is set
        haschildren = 1 << 2, // rfc 3348/ 5258
        hasnochildren = 1 << 3, // rfc 3348/ 5258
        nonexistent = 1 << 4, // rfc 5258, noselect must be set if this is set
        noselect = 1 << 5, // rfc 3501 \
        marked = 1 << 6, // rfc 3501    > only one of these may be true
        unmarked = 1 << 7, // rfc 3501 /
        subscribed = 1 << 8, // rfc 5258 
        notsubscribed = 1 << 9,
        hassubscribedchildren = 1 << 10, // derived from the LIST/LSUB replies OR the CHILDINFO response, haschildren must be set if this is set
        hasnosubscribedchildren = 1 << 11,
        local = 1 << 12, // set if the mailbox is returned by list, lsub, or list-extended and no /remote was received
        remote = 1 << 13, // set by list-extended ONLY - if an RLIST/RLSUB returns extra rows to the corresponding LIST/LSUB it can't be certainly remote because of timing issues
        // next 7 rfc 6154 (specialuse)
        allmessages = 1 << 14,
        archive = 1 << 15,
        drafts = 1 << 16,
        flagged = 1 << 17,
        junk = 1 << 18,
        sent = 1 << 19,
        trash = 1 << 20
    }

    public class cMailboxFlags
    {
        public readonly fMailboxFlags Flags;

        public cMailboxFlags(fMailboxFlags pFlags) { Flags = pFlags; }

        public bool? CanHaveChildren
        {
            get
            {
                if ((Flags & fMailboxFlags.rfc3501) != 0) return (Flags & fMailboxFlags.noinferiors) == 0;
                return null;
            }
        }

        public bool? HasChildren
        {
            get
            {
                fMailboxFlags lFlags = Flags & (fMailboxFlags.haschildren | fMailboxFlags.hasnochildren);
                if (lFlags == fMailboxFlags.haschildren) return true;
                if (lFlags == fMailboxFlags.hasnochildren) return false;
                return null;
            }
        }

        // no accessor for non-existent because the noselect flag is implied by it, but there is no way to imply it without extended list

        public bool? CanSelect
        {
            get
            {
                if ((Flags & fMailboxFlags.rfc3501) != 0) return (Flags & fMailboxFlags.noselect) == 0;
                return null;
            }
        }

        public bool? IsMarked
        {
            get
            {
                fMailboxFlags lFlags = Flags & (fMailboxFlags.marked | fMailboxFlags.unmarked);
                if (lFlags == fMailboxFlags.marked) return true;
                if (lFlags == fMailboxFlags.unmarked) return false;
                return null;
            }
        }

        public bool? IsSubscribed
        {
            get
            {
                fMailboxFlags lFlags = Flags & (fMailboxFlags.subscribed | fMailboxFlags.notsubscribed);
                if (lFlags == fMailboxFlags.subscribed) return true;
                if (lFlags == fMailboxFlags.notsubscribed) return false;
                return null;
            }
        }

        public bool? HasSubscribedChildren
        {
            get
            {
                fMailboxFlags lFlags = Flags & (fMailboxFlags.hassubscribedchildren | fMailboxFlags.hasnosubscribedchildren);
                if (lFlags == fMailboxFlags.hassubscribedchildren) return true;
                if (lFlags == fMailboxFlags.hasnosubscribedchildren) return false;
                return null;
            }
        }

        public bool? IsRemote
        {
            get
            {
                fMailboxFlags lFlags = Flags & (fMailboxFlags.local | fMailboxFlags.remote);
                if (lFlags == fMailboxFlags.remote) return true;
                if (lFlags == fMailboxFlags.local) return false;
                return null;
            }
        }

        public bool ContainsAll => (Flags & fMailboxFlags.allmessages) != 0;
        public bool ContainsArchived => (Flags & fMailboxFlags.archive) != 0;
        public bool ContainsDrafts => (Flags & fMailboxFlags.drafts) != 0;
        public bool ContainsFlagged => (Flags & fMailboxFlags.flagged) != 0;
        public bool ContainsJunk => (Flags & fMailboxFlags.junk) != 0;
        public bool ContainsSent => (Flags & fMailboxFlags.sent) != 0;
        public bool ContainsTrash => (Flags & fMailboxFlags.trash) != 0;

        public override string ToString() => $"{nameof(cMailboxFlags)}({Flags})";
    }
}