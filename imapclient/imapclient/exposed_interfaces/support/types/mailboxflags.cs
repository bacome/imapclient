﻿using System;
using System.Threading;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fMailboxFlags
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

    public class cMailboxFlags
    {
        private static int mLastSequence = 0;    

        public readonly int Sequence;
        private readonly fMailboxFlags mFlags;

        public cMailboxFlags(fMailboxFlags pFlags)
        {
            Sequence = Interlocked.Increment(ref mLastSequence);
            mFlags = pFlags;
        }

        public bool CanHaveChildren => (mFlags & fMailboxFlags.noinferiors) == 0;
        public bool CanSelect => (mFlags & fMailboxFlags.noselect) == 0;

        public bool? IsMarked
        {
            get
            {
                fMailboxFlags lFlags = mFlags & (fMailboxFlags.marked | fMailboxFlags.unmarked);
                if (lFlags == fMailboxFlags.marked) return true;
                if (lFlags == fMailboxFlags.unmarked) return false;
                return null;
            }
        }

        public bool NonExistent => (mFlags & fMailboxFlags.nonexistent) != 0;
        public bool IsSubscribed => (mFlags & fMailboxFlags.subscribed) != 0;
        public bool IsRemote => (mFlags & fMailboxFlags.remote) != 0;

        public bool? HasChildren
        {
            get
            {
                fMailboxFlags lFlags = mFlags & (fMailboxFlags.haschildren | fMailboxFlags.hasnochildren);
                if (lFlags == fMailboxFlags.haschildren) return true;
                if (lFlags == fMailboxFlags.hasnochildren) return false;
                return null;
            }
        }

        public bool HasSubscribedChildren => (mFlags & fMailboxFlags.hassubscribedchildren) != 0;

        public bool ContainsAll => (mFlags & fMailboxFlags.all) != 0;
        public bool IsArchive => (mFlags & fMailboxFlags.archive) != 0;
        public bool ContainsDrafts => (mFlags & fMailboxFlags.drafts) != 0;
        public bool ContainsFlagged => (mFlags & fMailboxFlags.flagged) != 0;
        public bool ContainsJunk => (mFlags & fMailboxFlags.junk) != 0;
        public bool ContainsSent => (mFlags & fMailboxFlags.sent) != 0;
        public bool ContainsTrash => (mFlags & fMailboxFlags.trash) != 0;

        public cMailboxFlags Merge(cLSubFlags pLSubFlags)
        {
            if (pLSubFlags == null) return this;
            return new cMailboxFlags(mFlags & cLSubFlags.ClearFlagsMask | pLSubFlags.Flags);
        }

        public override string ToString() => $"{nameof(cMailboxFlags)}({Sequence},{mFlags})";

        public static int LastSequence = mLastSequence;

        public static fMailboxProperties Differences(cMailboxFlags pOld, cMailboxFlags pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));
        
            if (pOld == null) return 0;
            if (pOld.mFlags == pNew.mFlags) return 0;

            fMailboxProperties lProperties = 0;

            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.noinferiors, fMailboxProperties.canhavechildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.noselect, fMailboxProperties.canselect);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.marked | fMailboxFlags.unmarked, fMailboxProperties.ismarked);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.nonexistent, fMailboxProperties.nonexistent);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.subscribed, fMailboxProperties.issubscribed);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.remote, fMailboxProperties.isremote);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.haschildren | fMailboxFlags.hasnochildren, fMailboxProperties.haschildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.hassubscribedchildren, fMailboxProperties.hassubscribedchildren);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.all, fMailboxProperties.containsall);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.archive, fMailboxProperties.isarchive);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.drafts, fMailboxProperties.containsdrafts);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.flagged, fMailboxProperties.containsflagged);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.junk, fMailboxProperties.containsjunk);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.sent, fMailboxProperties.containssent);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, fMailboxFlags.trash, fMailboxProperties.containstrash);

            if (lProperties != 0) lProperties |= fMailboxProperties.mailboxflags;

            return lProperties;
        }

        private static fMailboxProperties ZPropertyIfDifferent(cMailboxFlags pA, cMailboxFlags pB, fMailboxFlags pFlags, fMailboxProperties pProperty)
        {
            if ((pA.mFlags & pFlags) == (pB.mFlags & pFlags)) return 0;
            return pProperty;
        }
    } 
}