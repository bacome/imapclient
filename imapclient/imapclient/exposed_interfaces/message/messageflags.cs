using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public static class kMessageFlagName
    {
        public static readonly StringComparer Comparer = new cComparer();

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

        private class cComparer : StringComparer
        {
            // add more to this list as they become known
            private static readonly string[] kCaseInsensitiveKeywords = new string[] { kMessageFlagName.MDNSent };

            public cComparer() { }

            public override int Compare(string pA, string pB) => ZConditionalToUpperInvariant(pA).CompareTo(ZConditionalToUpperInvariant(pB));
            public override int GetHashCode(string pFlag) => ZConditionalToUpperInvariant(pFlag).GetHashCode();
            public override bool Equals(string pA, string pB) => ZConditionalToUpperInvariant(pA).Equals(ZConditionalToUpperInvariant(pB));

            private string ZConditionalToUpperInvariant(string pString)
            {
                if (pString != null && pString.Length != 0 && (pString[0] == '\\' || kCaseInsensitiveKeywords.Contains(pString, StringComparer.InvariantCultureIgnoreCase))) return pString.ToUpperInvariant();
                return pString;
            }
        }
    }

    public abstract class cMessageFlags : IReadOnlyCollection<string>
    {
        private readonly cMessageFlagList mFlags;

        public cMessageFlags(cMessageFlagList pFlags) => mFlags = pFlags;

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

        public cSettableFlags(params string[] pFlags) : base(new cSettableFlagList(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlags(IEnumerable<string> pFlags) : base(new cSettableFlagList(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlags(cSettableFlagList pFlags) : base(new cSettableFlagList(pFlags)) { } // duplicates

        public static implicit operator cSettableFlags(cSettableFlagList pFlags) => new cSettableFlags(pFlags);
    }

    public class cFetchableFlags : cMessageFlags
    {
        // immutable (for passing in and out)

        public cFetchableFlags(params string[] pFlags) : base(new cFetchableFlagList(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlags(IEnumerable<string> pFlags) : base(new cFetchableFlagList(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlags(cFetchableFlagList pFlags) : base(new cFetchableFlagList(pFlags)) { } // duplicates
        private cFetchableFlags(cFetchableFlagList pFlags, bool pWrap) : base(pFlags) { } // wraps

        public static implicit operator cFetchableFlags(cFetchableFlagList pFlags) => new cFetchableFlags(pFlags);

        public static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlags rFlags)
        {
            if (!cFetchableFlagList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cFetchableFlags(lFlags, true);
            return true;
        }
    }

    public class cPermanentFlags : cMessageFlags
    {
        // read only wrapper (for passing out)

        private cPermanentFlags(cPermanentFlagList pFlags) : base(pFlags) { }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlags rFlags)
        {
            if (!cPermanentFlagList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cPermanentFlags(lFlags);
            return true;
        }
    }

    public abstract class cMessageFlagList : IReadOnlyCollection<string>
    {
        // implements case insensitivity (note that the specs do NOT say that keywords are case insensitive OTHER than the spec for MDNSent) via the Comparer
        //  implements uniqueness (via mutation, not via construct)
        //  implements validity (via mutation, not via construct)


        private readonly List<string> mFlags;

        public cMessageFlagList(List<string> pFlags)
        {
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        public bool Contains(string pFlag) => mFlags.Contains(pFlag, kMessageFlagName.Comparer);
        public bool Contains(params string[] pFlags) => ZContains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, kMessageFlagName.Comparer)) return false;
            return true;
        }

        public void Add(string pFlag)
        {
            if (!YIsValidFlag(pFlag)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            if (!mFlags.Contains(pFlag, kMessageFlagName.Comparer)) mFlags.Add(pFlag);
        }

        public void Add(params string[] pFlags) => ZAdd(pFlags);
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, kMessageFlagName.Comparer)) mFlags.Add(lFlag);
        }

        public void Remove(string pFlag) => mFlags.RemoveAll(f => Comparer.Equals(f, pFlag));
        public void Remove(params string[] pFlags) => ZRemove(pFlags);
        public void Remove(IEnumerable<string> pFlags) => ZRemove(pFlags);

        private void ZRemove(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) mFlags.RemoveAll(f => Comparer.Equals(f, lFlag));
        }

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        protected abstract bool YIsValidFlag(string pFlag);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlagList));
            foreach (var lFlag in mFlags) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }
    }

    public class cSettableFlagList : cMessageFlagList
    {
        public cSettableFlagList() : base(new List<string>()) { }
        public cSettableFlagList(params string[] pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlagList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlagList(cSettableFlagList pFlags) : base(new List<string>(pFlags)) { } // duplicates

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag.Equals(kMessageFlagName.Recent, StringComparison.InvariantCultureIgnoreCase)) return false;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        private static List<string> ZCtor(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            return new List<string>(pFlags.Distinct(kMessageFlagName.Comparer));
        }

        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            bool lFailed;

            var lFlags = new cSettableFlagList();

            lFlags.Add("a");
            lFlags.Add("b");
            lFlags.Add(kMessageFlagName.Answered, kMessageFlagName.Deleted);
            lFlags.Add(kMessageFlagName.Answered, kMessageFlagName.Deleted, kMessageFlagName.Forwarded);
            lFlags.Add(@"\answereD");
            lFlags.Add(@"\ansWereD", "A", @"\deleteD");

            lFailed = false;
            try { lFlags.Add("fr ed"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cSettableFlagList)}.1");

            lFailed = false;
            try { lFlags.Add(kMessageFlagName.Answered, kMessageFlagName.Deleted, kMessageFlagName.Draft, kMessageFlagName.Recent); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cSettableFlagList)}.1");

            if (lFlags.Count != 6) throw new cTestsException($"{nameof(cSettableFlagList)}.2");
            if (!lFlags.Contains("A") || lFlags.Contains("B") || !lFlags.Contains(@"\aNswereD") || lFlags.Contains(kMessageFlagName.Draft) || lFlags.Contains("$forwarded")) throw new cTestsException($"{nameof(cSettableFlagList)}.3");

            cSettableFlags lF1 = new cSettableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlagName.Forwarded);
            cFetchableFlags lF2 = new cFetchableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlagName.Recent);
            cSettableFlags lF3 = new cSettableFlags("a", "b", "\\deleted", kMessageFlagName.Forwarded);

            if (!lFlags.Contains(lF1) || lFlags.Contains(lF2) || !lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cSettableFlagList)}.4");

            lFlags.Remove("A");
            if (lFlags.Count != 5 || lFlags.Contains(lF1) || !lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cSettableFlagList)}.5");

            lFlags.Remove("B", "$forwarded", @"\answered");
            if (lFlags.Count != 4 || !lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cSettableFlagList)}.6");
        }
    }

    public class cFetchableFlagList : cMessageFlagList
    {
        public cFetchableFlagList() : base(new List<string>()) { }
        public cFetchableFlagList(params string[] pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlagList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cFetchableFlagList(cFetchableFlagList pFlags) : base(new List<string>(pFlags)) { } // duplicates
        private cFetchableFlagList(List<string> pFlags) : base(pFlags) { } // wraps

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
            return new List<string>(pFlags.Distinct(kMessageFlagName.Comparer));
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlagList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cFetchableFlagList(new List<string>(pFlags.Distinct(kMessageFlagName.Comparer)));
            return true;
        }
    }

    public class cPermanentFlagList : cMessageFlagList
    {
        private cPermanentFlagList(List<string> pFlags) : base(pFlags) { } // wraps

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag == kMessageFlagName.CreateNewIsPossible) return true;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlagList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cPermanentFlagList(new List<string>(pFlags.Distinct(kMessageFlagName.Comparer)));
            return true;
        }
    }
}
