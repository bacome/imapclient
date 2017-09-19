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

    public class cHeaderFieldMsgId : cHeaderField
    {
        public readonly string MsgId;

        private cHeaderFieldMsgId(string pName, cBytes pValue, string pMsgId) : base(pName, pValue) { MsgId = pMsgId; }

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

        private cHeaderFieldMsgIds(string pName, cBytes pValue, cStrings pMsgIds) : base(pName, pValue) { MsgIds = pMsgIds; }

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

    public class cHeaderFieldImportance : cHeaderField
    {
        private static readonly cBytes kLow = new cBytes("low");
        private static readonly cBytes kNormal = new cBytes("normal");
        private static readonly cBytes kHigh = new cBytes("high");

        public readonly eImportance Importance;

        private cHeaderFieldImportance(cBytes pValue, eImportance pImportance) : base(cHeaderFieldNames.Importance, pValue) { Importance = pImportance; }

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

    public class cHeaderFields
    {
        private readonly cHeaderFieldNames mNames; // not null (may be empty)
        private readonly bool mNot;
        private readonly ReadOnlyCollection<cHeaderField> mFields; // not null (may be empty)

        private cHeaderFields(cHeaderFieldNames pNames, bool pNot, IList<cHeaderField> pFields)
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

        public static bool TryConstruct(IList<byte> pBytes, out cHeaderFields rFields) => ZTryConstruct(pBytes, cHeaderFieldNames.None, true, out rFields);
        public static bool TryConstruct(IList<byte> pBytes, cHeaderFieldNames pNames, bool pNot, out cHeaderFields rFields) => ZTryConstruct(pBytes, pNames, pNot, out rFields);

        private static bool ZTryConstruct(IList<byte> pBytes, cHeaderFieldNames pNames, bool pNot, out cHeaderFields rFields)
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

                if (lName.Equals(cHeaderFieldNames.References, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cHeaderFieldMsgIds.TryConstruct(lName, lValue, out var lReferences)) lFields.Add(lReferences);
                    else lFields.Add(new cHeaderField(lName, new cBytes(lValue)));
                }
                else if (lName.Equals(cHeaderFieldNames.Importance, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cHeaderFieldImportance.TryConstruct(lValue, out var lImportance)) lFields.Add(lImportance);
                    else lFields.Add(new cHeaderField(lName, new cBytes(lValue)));
                }
                else lFields.Add(new cHeaderField(lName, new cBytes(lValue)));
            }

            if (!lCursor.Position.AtEnd || !lCursor.SkipBytes(cBytesCursor.CRLF)) { rFields = null; return false; }

            rFields = new cHeaderFields(pNames, pNot, lFields);
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
                    "MESSAGE-id   <1234   @   local(blah)  .machine .example> \r\n" +
                    "IN-reply-TO:    <12341@local.machine.example><12342@local.machine.example>\r\n\t<12343@local.machine.example>\r\n" +
                    "REfEReNCeS:\r\n\t<12344@local.machine.example>\r\n\t<12345@local.machine.example>\r\n" +
                    "Importance: low\r\n" +
                    "anotherone: just in case\r\n" +
                    "\r\n" +
                    "check: stop\r\n");

            if (!TryConstruct(lBytes, out var lFields)) throw new cTestsException($"{nameof(cHeaderFields)}.1.1");

            if (lFields.mFields.Count != 9) throw new cTestsException($"{nameof(cHeaderFields)}.1.1.1");

            if (!lFields.Contains("a")) throw new cTestsException($"{nameof(cHeaderFields)}.1.2");
            if (!lFields.ContainsAll(lABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.1.3");
            if (!lFields.ContainsAll(lDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.1.4");
            if (!lFields.ContainsAll(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.1.5");
            if (lFields.ContainsNone(lABCDE) || lFields.ContainsNone(lDEFGH) || lFields.ContainsNone(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.1.6");

            if (lFields.Missing(lABCDE).Count != 0 || lFields.Missing(lDEFGH).Count != 0) throw new cTestsException($"{nameof(cHeaderFields)}.1.7");

            if (cTools.ASCIIBytesToString(lFields.First("fred").Value) != "     value of fred      \r\n") throw new cTestsException($"{nameof(cHeaderFields)}.1.8");
            if (cTools.ASCIIBytesToString(lFields.First("a").Value) != null) throw new cTestsException($"{nameof(cHeaderFields)}.1.9");

            if (lFields.All("fred").Count() != 1) throw new cTestsException($"{nameof(cHeaderFields)}.1.10");
            if (lFields.All("a").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.1.11");
            if (lFields.All("mEsSaGe-ID").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.1.12");

            if (!lFields.All("mEsSaGe-ID").All(h => h is cHeaderFieldMsgId lMsgId && lMsgId.MsgId == "1234@local.machine.example")) throw new cTestsException($"{nameof(cHeaderFields)}.1.13");

            if (lFields.First(cHeaderFieldNames.MessageId) as c != "1234@local.machine.example") throw new cTestsException($"{nameof(cHeaderFields)}.1.14");

            lStrings = lFields.InReplyTo;
            if (lStrings.Count != 3 || !lStrings.Contains("12341@local.machine.example") || !lStrings.Contains("12342@local.machine.example") || !lStrings.Contains("12343@local.machine.example")) throw new cTestsException($"{nameof(cHeaderFields)}.1.15");

            lStrings = lFields.References;
            if (lStrings.Count != 2 || !lStrings.Contains("12344@local.machine.example") || !lStrings.Contains("12345@local.machine.example")) throw new cTestsException($"{nameof(cHeaderFields)}.1.16");

            if (lFields.Importance != eImportance.low) throw new cTestsException($"{nameof(cHeaderFields)}.1.17");


            if (!lFields.Contains("check") || lFields.All("check").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.1.18");




            lBytes = new cBytes("a: 1\r\nc: 2\r\nc: two\r\ne: 3\r\n\r\n");
            if (!TryConstruct(lBytes, lABCDE, false, out var lFieldsABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.1");
            if (!TryConstruct(lBytes, lABCDE, false, out var lFieldsABCDE2)) throw new cTestsException($"{nameof(cHeaderFields)}.2.1.1");

            lBytes = new cBytes("e: 3\r\ng: 4\r\ng: four\r\n\r\n");
            if (!TryConstruct(lBytes, lDEFGH, false, out var lFieldsDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.2.2");

            lBytes = new cBytes("g: 4\r\ni: 5\r\nk: 6\r\nk: six\r\n");
            if (!TryConstruct(lBytes, lGHIJK, false, out var lFieldsGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.3");

            lBytes =
                new cBytes(
                    "Importance: normal\r\n" +
                    "message-id   :  <1234@local.machine.example  \r\n" +
                    "IN-reply-TO:    <12341@local.machine.example><12342@local.machine.example\r\n\t<12343@local.machine.example>\r\n" +
                    "g: 4\r\ng: four\r\ni: 5\r\nk: 6\r\nk: six\r\n");

            if (!TryConstruct(lBytes, lABCDE, true, out var lNotABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.4");

            lBytes =
                new cBytes(
                    "Importance: high\r\n" +
                    "a: 1\r\nc: 2\r\nc: two\r\ni: 5\r\nk: 6\r\nk: six\r\n");

            if (!TryConstruct(lBytes, lDEFGH, true, out var lNotDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.2.4");

            lBytes =
                new cBytes(
                    "Importance: error\r\n" +
                    "a: 1\r\nc: 2\r\nc: two\r\ne: 3\r\n");

            if (!TryConstruct(lBytes, lGHIJK, true, out var lNotGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.5");


            if (lNotABCDE.Contains("a") || lNotABCDE.Contains("e") || !lNotABCDE.Contains("f") || !lNotABCDE.Contains("g")) throw new cTestsException($"{nameof(cHeaderFields)}.2.6");
            if (lNotABCDE.ContainsAll(lABCDE) || lNotABCDE.ContainsAll(lDEFGH) || !lNotABCDE.ContainsAll(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.7");
            if (!lNotABCDE.ContainsNone(lABCDE) || lNotABCDE.ContainsNone(lDEFGH) || lNotABCDE.ContainsNone(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.8");
            if (lNotABCDE.Missing(lABCDE) != lABCDE || lNotABCDE.Missing(lDEFGH) != new cHeaderFieldNames("d", "E") || lNotABCDE.Missing(lGHIJK).Count != 0) throw new cTestsException($"{nameof(cHeaderFields)}.2.9");

            bool lFailed;

            lFailed = false;
            try { lNotABCDE.First("A"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFields)}.2.10");

            if (lNotABCDE.MessageId != null || lNotABCDE.InReplyTo != null || lNotABCDE.Importance != eImportance.normal) throw new cTestsException($"{nameof(cHeaderFields)}.2.11");
            if (lNotDEFGH.Importance != eImportance.high) throw new cTestsException($"{nameof(cHeaderFields)}.2.12");
            if (lNotGHIJK.Importance != null) throw new cTestsException($"{nameof(cHeaderFields)}.2.13");

            if (!ReferenceEquals((lFieldsABCDE + null), lFieldsABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.1");
            if (!ReferenceEquals((null + lFieldsABCDE), lFieldsABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.2");
            if (!ReferenceEquals((lFieldsABCDE + lFields), lFields)) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.3");
            if (!ReferenceEquals(lFields, (lFieldsABCDE + lFields))) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.4");
            if (!ReferenceEquals(lFieldsABCDE, (lFieldsABCDE + lFieldsABCDE2))) throw new cTestsException($"{nameof(cHeaderFields)}.2.14.5");

            var lNotDE = lNotABCDE + lNotDEFGH;
            if (!lNotDE.ContainsNone(new cHeaderFieldNames("d", "E"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.1");
            if (!lNotDE.ContainsAll(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.2");
            if (!lNotDE.ContainsAll(new cHeaderFieldNames("a", "c", "f", "h"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.3");
            if (lNotDE.All("a").Count() != 1 || lNotDE.All("b").Count() != 0 || lNotDE.All("c").Count() != 2 || lNotDE.All("f").Count() != 0 || lNotDE.All("g").Count() != 2 || lNotDE.All("h").Count() != 0 || lNotDE.All("i").Count() != 1 || lNotDE.All("j").Count() != 0 || lNotDE.All("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.15.4");

            var lAll = lNotABCDE + lNotGHIJK;
            if (!lAll.ContainsAll(lABCDE) || !lAll.ContainsAll(lDEFGH) || !lAll.ContainsAll(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.16.1");
            if (lAll.All("a").Count() != 1 || lAll.All("b").Count() != 0 || lAll.All("c").Count() != 2 || lAll.All("d").Count() != 0 || lAll.All("e").Count() != 1 || lAll.All("f").Count() != 0 || lAll.All("g").Count() != 2 || lAll.All("h").Count() != 0 || lAll.All("i").Count() != 1 || lAll.All("j").Count() != 0 || lAll.All("k").Count() != 2 ) throw new cTestsException($"{nameof(cHeaderFields)}.2.16.2");

            var lNotABC = lNotABCDE + lFieldsDEFGH;
            if (!lNotABC.ContainsNone(new cHeaderFieldNames("a", "B", "C"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.17.1");
            if (!lNotABC.ContainsAll(new cHeaderFieldNames("d", "e", "f", "g", "h"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.17.2");
            if (lNotABC.All("d").Count() != 0 || lNotABC.All("e").Count() != 1 || lNotABC.All("f").Count() != 0 || lNotABC.All("g").Count() != 2 || lNotABC.All("h").Count() != 0 || lNotABC.All("i").Count() != 1 || lNotABC.All("j").Count() != 0 || lNotABC.All("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.16.2");

            var lNotABCDE2 = lNotABCDE + lFieldsGHIJK;
            if (!lNotABCDE2.ContainsNone(lABCDE) || !lNotABCDE2.ContainsAll(lGHIJK)) throw new cTestsException($"{nameof(cHeaderFields)}.2.18.1");
            if (lNotABCDE2.All("f").Count() != 0 || lNotABCDE2.All("g").Count() != 2 || lNotABCDE2.All("h").Count() != 0 || lNotABCDE2.All("i").Count() != 1 || lNotABCDE2.All("j").Count() != 0 || lNotABCDE2.All("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.18.2");

            var lNotFGH = lFieldsABCDE + lNotDEFGH;
            if (!lNotFGH.ContainsNone(new cHeaderFieldNames("F","G","H")) || !lNotFGH.ContainsAll(lABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.19.1");
            if (lNotFGH.All("a").Count() != 1 || lNotFGH.All("b").Count() != 0 || lNotFGH.All("c").Count() != 2 || lNotFGH.All("d").Count() != 0 || lNotFGH.All("e").Count() != 1 || lNotFGH.All("i").Count() != 1 || lNotFGH.All("j").Count() != 0 || lNotFGH.All("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.19.2");

            var lNotGHIJK2 = lFieldsABCDE + lNotGHIJK;
            if (!lNotGHIJK2.ContainsNone(lGHIJK) || !lNotGHIJK2.ContainsAll(lABCDE)) throw new cTestsException($"{nameof(cHeaderFields)}.2.20.1");
            if (lNotGHIJK2.All("a").Count() != 1 || lNotGHIJK2.All("b").Count() != 0 || lNotGHIJK2.All("c").Count() != 2 || lNotGHIJK2.All("d").Count() != 0 || lNotGHIJK2.All("e").Count() != 1 || lNotGHIJK2.All("f").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.2.20.2");

            var lAtoH = lFieldsABCDE + lFieldsDEFGH;
            if (!lAtoH.ContainsAll(lABCDE) || !lAtoH.ContainsAll(lDEFGH) || !lAtoH.ContainsNone(new cHeaderFieldNames("I", "J", "K"))) throw new cTestsException($"{nameof(cHeaderFields)}.2.21.1");
            if (lAtoH.All("a").Count() != 1 || lAtoH.All("b").Count() != 0 || lAtoH.All("c").Count() != 2 || lAtoH.All("d").Count() != 0 || lAtoH.All("e").Count() != 1 || lAtoH.All("f").Count() != 0 || lAtoH.All("g").Count() != 2 || lAtoH.All("h").Count() != 0) throw new cTestsException($"{nameof(cHeaderFields)}.2.16.2");

            lFailed = false;
            try { lAtoH.All("i"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFields)}.2.21.3");

            var lNotF = lFieldsABCDE + lFieldsGHIJK;
            if (lNotF.Contains("F") || !lNotF.ContainsAll(lABCDE) || !lNotF.ContainsAll(lGHIJK) || lNotF.ContainsAll(lDEFGH)) throw new cTestsException($"{nameof(cHeaderFields)}.2.22.1");
            if (lNotF.All("a").Count() != 1 || lNotF.All("b").Count() != 0 || lNotF.All("c").Count() != 2 || lNotF.All("d").Count() != 0 || lNotF.All("e").Count() != 1  || lNotF.All("g").Count() != 2 || lNotF.All("h").Count() != 0 || lNotF.All("i").Count() != 1 || lNotF.All("j").Count() != 0 || lNotF.All("k").Count() != 2) throw new cTestsException($"{nameof(cHeaderFields)}.2.12.2");




        }
    }
}