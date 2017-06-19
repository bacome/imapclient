﻿using System;
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

        private static readonly cAnd kFalse = new cAnd(new cFilter[] { new cWithFlags(fMessageFlags.seen), new cWithoutFlags(fMessageFlags.seen) });

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

        public class cWithFlags : cFilter
        {
            public readonly fMessageFlags Flags;

            public cWithFlags(fMessageFlags pFlags) : base(null)
            {
                if ((pFlags & fMessageFlags.asterisk) != 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
                if ((pFlags & fMessageFlags.allfilterflags) == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
                Flags = pFlags;
            }

            public override string ToString() => $"{nameof(cWithFlags)}({Flags})";
        }

        public static cFilter WithFlags(fMessageFlags pFlags) => new cWithFlags(pFlags);

        public class cWithoutFlags : cFilter
        {
            public readonly fMessageFlags Flags;

            public cWithoutFlags(fMessageFlags pFlags) : base(null)
            {
                if ((pFlags & fMessageFlags.asterisk) != 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
                if ((pFlags & fMessageFlags.allfilterflags) == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
                Flags = pFlags;
            }

            public override string ToString() => $"{nameof(cWithoutFlags)}({Flags})";
        }

        public static cFilter WithoutFlags(fMessageFlags pFlags) => new cWithoutFlags(pFlags);

        public class cWithKeyword : cFilter
        {
            public readonly string Keyword;

            public cWithKeyword(string pKeyword) : base(null)
            {
                if (!cCommandPart.TryAsAtom(pKeyword, out _)) throw new ArgumentOutOfRangeException(pKeyword);
                Keyword = pKeyword;
            }

            public override string ToString() => $"{nameof(cWithKeyword)}({Keyword})";
        }

        public static cFilter WithKeyword(string pKeyword) => new cWithKeyword(pKeyword);

        public class cWithoutKeyword : cFilter
        {
            public readonly string Keyword;

            public cWithoutKeyword(string pKeyword) : base(null)
            {
                if (!cCommandPart.TryAsAtom(pKeyword, out _)) throw new ArgumentOutOfRangeException(pKeyword);
                Keyword = pKeyword;
            }

            public override string ToString() => $"{nameof(cWithoutKeyword)}({Keyword})";
        }

        public static cFilter WithoutKeyword(string pKeyword) => new cWithoutKeyword(pKeyword);

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
                if (!cCommandPart.TryAsRFC822HeaderField(HeaderField, out _)) throw new ArgumentOutOfRangeException(nameof(HeaderField));
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

            List<cFilter> lItems = new List<cFilter>();

            if (pA is cAnd lA) lItems.AddRange(lA.Terms);
            else lItems.Add(pA);

            if (pB is cAnd lB) lItems.AddRange(lB.Terms);
            else lItems.Add(pB);

            return new cAnd(lItems);
        }

        public static cFilter operator |(cFilter pA, cFilter pB) => new cOr(pA, pB);
        public static cFilter operator !(cFilter pNot) => new cNot(pNot);
    }
}
