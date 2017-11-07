using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains named IMAP flag name contants.
    /// </summary>
    public static class kMessageFlagName
    {
        public const string CreateNewIsPossible = @"\*";
        public const string Recent = @"\Recent";

        public const string Answered = @"\Answered";
        public const string Flagged = @"\Flagged";
        public const string Deleted = @"\Deleted";
        public const string Seen = @"\Seen";
        public const string Draft = @"\Draft";

        // rfc 5788/ 5550
        public const string Forwarded = "$Forwarded";
        public const string SubmitPending = "$SubmitPending";
        public const string Submitted = "$Submitted";

        // rfc 3503/ 5550
        // see comments elsewhere as to why this is commented out
        //public const string MDNSent = "$MDNSent";

        /* according to Mark Crispin (2000-06-09 17:46:16) flags are case-insensitive, so at this stage this is out
            Dovecot (at the least) treats them as case-insensitive, so if it were added back I'd have the issue of how to make it configurable

        public static readonly StringComparer Comparer = StringComparer.InvariantCultureIgnoreCase;

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
        } */
    }

    /// <summary>
    /// A read only collection of IMAP message flags.
    /// </summary>
    public abstract class cMessageFlags : IReadOnlyCollection<string>
    {
        private readonly cMessageFlagList mFlags;

        public cMessageFlags(cMessageFlagList pFlags) => mFlags = pFlags;

        public bool Contains(string pFlag) => mFlags.Contains(pFlag);
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        public IEnumerable<string> SymmetricDifference(cMessageFlags pOther, params string[] pExcept)
        {
            var lSymmetricDifference = mFlags.Except(pOther.mFlags, StringComparer.InvariantCultureIgnoreCase).Union(pOther.mFlags.Except(mFlags, StringComparer.InvariantCultureIgnoreCase), StringComparer.InvariantCultureIgnoreCase);
            if (pExcept == null || pExcept.Length == 0) return lSymmetricDifference;
            return lSymmetricDifference.Except(pExcept);
        }

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString() => mFlags.ToString();
    }

    /// <summary>
    /// <para>A read only collection of settable IMAP message flags.</para>
    /// <para>(e.g. It is not possible to set the \Recent flag.)</para>
    /// </summary>
    public class cSettableFlags : cMessageFlags
    {
        // immutable (for passing in)

        public static readonly cSettableFlags None = new cSettableFlags();

        /** <summary>A set of flags containing just the \Answered flag</summary> */
        public static readonly cSettableFlags Answered = new cSettableFlags(kMessageFlagName.Answered);

        /** <summary>A set of flags containing just the \Flagged flag</summary> */
        public static readonly cSettableFlags Flagged = new cSettableFlags(kMessageFlagName.Flagged);

        /** <summary>A set of flags containing just the \Deleted flag</summary> */
        public static readonly cSettableFlags Deleted = new cSettableFlags(kMessageFlagName.Deleted);

        /** <summary>A set of flags containing just the \Seen flag</summary> */
        public static readonly cSettableFlags Seen = new cSettableFlags(kMessageFlagName.Seen);

        /** <summary>A set of flags containing just the \Draft flag</summary> */
        public static readonly cSettableFlags Draft = new cSettableFlags(kMessageFlagName.Draft);

        /** <summary>A set of flags containing just the $Forwarded flag</summary> */
        public static readonly cSettableFlags Forwarded = new cSettableFlags(kMessageFlagName.Forwarded);

        /** <summary>A set of flags containing just the $SubmitPending flag</summary> */
        public static readonly cSettableFlags SubmitPending = new cSettableFlags(kMessageFlagName.SubmitPending);

        /** <summary>A set of flags containing just the $Submitted flag</summary> */
        public static readonly cSettableFlags Submitted = new cSettableFlags(kMessageFlagName.Submitted);

        // see comments elsewhere as to why this is commented out
        //public static readonly cSettableFlags MDNSent = new cSettableFlags(kMessageFlagName.MDNSent);

        public cSettableFlags(params string[] pFlags) : base(new cSettableFlagList(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlags(IEnumerable<string> pFlags) : base(new cSettableFlagList(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlags(cSettableFlagList pFlags) : base(new cSettableFlagList(pFlags)) { } // duplicates

        public static implicit operator cSettableFlags(cSettableFlagList pFlags) => new cSettableFlags(pFlags);
    }

    /// <summary>
    /// <para>A read only collection of fetchable IMAP message flags.</para>
    /// <para>(e.g. It is not possible to set \Recent flag however it is possible to receive it set on a message.)</para>
    /// </summary>
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

    /// <summary>
    /// <para>A read only collection of IMAP message flags that it is possible to set permanently on messages in a mailbox.</para>
    /// <para>(e.g. This may include the \* flag indicating that it is possible to create new flags by setting them.)</para>
    /// </summary>
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

    /// <summary>
    /// A list of IMAP message flags.
    /// </summary>
    public abstract class cMessageFlagList : IReadOnlyCollection<string>
    {
        // implements case insensitivity (note that the specs do NOT explicitly say that keywords are case insensitive OTHER than the spec for MDNSent) via the Comparer [see the notes above though: currently the implementation is case-insensitive]
        //  implements uniqueness (via mutation, not via construct)
        //  implements validity (via mutation, not via construct)

        private readonly List<string> mFlags;

        public cMessageFlagList(List<string> pFlags)
        {
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        public bool Contains(string pFlag) => mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase);
        public bool Contains(params string[] pFlags) => ZContains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, StringComparer.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        public void Add(string pFlag)
        {
            if (!YIsValidFlag(pFlag)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            if (!mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.Add(pFlag);
        }

        public void Add(params string[] pFlags) => ZAdd(pFlags);
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.Add(lFlag);
        }

        public void Remove(string pFlag) => mFlags.RemoveAll(f => f.Equals(pFlag, StringComparison.InvariantCultureIgnoreCase));
        public void Remove(params string[] pFlags) => ZRemove(pFlags);
        public void Remove(IEnumerable<string> pFlags) => ZRemove(pFlags);

        private void ZRemove(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) mFlags.RemoveAll(f => f.Equals(lFlag, StringComparison.InvariantCultureIgnoreCase));
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

    /// <summary>
    /// <para>A list of settable IMAP message flags.</para>
    /// <para>(e.g. It is not possible to set the \Recent flag.)</para>
    /// </summary>
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
            if (pFlags == null) return new List<string>();
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            return new List<string>(pFlags.Distinct(StringComparer.InvariantCultureIgnoreCase));
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

            if (lFlags.Count != 5) throw new cTestsException($"{nameof(cSettableFlagList)}.2");
            if (!lFlags.Contains("A") || !lFlags.Contains("B") || !lFlags.Contains(@"\aNswereD") || lFlags.Contains(kMessageFlagName.Draft) || !lFlags.Contains("$forwarded")) throw new cTestsException($"{nameof(cSettableFlagList)}.3");

            cSettableFlags lF1 = new cSettableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlagName.Forwarded);
            cFetchableFlags lF2 = new cFetchableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlagName.Recent);
            cSettableFlags lF3 = new cSettableFlags("a", "b", "\\deleted", kMessageFlagName.Forwarded);

            if (!lFlags.Contains(lF1) || lFlags.Contains(lF2) || !lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cSettableFlagList)}.4");

            lFlags.Remove("A");
            if (lFlags.Count != 4 || lFlags.Contains(lF1) || lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cSettableFlagList)}.5");

            lFlags.Remove("B", "$forwarded", @"\answered");
            if (lFlags.Count != 1 || lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cSettableFlagList)}.6");
        }
    }

    /// <summary>
    /// <para>A list of fetchable IMAP message flags.</para>
    /// <para>(e.g. It is not possible to set \Recent flag however it is possible to receive it set on a message.)</para>
    /// </summary>
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
            if (pFlags == null) return new List<string>();
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            return new List<string>(pFlags.Distinct(StringComparer.InvariantCultureIgnoreCase));
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlagList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cFetchableFlagList(new List<string>(pFlags.Distinct(StringComparer.InvariantCultureIgnoreCase)));
            return true;
        }
    }

    /// <summary>
    /// <para>A list of IMAP message flags that it is possible to set permanently on messages in a mailbox.</para>
    /// <para>(e.g. This may include the \* flag indicating that it is possible to create new flags by setting them.)</para>
    /// </summary>
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
            rFlags = new cPermanentFlagList(new List<string>(pFlags.Distinct(StringComparer.InvariantCultureIgnoreCase)));
            return true;
        }
    }
}
