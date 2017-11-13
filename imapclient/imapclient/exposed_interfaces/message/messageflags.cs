using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains named IMAP flag name contants.
    /// </summary>
    public static class kMessageFlagName
    {
        /**<summary>\*</summary>*/
        public const string CreateNewIsPossible = @"\*";
        /**<summary>\Recent</summary>*/
        public const string Recent = @"\Recent";

        /**<summary>\Answered</summary>*/
        public const string Answered = @"\Answered";
        /**<summary>\Flagged</summary>*/
        public const string Flagged = @"\Flagged";
        /**<summary>\Deleted</summary>*/
        public const string Deleted = @"\Deleted";
        /**<summary>\Seen</summary>*/
        public const string Seen = @"\Seen";
        /**<summary>\Draft</summary>*/
        public const string Draft = @"\Draft";

        // rfc 5788/ 5550
        /**<summary>$Forwarded</summary>*/
        public const string Forwarded = "$Forwarded";
        /**<summary>$SubmitPending</summary>*/
        public const string SubmitPending = "$SubmitPending";
        /**<summary>$Submitted</summary>*/
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
    /// A unique read-only message flag collection. Message flag names are case insensitive. See <see cref="cMailbox.ForUpdatePermanentFlags"/> and <see cref="cMailbox.ReadOnlyPermanentFlags"/>.
    /// </summary>
    public abstract class cMessageFlags : IReadOnlyCollection<string>
    {
        private readonly cMessageFlagList mFlags;

        /// <summary>
        /// Creates a read-only wrapper around the specified list.
        /// </summary>
        /// <param name="pFlags"></param>
        public cMessageFlags(cMessageFlagList pFlags) => mFlags = pFlags;

        /// <summary>
        /// Determines if the collection contains the flag (case insensitive).
        /// </summary>
        /// <param name="pFlag"></param>
        /// <returns></returns>
        public bool Contains(string pFlag) => mFlags.Contains(pFlag);

        /// <summary>
        /// Determines if the collection contains all the flags (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);

        /// <summary>
        /// Determines if the collection contains all the flags (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        /// <summary>
        /// Gets the symmetric difference between two collections of flags ignoring an optional set of flags (case insensitive).
        /// </summary>
        /// <param name="pOther"></param>
        /// <param name="pExcept">The flags to ignore when doing the difference.</param>
        /// <returns></returns>
        public IEnumerable<string> SymmetricDifference(cMessageFlags pOther, params string[] pExcept)
        {
            var lSymmetricDifference = mFlags.Except(pOther.mFlags, StringComparer.InvariantCultureIgnoreCase).Union(pOther.mFlags.Except(mFlags, StringComparer.InvariantCultureIgnoreCase), StringComparer.InvariantCultureIgnoreCase);
            if (pExcept == null || pExcept.Length == 0) return lSymmetricDifference;
            return lSymmetricDifference.Except(pExcept);
        }

        /**<summary>Gets the number of flags in the collection.</summary>*/
        public int Count => mFlags.Count;

        /**<summary>Returns an enumerator that iterates through the flags.</summary>*/
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString() => mFlags.ToString();
    }

    /// <summary>
    /// A unique read-only storable message flag collection. Message flag names are case insensitive. The <see cref="kMessageFlagName.Recent"/> flag is not a storable flag. Used in the 'store' APIs. This class defines an implicit conversion from <see cref="cStorableFlagList"/>.
    /// </summary>
    /// <remarks>
    /// See 
    /// <see cref="cMessage.Store(eStoreOperation, cStorableFlags, ulong?)"/>,
    /// <see cref="cMailbox.UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
    /// <see cref="cMailbox.UIDStore(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
    /// <see cref="cIMAPClient.Store(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
    /// </remarks>
    public class cStorableFlags : cMessageFlags
    {
        // immutable (for passing in)

        /** <summary>An empty set of flags.</summary> */
        public static readonly cStorableFlags None = new cStorableFlags();

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.Answered"/> flag.</summary> */
        public static readonly cStorableFlags Answered = new cStorableFlags(kMessageFlagName.Answered);

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.Flagged"/> flag.</summary> */
        public static readonly cStorableFlags Flagged = new cStorableFlags(kMessageFlagName.Flagged);

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.Deleted"/> flag.</summary> */
        public static readonly cStorableFlags Deleted = new cStorableFlags(kMessageFlagName.Deleted);

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.Seen"/> flag.</summary> */
        public static readonly cStorableFlags Seen = new cStorableFlags(kMessageFlagName.Seen);

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.Draft "/> flag.</summary> */
        public static readonly cStorableFlags Draft = new cStorableFlags(kMessageFlagName.Draft);

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.Forwarded"/> flag.</summary> */
        public static readonly cStorableFlags Forwarded = new cStorableFlags(kMessageFlagName.Forwarded);

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.SubmitPending"/> flag.</summary> */
        public static readonly cStorableFlags SubmitPending = new cStorableFlags(kMessageFlagName.SubmitPending);

        /** <summary>A collection of flags containing just the <see cref="kMessageFlagName.Submitted"/> flag.</summary> */
        public static readonly cStorableFlags Submitted = new cStorableFlags(kMessageFlagName.Submitted);

        // see comments elsewhere as to why this is commented out
        //public static readonly cSettableFlags MDNSent = new cSettableFlags(kMessageFlagName.MDNSent);

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are storable flags. May throw if the specified flags aren't valid storable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cStorableFlags(params string[] pFlags) : base(new cStorableFlagList(pFlags)) { }

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are storable flags. May throw if the specified flags aren't valid IMAP storable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cStorableFlags(IEnumerable<string> pFlags) : base(new cStorableFlagList(pFlags)) { }

        /// <summary>
        /// Creates a copy of the specified storable flag list.
        /// </summary>
        /// <param name="pFlags"></param>
        public cStorableFlags(cStorableFlagList pFlags) : base(new cStorableFlagList(pFlags)) { }

        /// <summary>
        /// Creates a copy of the specified storable flag list.
        /// </summary>
        /// <param name="pFlags"></param>
        public static implicit operator cStorableFlags(cStorableFlagList pFlags) => new cStorableFlags(pFlags);
    }

    /// <summary>
    /// A unique read-only fetchable message flag collection. Message flag names are case insensitive. (The <see cref="kMessageFlagName.CreateNewIsPossible"/> flag is not a fetchable flag.) See <see cref="cMessage.Flags"/>, <see cref="cMailbox.MessageFlags"/>, <see cref="cFilter.FlagsContain(cFetchableFlags)"/>.
    /// </summary>
    public class cFetchableFlags : cMessageFlags
    {
        // immutable (for passing in and out)

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are fetchable flags. May throw if the specified flags aren't valid IMAP fetchable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cFetchableFlags(params string[] pFlags) : base(new cFetchableFlagList(pFlags)) { }

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are fetchable flags. May throw if the specified flags aren't valid IMAP fetchable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cFetchableFlags(IEnumerable<string> pFlags) : base(new cFetchableFlagList(pFlags)) { }

        /// <summary>
        /// Copies the specified fetchable flag list.
        /// </summary>
        /// <param name="pFlags"></param>
        public cFetchableFlags(cFetchableFlagList pFlags) : base(new cFetchableFlagList(pFlags)) { }

        private cFetchableFlags(cFetchableFlagList pFlags, bool pWrap) : base(pFlags) { } // wraps

        /// <summary>
        /// Copies the specified fetchable flag list.
        /// </summary>
        /// <param name="pFlags"></param>
        public static implicit operator cFetchableFlags(cFetchableFlagList pFlags) => new cFetchableFlags(pFlags);

        internal static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlags rFlags)
        {
            if (!cFetchableFlagList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cFetchableFlags(lFlags, true);
            return true;
        }
    }

    /// <summary>
    /// A unique message flag list. Message flag names are case insensitive and have a limited grammar (see RFC 3501).
    /// </summary>
    public abstract class cMessageFlagList : IReadOnlyCollection<string>
    {
        // implements case insensitivity (note that the specs do NOT explicitly say that keywords are case insensitive OTHER than the spec for MDNSent) via the Comparer [see the notes above though: currently the implementation is case-insensitive]
        //  implements uniqueness (via mutation, not via construct)
        //  implements validity (via mutation, not via construct)

        private readonly List<string> mFlags;

        /// <summary>
        /// Creates a message flag list around the specified list. The list is not copied.
        /// </summary>
        /// <param name="pFlags"></param>
        public cMessageFlagList(List<string> pFlags)
        {
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        /// <summary>
        /// Determines whether the list contains the flag (case insensitive).
        /// </summary>
        /// <param name="pFlag"></param>
        /// <returns></returns>
        public bool Contains(string pFlag) => mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Determines whether the list contains all the specified flags (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public bool Contains(params string[] pFlags) => ZContains(pFlags);

        /// <summary>
        /// Determines whether the list contains all the specified flags (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, StringComparer.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        /// <summary>
        /// Adds the flag to the list if it isn't already there (case insensitive).
        /// </summary>
        /// <param name="pFlag"></param>
        public void Add(string pFlag)
        {
            if (!YIsValidFlag(pFlag)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            if (!mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.Add(pFlag);
        }

        /// <summary>
        /// Adds each flag to the list if it isn't already there (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        public void Add(params string[] pFlags) => ZAdd(pFlags);

        /// <summary>
        /// Adds each flag to the list if it isn't already there (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.Add(lFlag);
        }

        /// <summary>
        /// Removes the flag from the list if it is there (case insensitive).
        /// </summary>
        /// <param name="pFlag"></param>
        public void Remove(string pFlag) => mFlags.RemoveAll(f => f.Equals(pFlag, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Removes the flags from the list if they are there (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        public void Remove(params string[] pFlags) => ZRemove(pFlags);

        /// <summary>
        /// Removes the flags from the list if they are there (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        public void Remove(IEnumerable<string> pFlags) => ZRemove(pFlags);

        private void ZRemove(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) mFlags.RemoveAll(f => f.Equals(lFlag, StringComparison.InvariantCultureIgnoreCase));
        }

        /**<summary>Gets the number of flags in the list.</summary>*/
        public int Count => mFlags.Count;
        /**<summary>Returns an enumerator that iterates through the flags.</summary>*/
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
    /// A unique storable message flag list. Message flag names are case insensitive and have a limited grammar (see RFC 3501). The <see cref="kMessageFlagName.Recent"/> flag is not a storable flag. See <see cref="cStorableFlags"/>.
    /// </summary>
    public class cStorableFlagList : cMessageFlagList
    {
        /// <summary>
        /// Creates an empty list.
        /// </summary>
        public cStorableFlagList() : base(new List<string>()) { }

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are storable flags. May throw if the specified flags aren't valid storable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cStorableFlagList(params string[] pFlags) : base(ZCtor(pFlags)) { }

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are storable flags. May throw if the specified flags aren't valid storable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cStorableFlagList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { }

        /// <summary>
        /// Creates a copy of the specified storable flag list.
        /// </summary>
        /// <param name="pFlags"></param>
        public cStorableFlagList(cStorableFlagList pFlags) : base(new List<string>(pFlags)) { }

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
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            bool lFailed;

            var lFlags = new cStorableFlagList();

            lFlags.Add("a");
            lFlags.Add("b");
            lFlags.Add(kMessageFlagName.Answered, kMessageFlagName.Deleted);
            lFlags.Add(kMessageFlagName.Answered, kMessageFlagName.Deleted, kMessageFlagName.Forwarded);
            lFlags.Add(@"\answereD");
            lFlags.Add(@"\ansWereD", "A", @"\deleteD");

            lFailed = false;
            try { lFlags.Add("fr ed"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cStorableFlagList)}.1");

            lFailed = false;
            try { lFlags.Add(kMessageFlagName.Answered, kMessageFlagName.Deleted, kMessageFlagName.Draft, kMessageFlagName.Recent); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cStorableFlagList)}.1");

            if (lFlags.Count != 5) throw new cTestsException($"{nameof(cStorableFlagList)}.2");
            if (!lFlags.Contains("A") || !lFlags.Contains("B") || !lFlags.Contains(@"\aNswereD") || lFlags.Contains(kMessageFlagName.Draft) || !lFlags.Contains("$forwarded")) throw new cTestsException($"{nameof(cStorableFlagList)}.3");

            cStorableFlags lF1 = new cStorableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlagName.Forwarded);
            cFetchableFlags lF2 = new cFetchableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlagName.Recent);
            cStorableFlags lF3 = new cStorableFlags("a", "b", "\\deleted", kMessageFlagName.Forwarded);

            if (!lFlags.Contains(lF1) || lFlags.Contains(lF2) || !lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cStorableFlagList)}.4");

            lFlags.Remove("A");
            if (lFlags.Count != 4 || lFlags.Contains(lF1) || lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cStorableFlagList)}.5");

            lFlags.Remove("B", "$forwarded", @"\answered");
            if (lFlags.Count != 1 || lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cStorableFlagList)}.6");
        }
    }

    /// <summary>
    /// A unique fetchable message flag list. Message flag names are case insensitive and have a limited grammar (see RFC 3501). The <see cref="kMessageFlagName.CreateNewIsPossible"/> flag is not a fetchable flag. See <see cref="cFetchableFlags"/>.
    /// </summary>
    public class cFetchableFlagList : cMessageFlagList
    {
        /// <summary>
        /// Creates an empty list.
        /// </summary>
        public cFetchableFlagList() : base(new List<string>()) { }

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are fetchable flags. May throw if the specified flags aren't valid fetchable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cFetchableFlagList(params string[] pFlags) : base(ZCtor(pFlags)) { }

        /// <summary>
        /// Creates a duplicate free copy of the specified flags, validating that they are fetchable flags. May throw if the specified flags aren't valid fetchable flags.
        /// </summary>
        /// <param name="pFlags"></param>
        public cFetchableFlagList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { } 

        /// <summary>
        /// Creates a copy of the specified fetchable flag list.
        /// </summary>
        /// <param name="pFlags"></param>
        public cFetchableFlagList(cFetchableFlagList pFlags) : base(new List<string>(pFlags)) { } 

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

        internal static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlagList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cFetchableFlagList(new List<string>(pFlags.Distinct(StringComparer.InvariantCultureIgnoreCase)));
            return true;
        }
    }
}
