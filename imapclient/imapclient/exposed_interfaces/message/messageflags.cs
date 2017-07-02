using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMessageFlags
    {
        private const string kRecent = "RECENT";

        private readonly Dictionary<string, bool> mDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public cMessageFlags() { }

        public virtual bool Has(string pFlag) => mDictionary.ContainsKey(pFlag);

        public virtual void Set(string pFlag)
        {
            if (pFlag == null) throw new ArgumentNullException(nameof(pFlag));
            if (pFlag.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFlag));

            if (mDictionary.ContainsKey(pFlag)) return;

            if (pFlag[0] == '\\')
            {
                var lFlag = pFlag.Remove(0, 1).ToUpperInvariant();
                if (lFlag == kRecent) throw new ArgumentOutOfRangeException(nameof(pFlag));
                if (!cCommandPart.TryAsAtom(pFlag.Remove(0, 1), out _)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            }
            else if (!cCommandPart.TryAsAtom(pFlag, out _)) throw new ArgumentOutOfRangeException(nameof(pFlag));

            mDictionary.Add(pFlag, true);
        }

        ;?; // special set accessors

        public List<string> ToSortedUpperList()
        {
            List<string> lFlags = new List<string>();
            foreach (string lFlag in mDictionary.Keys) lFlags.Add(lFlag.ToUpperInvariant());
            lFlags.Sort();
            return lFlags;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlags));
            foreach (var lFlag in mDictionary.Keys) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }
    }

    public class cReadOnlyMessageFlags : cStrings
    {


        public cReadOnlyMessageFlags(cMessageFlags pFlags) : base(pFlags.ToSortedUpperList()) { }

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

    }

    public class cPermanentFlags : cMessageFlags
    {
        private const string kAllowsCreateNew = "\\*";

        public bool AllowsCreateNew { get; private set; } = false;

        public override bool Has(string pFlag)
        {
            if (pFlag == kAllowsCreateNew) return AllowsCreateNew;
            else return base.Has(pFlag);
        }

        public override void Set(string pFlag)
        {
            if (pFlag == kAllowsCreateNew) AllowsCreateNew = true;
            else base.Set(pFlag);
        }

        public override string ToString() => $"{nameof(cPermanentFlags)}({AllowsCreateNew},{base.ToString()})";
    }

    public class cFetchFlags : cMessageFlags
    {
        private const string kRecent = "\\RECENT";

        public bool IsRecent { get; private set; } = false;

        public override bool Has(string pFlag)
        {
            if (pFlag == kRecent) return IsRecent;
            else return base.Has(pFlag);
        }

        public override void Set(string pFlag)
        {
            if (pFlag == kRecent) IsRecent = true;
            else base.Set(pFlag);
        }

        public override string ToString() => $"{nameof(cFetchFlags)}({IsRecent},{base.ToString()})";
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
