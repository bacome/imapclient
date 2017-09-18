using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cHeaderField
    {
        public readonly string Name; // uppercase
        public readonly cBytes Value;

        public cHeaderField(string pName, cBytes pValue)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            Name = pName.ToUpperInvariant();
            Value = pValue ?? throw new ArgumentNullException(nameof(pValue));
        }

        public override string ToString() => $"{nameof(cHeaderField)}({Name},{Value})";
    }

    public class cHeaderFieldImportance : cHeaderField
    {
        private static readonly cBytes kLow = new cBytes("low");
        private static readonly cBytes kNormal = new cBytes("normal");
        private static readonly cBytes kHigh = new cBytes("high");

        public readonly eImportance Importance;

        public cHeaderFieldImportance(cBytes pValue, eImportance pImportance) : base(cHeaderFieldNames.Importance, pValue) { Importance = pImportance; }

        public static bool TryConstruct(IList<byte> pValue, out cHeaderFieldImportance rImportance)
        {
            if (pValue == null) { rImportance = null; return false; }

            eImportance lImportance;

            cBytesCursor lCursor = new cBytesCursor(pValue);

            lCursor.SkipRFC822CFWS();

            if (lCursor.SkipBytes(kLow)) lImportance = eImportance.low;
            else if (lCursor.SkipBytes(kNormal)) lImportance = eImportance.normal;
            else if (lCursor.SkipBytes(kHigh)) lImportance = eImportance.high;
            else { rImportance = null; return false; }

            lCursor.SkipRFC822CFWS();

            if (lCursor.Position.AtEnd)
            {
                rImportance = new cHeaderFieldImportance(new cBytes(pValue), lImportance);
                return true;
            }

            rImportance = null;
            return false;
        }

        public override string ToString() => $"{nameof(cHeaderFieldImportance)}({Importance})";
    }

    public class cHeaderFieldMsgId : cHeaderField
    {
        public readonly string MsgId;

        public cHeaderFieldMsgId(string pName, cBytes pValue, string pMsgId) : base(pName, pValue) { MsgId = pMsgId; }

        public static bool TryConstruct(string pName, IList<byte> pValue, out cHeaderFieldMsgId rMsgId)
        {
            if (pValue == null) { rMsgId = null; return false; }

            cBytesCursor lCursor = new cBytesCursor(pValue);

            if (!lCursor.GetRFC822MsgId(out var lMsgId) || !lCursor.Position.AtEnd)
            {
                rMsgId = null;
                return false;
            }

            rMsgId = new cHeaderFieldMsgId(pName, new cBytes(pValue), lMsgId);
            return true;
        }

        public override string ToString() => $"{nameof(cHeaderFieldMsgId)}({MsgId})";
    }

    public class cHeaderFieldMsgIds : cHeaderField
    {
        public readonly cStrings MsgIds;

        public cHeaderFieldMsgIds(string pName, cBytes pValue, cStrings pMsgIds) : base(pName, pValue) { MsgIds = pMsgIds; }

        public static bool TryConstruct(string pName, IList<byte> pValue, out cHeaderFieldMsgIds rMsgIds)
        {
            if (pValue == null) { rMsgIds = null; return false; }

            List<string> lMsgIds = new List<string>();

            cBytesCursor lCursor = new cBytesCursor(pValue);

            while (true)
            {
                if (!lCursor.GetRFC822MsgId(out var lMsgId)) break;
                lMsgIds.Add(lMsgId);
            }

            if (lCursor.Position.AtEnd)
            {
                rMsgIds = new cHeaderFieldMsgIds(pName, new cBytes(pValue), new cStrings(lMsgIds));
                return true;
            }

            rMsgIds = null;
            return false;
        }

        public override string ToString() => $"{nameof(cHeaderFieldMsgIds)}({MsgIds})";
    }

    public class cHeaderFields
    {
        private readonly cHeaderFieldNames mNames; // not null (may be empty)
        private readonly bool mNot;
        private readonly ReadOnlyCollection<cHeaderField> mFields; // not null (may be empty)

        public cHeaderFields(cHeaderFieldNames pNames, bool pNot, IList<cHeaderField> pFields)
        {
            mNames = pNames ?? throw new ArgumentNullException(nameof(pNames));
            mNot = pNot;
            if (pFields == null) throw new ArgumentNullException(nameof(pFields));
            mFields = new ReadOnlyCollection<cHeaderField>(pFields);
        }

        public bool Contains(string pFieldName) => mNot != mNames.Contains(pFieldName);

        public bool ContainsAll(cHeaderFieldNames pNames)
        {
            if (mNot) return pNames.Intersect(mNames).Count == 0;
            else return pNames.Except(mNames).Count == 0;
        }

        public bool ContainsNone(cHeaderFieldNames pNames)
        {
            if (mNot) return pNames.Except(mNames).Count == 0;
            else return pNames.Intersect(mNames).Count == 0;
        }

        public cHeaderFieldNames Missing(cHeaderFieldNames pNames)
        {
            if (mNot) return pNames.Intersect(mNames);
            else return pNames.Except(mNames);
        }

        public cHeaderField First(string pFieldName)
        {
            if (!Contains(pFieldName)) throw new InvalidOperationException();
            return mFields.FirstOrDefault(f => f.Name.Equals(pFieldName, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<cHeaderField> All(string pFieldName)
        {
            if (!Contains(pFieldName)) throw new InvalidOperationException();
            return from f in mFields where f.Name.Equals(pFieldName, StringComparison.InvariantCultureIgnoreCase) select f;
        }

        public string MessageId => (First(cHeaderFieldNames.MessageId) as cHeaderFieldMsgId)?.MsgId;
        public cStrings InReplyTo => (First(cHeaderFieldNames.InReplyTo) as cHeaderFieldMsgIds)?.MsgIds;
        public cStrings References => (First(cHeaderFieldNames.References) as cHeaderFieldMsgIds)?.MsgIds;
        public eImportance? Importance => (First(cHeaderFieldNames.Importance) as cHeaderFieldImportance)?.Importance;

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHeaderFields));
            lBuilder.Append(mNames);
            lBuilder.Append(mNot);
            foreach (var lField in mFields) lBuilder.Append(lField);
            return lBuilder.ToString();
        }

        public static bool TryConstruct(cSection pSection, IList<byte> pBytes, out cHeaderFields rFields)
        {
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pSection.TextPart != eSectionPart.all && pSection.TextPart != eSectionPart.header && pSection.TextPart != eSectionPart.headerfields && pSection.TextPart != eSectionPart.headerfieldsnot) { rFields = null; return false; }
            if (pBytes == null) { rFields = null; return false; }

            cBytesCursor lCursor = new cBytesCursor(pBytes);
            List<cHeaderField> lFields = new List<cHeaderField>();

            while (true)
            {
                if (!lCursor.GetRFC822FieldName(out var lName)) break;
                lCursor.SkipRFC822WSP();
                if (!lCursor.SkipByte(cASCII.COLON)) { rFields = null; return false; }
                if (!lCursor.GetRFC822FieldValue(out var lValue)) { rFields = null; return false; }

                if (lName == cHeaderFieldNames.MessageId)
                {
                    if (cHeaderFieldMsgId.TryConstruct(lName, lValue, out var lMessageId)) lFields.Add(lMessageId);
                }
                else if (lName == cHeaderFieldNames.InReplyTo)
                {
                    if (cHeaderFieldMsgIds.TryConstruct(lName, lValue, out var lInReplyTo)) lFields.Add(lInReplyTo);
                }
                else if (lName == cHeaderFieldNames.References)
                {
                    if (cHeaderFieldMsgIds.TryConstruct(lName, lValue, out var lReferences)) lFields.Add(lReferences);
                }
                else if (lName == cHeaderFieldNames.Importance)
                {
                    if (cHeaderFieldImportance.TryConstruct(lValue, out var lImportance)) lFields.Add(lImportance);
                }
                else lFields.Add(new cHeaderField(lName, new cBytes(lValue)));
            }

            if (!lCursor.Position.AtEnd || !lCursor.SkipBytes(cBytesCursor.CRLF)) { rFields = null; return false; }

            cHeaderFieldNames lNames;
            if (pSection.TextPart == eSectionPart.headerfields || pSection.TextPart == eSectionPart.headerfieldsnot) lNames = pSection.Names;
            else lNames = cHeaderFieldNames.None;

            rFields = new cHeaderFields(lNames, pSection.TextPart != eSectionPart.headerfields, lFields);
            return true;
        }

        public static cHeaderFields operator +(cHeaderFields pA, cHeaderFields pB)
        {
            if (pA == null) return pB;
            if (pB == null) return pA;

            if (pA.mNames.Count == 0 && pA.mNot) return pA; // pA has all headers
            if (pB.mNames.Count == 0 && pB.mNot) return pB; // pB has all headers

            if (pA.mNames == pB.mNames && pA.mNot == pB.mNot) return pA; // they are the same, return either one

            if (pA.mNot)
            {
                if (pB.mNot)
                {
                    // pA contains all headers except some, pB contains all headers except some
                    List<cHeaderField> lFields = new List<cHeaderField>(pA.mFields);
                    foreach (var lFieldName in pA.mNames.Except(pB.mNames)) lFields.AddRange(pB.All(lFieldName));
                    return new cHeaderFields(pA.mNames.Intersect(pB.mNames), true, lFields);
                }
                else
                {
                    // pA contains all headers except some, pB contains a named list 
                    List<cHeaderField> lFields = new List<cHeaderField>(pA.mFields);
                    foreach (var lFieldName in pA.mNames.Intersect(pB.mNames)) lFields.AddRange(pB.All(lFieldName));
                    return new cHeaderFields(pA.mNames.Except(pB.mNames), true, lFields);
                }
            }
            else
            {
                if (pB.mNot)
                {
                    // pA contains a named list, pB contains all headers except some
                    List<cHeaderField> lFields = new List<cHeaderField>(pB.mFields);
                    foreach (var lFieldName in pA.mNames.Intersect(pB.mNames)) lFields.AddRange(pA.All(lFieldName));
                    return new cHeaderFields(pB.mNames.Except(pA.mNames), true, lFields);
                }
                else
                {
                    // pA contains a subset of header values and pB does too, add them together
                    List<cHeaderField> lFields = new List<cHeaderField>(pA.mFields);
                    foreach (var lFieldName in pB.mNames.Except(pA.mNames)) lFields.AddRange(pB.All(lFieldName));
                    return new cHeaderFields(pA.mNames.Union(pB.mNames), false, lFields);
                }
            }
        }









        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cHeaderFields), nameof(_Tests));

            // especially contains, containsall and the various join types







            cBytes lWhole =
                new cBytes(
                    "angus:  value of angus\r\n" +
                    "fred      :     value of fred      \r\n" +
                    "charlie  \t  :   value    \r\n    \t    of    \r\n   charlie  \t\t   \r\n" +
                    "message-id   :  <1234@local.machine.example>  \r\n" +
                    "MESSAGE-id   <1234   @   local(blah)  .machine .example> \r\n" +
                    "IN-reply-TO:    <12341@local.machine.example><12342@local.machine.example>\r\n\t<12343@local.machine.example>\r\n" +
                    "REfEReNCeS:\r\n\t<12344@local.machine.example>\r\n\t<12345@local.machine.example>\r\n" +
                    "Importance: low\r\n" +
                    "anotherone: just in case\r\n" +
                    "\r\n" +
                    "check: stop\r\n");

            cSection lABCDE = ;?; new cSection(null, new cHeaderFieldNames("angus", "fred", "charlie", "max"));

            ;?;
            cBytes ABCDE =
                new cBytes(
                    "a:  arnie\r\n" +
                    "b      :     barney     \r\n" + 
                    "c      :     charlie      \r\n");

            cBytes DEF =
                new cBytes(
                    "d:  danny\r\n" +
                    "e      :     ernie      \r\n" +
                    "f      :     freddy      \r\n");

            cBytes CDE =
                new cBytes(
                    "c      :     charlie      \r\n" +
                    "d:  danny\r\n" +
                    "e      :     ernie      \r\n"  );

            cBytes lInvalid1 =
                new cBytes(
                    "angus:  value of angus\r\n" +
                    "a test      :     value of fred      \r\n");

            cBytes lInvalid2 =
                new cBytes(
                    "angus:  value of angus\r\n" +
                    "fred      :     value of fred     \r\n ");

            if (!TryConstruct(cSection.All, lWhole, out var lFieldsAll)) throw new cTestsException($"{nameof(cHeaderFields)}.1.1");
            if (!TryConstruct(lNone, lWhole, out var lFieldsNone)) throw new cTestsException($"{nameof(cHeaderFields)}.1.2");
            if (!TryConstruct(lAFC, lSome, out var lFieldsAFC)) throw new cTestsException($"{nameof(cHeaderFields)}.1.3");
            if (TryConstruct(lAll, lInvalid1, out var lFieldsInvalid1)) throw new cTestsException($"{nameof(cHeaderFields)}.1.4");
            if (TryConstruct(lAll, lInvalid2, out var lFieldsInvalid2)) throw new cTestsException($"{nameof(cHeaderFields)}.1.5");


            if (!lFields1.Contains("mike") || !lFields1.Contains("angus")) throw new cTestsException($"{nameof(cHeaderFields)}.3");












        }
    }
}