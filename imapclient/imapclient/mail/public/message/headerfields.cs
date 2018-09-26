using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents a message header field.
    /// </summary>
    [Serializable]
    public class cHeaderField
    {
        /// <summary>
        /// The header field name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The header field value.
        /// </summary>
        public readonly cBytes Value;

        internal cHeaderField(string pName, cBytes pValue)
        {
            Name = pName ?? throw new ArgumentNullException(nameof(pName));
            Value = pValue ?? throw new ArgumentNullException(nameof(pValue));
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Name == null) throw new cDeserialiseException(nameof(cHeaderField), nameof(Name), kDeserialiseExceptionMessage.IsNull);
            if (Value == null) throw new cDeserialiseException(nameof(cHeaderField), nameof(Value), kDeserialiseExceptionMessage.IsNull);
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cHeaderField)}({Name},{Value})";
    }

    /// <summary>
    /// Represents a header field where the value is a message-id.
    /// </summary>
    [Serializable]
    public class cHeaderFieldMsgId : cHeaderField
    {
        [NonSerialized]
        private string mMessageId;

        private cHeaderFieldMsgId(string pName, cBytes pValue, string pMessageId) : base(pName, pValue) { mMessageId = pMessageId; }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (!ZTryParse(Value, out mMessageId)) throw new cDeserialiseException(nameof(cHeaderFieldMsgId), nameof(Value), kDeserialiseExceptionMessage.IsInvalid);
        }

        /// <summary>
        /// The value of the field as a normalised (quoting, comments and white space removed) message-id.
        /// </summary>
        public string MessageId => mMessageId;

        private static bool ZTryParse(IList<byte> pValue, out string rMessageId)
        {
            cBytesCursor lCursor = new cBytesCursor(pValue);

            if (lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight) && lCursor.Position.AtEnd)
            {
                rMessageId = cTools.MessageId(lIdLeft, lIdRight);
                return true;
            }

            rMessageId = null;
            return false;
        }

        internal static bool TryConstruct(string pName, IList<byte> pValue, out cHeaderFieldMsgId rMsgId)
        {
            if (pName != null && pValue != null && ZTryParse(pValue, out var lMessageId))
            {
                rMsgId = new cHeaderFieldMsgId(pName, new cBytes(pValue), lMessageId);
                return true;
            }

            rMsgId = null;
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cHeaderFieldMsgId)}({mMessageId})";
    }

    /// <summary>
    /// Represents a header field where the value is a set of message-ids.
    /// </summary>
    [Serializable]
    public class cHeaderFieldMsgIds : cHeaderField
    {
        [NonSerialized]
        private cStrings mMessageIds;

        private cHeaderFieldMsgIds(string pName, cBytes pValue, cStrings pMessageIds) : base(pName, pValue) { mMessageIds = pMessageIds; }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (!ZTryParse(Value, out mMessageIds)) throw new cDeserialiseException(nameof(cHeaderFieldMsgIds), nameof(Value), kDeserialiseExceptionMessage.IsInvalid);
        }

        /// <summary>
        /// The value of the field as normalised (quoting, comments and white space removed) message-ids.
        /// </summary>
        public cStrings MessageIds => mMessageIds;

        private static bool ZTryParse(IList<byte> pValue, out cStrings rMessageIds)
        {
            List<string> lMessageIds = new List<string>();

            cBytesCursor lCursor = new cBytesCursor(pValue);

            while (true)
            {
                if (!lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight)) break;
                lMessageIds.Add(cTools.MessageId(lIdLeft, lIdRight));
            }

            if (lCursor.Position.AtEnd)
            {
                rMessageIds = new cStrings(lMessageIds);
                return true;
            }

            rMessageIds = null;
            return false;
        }

        internal static bool TryConstruct(string pName, IList<byte> pValue, out cHeaderFieldMsgIds rMsgIds)
        {
            if (pName != null && pValue != null && ZTryParse(pValue, out var lMessageIds))
            {
                rMsgIds = new cHeaderFieldMsgIds(pName, new cBytes(pValue), lMessageIds);
                return true;
            }

            rMsgIds = null;
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cHeaderFieldMsgIds)}({mMessageIds})";
    }

    /// <summary>
    /// Represents a header field where the value is an importance.
    /// </summary>
    [Serializable]
    public class cHeaderFieldImportance : cHeaderField
    {
        /** <summary>The string constant for low importance.</summary>*/
        public const string Low = "Low";
        /** <summary>The string constant for normal importance.</summary>*/
        public const string Normal = "Normal";
        /** <summary>The string constant for high importance.</summary>*/
        public const string High = "High";

        private static readonly cBytes kLow = new cBytes(Low);
        private static readonly cBytes kNormal = new cBytes(Normal);
        private static readonly cBytes kHigh = new cBytes(High);

        [NonSerialized]
        private eImportance mImportance;

        private cHeaderFieldImportance(cBytes pValue, eImportance pImportance) : base(kHeaderFieldName.Importance, pValue) { mImportance = pImportance; }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (!Name.Equals(kHeaderFieldName.Importance, StringComparison.InvariantCultureIgnoreCase)) throw new cDeserialiseException(nameof(cHeaderFieldImportance), nameof(Name), kDeserialiseExceptionMessage.IsInvalid);
            if (!ZTryParse(Value, out mImportance)) throw new cDeserialiseException(nameof(cHeaderFieldImportance), nameof(Value), kDeserialiseExceptionMessage.IsInvalid);
        }

        /** <summary>The value of the field as an importance code.</summary>*/
        public eImportance Importance => mImportance;

        private static bool ZTryParse(IList<byte> pValue, out eImportance rImportance)
        {
            cBytesCursor lCursor = new cBytesCursor(pValue);

            lCursor.SkipRFC822CFWS();

            if (lCursor.SkipBytes(kLow)) rImportance = eImportance.low;
            else if (lCursor.SkipBytes(kNormal)) rImportance = eImportance.normal;
            else if (lCursor.SkipBytes(kHigh)) rImportance = eImportance.high;
            else { rImportance = 0; return false; }

            lCursor.SkipRFC822CFWS();

            return lCursor.Position.AtEnd;
        }

        internal static bool TryConstruct(IList<byte> pValue, out cHeaderFieldImportance rImportance)
        {
            if (pValue != null && ZTryParse(pValue, out var lImportance))
            {
                rImportance = new cHeaderFieldImportance(new cBytes(pValue), lImportance;
                return true;
            }

            rImportance = null;
            return false;
        }

        /// <summary>
        /// Returns the string constant associated with the specified importance.
        /// </summary>
        /// <param name="pImportance"></param>
        /// <returns></returns>
        public static string FieldValue(eImportance pImportance)
        {
            switch (pImportance)
            {
                case eImportance.low: return Low;
                case eImportance.normal: return Normal;
                case eImportance.high: return High;
                default: throw new ArgumentOutOfRangeException(nameof(pImportance));
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cHeaderFieldImportance)}({mImportance})";
    }

    /// <summary>
    /// An immutable collection of message header fields.
    /// </summary>
    /// <seealso cref="iMessageHandle.HeaderFields"/>
    [Serializable]
    public class cHeaderFields : ReadOnlyCollection<cHeaderField>
    {
        /** <summary>An empty collection.</summary>*/
        public static readonly cHeaderFields Empty = new cHeaderFields(cHeaderFieldNames.Empty, false, new List<cHeaderField>());

        private readonly cHeaderFieldNames mNames;
        private readonly bool mNot;

        private cHeaderFields(cHeaderFieldNames pNames, bool pNot, IList<cHeaderField> pFields) : base(pFields)
        {
            mNames = pNames ?? throw new ArgumentNullException(nameof(pNames));
            mNot = pNot;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mNames == null) throw new cDeserialiseException(nameof(cHeaderFields), nameof(mNames), kDeserialiseExceptionMessage.IsNull);
        }

        /// <summary>
        /// Determines whether the collection has been populated with header fields of the name specified (case insensitive). 
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        /// <remarks>
        /// <see langword="true"/> does not mean that there are header fields of the specified name in the collection.
        /// </remarks>
        public bool Contains(string pName) => mNot != mNames.Contains(pName);

        /// <summary>
        /// Determines whether the collection has been populated with header fields of all the names specified (case insensitive).
        /// </summary>
        /// <param name="pNames"></param>
        /// <returns></returns>
        /// <remarks>
        /// <see langword="true"/> does not mean that there are any header fields of the specified names in the collection.
        /// </remarks>
        public bool Contains(cHeaderFieldNames pNames)
        {
            if (!mNot) return mNames.Contains(pNames);
            foreach (var lName in pNames) if (mNames.Contains(lName)) return false;
            return true;
        }

        /// <summary>
        /// Determines whether the collection has not been populated with header fields of any of the names specified (case insensitive). 
        /// </summary>
        /// <param name="pNames"></param>
        /// <returns></returns>
        /// <remarks>
        /// <see langword="false"/> does not mean that there are any header fields of the specified names in the collection.
        /// </remarks>
        public bool ContainsNone(cHeaderFieldNames pNames)
        {
            if (mNot) return mNames.Contains(pNames);
            foreach (var lName in pNames) if (mNames.Contains(lName)) return false;
            return true;
        }

        /// <summary>
        /// Returns the header field names from the specified collection that this instance has not been populated with (case insensitive).
        /// </summary>
        /// <param name="pNames"></param>
        /// <returns></returns>
        public cHeaderFieldNames GetMissing(cHeaderFieldNames pNames)
        {
            if (mNot) return pNames.Intersect(mNames);
            else return pNames.Except(mNames);
        }

        /// <summary>
        /// Returns one header field that has the specified name (case insensitive), or <see langword="null"/>. Throws if the collection has not been populated with header fields of the specified name.
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        /// <remarks>
        /// <see langword="null"/> indicates that there are no header fields of the specified name in the collection.
        /// </remarks>
        public cHeaderField GetFirstNamed(string pName)
        {
            if (!Contains(pName)) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotPopulatedWithData);
            return this.FirstOrDefault(f => f.Name.Equals(pName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Returns all header fields that have the specified name (case insensitive). Throws if the collection has not been populated with header fields of the specified name.
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        /// <remarks>
        /// An empty set will be returned if there are no header fields of the specified name in the collection.
        /// </remarks>
        public IEnumerable<cHeaderField> GetAllNamed(string pName)
        {
            if (!Contains(pName)) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotPopulatedWithData);
            return from f in this where f.Name.Equals(pName, StringComparison.InvariantCultureIgnoreCase) select f;
        }

        /// <summary>
        /// Returns the normalised message-ids from the references header field, or <see langword="null"/>. Throws if the collection has not been populated with the references header field.
        /// </summary>
        /// <remarks>
        /// Normalised message-ids have the quoting, comments and white space removed.
        /// <see langword="null"/> indicates that there is either no references header field in the collection or that the references header field could not be parsed.
        /// </remarks>
        public cStrings References => (GetFirstNamed(kHeaderFieldName.References) as cHeaderFieldMsgIds)?.MessageIds;

        /// <summary>
        /// Returns the importance value from the importance header field, or <see langword="null"/>. Throws if the collection has not been populated with the importance header field.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that there is either no importance header field in the collection or that the importance header field could not be parsed.
        /// </remarks>
        public eImportance? Importance => (GetFirstNamed(kHeaderFieldName.Importance) as cHeaderFieldImportance)?.Importance;

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHeaderFields));
            lBuilder.Append(mNames);
            lBuilder.Append(mNot);
            foreach (var lField in this) lBuilder.Append(lField);
            return lBuilder.ToString();
        }

        internal static bool TryConstruct(IList<byte> pBytes, out cHeaderFields rFields) => ZTryConstruct(cHeaderFieldNames.Empty, true, pBytes, out rFields);
        internal static bool TryConstruct(cHeaderFieldNames pNames, bool pNot, IList<byte> pBytes, out cHeaderFields rFields) => ZTryConstruct(pNames, pNot, pBytes, out rFields);

        private static bool ZTryConstruct(cHeaderFieldNames pNames, bool pNot, IList<byte> pBytes, out cHeaderFields rFields)
        {
            if (pBytes == null) { rFields = null; return false; }

            cBytesCursor lCursor = new cBytesCursor(pBytes);
            List<cHeaderField> lFields = new List<cHeaderField>();

            while (true)
            {
                if (!lCursor.GetRFC822FieldName(out var lName)) break;
                lCursor.SkipRFC822WSP();
                if (!lCursor.SkipByte(cASCII.COLON)) { rFields = null; return false; }
                if (!lCursor.GetRFC822FieldValue(out var lValue)) { rFields = null; return false; }

                if (lName.Equals(kHeaderFieldName.References, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cHeaderFieldMsgIds.TryConstruct(lName, lValue, out var lReferences)) lFields.Add(lReferences);
                    else lFields.Add(new cHeaderField(lName, new cBytes(lValue)));
                }
                else if (lName.Equals(kHeaderFieldName.Importance, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cHeaderFieldImportance.TryConstruct(lValue, out var lImportance)) lFields.Add(lImportance);
                    else lFields.Add(new cHeaderField(lName, new cBytes(lValue)));
                }
                else lFields.Add(new cHeaderField(lName, new cBytes(lValue)));
            }

            if (!lCursor.Position.AtEnd && !lCursor.SkipBytes(cBytesCursor.CRLF)) { rFields = null; return false; }

            rFields = new cHeaderFields(pNames, pNot, lFields);
            return true;
        }

        /// <summary>
        /// Returns a collection that is the combination of the two specified header field collections.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static cHeaderFields operator +(cHeaderFields pA, cHeaderFields pB)
        {
            if (pA == null || (pA.mNames.Count == 0 && !pA.mNot)) return pB ?? Empty; // pA is null or Empty
            if (pB == null || (pB.mNames.Count == 0 && !pB.mNot)) return pA; // pB is null or Empty

            if (pA.mNames.Count == 0 && pA.mNot) return pA; // pA has all headers
            if (pB.mNames.Count == 0 && pB.mNot) return pB; // pB has all headers

            if (pA.mNames == pB.mNames && pA.mNot == pB.mNot) return pA; // they are the same, return either one

            if (pA.mNot)
            {
                if (pB.mNot)
                {
                    // pA contains all headers except some, pB contains all headers except some
                    List<cHeaderField> lFields = new List<cHeaderField>(pA);
                    foreach (var lName in pA.mNames.Except(pB.mNames)) lFields.AddRange(pB.GetAllNamed(lName));
                    return new cHeaderFields(pA.mNames.Intersect(pB.mNames), true, lFields);
                }
                else
                {
                    // pA contains all headers except some, pB contains a named list 
                    List<cHeaderField> lFields = new List<cHeaderField>(pA);
                    foreach (var lName in pA.mNames.Intersect(pB.mNames)) lFields.AddRange(pB.GetAllNamed(lName));
                    return new cHeaderFields(pA.mNames.Except(pB.mNames), true, lFields);
                }
            }
            else
            {
                if (pB.mNot)
                {
                    // pA contains a named list, pB contains all headers except some
                    List<cHeaderField> lFields = new List<cHeaderField>(pB);
                    foreach (var lName in pA.mNames.Intersect(pB.mNames)) lFields.AddRange(pA.GetAllNamed(lName));
                    return new cHeaderFields(pB.mNames.Except(pA.mNames), true, lFields);
                }
                else
                {
                    // pA contains a subset of header values and pB does too, add them together
                    List<cHeaderField> lFields = new List<cHeaderField>(pA);
                    foreach (var lName in pB.mNames.Except(pA.mNames)) lFields.AddRange(pB.GetAllNamed(lName));
                    return new cHeaderFields(pA.mNames.Union(pB.mNames), false, lFields);
                }
            }
        }









        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cHeaderFields), nameof(_Tests));

            cStrings lStrings;
            cHeaderFieldNames lABCDE = new cHeaderFieldNames("a", "B", "c", "D", "e");
            cHeaderFieldNames lDEFGH = new cHeaderFieldNames("f", "g", "h", "D", "e");
            cHeaderFieldNames lGHIJK = new cHeaderFieldNames("i", "g", "h", "j", "K");

            var lBytes =
                new cBytes(
                    "angus:  value of angus\r\n" +
                    "fred      :     value of fred      \r\n" +
                    "charlie  \t  :   value    \r\n    \t    of    \r\n   charlie  \t\t   \r\n" +
                    "message-id   :  <1234@local.machine.example>  \r\n" +
                    "MESSAGE-id  : <1234   @   local(blah)  .machine .example> \r\n" +
                    "IN-reply-TO:    <12341@local.machine.example><12342@local.machine.example>\r\n\t<12343@local.machine.example>\r\n" +
                    "REfEReNCeS:\r\n\t<12344@local.machine.example>\r\n\t<   \"12345\"   @   local(blah)   .machine .example>   \r\n" +
                    "Importance: low\r\n" +
                    "anotherone: just in case\r\n" +
                    "\r\n" +
                    "check: stop\r\n");

            if (!TryConstruct(lBytes, out var lFields)) throw new cTestsException($"{nameof(cHeaderFields)}.1.1");

            if (lFields.Count != 9) throw new cTestsException($"{nameof(cHeaderFields)}.1.1.1");

            if (!lFields.Contains("a")) throw new cTestsException($"{nameof(cHeaderFields)}.1.2");
            if (!lFields.Contains(lABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.1.3");
            if (!lFields.Contains(lDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.1.4");
            if (!lFields.Contains(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.1.5");
            if (lFields.ContainsNone(lABCDE) || lFields.ContainsNone(lDEFGH) || lFields.ContainsNone(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.1.6");

            if (lFields.GetMissing(lABCDE).Count != 0 || lFields.GetMissing(lDEFGH).Count != 0) throw new cTestsException($"{nameof(cHeaderFields)}.1.7");

            if (cTools.ASCIIBytesToString(lFields.GetFirstNamed("fred").Value) != "     value of fred      ") throw new cTestsException($"{nameof(cHeaderFields)}.1.8");
            if (lFields.GetFirstNamed("a") != null) throw new cTestsException($"{nameof(cHeaderFields)}.1.9");

            if (lFields.GetAllNamed("fred").Count() != 1) throw new cTestsException($"{nameof(cHeaderFields)}.1.10");
            if (lFields.GetAllNamed("a").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.1.11");
            if (lFields.GetAllNamed("mEsSaGe-ID").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.1.12");

            //if (!lFields.All("mEsSaGe-ID").All(h => h is cHeaderFieldMsgId lMsgId && lMsgId.MsgId == "1234@local.machine.example")) throw new cTestsException($"{nameof(cHeaderFields)}.1.13");


            //lStrings = (lFields.First(cHeaderFieldNames.InReplyTo) as cHeaderFieldMsgIds)?.MsgIds;
            //if (lStrings.Count != 3 || !lStrings.Contains("12341@local.machine.example") || !lStrings.Contains("12342@local.machine.example") || !lStrings.Contains("12343@local.machine.example")) throw new cTestsException($"{nameof(cHeaderFields)}.1.15");

            lStrings = lFields.References;
            if (lStrings.Count != 2 || !lStrings.Contains("12344@local.machine.example") || !lStrings.Contains("12345@local.machine.example")) throw new cTestsException($"{nameof(cHeaderFields)}.1.16");

            if (lFields.Importance != eImportance.low) throw new cTestsException($"{nameof(cHeaderFields)}.1.17");


            if (!lFields.Contains("check") || lFields.GetAllNamed("check").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.1.18");




            lBytes = new cBytes("a: 1\r\nc: 2\r\nc: two\r\ne: 3\r\n\r\n");
            if (!TryConstruct(lABCDE, false, lBytes, out var lFieldsABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.1");
            if (!TryConstruct(lABCDE, false, lBytes, out var lFieldsABCDE2)) throw new cTestsException($"{nameof(cHeaderFields)}.2.1.1");

            lBytes = new cBytes("e: 3\r\ng: 4\r\ng: four\r\n\r\n");
            if (!TryConstruct(lDEFGH, false, lBytes, out var lFieldsDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.2.2");

            lBytes = new cBytes("g: 4\r\ni: 5\r\nk: 6\r\nk: six\r\n");
            if (!TryConstruct(lGHIJK, false, lBytes, out var lFieldsGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.3");

            lBytes =
                new cBytes(
                    "Importance: normal\r\n" +
                    "message-id   :  <1234@local.machine.example  \r\n" +
                    "IN-reply-TO:    <12341@local.machine.example><12342@local.machine.example\r\n\t<12343@local.machine.example>\r\n" +
                    "g: 4\r\ng: four\r\ni: 5\r\nk: 6\r\nk: six\r\n");

            if (!TryConstruct(lABCDE, true, lBytes, out var lNotABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.4");

            lBytes =
                new cBytes(
                    "Importance: high\r\n" +
                    "a: 1\r\nc: 2\r\nc: two\r\ni: 5\r\nk: 6\r\nk: six\r\n");

            if (!TryConstruct(lDEFGH, true, lBytes, out var lNotDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.2.4");

            lBytes =
                new cBytes(
                    "Importance: error\r\n" +
                    "a: 1\r\nc: 2\r\nc: two\r\ne: 3\r\n");

            if (!TryConstruct(lGHIJK, true, lBytes, out var lNotGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.5");


            if (lNotABCDE.Contains("a") || lNotABCDE.Contains("e") || !lNotABCDE.Contains("f") || !lNotABCDE.Contains("g")) throw new cTestsException($"{nameof(cHeaderFields)}.2.6");
            if (lNotABCDE.Contains(lABCDE) || lNotABCDE.Contains(lDEFGH) || !lNotABCDE.Contains(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.7");
            if (!lNotABCDE.ContainsNone(lABCDE) || lNotABCDE.ContainsNone(lDEFGH) || lNotABCDE.ContainsNone(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.8");
            if (lNotABCDE.GetMissing(lABCDE) != lABCDE || lNotABCDE.GetMissing(lDEFGH) != new cHeaderFieldNames("d", "E") || lNotABCDE.GetMissing(lGHIJK).Count != 0) throw new cTestsException($"{nameof(cHeaderFields)}.2.9");

            bool lFailed;

            lFailed = false;
            try { lNotABCDE.GetFirstNamed("A"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFields)}.2.10");

            if (lNotABCDE.GetFirstNamed(kHeaderFieldName.MessageId) == null || lNotABCDE.GetFirstNamed(kHeaderFieldName.InReplyTo) == null || lNotABCDE.Importance != eImportance.normal) throw new cTestsException($"{nameof(cHeaderFields)}.2.11");
            if (lNotDEFGH.Importance != eImportance.high) throw new cTestsException($"{nameof(cHeaderFields)}.2.12");
            if (lNotGHIJK.Importance != null) throw new cTestsException($"{nameof(cHeaderFields)}.2.13");

            if (!ReferenceEquals((lFieldsABCDE + null), lFieldsABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.1");
            if (!ReferenceEquals((null + lFieldsABCDE), lFieldsABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.2");
            if (!ReferenceEquals((lFieldsABCDE + lFields), lFields)) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.3");
            if (!ReferenceEquals(lFields, (lFieldsABCDE + lFields))) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.4");
            if (!ReferenceEquals(lFieldsABCDE, (lFieldsABCDE + lFieldsABCDE2))) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.5");

            var lNotDE = lNotABCDE + lNotDEFGH;
            if (!lNotDE.ContainsNone(new cHeaderFieldNames("d", "E"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.1");
            if (!lNotDE.Contains(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.2");
            if (!lNotDE.Contains(new cHeaderFieldNames("a", "c", "f", "h"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.3");
            if (lNotDE.GetAllNamed("a").Count() != 1 || lNotDE.GetAllNamed("b").Count() != 0 || lNotDE.GetAllNamed("c").Count() != 2 || lNotDE.GetAllNamed("f").Count() != 0 || lNotDE.GetAllNamed("g").Count() != 2 || lNotDE.GetAllNamed("h").Count() != 0 || lNotDE.GetAllNamed("i").Count() != 1 || lNotDE.GetAllNamed("j").Count() != 0 || lNotDE.GetAllNamed("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.4");

            var lAll = lNotABCDE + lNotGHIJK;
            if (!lAll.Contains(lABCDE) || !lAll.Contains(lDEFGH) || !lAll.Contains(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.16.1");
            if (lAll.GetAllNamed("a").Count() != 1 || lAll.GetAllNamed("b").Count() != 0 || lAll.GetAllNamed("c").Count() != 2 || lAll.GetAllNamed("d").Count() != 0 || lAll.GetAllNamed("e").Count() != 1 || lAll.GetAllNamed("f").Count() != 0 || lAll.GetAllNamed("g").Count() != 2 || lAll.GetAllNamed("h").Count() != 0 || lAll.GetAllNamed("i").Count() != 1 || lAll.GetAllNamed("j").Count() != 0 || lAll.GetAllNamed("k").Count() != 2 ) throw new cTestsException($"{nameof(cHeaderFields)}.2.16.2");

            var lNotABC = lNotABCDE + lFieldsDEFGH;
            if (!lNotABC.ContainsNone(new cHeaderFieldNames("a", "B", "C"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.17.1");
            if (!lNotABC.Contains(new cHeaderFieldNames("d", "e", "f", "g", "h"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.17.2");
            if (lNotABC.GetAllNamed("d").Count() != 0 || lNotABC.GetAllNamed("e").Count() != 1 || lNotABC.GetAllNamed("f").Count() != 0 || lNotABC.GetAllNamed("g").Count() != 2 || lNotABC.GetAllNamed("h").Count() != 0 || lNotABC.GetAllNamed("i").Count() != 1 || lNotABC.GetAllNamed("j").Count() != 0 || lNotABC.GetAllNamed("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.16.2");

            var lNotABCDE2 = lNotABCDE + lFieldsGHIJK;
            if (!lNotABCDE2.ContainsNone(lABCDE) || !lNotABCDE2.Contains(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.18.1");
            if (lNotABCDE2.GetAllNamed("f").Count() != 0 || lNotABCDE2.GetAllNamed("g").Count() != 2 || lNotABCDE2.GetAllNamed("h").Count() != 0 || lNotABCDE2.GetAllNamed("i").Count() != 1 || lNotABCDE2.GetAllNamed("j").Count() != 0 || lNotABCDE2.GetAllNamed("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.18.2");

            var lNotFGH = lFieldsABCDE + lNotDEFGH;
            if (!lNotFGH.ContainsNone(new cHeaderFieldNames("F","G","H")) || !lNotFGH.Contains(lABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.19.1");
            if (lNotFGH.GetAllNamed("a").Count() != 1 || lNotFGH.GetAllNamed("b").Count() != 0 || lNotFGH.GetAllNamed("c").Count() != 2 || lNotFGH.GetAllNamed("d").Count() != 0 || lNotFGH.GetAllNamed("e").Count() != 1 || lNotFGH.GetAllNamed("i").Count() != 1 || lNotFGH.GetAllNamed("j").Count() != 0 || lNotFGH.GetAllNamed("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.19.2");

            var lNotGHIJK2 = lFieldsABCDE + lNotGHIJK;
            if (!lNotGHIJK2.ContainsNone(lGHIJK) || !lNotGHIJK2.Contains(lABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.20.1");
            if (lNotGHIJK2.GetAllNamed("a").Count() != 1 || lNotGHIJK2.GetAllNamed("b").Count() != 0 || lNotGHIJK2.GetAllNamed("c").Count() != 2 || lNotGHIJK2.GetAllNamed("d").Count() != 0 || lNotGHIJK2.GetAllNamed("e").Count() != 1 || lNotGHIJK2.GetAllNamed("f").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.2.20.2");

            var lAtoH = lFieldsABCDE + lFieldsDEFGH;
            if (!lAtoH.Contains(lABCDE) || !lAtoH.Contains(lDEFGH) || !lAtoH.ContainsNone(new cHeaderFieldNames("I", "J", "K"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.21.1");
            if (lAtoH.GetAllNamed("a").Count() != 1 || lAtoH.GetAllNamed("b").Count() != 0 || lAtoH.GetAllNamed("c").Count() != 2 || lAtoH.GetAllNamed("d").Count() != 0 || lAtoH.GetAllNamed("e").Count() != 1 || lAtoH.GetAllNamed("f").Count() != 0 || lAtoH.GetAllNamed("g").Count() != 2 || lAtoH.GetAllNamed("h").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.2.21.2");

            lFailed = false;
            try { lAtoH.GetAllNamed("i"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFields)}.2.21.3");

            var lNotF = lFieldsABCDE + lFieldsGHIJK;
            if (lNotF.Contains("F") || !lNotF.Contains(lABCDE) || !lNotF.Contains(lGHIJK) || lNotF.Contains(lDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.2.22.1");
            if (lNotF.GetAllNamed("a").Count() != 1 || lNotF.GetAllNamed("b").Count() != 0 || lNotF.GetAllNamed("c").Count() != 2 || lNotF.GetAllNamed("d").Count() != 0 || lNotF.GetAllNamed("e").Count() != 1  || lNotF.GetAllNamed("g").Count() != 1 || lNotF.GetAllNamed("h").Count() != 0 || lNotF.GetAllNamed("i").Count() != 1 || lNotF.GetAllNamed("j").Count() != 0 || lNotF.GetAllNamed("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.22.2");




        }
    }
}