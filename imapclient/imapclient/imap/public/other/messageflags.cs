using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains named message-flag contants.
    /// </summary>
    public static class kMessageFlag
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
    /// Represents an immutable message-flag collection.
    /// </summary>
    /// <remarks>
    /// Message flags are case insensitive and have a limited grammer - see RFC 3501.
    /// (Generally user-defined message-flags may only include <see cref="cCharset.Atom"/> characters.)
    /// </remarks>
    /// <seealso cref="cMailbox.ForUpdatePermanentFlags"/>
    /// <seealso cref="cMailbox.ReadOnlyPermanentFlags"/>
    public abstract class cMessageFlags : IReadOnlyList<string>, IEquatable<cMessageFlags>
    {
        // ordered (case insensitive) list of flags (the ordering is required for the hashcode and == implementations)
        private readonly List<string> mFlags;

        /// <summary>
        /// Initialises a new instance with a copy of the specified flags.
        /// </summary>
        /// <param name="pFlags"></param>
        internal cMessageFlags(IEnumerable<string> pFlags) => mFlags = new List<string>(from f in pFlags orderby f.ToUpperInvariant() select f);

        /// <summary>
        /// Determines whether the collection contains the specified flag (case insensitive).
        /// </summary>
        /// <param name="pFlag"></param>
        /// <returns></returns>
        public bool Contains(string pFlag) => mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Determines whether the collection contains all the specified flags (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public bool Contains(params string[] pFlags) => ZContains(pFlags);

        /// <inheritdoc cref="Contains(string[])"/>
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, StringComparer.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        /// <summary>
        /// Returns the symmetric difference between this and the specified collection ignoring an optional set of flags (case insensitive).
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mFlags.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public string this[int i] => mFlags[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMessageFlags pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cMessageFlags;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (var lFlag in mFlags) lHash = lHash * 23 + lFlag.ToUpperInvariant().GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlags));
            foreach (var lFlag in mFlags) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMessageFlags pA, cMessageFlags pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            if (pA.mFlags.Count != pB.mFlags.Count) return false;
            for (int i = 0; i < pA.Count; i++) if (!pA.mFlags[i].Equals(pB.mFlags[i], StringComparison.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMessageFlags pA, cMessageFlags pB) => !(pA == pB);
    }

    /// <summary>
    /// An immutable storable-message-flag collection.
    /// </summary>
    /// <remarks>
    /// Message flags are case insensitive and have a limited grammar - see RFC 3501.
    /// (Generally user-defined message-flags may only include <see cref="cCharset.Atom"/> characters.)
    /// <see cref="kMessageFlag.Recent"/> is not a storable-message-flag.
    /// <see cref="kMessageFlag.CreateNewIsPossible"/> is not a storable-message-flag.
    /// </remarks>
    /// <seealso cref="cIMAPMessage.Store(eStoreOperation, cStorableFlags, ulong?)"/>,
    /// <seealso cref="cIMAPClient.Store(IEnumerable{cIMAPMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
    /// <seealso cref="cMailbox.UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
    /// <seealso cref="cMailbox.UIDStore(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
    public class cStorableFlags : cMessageFlags, IEquatable<cStorableFlags>
    {
        // immutable (for passing in)

        /** <summary>An empty storable-message-flag collection.</summary> */
        public static readonly cStorableFlags Empty = new cStorableFlags();

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.Answered"/>.</summary> */
        public static readonly cStorableFlags Answered = new cStorableFlags(kMessageFlag.Answered);

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.Flagged"/>.</summary> */
        public static readonly cStorableFlags Flagged = new cStorableFlags(kMessageFlag.Flagged);

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.Deleted"/>.</summary> */
        public static readonly cStorableFlags Deleted = new cStorableFlags(kMessageFlag.Deleted);

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.Seen"/>.</summary> */
        public static readonly cStorableFlags Seen = new cStorableFlags(kMessageFlag.Seen);

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.Draft"/>.</summary> */
        public static readonly cStorableFlags Draft = new cStorableFlags(kMessageFlag.Draft);

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.Forwarded"/>.</summary> */
        public static readonly cStorableFlags Forwarded = new cStorableFlags(kMessageFlag.Forwarded);

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.SubmitPending"/>.</summary> */
        public static readonly cStorableFlags SubmitPending = new cStorableFlags(kMessageFlag.SubmitPending);

        /** <summary>A storable-message-flag collection containing only <see cref="kMessageFlag.Submitted"/>.</summary> */
        public static readonly cStorableFlags Submitted = new cStorableFlags(kMessageFlag.Submitted);

        // see comments elsewhere as to why this is commented out
        //public static readonly cSettableFlags MDNSent = new cSettableFlags(kMessageFlagName.MDNSent);

        /// <summary>
        /// Initialises a new instance with a duplicate free (case insensitive) copy of the specified flags. Will throw if the specified flags aren't valid storable-message-flags.
        /// </summary>
        /// <param name="pFlags"></param>
        /// <inheritdoc cref="cStorableFlags" select="remarks"/>
        public cStorableFlags(params string[] pFlags) : base(new cStorableFlagList(pFlags)) { }

        /// <inheritdoc cref="cStorableFlags(string[])"/>
        public cStorableFlags(IEnumerable<string> pFlags) : base(new cStorableFlagList(pFlags)) { }

        /// <summary>
        /// Initialises a new instance with a copy of the specified list.
        /// </summary>
        /// <param name="pFlags"></param>
        public cStorableFlags(cStorableFlagList pFlags) : base(pFlags) { }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cStorableFlags pObject) => this == pObject;

        /// <summary>
        /// Returns a new instance containing a copy of the specified list.
        /// </summary>
        /// <param name="pFlags"></param>
        public static implicit operator cStorableFlags(cStorableFlagList pFlags)
        {
            if (pFlags == null) return null;
            return new cStorableFlags(pFlags);
        }
    }

    /// <summary>
    /// An immutable fetchable-message-flag collection.
    /// </summary>
    /// <remarks>
    /// Message flags are case insensitive and have a limited grammar - see RFC 3501.
    /// (Generally user-defined message-flags may only include <see cref="cCharset.Atom"/> characters.)
    /// <see cref="kMessageFlag.CreateNewIsPossible"/> is not a fetchable-message-flag.
    /// </remarks>
    /// <seealso cref="cMailbox.MessageFlags"/>
    /// <seealso cref="cFilter.FlagsContain(cFetchableFlags)"/>
    public class cFetchableFlags : cMessageFlags, IEquatable<cFetchableFlags>
    {
        // immutable (for passing in and out)

        /** <summary>A fetchable-message-flag collection containing only <see cref="kMessageFlag.Recent"/>.</summary> */
        public static readonly cFetchableFlags Recent = new cFetchableFlags(kMessageFlag.Recent);

        /// <summary>
        /// Initialises a new instance with a duplicate free (case insensitive) copy of the specified flags. Will throw if the specified flags aren't valid fetchable-message-flags.
        /// </summary>
        /// <param name="pFlags"></param>
        /// <inheritdoc cref="cFetchableFlags" select="remarks"/>
        public cFetchableFlags(params string[] pFlags) : base(new cFetchableFlagList(pFlags)) { }

        /// <inheritdoc cref="cFetchableFlags(string[])"/>
        public cFetchableFlags(IEnumerable<string> pFlags) : base(new cFetchableFlagList(pFlags)) { }

        /// <summary>
        /// Initialises a new instance with a copy of the specified list.
        /// </summary>
        /// <param name="pFlags"></param>
        public cFetchableFlags(cFetchableFlagList pFlags) : base(pFlags) { }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cFetchableFlags pObject) => this == pObject;

        /// <summary>
        /// Returns a new instance containing a copy of the specified list.
        /// </summary>
        /// <param name="pFlags"></param>
        public static implicit operator cFetchableFlags(cFetchableFlagList pFlags)
        {
            if (pFlags == null) return null;
            return new cFetchableFlags(pFlags);
        }

        internal static bool TryConstruct(IEnumerable<string> pFlags, out cFetchableFlags rFlags)
        {
            if (!cFetchableFlagList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cFetchableFlags(lFlags);
            return true;
        }
    }

    /// <summary>
    /// Represents a message-flag list.
    /// </summary>
    /// <inheritdoc cref="cMessageFlags" select="remarks"/>
    public abstract class cMessageFlagList : IReadOnlyList<string>
    {
        // implements case insensitivity (note that the specs do NOT explicitly say that keywords are case insensitive OTHER than the spec for MDNSent) via the Comparer [see the notes above though: currently the implementation is case-insensitive]
        //  implements uniqueness (via mutation, not via construct)
        //  implements validity (via mutation, not via construct)

        private readonly List<string> mFlags;
    
        internal cMessageFlagList(List<string> pFlags)
        {
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        /// <summary>
        /// Determines whether the list contains the specified flag (case insensitive).
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

        /// <inheritdoc cref="Contains(string[])"/>
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, StringComparer.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        /// <summary>
        /// Adds the specified flag to the list if it isn't already there (case insensitive).
        /// </summary>
        /// <param name="pFlag"></param>
        public void Add(string pFlag)
        {
            if (!YIsValidFlag(pFlag)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            if (!mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.Add(pFlag);
        }

        /// <summary>
        /// Adds each specified flag to the list if it isn't already there (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        public void Add(params string[] pFlags) => ZAdd(pFlags);

        /// <inheritdoc cref="Add(string[])"/>
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!mFlags.Contains(lFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.Add(lFlag);
        }

        /// <summary>
        /// Removes the specified flag from the list if it is there (case insensitive).
        /// </summary>
        /// <param name="pFlag"></param>
        public void Remove(string pFlag) => mFlags.RemoveAll(f => f.Equals(pFlag, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Removes each specified flag from the list if it is there (case insensitive).
        /// </summary>
        /// <param name="pFlags"></param>
        public void Remove(params string[] pFlags) => ZRemove(pFlags);

        /// <inheritdoc cref="Remove(string[])"/>
        public void Remove(IEnumerable<string> pFlags) => ZRemove(pFlags);

        private void ZRemove(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) mFlags.RemoveAll(f => f.Equals(lFlag, StringComparison.InvariantCultureIgnoreCase));
        }

        /**<summary></summary>*/
        protected abstract bool YIsValidFlag(string pFlag);

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mFlags.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public string this[int i] => mFlags[i];

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlagList));
            foreach (var lFlag in mFlags) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// A storable-message-flag list.
    /// </summary>
    /// <inheritdoc cref="cStorableFlags" select="remarks"/>
    /// <seealso cref="cStorableFlags"/>
    public class cStorableFlagList : cMessageFlagList
    {
        /// <summary>
        /// Initialises a new empty instance.
        /// </summary>
        public cStorableFlagList() : base(new List<string>()) { }

        /// <inheritdoc cref="cStorableFlags(string[])"/>
        public cStorableFlagList(params string[] pFlags) : base(ZCtor(pFlags)) { }

        /// <inheritdoc cref="cStorableFlags(string[])"/>
        public cStorableFlagList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { }

        /// <inheritdoc cref="cStorableFlags(cStorableFlagList)"/>
        public cStorableFlagList(cStorableFlagList pFlags) : base(new List<string>(pFlags)) { }

        /**<summary></summary>*/
        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag.Equals(kMessageFlag.Recent, StringComparison.InvariantCultureIgnoreCase)) return false;

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
            lFlags.Add(kMessageFlag.Answered, kMessageFlag.Deleted);
            lFlags.Add(kMessageFlag.Answered, kMessageFlag.Deleted, kMessageFlag.Forwarded);
            lFlags.Add(@"\answereD");
            lFlags.Add(@"\ansWereD", "A", @"\deleteD");

            lFailed = false;
            try { lFlags.Add("fr ed"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cStorableFlagList)}.1");

            lFailed = false;
            try { lFlags.Add(kMessageFlag.Answered, kMessageFlag.Deleted, kMessageFlag.Draft, kMessageFlag.Recent); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cStorableFlagList)}.1");

            if (lFlags.Count != 5) throw new cTestsException($"{nameof(cStorableFlagList)}.2");
            if (!lFlags.Contains("A") || !lFlags.Contains("B") || !lFlags.Contains(@"\aNswereD") || lFlags.Contains(kMessageFlag.Draft) || !lFlags.Contains("$forwarded")) throw new cTestsException($"{nameof(cStorableFlagList)}.3");

            cStorableFlags lF1 = new cStorableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlag.Forwarded);
            cFetchableFlags lF2 = new cFetchableFlags("a", "A", "b", @"\answered", "\\deleted", kMessageFlag.Recent);
            cStorableFlags lF3 = new cStorableFlags("a", "b", "\\deleted", kMessageFlag.Forwarded);

            if (!lFlags.Contains(lF1) || lFlags.Contains(lF2) || !lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cStorableFlagList)}.4");

            lFlags.Remove("A");
            if (lFlags.Count != 4 || lFlags.Contains(lF1) || lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cStorableFlagList)}.5");

            lFlags.Remove("B", "$forwarded", @"\answered");
            if (lFlags.Count != 1 || lFlags.Contains(lF3)) throw new cTestsException($"{nameof(cStorableFlagList)}.6");
        }
    }

    /// <summary>
    /// A fetchable-message-flag list.
    /// </summary>
    /// <inheritdoc cref="cFetchableFlags" select="remarks"/>
    /// <seealso cref="cFetchableFlags"/>
    public class cFetchableFlagList : cMessageFlagList
    {
        /// <summary>
        /// Initialises a new empty instance.
        /// </summary>
        public cFetchableFlagList() : base(new List<string>()) { }

        /// <inheritdoc cref="cFetchableFlags(string[])"/>
        public cFetchableFlagList(params string[] pFlags) : base(ZCtor(pFlags)) { }

        /// <inheritdoc cref="cFetchableFlags(string[])"/>
        public cFetchableFlagList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { }

        /// <inheritdoc cref="cFetchableFlags(cFetchableFlagList)"/>
        public cFetchableFlagList(cFetchableFlagList pFlags) : base(new List<string>(pFlags)) { }

        private cFetchableFlagList(List<string> pFlags) : base(pFlags) { } // wraps

        /**<summary></summary>*/
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
