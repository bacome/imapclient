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

        // they are 'is' here because they ae settable

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

        public bool ContainsCreateNewPossible => false;

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
        public const string Answered = "\\ANSWERED";
        public const string Flagged = "\\FLAGGED";
        public const string Deleted = "\\DELETED";
        public const string Seen = "\\SEEN";
        public const string Draft = "\\DRAFT";
        public const string Recent = "\\RECENT";
        public const string CreateNewPossible = "\\*";
        public const string MDNSent = "$MDNSENT";
        public const string Forwarded = "$FORWARDED";
        public const string SubmitPending = "$SUBMITPENDING";
        public const string Submitted = "$SUBMITTED";

        public cMessageFlags(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { }

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

        public bool ContainsAnswered => Contains(Answered);
        public bool ContainsFlagged => Contains(Flagged);
        public bool ContainsDeleted => Contains(Deleted);
        public bool ContainsSeen => Contains(Seen);
        public bool ContainsDraft => Contains(Draft);
        public bool ContainsRecent => Contains(Recent);

        public bool ContainsCreateNewPossible => Contains(CreateNewPossible);

        public bool ContainsMDNSent => Contains(MDNSent);
        public bool ContainsForwarded => Contains(Forwarded);
        public bool ContainsSubmitPending => Contains(SubmitPending);
        public bool ContainsSubmitted => Contains(Submitted);

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







    /*
    [Flags]
    public enum fMessageFlags
    {
        // rfc 3501
        asterisk = 1,
        answered = 1 << 1,
        flagged = 1 << 2,
        deleted = 1 << 3,
        seen = 1 << 4,
        draft = 1 << 5,
        recent = 1 << 6,

        // rfc 5788
        mdnsent = 1 << 7, // 3503
        forwarded = 1 << 8, // 5550
        submitpending = 1 << 9, // 5550
        submitted = 1 << 10, // 5550

        allsettableflags = 0b11111111110
    } 

    public class cMessageFlags
    {
        public readonly fMessageFlags KnownFlags;
        public readonly cStrings AllFlags; // sorted, upppercased

        public cMessageFlags(cFlags pFlags)
        {
            KnownFlags = 0;

            if (pFlags.Has(@"\*")) KnownFlags |= fMessageFlags.asterisk;

            if (pFlags.Has(@"\answered")) KnownFlags |= fMessageFlags.answered;
            if (pFlags.Has(@"\flagged")) KnownFlags |= fMessageFlags.flagged;
            if (pFlags.Has(@"\deleted")) KnownFlags |= fMessageFlags.deleted;
            if (pFlags.Has(@"\seen")) KnownFlags |= fMessageFlags.seen;
            if (pFlags.Has(@"\draft")) KnownFlags |= fMessageFlags.draft;
            if (pFlags.Has(@"\recent")) KnownFlags |= fMessageFlags.recent;

            if (pFlags.Has("$mdnsent")) KnownFlags |= fMessageFlags.mdnsent;
            if (pFlags.Has("$forwarded")) KnownFlags |= fMessageFlags.forwarded;
            if (pFlags.Has("$submitpending")) KnownFlags |= fMessageFlags.submitpending;
            if (pFlags.Has("$submitted")) KnownFlags |= fMessageFlags.submitted;

            AllFlags = new cStrings(pFlags.ToSortedUpperList());
        }

        public bool CanCreateNewKeywords => (KnownFlags & fMessageFlags.asterisk) != 0;

        public bool Answered => (KnownFlags & fMessageFlags.answered) != 0;
        public bool Flagged => (KnownFlags & fMessageFlags.flagged) != 0;
        public bool Deleted => (KnownFlags & fMessageFlags.deleted) != 0;
        public bool Seen => (KnownFlags & fMessageFlags.seen) != 0;
        public bool Draft => (KnownFlags & fMessageFlags.draft) != 0;
        public bool Recent => (KnownFlags & fMessageFlags.recent) != 0;

        public bool MDNSent => (KnownFlags & fMessageFlags.mdnsent) != 0;
        public bool Forwarded => (KnownFlags & fMessageFlags.forwarded) != 0;
        public bool SubmitPending => (KnownFlags & fMessageFlags.submitpending) != 0;
        public bool Submitted => (KnownFlags & fMessageFlags.submitted) != 0;

        public bool Has(string pFlag) => AllFlags.Contains(pFlag.ToUpperInvariant());

        public override bool Equals(object pObject) => this == pObject as cMessageFlags;

        public override int GetHashCode() => AllFlags.GetHashCode();

        public override string ToString() => $"{nameof(cMessageFlags)}({KnownFlags},{AllFlags})";

        public static bool operator ==(cMessageFlags pA, cMessageFlags pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.KnownFlags == pB.KnownFlags && pA.AllFlags == pB.AllFlags);
        }

        public static bool operator !=(cMessageFlags pA, cMessageFlags pB) => !(pA == pB);
    } */
}
