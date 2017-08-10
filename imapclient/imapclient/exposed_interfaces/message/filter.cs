using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFilter
    {
        public static readonly cFilterUID UID = new cFilterUID();

        public static readonly cFilter IsAnswered = new cFilterFlagsContain(cMessageFlags.Answered);
        public static readonly cFilter IsFlagged = new cFilterFlagsContain(cMessageFlags.Flagged);
        public static readonly cFilter IsDeleted = new cFilterFlagsContain(cMessageFlags.Deleted);
        public static readonly cFilter IsSeen = new cFilterFlagsContain(cMessageFlags.Seen);
        public static readonly cFilter IsDraft = new cFilterFlagsContain(cMessageFlags.Draft);
        public static readonly cFilter IsRecent = new cFilterFlagsContain(cMessageFlags.Recent);

        public static readonly cFilter IsMDNSent = new cFilterFlagsContain(cMessageFlags.MDNSent);
        public static readonly cFilter IsForwarded = new cFilterFlagsContain(cMessageFlags.Forwarded);
        public static readonly cFilter IsSubmitPending = new cFilterFlagsContain(cMessageFlags.SubmitPending);
        public static readonly cFilter IsSubmitted = new cFilterFlagsContain(cMessageFlags.Submitted);

        public static readonly cFilterPart BCC = new cFilterPart(eFilterPart.bcc);
        public static readonly cFilterPart Body = new cFilterPart(eFilterPart.body);
        public static readonly cFilterPart CC = new cFilterPart(eFilterPart.cc);
        public static readonly cFilterPart From = new cFilterPart(eFilterPart.from);
        public static readonly cFilterPart Subject = new cFilterPart(eFilterPart.subject);
        public static readonly cFilterPart Text = new cFilterPart(eFilterPart.text);
        public static readonly cFilterPart To = new cFilterPart(eFilterPart.to);

        public static readonly cFilterDate Received = new cFilterDate(eFilterDate.arrival);
        public static readonly cFilterDate Sent = new cFilterDate(eFilterDate.sent);

        public static readonly cFilterSize Size = new cFilterSize();

        public readonly uint? UIDValidity;

        public cFilter(uint? pUIDValidity) { UIDValidity = pUIDValidity; }

        public static cFilter FlagsContain(params string[] pFlags) => new cFilterFlagsContain(pFlags);
        public static cFilter FlagsContain(cFetchableFlags pFlags) => new cFilterFlagsContain(pFlags);

        public static cFilter HeaderFieldContains(string pHeaderField, string pContains) => new cFilterHeaderFieldContains(pHeaderField, pContains);
        public static cFilter HasHeaderField(string pHeaderField) => new cFilterHeaderFieldContains(pHeaderField, string.Empty);

        public static cFilter operator &(cFilter pA, cFilter pB)
        {
            if (pA == null) throw new ArgumentNullException(nameof(pA));
            if (pB == null) throw new ArgumentNullException(nameof(pB));

            if (pA is cFilterFlagsContain lFCA && pB is cFilterFlagsContain lFCB)
            {
                cFetchableFlags lFlags = new cFetchableFlags();
                lFlags.Add(lFCA.Flags);
                lFlags.Add(lFCB.Flags);
                return new cFilterFlagsContain(lFlags);
            }

            List<cFilter> lItems = new List<cFilter>();

            if (pA is cFilterAnd lAA) lItems.AddRange(lAA.Terms);
            else lItems.Add(pA);

            if (pB is cFilterAnd lAB) lItems.AddRange(lAB.Terms);
            else lItems.Add(pB);

            return new cFilterAnd(lItems);
        }

        public static cFilter operator |(cFilter pA, cFilter pB) => new cFilterOr(pA, pB);
        public static cFilter operator !(cFilter pNot) => new cFilterNot(pNot);
    }

    public enum eFilterPart { bcc, body, cc, from, subject, text, to }
    public enum eFilterDate { arrival, sent }
    public enum eFilterDateCompare { before, on, since }
    public enum eFilterSizeCompare { smaller, larger }

    // suppress the warnings about not implementing == properly: here == is being used as an expression builder
    #pragma warning disable 660
    #pragma warning disable 661

    public class cFilterUIDIn : cFilter
    {
        public readonly cSequenceSet SequenceSet;
        public cFilterUIDIn(uint pUIDValidity, cSequenceSet pSequenceSet) : base(pUIDValidity) { SequenceSet = pSequenceSet ?? throw new ArgumentNullException(nameof(pSequenceSet)); }
        public override string ToString() => $"{nameof(cFilterUIDIn)}({UIDValidity},{SequenceSet})";
    }

    public class cFilterUID
    {
        private static readonly cFilterAnd kFalse = new cFilterAnd(new cFilter[] { new cFilterFlagsContain(cMessageFlags.Seen), new cFilterNot(new cFilterFlagsContain(cMessageFlags.Seen)) });

        public cFilterUID() { }

        public static cFilter operator <(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID.UID < 2) return kFalse;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID - 1));
        }

        public static cFilter operator >(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID.UID == uint.MaxValue) return kFalse;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID + 1, uint.MaxValue));
        }

        public static cFilter operator <=(cFilterUID pFilterUID, cUID pUID) => new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID));
        public static cFilter operator >=(cFilterUID pFilterUID, cUID pUID) => new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID, uint.MaxValue));

        public static cFilter operator ==(cFilterUID pFilterUID, cUID pUID) => new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID));
        public static cFilter operator !=(cFilterUID pFilterUID, cUID pUID) => new cFilterNot(new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID)));
    }

    public class cFilterFlagsContain : cFilter
    {
        public readonly cMessageFlags Flags;

        public cFilterFlagsContain(params string[] pFlags) : base(null)
        {
            Flags = new cMessageFlags(new cFetchableFlags(pFlags));
        }

        public cFilterFlagsContain(cFetchableFlags pFlags) : base(null)
        {
            Flags = new cMessageFlags(pFlags);
        }

        public override string ToString() => $"{nameof(cFilterFlagsContain)}({Flags})";
    }

    public class cFilterPartContains : cFilter
    {
        public readonly eFilterPart Part;
        public readonly string Contains; // have to convert to an astring

        public cFilterPartContains(eFilterPart pPart, string pContains) : base(null)
        {
            Part = pPart;
            Contains = pContains ?? throw new ArgumentNullException(nameof(pContains));
        }

        public override string ToString() => $"{nameof(cFilterPartContains)}({Part},{Contains})";
    }

    public class cFilterPart
    {
        private readonly eFilterPart Part;
        public cFilterPart(eFilterPart pPart) { Part = pPart; }
        public cFilter Contains(string pContains) => new cFilterPartContains(Part, pContains);
    }

    public class cFilterDateCompare : cFilter
    {
        public readonly eFilterDate Date;
        public readonly eFilterDateCompare Compare;
        public readonly DateTime WithDate;

        public cFilterDateCompare(eFilterDate pDate, eFilterDateCompare pCompare, DateTime pWithDate) : base(null)
        {
            Date = pDate;
            Compare = pCompare;
            WithDate = pWithDate;
        }

        public override string ToString() => $"{nameof(cFilterDateCompare)}({Date},{Compare},{WithDate})";
    }

    public class cFilterDate
    {
        private readonly eFilterDate Date;

        public cFilterDate(eFilterDate pDate) { Date = pDate; }

        public static cFilter operator <(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.before, pDate);
        public static cFilter operator >(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate.AddDays(1));

        public static cFilter operator ==(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate);
        public static cFilter operator !=(cFilterDate pFilterDate, DateTime pDate) => new cFilterNot(new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate));

        public static cFilter operator >=(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate);
        public static cFilter operator <=(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.before, pDate.AddDays(1));
    }

    public class cFilterHeaderFieldContains : cFilter
    {
        public readonly string HeaderField;
        public readonly string Contains; // have to convert to an astring

        public cFilterHeaderFieldContains(string pHeaderField, string pContains) : base(null)
        {
            HeaderField = pHeaderField ?? throw new ArgumentNullException(nameof(HeaderField));
            if (!cCommandPartFactory.TryAsASCIIAString(HeaderField, out _)) throw new ArgumentOutOfRangeException(nameof(HeaderField));
            Contains = pContains ?? throw new ArgumentNullException(nameof(pContains));
        }

        public override string ToString() => $"{nameof(cFilterHeaderFieldContains)}({HeaderField},{Contains})";
    }

    public class cFilterSizeCompare : cFilter
    {
        public readonly eFilterSizeCompare Compare;
        public readonly uint WithSize;

        public cFilterSizeCompare(eFilterSizeCompare pCompare, uint pSize) : base(null)
        {
            Compare = pCompare;
            WithSize = pSize;
        }

        public override string ToString() => $"{nameof(cFilterSizeCompare)}({Compare},{WithSize})";
    }

    public class cFilterSize
    {
        public cFilterSize() { }
        public static cFilter operator <(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);
        public static cFilter operator >(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);
    }

    public class cFilterAnd : cFilter
    {
        public readonly ReadOnlyCollection<cFilter> Terms;

        public cFilterAnd(IList<cFilter> pTerms) : base(ZCheckTerms(pTerms))
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
            cListBuilder lBuilder = new cListBuilder(nameof(cFilterAnd));
            foreach (var lTerm in Terms) lBuilder.Append(lTerm);
            return lBuilder.ToString();
        }
    }

    public class cFilterOr : cFilter
    {
        public readonly cFilter A;
        public readonly cFilter B;

        public cFilterOr(cFilter pA, cFilter pB) : base(ZCheckParams(pA, pB))
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

        public override string ToString() => $"{nameof(cFilterOr)}({A},{B})";
    }

    public class cFilterNot : cFilter
    {
        public readonly cFilter Not;

        public cFilterNot(cFilter pNot) : base(pNot.UIDValidity)
        {
            Not = pNot ?? throw new ArgumentNullException(nameof(pNot));
        }

        public override string ToString() => $"{nameof(cFilterNot)}({Not})";
    }
}
