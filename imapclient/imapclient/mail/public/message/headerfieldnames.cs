using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains named header-field-name constants.
    /// </summary>
    public static class kHeaderFieldName
    {
        /**<summary>In-Reply-To</summary>*/
        public const string InReplyTo = "In-Reply-To";
        /**<summary>Message-Id</summary>*/
        public const string MessageId = "Message-Id";
        /**<summary>References</summary>*/
        public const string References = "References";
        /**<summary>Importance</summary>*/
        public const string Importance = "Importance";
    }

    /// <summary>
    /// An immutable header-field-name collection. 
    /// </summary>
    /// <remarks>
    /// Header field names are case insensitive and have a limited grammar - see RFC 5322. 
    /// (Header field names may only include <see cref="cCharset.FText"/> characters.)
    /// </remarks>
    [Serializable]
    [DataContract]
    public class cHeaderFieldNames : IReadOnlyList<string>, IEquatable<cHeaderFieldNames>
    {
        // immutable (for passing in and out)

        /** <summary>An empty header-field-name collection.</summary>*/
        public static readonly cHeaderFieldNames Empty = new cHeaderFieldNames();
        /** <summary>A header-field-name collection containing only <see cref="kHeaderFieldName.References"/>.</summary>*/
        public static readonly cHeaderFieldNames References = new cHeaderFieldNames(kHeaderFieldName.References);
        /** <summary>A header-field-name collection containing only <see cref="kHeaderFieldName.Importance"/>.</summary>*/
        public static readonly cHeaderFieldNames Importance = new cHeaderFieldNames(kHeaderFieldName.Importance);

        // ordered (case insensitive) list of names (the ordering is required for the hashcode and == implementations)
        [DataMember]
        private readonly cHeaderFieldNameList mNames;

        private cHeaderFieldNames() => mNames = new cHeaderFieldNameList();

        /// <summary>
        /// Initalises a new instance with a duplicate free (case insensitive) copy of the specified names. Will throw if the specified names aren't valid header field names.
        /// </summary>
        /// <param name="pNames"></param>
        /// <inheritdoc cref="cHeaderFieldNames" select="remarks"/>
        public cHeaderFieldNames(params string[] pNames) => mNames = new cHeaderFieldNameList(from n in pNames orderby n.ToUpperInvariant() select n);

        /// <inheritdoc cref="cHeaderFieldNames(string[])"/>
        public cHeaderFieldNames(IEnumerable<string> pNames) => mNames = new cHeaderFieldNameList(from n in pNames orderby n.ToUpperInvariant() select n);

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mNames == null) throw new cDeserialiseException($"{nameof(cHeaderFieldNames)}.{nameof(mNames)}.null");

            bool lFirst = true;
            string lLastName = null;

            foreach (var lName in mNames)
            {
                string lThisName = lName.ToUpperInvariant();

                if (lFirst) lFirst = false;
                else if (lThisName.CompareTo(lLastName) != 1) throw new cDeserialiseException($"{nameof(cHeaderFieldNames)}.{nameof(mNames)}.order");

                lLastName = lThisName;
            }
        }

        /// <summary>
        /// Determines whether the collection contains the specifed name (case insensitive).
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        public bool Contains(string pName) => mNames.Contains(pName);

        /// <summary>
        /// Determines whether the collection contains all the specified names (case insensitive).
        /// </summary>
        /// <param name="pNames"></param>
        /// <returns></returns>
        public bool Contains(params string[] pNames) => mNames.Contains(pNames);

        /// <inheritdoc cref="Contains(string[])"/>
        public bool Contains(IEnumerable<string> pNames) => mNames.Contains(pNames);

        /// <summary>
        /// Returns the set-union of this and the specified collection of names (case insensitive).
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public cHeaderFieldNames Union(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Union(pOther.mNames));

        /// <summary>
        /// Returns the set-intersection of this and the specified collection of names (case insensitive).
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public cHeaderFieldNames Intersect(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Intersect(pOther.mNames));

        /// <summary>
        /// Returns the set-difference of this and the specified collection of names (case insensitive).
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public cHeaderFieldNames Except(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Except(pOther.mNames));

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mNames.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<string> GetEnumerator() => mNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public string this[int i] => mNames[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cHeaderFieldNames pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cHeaderFieldNames;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (var lName in mNames) lHash = lHash * 23 + lName.ToUpperInvariant().GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => mNames.ToString();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cHeaderFieldNames pA, cHeaderFieldNames pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            if (pA.mNames.Count != pB.mNames.Count) return false;
            for (int i = 0; i < pA.Count; i++) if (!pA.mNames[i].Equals(pB.mNames[i], StringComparison.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cHeaderFieldNames pA, cHeaderFieldNames pB) => !(pA == pB);

        /// <summary>
        /// Returns a new instance containing a copy of the specified list.
        /// </summary>
        /// <param name="pNames"></param>
        public static implicit operator cHeaderFieldNames(cHeaderFieldNameList pNames)
        {
            if (pNames == null) return null;
            return new cHeaderFieldNames(pNames);
        }

        internal static bool TryConstruct(IEnumerable<string> pNames, out cHeaderFieldNames rNames)
        {
            if (!cHeaderFieldNameList.TryConstruct(pNames, out var lNames)) { rNames = null; return false; }
            rNames = new cHeaderFieldNames(lNames);
            return true;
        }
    }

    /// <summary>
    /// A header field name list.
    /// </summary>
    /// <inheritdoc cref="cHeaderFieldNames" select="remarks"/>
    [Serializable]
    [DataContract]
    public class cHeaderFieldNameList : IReadOnlyList<string>
    {
        // implements case insensitivity
        //  implements only one copy of each header field
        //  implements the grammar for header field names

        [DataMember]
        private readonly List<string> mNames;

        /// <summary>
        /// Initialises a new empty instance.
        /// </summary>
        public cHeaderFieldNameList()
        {
            mNames = new List<string>();
        }

        /// <inheritdoc cref="cHeaderFieldNames(string[])"/>
        public cHeaderFieldNameList(params string[] pNames)
        {
            if (pNames == null)
            {
                mNames = new List<string>();
                return;
            }

            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            mNames = new List<string>(pNames.Distinct(StringComparer.InvariantCultureIgnoreCase));
        }

        /// <inheritdoc cref="cHeaderFieldNames(string[])"/>
        public cHeaderFieldNameList(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            mNames = new List<string>(pNames.Distinct(StringComparer.InvariantCultureIgnoreCase));
        }
    
        /// <summary>
        /// Initalises a new instance with a copy of the specified names.
        /// </summary>
        /// <param name="pNames"></param>
        public cHeaderFieldNameList(cHeaderFieldNameList pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            mNames = new List<string>(pNames.mNames);
        }

        private cHeaderFieldNameList(IEnumerable<string> pNames, bool pUnique) // duplicates, optionally removes duplicates
        {
            if (pUnique) mNames = new List<string>(pNames);
            else mNames = new List<string>(pNames.Distinct(StringComparer.InvariantCultureIgnoreCase));
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mNames == null) throw new cDeserialiseException($"{nameof(cHeaderFieldNameList)}.{nameof(mNames)}.null");
            foreach (var lName in mNames) if (!ZIsValidName(lName)) throw new cDeserialiseException($"{nameof(cHeaderFieldNameList)}.{nameof(mNames)}.isvalidname");
            if (mNames.Distinct(StringComparer.InvariantCultureIgnoreCase).Count() != mNames.Count) throw new cDeserialiseException($"{nameof(cHeaderFieldNameList)}.{nameof(mNames)}.distinct");
        }

        /// <summary>
        /// Determines whether the list contains the specified name (case insensitive).
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        public bool Contains(string pName) => mNames.Contains(pName, StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Determines whether the list contains all the specified names (case insensitive).
        /// </summary>
        /// <param name="pNames"></param>
        /// <returns></returns>
        public bool Contains(params string[] pNames) => ZContains(pNames);

        /// <inheritdoc cref="Contains(string[])"/>
        public bool Contains(IEnumerable<string> pNames) => ZContains(pNames);

        private bool ZContains(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) if (!Contains(lName)) return false;
            return true;
        }

        /// <summary>
        /// Adds the specified name to the list if it isn't already there (case insensitive).
        /// </summary>
        /// <param name="pName"></param>
        public void Add(string pName)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (!ZIsValidName(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!Contains(pName)) mNames.Add(pName);
        }

        /// <summary>
        /// Adds each specified name to the list if it isn't already there (case insensitive).
        /// </summary>
        /// <param name="pNames"></param>
        public void Add(params string[] pNames) => ZAdd(pNames);

        /// <inheritdoc cref="Add(string[])"/>
        public void Add(IEnumerable<string> pNames) => ZAdd(pNames);

        private void ZAdd(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            foreach (var lName in pNames) if (!Contains(lName)) mNames.Add(lName);
        }

        /// <summary>
        /// Removes the specified name from the list if it is there (case insensitive).
        /// </summary>
        /// <param name="pName"></param>
        public void Remove(string pName) => mNames.RemoveAll(n => n.Equals(pName, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Removes each specified name from the list if it is there (case insensitive).
        /// </summary>
        /// <param name="pNames"></param>
        public void Remove(params string[] pNames) => ZRemove(pNames);

        /// <inheritdoc cref="Remove(string[])"/>
        public void Remove(IEnumerable<string> pNames) => ZRemove(pNames);

        private void ZRemove(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) Remove(lName);
        }

        /// <summary>
        /// Returns the set-union of this and the specified list of names (case insensitive).
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public cHeaderFieldNameList Union(cHeaderFieldNameList pOther) => new cHeaderFieldNameList(mNames.Union(pOther.mNames, StringComparer.InvariantCultureIgnoreCase), true);

        /// <summary>
        /// Returns the set-intersection of this and the specified list of names (case insensitive).
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public cHeaderFieldNameList Intersect(cHeaderFieldNameList pOther) => new cHeaderFieldNameList(mNames.Intersect(pOther.mNames, StringComparer.InvariantCultureIgnoreCase), true);

        /// <summary>
        /// Returns the set-difference of this and the specified list of names (case insensitive).
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public cHeaderFieldNameList Except(cHeaderFieldNameList pOther) => new cHeaderFieldNameList(mNames.Except(pOther.mNames, StringComparer.InvariantCultureIgnoreCase), true);

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mNames.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<string> GetEnumerator() => mNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mNames.GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public string this[int i] => mNames[i];

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHeaderFieldNameList));
            foreach (var lName in mNames) lBuilder.Append(lName);
            return lBuilder.ToString();
        }

        private static bool ZIsValidName(string pName)
        {
            if (pName == null) return false;
            if (pName.Length == 0) return false;
            return cCharset.FText.ContainsAll(pName);
        }

        internal static bool TryConstruct(IEnumerable<string> pNames, out cHeaderFieldNameList rNames)
        {
            if (pNames == null) { rNames = null; return false; }
            foreach (var lName in pNames) if (!ZIsValidName(lName)) { rNames = null; return false; }
            rNames = new cHeaderFieldNameList(pNames, false);
            return true;
        }





        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cHeaderFieldNameList), nameof(_Tests));

            cHeaderFieldNameList lNames1;
            cHeaderFieldNameList lNames2;
            cHeaderFieldNameList lNames3;
            cHeaderFieldNameList lNames4;

            if (!TryConstruct(new string[] { }, out lNames1) || lNames1.Count != 0) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.1");
            if (!TryConstruct(new string[] { "fred", "angus" }, out lNames1) || !TryConstruct(new string[] { "AnGuS", "ANGUS", "FrEd" }, out lNames2) || !lNames1.Contains(lNames2) || !lNames2.Contains(lNames1)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.2");
            if (!TryConstruct(new string[] { "fred", "charlie" }, out lNames3) || !TryConstruct(new string[] { "CHARLie", "mAx" }, out lNames4) || lNames3.Contains(lNames4) || lNames4.Contains(lNames3)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.3");
            if (lNames2.Contains("max") || !lNames2.Contains("FREd") || !lNames4.Contains("max") || lNames4.Contains("FREd")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.4");

            lNames2 = new cHeaderFieldNameList(lNames1);
            lNames2.Add("fReD");

            if ((cHeaderFieldNames)lNames1 != lNames2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.5");

            lNames2.Add("charlie");
            if (lNames2.Count != 3 || !lNames2.Contains("Fred") || !lNames2.Contains("ANgUS") || !lNames2.Contains("CHArLIE")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.6");

            var lNames5 = lNames1.Union(lNames3);
            if ((cHeaderFieldNames)lNames5 != lNames2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.7");

            lNames2 = lNames1.Intersect(lNames3);
            if (lNames2.Count != 1 || !lNames2.Contains("fReD")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.8");

            lNames2 = lNames5.Except(lNames4);
            if (lNames2.Count != 2 || (cHeaderFieldNames)lNames2 != lNames1) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.9");

            bool lFailed = false;
            try { cHeaderFieldNames lF = new cHeaderFieldNames("dd ff"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.13");

            lFailed = false;
            try { cHeaderFieldNames lF = new cHeaderFieldNames("dd:ff"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.14");
        }
    }
}