using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public static class kFlagName
    {
        public const string CreateNewIsPossible = @"\*";
        public const string Recent = @"\ReCeNt";

        public const string Answered = @"\AnSwErEd";
        public const string Flagged = @"\FlAgGeD";
        public const string Deleted = @"\DeLeTeD";
        public const string Seen = @"\SeEn";
        public const string Draft = @"\DrAfT";

        // rfc 5788/ 5550
        public const string Forwarded = "$Forwarded";
        public const string SubmitPending = "$SubmitPending";
        public const string Submitted = "$Submitted";

        // rfc 3503/ 5550
        public const string MDNSent = "$MdNsEnT";
    }

    public abstract class cMessageFlags : IReadOnlyCollection<string>
    {
        private readonly cMessageFlagsList mFlags;

        public cMessageFlags(cMessageFlagsList pFlags) => mFlags = pFlags;

        public bool Contains(string pFlag) => mFlags.Contains(pFlag);
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString() => mFlags.ToString();
    }

    public class cSettableFlags : cMessageFlags
    {
        // immutable (for passing in)

        public cSettableFlags(params string[] pFlags) : base(new cSettableFlagsList(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlags(IEnumerable<string> pFlags) : base(new cSettableFlagsList(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlags(cSettableFlagsList pFlags) : base(new cSettableFlagsList(pFlags)) { } // duplicates

        public static implicit operator cSettableFlags(cSettableFlagsList pFlags) => new cSettableFlags(pFlags);
    }

    ;?; // create flag comparer and use that instead of the invariant...

    ;?; // TESTS

    public class cFetchableFlags : cMessageFlags
    {
        // immutable (for passing in and out)

        public cFetchableFlags(params string[] pFlags) : base(new cFetchableFlagsList(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlags(IEnumerable<string> pFlags) : base(new cFetchableFlagsList(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlags(cFetchableFlagsList pFlags) : base(new cFetchableFlagsList(pFlags)) { } // duplicates
        private cFetchableFlags(cFetchableFlagsList pFlags, bool pWrap) : base(pFlags) { } // wraps

        ;?; // remove it does a string difference not a flag difference
        public IEnumerable<string> SymmetricDifference(cFetchableFlags pOther) => this.Except(pOther, StringComparer.InvariantCultureIgnoreCase).Union(pOther.Except(this, StringComparer.InvariantCultureIgnoreCase), StringComparer.InvariantCultureIgnoreCase);

        public static implicit operator cFetchableFlags(cFetchableFlagsList pFlags) => new cFetchableFlags(pFlags);

        public static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlags rFlags)
        {
            if (!cFetchableFlagsList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cFetchableFlags(lFlags, true);
            return true;
        }
    }

    public class cPermanentFlags : cMessageFlags
    {
        // read only wrapper (for passing out)

        private cPermanentFlags(cPermanentFlagsList pFlags) : base(pFlags) { }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlags rFlags)
        {
            if (!cPermanentFlagsList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cPermanentFlags(lFlags);
            return true;
        }
    }

    public abstract class cMessageFlagsList : IReadOnlyCollection<string>
    {
        // implements case insensitivity (note that the specs do NOT say that keywords are case insensitive OTHER than the spec for MDNSent)
        //  implements uniqueness (via mutation, not via construct)
        //  implements validity (via mutation, not via construct)

        // add more as they become known
        private static readonly string[] kCaseInsensitiveFlags = new string[] { kFlagName.MDNSent };

        private readonly List<string> mFlags;

        public cMessageFlagsList(List<string> pFlags)
        {
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        public bool Contains(string pFlag) => ZContains(mFlags, pFlag);
        public bool Contains(params string[] pFlags) => ZContains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZContains(mFlags, lFlag)) return false;
            return true;
        }

        public void Add(string pFlag)
        {
            if (!YIsValidFlag(pFlag)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            if (!ZContains(mFlags, pFlag)) mFlags.Add(pFlag);
        }

        public void Add(params string[] pFlags) => ZAdd(pFlags);
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZContains(mFlags, lFlag)) mFlags.Add(lFlag);
        }

        public void Remove(string pFlag)
        {
            if (pFlag != null && pFlag.Length != 0 && (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase))) mFlags.RemoveAll(f => f.Equals(pFlag, StringComparison.InvariantCultureIgnoreCase));
            mFlags.Remove(pFlag);
        }

        public void Remove(params string[] pFlags) => ZRemove(pFlags);
        public void Remove(IEnumerable<string> pFlags) => ZRemove(pFlags);

        private void ZRemove(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) Remove(lFlag);
        }

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        protected abstract bool YIsValidFlag(string pFlag);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlagsList));
            foreach (var lFlag in mFlags) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }

        private static bool ZContains(List<string> pFlags, string pFlag)
        {
            if (pFlag != null && pFlag.Length != 0 && (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase))) return pFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase);
            return pFlags.Contains(pFlag);
        }

        protected static List<string> YDistinctFlagList(IEnumerable<string> pFlags)
        {
            var lFlags = new List<string>();
            foreach (string lFlag in pFlags) if (!ZContains(lFlags, lFlag)) lFlags.Add(lFlag);
            return lFlags;
        }
    }

    public class cSettableFlagsList : cMessageFlagsList
    {
        public cSettableFlagsList() : base(new List<string>()) { }
        public cSettableFlagsList(params string[] pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlagsList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlagsList(cSettableFlagsList pFlags) : base(new List<string>(pFlags)) { } // duplicates

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag.Equals(kFlagName.Recent, StringComparison.InvariantCultureIgnoreCase)) return false;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        private static List<string> ZCtor(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            return YDistinctFlagList(pFlags);
        }
    }

    public class cFetchableFlagsList : cMessageFlagsList
    {
        public cFetchableFlagsList() : base(new List<string>()) { }
        public cFetchableFlagsList(params string[] pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlagsList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlagsList(cFetchableFlagsList pFlags) : base(new List<string>(pFlags)) { } // duplicates
        private cFetchableFlagsList(List<string> pFlags) : base(pFlags) { } // wraps

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        private static List<string> ZCtor(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            return YDistinctFlagList(pFlags);
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlagsList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cFetchableFlagsList(YDistinctFlagList(pFlags));
            return true;
        }
    }

    public class cPermanentFlagsList : cMessageFlagsList
    {
        private cPermanentFlagsList(List<string> pFlags) : base(pFlags) { } // wraps

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag == kFlagName.CreateNewIsPossible) return true;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlagsList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cPermanentFlagsList(YDistinctFlagList(pFlags));
            return true;
        }
    }
}
