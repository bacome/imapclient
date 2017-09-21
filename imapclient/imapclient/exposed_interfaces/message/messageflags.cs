using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iMessageFlags : IReadOnlyCollection<string>
    {
        bool IsAnswered { get; }
        bool IsFlagged { get; }
        bool IsDeleted { get; }
        bool IsSeen { get; }
        bool IsDraft { get; }
        bool IsRecent { get; }
        bool IsMDNSent { get; }
        bool IsForwarded { get; }
        bool IsSubmitPending { get; }
        bool IsSubmitted { get; }
    }

    public abstract class cMessageFlagsBase 
    {
        public const string Asterisk = "\\*";
        public const string Answered = "\\AnSwErEd";
        public const string Flagged = "\\FlAgGeD";
        public const string Deleted = "\\DeLeTeD";
        public const string Seen = "\\SeEn";
        public const string Draft = "\\DrAfT";
        public const string Recent = "\\ReCeNt";
        public const string MDNSent = "$MdNsEnT";
        public const string Forwarded = "$FoRwArDeD";
        public const string SubmitPending = "$SuBmItPeNdInG";
        public const string Submitted = "$SuBmItTeD";
    }

    public abstract class cMessageFlagsBuilderBase
    { 

        private readonly bool mAllowRecent;

        ;?; // note that the spec doesn't say that falgs are case 


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

        public int Count => mDictionary.Count;
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

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
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

    [Flags]
    public enum fMessageFlags
    {
        // rfc 3501
        asterisk = 1 << 0,
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

    public class cMessageFlags : cStrings
    {

        public readonly fMessageFlags Flags;

        public cMessageFlags(IEnumerable<string> pFlags) : base(ZCtor(pFlags))
        {
            Flags = 0;

            if (Contains(Asterisk)) Flags |= fMessageFlags.asterisk;

            ;?; // all case ins
            if (Contains(Answered)) Flags |= fMessageFlags.answered;
            if (Contains(Flagged)) Flags |= fMessageFlags.flagged;
            if (Contains(Deleted)) Flags |= fMessageFlags.deleted;
            if (Contains(Seen)) Flags |= fMessageFlags.seen;
            if (Contains(Draft)) Flags |= fMessageFlags.draft;

            if (Contains(Recent)) Flags |= fMessageFlags.recent;

            if (Contains(MDNSent)) Flags |= fMessageFlags.mdnsent;
            if (Contains(Forwarded)) Flags |= fMessageFlags.forwarded;
            if (Contains(SubmitPending)) Flags |= fMessageFlags.submitpending;
            if (Contains(Submitted)) Flags |= fMessageFlags.submitted;
        }

        private static List<string> ZCtor(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            List<string> lFlags = new List<string>();
            ;?;
            foreach (var lFlag in pFlags) if (lFlag != null) lFlags.Add(lFlag.ToUpperInvariant());
            lFlags.Sort(); // case insensitive sort?
            return lFlags;
        }

        /*
        public bool Contain(params string[] pFlags) => ZContain(pFlags);
        public bool Contain(IEnumerable<string> pFlags) => ZContain(pFlags);

        private bool ZContain(IEnumerable<string> pFlags)
        {
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) return false;
            return true;
        } */

        public bool ContainsCreateNewPossible => (Flags & fMessageFlags.asterisk) != 0;

        public bool ContainsAnswered => (Flags & fMessageFlags.answered) != 0;
        public bool ContainsFlagged => (Flags & fMessageFlags.flagged) != 0;
        public bool ContainsDeleted => (Flags & fMessageFlags.deleted) != 0;
        public bool ContainsSeen => (Flags & fMessageFlags.seen) != 0;
        public bool ContainsDraft => (Flags & fMessageFlags.draft) != 0;

        public bool ContainsRecent => (Flags & fMessageFlags.recent) != 0;

        public bool ContainsMDNSent => (Flags & fMessageFlags.mdnsent) != 0;
        public bool ContainsForwarded => (Flags & fMessageFlags.forwarded) != 0;
        public bool ContainsSubmitPending => (Flags & fMessageFlags.submitpending) != 0;
        public bool ContainsSubmitted => (Flags & fMessageFlags.submitted) != 0;

        public override bool Equals(object pObject) => (cStrings)this == pObject as cMessageFlags;
        public override int GetHashCode() => base.GetHashCode();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlags));
            foreach (var lFlag in this) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }

        ;??; // note: the system flags need to be compared case insensitive
        public static bool operator ==(cMessageFlags pA, cMessageFlags pB) => (cStrings)pA == pB;
        public static bool operator !=(cMessageFlags pA, cMessageFlags pB) => (cStrings)pA != pB;
    }
}
