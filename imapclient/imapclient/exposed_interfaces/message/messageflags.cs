using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cMessageFlagsBase : IEnumerable<string>
    {
        private readonly bool mAllowRecent;
        private readonly Dictionary<string, bool> mDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public cMessageFlagsBase(bool pAllowRecent)
        {
            mAllowRecent = pAllowRecent;
        }

        public cMessageFlagsBase(bool pAllowRecent, IEnumerable<string> pFlags)
        {
            mAllowRecent = pAllowRecent;
            ZAdd(pFlags);
        }

        public void Add(params string[] pFlags) => ZAdd(pFlags);
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        public void Remove(params string[] pFlags)
        {
            foreach (var lFlag in pFlags) mDictionary.Remove(lFlag);
        }

        public bool Contain(params string[] pFlags) => ZContain(pFlags);
        public bool Contain(IEnumerable<string> pFlags) => ZContain(pFlags);

        public bool IsAnswered
        {
            get => mDictionary.ContainsKey(cMessageFlags.Answered);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.Answered)) mDictionary.Add(cMessageFlags.Answered, true); }
                else mDictionary.Remove(cMessageFlags.Answered);
            }
        }

        public bool IsFlagged
        {
            get => mDictionary.ContainsKey(cMessageFlags.Flagged);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.Flagged)) mDictionary.Add(cMessageFlags.Flagged, true); }
                else mDictionary.Remove(cMessageFlags.Flagged);
            }
        }

        public bool IsDeleted
        {
            get => mDictionary.ContainsKey(cMessageFlags.Deleted);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.Deleted)) mDictionary.Add(cMessageFlags.Deleted, true); }
                else mDictionary.Remove(cMessageFlags.Deleted);
            }
        }

        public bool IsSeen
        {
            get => mDictionary.ContainsKey(cMessageFlags.Seen);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.Seen)) mDictionary.Add(cMessageFlags.Seen, true); }
                else mDictionary.Remove(cMessageFlags.Seen);
            }
        }

        public bool IsDraft
        {
            get => mDictionary.ContainsKey(cMessageFlags.Draft);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.Draft)) mDictionary.Add(cMessageFlags.Draft, true); }
                else mDictionary.Remove(cMessageFlags.Draft);
            }
        }

        public bool ContainsRecent => mDictionary.ContainsKey(cMessageFlags.Recent);

        public bool IsMDNSent
        {
            get => mDictionary.ContainsKey(cMessageFlags.MDNSent);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.MDNSent)) mDictionary.Add(cMessageFlags.MDNSent, true); }
                else mDictionary.Remove(cMessageFlags.MDNSent);
            }
        }

        public bool IsForwarded
        {
            get => mDictionary.ContainsKey(cMessageFlags.Forwarded);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.Forwarded)) mDictionary.Add(cMessageFlags.Forwarded, true); }
                else mDictionary.Remove(cMessageFlags.Forwarded);
            }
        }

        public bool IsSubmitPending
        {
            get => mDictionary.ContainsKey(cMessageFlags.SubmitPending);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.SubmitPending)) mDictionary.Add(cMessageFlags.SubmitPending, true); }
                else mDictionary.Remove(cMessageFlags.SubmitPending);
            }
        }

        public bool IsSubmitted
        {
            get => mDictionary.ContainsKey(cMessageFlags.Submitted);

            set
            {
                if (value) { if (!mDictionary.ContainsKey(cMessageFlags.Submitted)) mDictionary.Add(cMessageFlags.Submitted, true); }
                else mDictionary.Remove(cMessageFlags.Submitted);
            }
        }

        public IEnumerator<string> GetEnumerator() => mDictionary.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.Keys.GetEnumerator();

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) return;
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mDictionary.ContainsKey(lFlag)) mDictionary.Add(lFlag, true);
        }

        private bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag.Equals(cMessageFlags.Recent, StringComparison.InvariantCultureIgnoreCase)) return mAllowRecent;

            string lFlag;

            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPart.TryAsAtom(lFlag, out _);
        }

        private bool ZContain(IEnumerable<string> pFlags)
        {
            foreach (var lFlag in pFlags) if (!mDictionary.ContainsKey(lFlag)) return false;
            return true;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlagsBase));
            foreach (var lFlag in mDictionary.Keys) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }
    }

    public class cFetchableFlags : cMessageFlagsBase
    {
        public cFetchableFlags() : base(true) { }
        public cFetchableFlags(IEnumerable<string> pFlags) : base(true, pFlags) { }
        public cFetchableFlags(params string[] pFlags) : base(true, pFlags) { }

        public bool IsRecent
        {
            get => ContainsRecent;

            set
            {
                if (value) Add(cMessageFlags.Recent);
                else Remove(cMessageFlags.Recent);
            }
        }
    }

    public class cSettableFlags : cMessageFlagsBase
    {
        public cSettableFlags() : base(false) { }
        public cSettableFlags(IEnumerable<string> pFlags) : base(false, pFlags) { }
        public cSettableFlags(params string[] pFlags) : base(false, pFlags) { }
    }

    public class cMessageFlags : cStrings, IEnumerable<string>
    {
        public const string Asterisk = "\\*";
        public const string Answered = "\\ANSWERED";
        public const string Flagged = "\\FLAGGED";
        public const string Deleted = "\\DELETED";
        public const string Seen = "\\SEEN";
        public const string Draft = "\\DRAFT";
        public const string Recent = "\\RECENT";
        public const string MDNSent = "$MDNSENT";
        public const string Forwarded = "$FORWARDED";
        public const string SubmitPending = "$SUBMITPENDING";
        public const string Submitted = "$SUBMITTED";

        public readonly fKnownFlags KnownFlags;

        public cMessageFlags(IEnumerable<string> pFlags) : base(ZCtor(pFlags))
        {
            KnownFlags = 0;

            if (Contains(Asterisk)) KnownFlags |= fKnownFlags.asterisk;

            if (Contains(Answered)) KnownFlags |= fKnownFlags.answered;
            if (Contains(Flagged)) KnownFlags |= fKnownFlags.flagged;
            if (Contains(Deleted)) KnownFlags |= fKnownFlags.deleted;
            if (Contains(Seen)) KnownFlags |= fKnownFlags.seen;
            if (Contains(Draft)) KnownFlags |= fKnownFlags.draft;

            if (Contains(Recent)) KnownFlags |= fKnownFlags.recent;

            if (Contains(MDNSent)) KnownFlags |= fKnownFlags.mdnsent;
            if (Contains(Forwarded)) KnownFlags |= fKnownFlags.forwarded;
            if (Contains(SubmitPending)) KnownFlags |= fKnownFlags.submitpending;
            if (Contains(Submitted)) KnownFlags |= fKnownFlags.submitted;
        }

        private static List<string> ZCtor(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            List<string> lFlags = new List<string>();
            foreach (var lFlag in pFlags) if (lFlag != null) lFlags.Add(lFlag.ToUpperInvariant());
            lFlags.Sort();
            return lFlags;
        }

        public bool Contain(params string[] pFlags) => ZContain(pFlags);
        public bool Contain(IEnumerable<string> pFlags) => ZContain(pFlags);

        private bool ZContain(IEnumerable<string> pFlags)
        {
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) return false;
            return true;
        }

        public bool ContainsCreateNewPossible => (KnownFlags & fKnownFlags.asterisk) != 0;

        public bool ContainsAnswered => (KnownFlags & fKnownFlags.answered) != 0;
        public bool ContainsFlagged => (KnownFlags & fKnownFlags.flagged) != 0;
        public bool ContainsDeleted => (KnownFlags & fKnownFlags.deleted) != 0;
        public bool ContainsSeen => (KnownFlags & fKnownFlags.seen) != 0;
        public bool ContainsDraft => (KnownFlags & fKnownFlags.draft) != 0;

        public bool ContainsRecent => (KnownFlags & fKnownFlags.recent) != 0;

        public bool ContainsMDNSent => (KnownFlags & fKnownFlags.mdnsent) != 0;
        public bool ContainsForwarded => (KnownFlags & fKnownFlags.forwarded) != 0;
        public bool ContainsSubmitPending => (KnownFlags & fKnownFlags.submitpending) != 0;
        public bool ContainsSubmitted => (KnownFlags & fKnownFlags.submitted) != 0;

        public override bool Equals(object pObject) => (cStrings)this == pObject as cMessageFlags;
        public override int GetHashCode() => base.GetHashCode();
        public static bool operator ==(cMessageFlags pA, cMessageFlags pB) => (cStrings)pA == pB;
        public static bool operator !=(cMessageFlags pA, cMessageFlags pB) => (cStrings)pA != pB;

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlags));
            foreach (var lFlag in this) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }
    }
}
