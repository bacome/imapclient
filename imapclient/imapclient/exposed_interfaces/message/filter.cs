using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

// suppress the warnings about not implementing == properly: here == is being used as an expression builder
#pragma warning disable 660
#pragma warning disable 661

namespace work.bacome.imapclient
{
    public abstract class cFilter
    {
        public enum ePart { bcc, body, cc, from, subject, text, to }
        public enum eDate { arrival, sent }
        public enum eDateCompare { before, on, since }
        public enum eSizeCompare { smaller, larger }

        public static readonly cFilterUID UID = new cFilterUID();

        public static readonly cFilter IsAnswered = new cFlagsContain(cMessageFlags.Answered);
        public static readonly cFilter IsFlagged = new cFlagsContain(cMessageFlags.Flagged);
        public static readonly cFilter IsDeleted = new cFlagsContain(cMessageFlags.Deleted);
        public static readonly cFilter IsSeen = new cFlagsContain(cMessageFlags.Seen);
        public static readonly cFilter IsDraft = new cFlagsContain(cMessageFlags.Draft);
        public static readonly cFilter IsRecent = new cFlagsContain(cMessageFlags.Recent);

        public static readonly cFilter IsMDNSent = new cFlagsContain(cMessageFlags.MDNSent);
        public static readonly cFilter IsForwarded = new cFlagsContain(cMessageFlags.Forwarded);
        public static readonly cFilter IsSubmitPending = new cFlagsContain(cMessageFlags.SubmitPending);
        public static readonly cFilter IsSubmitted = new cFlagsContain(cMessageFlags.Submitted);

        public static readonly cPart BCC = new cPart(ePart.bcc);
        public static readonly cPart Body = new cPart(ePart.body);
        public static readonly cPart CC = new cPart(ePart.cc);
        public static readonly cPart From = new cPart(ePart.from);
        public static readonly cPart Subject = new cPart(ePart.subject);
        public static readonly cPart Text = new cPart(ePart.text);
        public static readonly cPart To = new cPart(ePart.to);

        public static readonly cDate Received = new cDate(eDate.arrival);
        public static readonly cDate Sent = new cDate(eDate.sent);

        public static readonly cSize Size = new cSize();

        private static readonly cAnd kFalse = new cAnd(new cFilter[] { new cFlagsContain(cMessageFlags.Seen), new cNot(new cFlagsContain(cMessageFlags.Seen)) });

        public readonly uint? UIDValidity;

        public cFilter(uint? pUIDValidity) { UIDValidity = pUIDValidity; }

        public class cUIDIn : cFilter
        {
            public readonly cSequenceSet SequenceSet;
            public cUIDIn(uint pUIDValidity, cSequenceSet pSequenceSet) : base(pUIDValidity) { SequenceSet = pSequenceSet ?? throw new ArgumentNullException(nameof(pSequenceSet)); }
            public override string ToString() => $"{nameof(cUIDIn)}({UIDValidity},{SequenceSet})";
        }

        public class cFilterUID
        {
            public cFilterUID() { }

            public static cFilter operator <(cFilterUID pFilterUID, cUID pUID)
            {
                if (pUID.UID < 2) return kFalse;
                return new cUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID - 1));
            }

            public static cFilter operator >(cFilterUID pFilterUID, cUID pUID)
            {
                if (pUID.UID == uint.MaxValue) return kFalse;
                return new cUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID + 1, uint.MaxValue));
            }

            public static cFilter operator <=(cFilterUID pFilterUID, cUID pUID) => new cUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID));
            public static cFilter operator >=(cFilterUID pFilterUID, cUID pUID) => new cUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID, uint.MaxValue));

            public static cFilter operator ==(cFilterUID pFilterUID, cUID pUID) => new cUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID));
            public static cFilter operator !=(cFilterUID pFilterUID, cUID pUID) => new cNot(new cUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID)));
        }

        public class cFlagsContain : cFilter
        {
            public readonly cMessageFlags Flags;

            public cFlagsContain(params string[] pFlags) : base(null)
            {
                Flags = new cMessageFlags(new cFetchableFlags(pFlags));
            }

            public cFlagsContain(cFetchableFlags pFlags) : base(null)
            {
                Flags = new cMessageFlags(pFlags);
            }

            public override string ToString() => $"{nameof(cFlagsContain)}({Flags})";
        }

        public static cFilter FlagsContain(params string[] pFlags) => new cFlagsContain(pFlags);
        public static cFilter FlagsContain(cFetchableFlags pFlags) => new cFlagsContain(pFlags);

        public class cPartContains : cFilter
        {
            public readonly ePart Part;
            public readonly string Contains; // have to convert to an astring

            public cPartContains(ePart pPart, string pContains) : base(null)
            {
                Part = pPart;
                Contains = pContains ?? throw new ArgumentNullException(nameof(pContains)); 
            }

            public override string ToString() => $"{nameof(cPartContains)}({Part},{Contains})";
        }

        public class cPart
        {
            private readonly ePart Part;
            public cPart(ePart pPart) { Part = pPart; }
            public cFilter Contains(string pContains) => new cPartContains(Part, pContains);
        }

        public class cDateCompare : cFilter
        {
            public readonly eDate Date;
            public readonly eDateCompare Compare;
            public readonly DateTime WithDate;

            public cDateCompare(eDate pDate, eDateCompare pCompare, DateTime pWithDate) : base(null)
            {
                Date = pDate;
                Compare = pCompare;
                WithDate = pWithDate;
            }

            public override string ToString() => $"{nameof(cDateCompare)}({Date},{Compare},{WithDate})";
        }

        public class cDate
        {
            private readonly eDate Date;

            public cDate(eDate pDate) { Date = pDate; }

            public static cFilter operator <(cDate pFilterDate, DateTime pDate) => new cDateCompare(pFilterDate.Date, eDateCompare.before, pDate);
            public static cFilter operator >(cDate pFilterDate, DateTime pDate) => new cDateCompare(pFilterDate.Date, eDateCompare.since, pDate.AddDays(1));

            public static cFilter operator ==(cDate pFilterDate, DateTime pDate) => new cDateCompare(pFilterDate.Date, eDateCompare.on, pDate);
            public static cFilter operator !=(cDate pFilterDate, DateTime pDate) => new cNot(new cDateCompare(pFilterDate.Date, eDateCompare.on, pDate));

            public static cFilter operator >=(cDate pFilterDate, DateTime pDate) => new cDateCompare(pFilterDate.Date, eDateCompare.since, pDate);
            public static cFilter operator <=(cDate pFilterDate, DateTime pDate) => new cDateCompare(pFilterDate.Date, eDateCompare.before, pDate.AddDays(1));
        }

        public class cHeaderFieldContains : cFilter
        {
            public readonly string HeaderField;
            public readonly string Contains; // have to convert to an astring

            public cHeaderFieldContains(string pHeaderField, string pContains) : base(null)
            {
                HeaderField = pHeaderField ?? throw new ArgumentNullException(nameof(HeaderField));
                if (!cCommandPartFactory.TryAsASCIIAString(HeaderField, out _)) throw new ArgumentOutOfRangeException(nameof(HeaderField));
                Contains = pContains ?? throw new ArgumentNullException(nameof(pContains));
            }

            public override string ToString() => $"{nameof(cHeaderFieldContains)}({HeaderField},{Contains})";
        }

        public static cFilter HeaderFieldContains(string pHeaderField, string pContains) => new cHeaderFieldContains(pHeaderField, pContains);
        public static cFilter HasHeaderField(string pHeaderField) => new cHeaderFieldContains(pHeaderField, string.Empty);

        public class cSizeCompare : cFilter
        {
            public readonly eSizeCompare Compare;
            public readonly uint WithSize;

            public cSizeCompare(eSizeCompare pCompare, uint pSize) : base(null)
            {
                Compare = pCompare;
                WithSize = pSize;
            }

            public override string ToString() => $"{nameof(cSizeCompare)}({Compare},{WithSize})";
        }

        public class cSize
        {
            public cSize() { }
            public static cFilter operator <(cSize pFitlerSize, uint pSize) => new cSizeCompare(eSizeCompare.smaller, pSize);
            public static cFilter operator >(cSize pFitlerSize, uint pSize) => new cSizeCompare(eSizeCompare.larger, pSize);
        }

        public class cAnd : cFilter
        {
            public readonly ReadOnlyCollection<cFilter> Terms;

            public cAnd(IList<cFilter> pTerms) : base(ZCheckTerms(pTerms))
            {
                Terms = new ReadOnlyCollection<cFilter>(new List<cFilter>(pTerms));
            }

            private static uint? ZCheckTerms(IList<cFilter> pTerms)
            {
                if (pTerms == null) throw new ArgumentNullException(nameof(pTerms));
                if (pTerms.Count < 2) throw new ArgumentOutOfRangeException(nameof(pTerms));

                uint? lUIDValidity = null;

                foreach (var lTerm in pTerms)
                {
                    if (lTerm == null) throw new ArgumentOutOfRangeException(nameof(pTerms), "null list elements");

                    if (lTerm.UIDValidity != null)
                    {
                        if (lUIDValidity == null) lUIDValidity = lTerm.UIDValidity;
                        else if (lTerm.UIDValidity != lUIDValidity) throw new ArgumentOutOfRangeException(nameof(pTerms), "inconsistent uidvalidities");
                    }
                }

                return lUIDValidity;
            }

            public override string ToString()
            {
                cListBuilder lBuilder = new cListBuilder(nameof(cAnd));
                foreach (var lTerm in Terms) lBuilder.Append(lTerm);
                return lBuilder.ToString();
            }
        }

        public class cOr : cFilter
        {
            public readonly cFilter A;
            public readonly cFilter B;

            public cOr(cFilter pA, cFilter pB) : base(ZCheckParams(pA, pB))
            {
                A = pA;
                B = pB;
            }

            private static uint? ZCheckParams(cFilter pA, cFilter pB)
            {
                if (pA == null) throw new ArgumentNullException(nameof(pA));
                if (pB == null) throw new ArgumentNullException(nameof(pB));
                if (pA.UIDValidity != null && pB.UIDValidity != null && pA.UIDValidity != pB.UIDValidity) throw new ArgumentOutOfRangeException(nameof(pB));
                return pA.UIDValidity ?? pB.UIDValidity;
            }

            public override string ToString() => $"{nameof(cOr)}({A},{B})";
        }

        public class cNot : cFilter
        {
            public readonly cFilter Not;

            public cNot(cFilter pNot) : base(pNot.UIDValidity)
            {
                Not = pNot ?? throw new ArgumentNullException(nameof(pNot));
            }

            public override string ToString() => $"{nameof(cNot)}({Not})";
        }

        public static cFilter operator &(cFilter pA, cFilter pB)
        {
            if (pA == null) throw new ArgumentNullException(nameof(pA));
            if (pB == null) throw new ArgumentNullException(nameof(pB));

            if (pA is cFlagsContain lFCA && pB is cFlagsContain lFCB)
            {
                cFetchableFlags lFlags = new cFetchableFlags();
                lFlags.Add(lFCA.Flags);
                lFlags.Add(lFCB.Flags);
                return new cFlagsContain(lFlags);
            }

            List<cFilter> lItems = new List<cFilter>();

            if (pA is cAnd lAA) lItems.AddRange(lAA.Terms);
            else lItems.Add(pA);

            if (pB is cAnd lAB) lItems.AddRange(lAB.Terms);
            else lItems.Add(pB);

            return new cAnd(lItems);
        }

        public static cFilter operator |(cFilter pA, cFilter pB) => new cOr(pA, pB);
        public static cFilter operator !(cFilter pNot) => new cNot(pNot);
    }
}
