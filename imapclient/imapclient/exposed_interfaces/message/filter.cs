using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFilter
    {
        public static readonly cFilterMessageHandle MessageHandle = new cFilterMessageHandle();
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

        public static readonly cFilter False = IsSeen & !IsSeen;

        public readonly cFilterReferences References;

        protected cFilter(cFilterReferences pReferences) => References = pReferences ?? throw new ArgumentNullException(nameof(pReferences));

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

    public class cFilterReferences
    {
        public static readonly cFilterReferences None = new cFilterReferences();

        public readonly iMessageCache Cache;
        public readonly uint? UIDValidity;

        private cFilterReferences()
        {
            Cache = null;
            UIDValidity = null;
        }

        public cFilterReferences(iMessageCache pCache)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            UIDValidity = pCache.UIDValidity;
        }

        public cFilterReferences(uint? pUIDValidity)
        {
            Cache = null;
            UIDValidity = pUIDValidity;
        }

        public cFilterReferences Combine(cFilterReferences pReferences)
        {
            if (pReferences == null) throw new ArgumentNullException(nameof(pReferences));
            if (Cache != null && pReferences.Cache != null && !ReferenceEquals(Cache, pReferences.Cache)) throw new ArgumentOutOfRangeException(nameof(pReferences), "inconsistent message cache");
            if (UIDValidity != null && pReferences.UIDValidity != null && UIDValidity != pReferences.UIDValidity) throw new ArgumentOutOfRangeException(nameof(pReferences), "inconsistent uidvalidity");
            if (Cache != null) return this;
            if (pReferences.Cache != null) return pReferences;
            if (UIDValidity != null) return this;
            if (pReferences.UIDValidity != null) return pReferences;
            return this;
        }

        public override string ToString() => $"{nameof(cFilterReferences)}({Cache},{UIDValidity})";
    }

    public enum eFilterHandleRelativity { less, lessequal, greaterequal, greater }
    public enum eFilterPart { bcc, body, cc, from, subject, text, to }
    public enum eFilterDate { arrival, sent }
    public enum eFilterDateCompare { before, on, since }
    public enum eFilterSizeCompare { smaller, larger }

    // suppress the warnings about not implementing == properly: here == is being used as an expression builder
#pragma warning disable 660
#pragma warning disable 661

    public class cFilterMessageHandleRelativity : cFilter
    {
        public readonly iMessageHandle Handle;
        public readonly eFilterHandleRelativity Relativity;

        public cFilterMessageHandleRelativity(iMessageHandle pHandle, eFilterHandleRelativity pRelativity) : base(new cFilterReferences(pHandle.Cache))
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            Relativity = pRelativity;
        }

        public override string ToString() => $"{nameof(cFilterMessageHandleRelativity)}({References},{Handle},{Relativity})";
    }

    public class cFilterMessageHandle
    {
        public cFilterMessageHandle() { }

        public static cFilter operator <(cFilterMessageHandle pFilterMessageHandle, iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cFilterMessageHandleRelativity(pHandle, eFilterHandleRelativity.less);
        }

        public static cFilter operator >(cFilterMessageHandle pFilterMessageHandle, iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cFilterMessageHandleRelativity(pHandle, eFilterHandleRelativity.greater);
        }

        public static cFilter operator <=(cFilterMessageHandle pFilterMessageHandle, iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cFilterMessageHandleRelativity(pHandle, eFilterHandleRelativity.lessequal);
        }

        public static cFilter operator >=(cFilterMessageHandle pFilterMessageHandle, iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cFilterMessageHandleRelativity(pHandle, eFilterHandleRelativity.greaterequal);
        }
    }

    public class cFilterUIDIn : cFilter
    {
        public readonly cSequenceSet SequenceSet;
        public cFilterUIDIn(uint pUIDValidity, cSequenceSet pSequenceSet) : base(new cFilterReferences(pUIDValidity)) { SequenceSet = pSequenceSet ?? throw new ArgumentNullException(nameof(pSequenceSet)); }
        public override string ToString() => $"{nameof(cFilterUIDIn)}({References},{SequenceSet})";
    }

    public class cFilterUID
    {
        public cFilterUID() { }

        public static cFilter operator <(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID < 2) return cFilter.False;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID - 1));
        }

        public static cFilter operator >(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID == uint.MaxValue) return cFilter.False;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID + 1, uint.MaxValue));
        }

        public static cFilter operator <=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID));
        }

        public static cFilter operator >=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID, uint.MaxValue));
        }
    }

    public class cFilterFlagsContain : cFilter
    {
        public readonly cMessageFlags Flags;

        public cFilterFlagsContain(params string[] pFlags) : base(cFilterReferences.None)
        {
            if (pFlags == null || pFlags.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
            Flags = new cMessageFlags(new cFetchableFlags(pFlags));
        }

        public cFilterFlagsContain(cFetchableFlags pFlags) : base(cFilterReferences.None)
        {
            if (pFlags == null || pFlags.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
            Flags = new cMessageFlags(pFlags);
        }

        public override string ToString() => $"{nameof(cFilterFlagsContain)}({Flags})";
    }

    public class cFilterPartContains : cFilter
    {
        public readonly eFilterPart Part;
        public readonly string Contains; // have to convert to an astring

        public cFilterPartContains(eFilterPart pPart, string pContains) : base(cFilterReferences.None)
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

        public cFilterDateCompare(eFilterDate pDate, eFilterDateCompare pCompare, DateTime pWithDate) : base(cFilterReferences.None)
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

        public cFilterHeaderFieldContains(string pHeaderField, string pContains) : base(cFilterReferences.None)
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

        public cFilterSizeCompare(eFilterSizeCompare pCompare, uint pSize) : base(cFilterReferences.None)
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

        public cFilterAnd(IList<cFilter> pTerms) : base(ZCombinedReferences(pTerms))
        {
            Terms = new ReadOnlyCollection<cFilter>(new List<cFilter>(pTerms));
        }

        private static cFilterReferences ZCombinedReferences(IList<cFilter> pTerms)
        {
            if (pTerms == null) throw new ArgumentNullException(nameof(pTerms));
            if (pTerms.Count < 2) throw new ArgumentOutOfRangeException(nameof(pTerms));

            cFilterReferences lReferences = null;

            foreach (var lTerm in pTerms)
            {
                if (lTerm == null) throw new ArgumentOutOfRangeException(nameof(pTerms), "null list elements");
                if (lReferences == null) lReferences = lTerm.References;
                else lReferences = lReferences.Combine(lTerm.References);
            }

            return lReferences;
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

        public cFilterOr(cFilter pA, cFilter pB) : base(ZCombinedReferences(pA, pB))
        {
            A = pA;
            B = pB;
        }

        private static cFilterReferences ZCombinedReferences(cFilter pA, cFilter pB)
        {
            if (pA == null) throw new ArgumentNullException(nameof(pA));
            if (pB == null) throw new ArgumentNullException(nameof(pB));
            return pA.References.Combine(pB.References);
        }

        public override string ToString() => $"{nameof(cFilterOr)}({A},{B})";
    }

    public class cFilterNot : cFilter
    {
        public readonly cFilter Not;

        public cFilterNot(cFilter pNot) : base(pNot.References)
        {
            Not = pNot ?? throw new ArgumentNullException(nameof(pNot));
        }

        public override string ToString() => $"{nameof(cFilterNot)}({Not})";
    }
}
