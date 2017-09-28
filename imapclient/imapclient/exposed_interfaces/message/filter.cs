using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFilter
    {
        public static readonly cFilter All = new cAll();

        public static readonly cFilterMSN MSN = new cFilterMSN();
        public static readonly cFilterUID UID = new cFilterUID();

        public static readonly cFilterEnd First = new cFilterEnd(eFilterEnd.first);
        public static readonly cFilterEnd Last = new cFilterEnd(eFilterEnd.last);

        public static readonly cFilter IsAnswered = new cFilterFlagsContain(kFlagName.Answered);
        public static readonly cFilter IsFlagged = new cFilterFlagsContain(kFlagName.Flagged);
        public static readonly cFilter IsDeleted = new cFilterFlagsContain(kFlagName.Deleted);
        public static readonly cFilter IsSeen = new cFilterFlagsContain(kFlagName.Seen);
        public static readonly cFilter IsDraft = new cFilterFlagsContain(kFlagName.Draft);
        public static readonly cFilter IsRecent = new cFilterFlagsContain(kFlagName.Recent);

        public static readonly cFilter IsMDNSent = new cFilterFlagsContain(kFlagName.MDNSent);
        public static readonly cFilter IsForwarded = new cFilterFlagsContain(kFlagName.Forwarded);
        public static readonly cFilter IsSubmitPending = new cFilterFlagsContain(kFlagName.SubmitPending);
        public static readonly cFilter IsSubmitted = new cFilterFlagsContain(kFlagName.Submitted);

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

        public readonly bool ContainsMessageHandles;
        public readonly uint? UIDValidity;

        protected cFilter()
        {
            ContainsMessageHandles = false;
            UIDValidity = null;
        }

        protected cFilter(uint pUIDValidity)
        {
            ContainsMessageHandles = false;
            UIDValidity = pUIDValidity;
        }

        protected cFilter(bool pContainsMessageHandles, uint? pUIDValidity)
        {
            ContainsMessageHandles = pContainsMessageHandles;
            UIDValidity = pUIDValidity;
        }

        protected cFilter(sCTorParams pParams)
        {
            ContainsMessageHandles = pParams.ContainsMessageHandles;
            UIDValidity = pParams.UIDValidity;
        }

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
                cFetchableFlagsList lFlags = new cFetchableFlagsList();
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

        private class cAll : cFilter
        {
            public cAll() { }
        }

        protected struct sCTorParams
        {
            public bool ContainsMessageHandles;
            public uint? UIDValidity;
        }
    }

    public enum eFilterHandleRelativity { less, lessequal, greaterequal, greater }
    public enum eFilterPart { bcc, body, cc, from, subject, text, to }
    public enum eFilterDate { arrival, sent }
    public enum eFilterDateCompare { before, on, since }
    public enum eFilterSizeCompare { smaller, larger }
    public enum eFilterEnd { first, last }

    // suppress the warnings about not implementing == properly: here == is being used as an expression builder
    #pragma warning disable 660
    #pragma warning disable 661

    public class cFilterMSNRelativity : cFilter
    {
        public readonly iMessageHandle Handle;
        public readonly eFilterEnd? End;
        public readonly int Offset;
        public readonly eFilterHandleRelativity Relativity;

        public cFilterMSNRelativity(iMessageHandle pHandle, eFilterHandleRelativity pRelativity) : base(true, pHandle.Cache.UIDValidity)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            End = null;
            Offset = 0;
            Relativity = pRelativity;
        }

        public cFilterMSNRelativity(cFilterMSNOffset pOffset, eFilterHandleRelativity pRelativity)
        {
            End = pOffset.End;
            Handle = pOffset.Handle;
            Offset = pOffset.Offset;
            Relativity = pRelativity;
        }

        public override string ToString() => $"{nameof(cFilterMSNRelativity)}({UIDValidity},{Handle},{End},{Offset},{Relativity})";
    }

    public class cFilterMSNOffset
    {
        public readonly iMessageHandle Handle;
        public readonly eFilterEnd? End;
        public readonly int Offset;

        public cFilterMSNOffset(iMessageHandle pHandle, int pOffset)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            End = null; 
            Offset = pOffset;
        }

        public cFilterMSNOffset(eFilterEnd pEnd, int pOffset)
        {
            Handle = null;
            End = pEnd;
            Offset = pOffset;
        }

        public override string ToString() => $"{nameof(cFilterMSNOffset)}({Handle},{End},{Offset})";
    }

    public class cFilterEnd
    {
        public readonly eFilterEnd End;
        public cFilterEnd(eFilterEnd pEnd) { End = pEnd; }
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(End, pOffset);
        public override string ToString() => $"{nameof(cFilterEnd)}({End})";
    }

    public class cFilterMSN
    {
        public cFilterMSN() { }

        public static cFilter operator <(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.less);
        }

        public static cFilter operator >(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greater);
        }

        public static cFilter operator <(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.less);
        }

        public static cFilter operator >(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greater);
        }

        public static cFilter operator <=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.lessequal);
        }

        public static cFilter operator >=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greaterequal);
        }

        public static cFilter operator <=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.lessequal);
        }

        public static cFilter operator >=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greaterequal);
        }
    }

    public class cFilterUIDIn : cFilter
    {
        public readonly cSequenceSet SequenceSet;
        public cFilterUIDIn(uint pUIDValidity, cSequenceSet pSequenceSet) : base(pUIDValidity) { SequenceSet = pSequenceSet ?? throw new ArgumentNullException(nameof(pSequenceSet)); }
        public override string ToString() => $"{nameof(cFilterUIDIn)}({UIDValidity},{SequenceSet})";
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

        public static cFilter operator ==(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID));
        }

        public static cFilter operator !=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterNot(new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID)));
        }
    }

    public class cFilterFlagsContain : cFilter
    {
        public readonly cFetchableFlags Flags;

        public cFilterFlagsContain(params string[] pFlags)
        {
            if (pFlags == null || pFlags.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
            Flags = new cFetchableFlags(pFlags);
        }

        public cFilterFlagsContain(cFetchableFlags pFlags)
        {
            if (pFlags == null || pFlags.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
            Flags = pFlags;
        }

        public override string ToString() => $"{nameof(cFilterFlagsContain)}({Flags})";
    }

    public class cFilterPartContains : cFilter
    {
        public readonly eFilterPart Part;
        public readonly string Contains; // have to convert to an astring

        public cFilterPartContains(eFilterPart pPart, string pContains)
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

        public cFilterDateCompare(eFilterDate pDate, eFilterDateCompare pCompare, DateTime pWithDate)
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

        public cFilterHeaderFieldContains(string pHeaderField, string pContains)
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

        public cFilterSizeCompare(eFilterSizeCompare pCompare, int pSize)
        {
            if (pSize < 0) throw new ArgumentOutOfRangeException(nameof(pSize));
            Compare = pCompare;
            WithSize = (uint)pSize;
        }

        public cFilterSizeCompare(eFilterSizeCompare pCompare, uint pSize)
        {
            Compare = pCompare;
            WithSize = pSize;
        }

        public override string ToString() => $"{nameof(cFilterSizeCompare)}({Compare},{WithSize})";
    }

    public class cFilterSize
    {
        public cFilterSize() { }
        public static cFilter operator <(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);
        public static cFilter operator >(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);
        public static cFilter operator <(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);
        public static cFilter operator >(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);
    }

    public class cFilterAnd : cFilter
    {
        public readonly ReadOnlyCollection<cFilter> Terms;

        public cFilterAnd(IList<cFilter> pTerms) : base(ZCTorParams(pTerms))
        {
            Terms = new ReadOnlyCollection<cFilter>(new List<cFilter>(pTerms));
        }

        private static sCTorParams ZCTorParams(IList<cFilter> pTerms)
        {
            if (pTerms == null) throw new ArgumentNullException(nameof(pTerms));
            if (pTerms.Count < 2) throw new ArgumentOutOfRangeException(nameof(pTerms));

            sCTorParams lParams = new sCTorParams();

            foreach (var lTerm in pTerms)
            {
                if (lTerm == null) throw new ArgumentOutOfRangeException(nameof(pTerms), "null list elements");
                if (lTerm.ContainsMessageHandles) lParams.ContainsMessageHandles = true;
                if (lParams.UIDValidity == null) lParams.UIDValidity = lTerm.UIDValidity;
                else if (lTerm.UIDValidity != null && lTerm.UIDValidity != lParams.UIDValidity) throw new ArgumentOutOfRangeException(nameof(pTerms));
            }

            return lParams;
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

        public cFilterOr(cFilter pA, cFilter pB) : base(ZCTorParams(pA, pB))
        {
            A = pA;
            B = pB;
        }

        private static sCTorParams ZCTorParams(cFilter pA, cFilter pB)
        {
            if (pA == null) throw new ArgumentNullException(nameof(pA));
            if (pB == null) throw new ArgumentNullException(nameof(pB));

            sCTorParams lParams = new sCTorParams();

            if (pA.ContainsMessageHandles || pB.ContainsMessageHandles) lParams.ContainsMessageHandles = true;

            if (pA.UIDValidity == null) lParams.UIDValidity = pB.UIDValidity;
            else if (pB.UIDValidity == null) lParams.UIDValidity = pA.UIDValidity;
            else if (pA.UIDValidity != pB.UIDValidity) throw new ArgumentOutOfRangeException();
            else lParams.UIDValidity = pA.UIDValidity;

            return lParams;
        }

        public override string ToString() => $"{nameof(cFilterOr)}({A},{B})";
    }

    public class cFilterNot : cFilter
    {
        public readonly cFilter Not;

        public cFilterNot(cFilter pNot) : base(pNot.ContainsMessageHandles, pNot.UIDValidity)
        {
            Not = pNot ?? throw new ArgumentNullException(nameof(pNot));
        }

        public override string ToString() => $"{nameof(cFilterNot)}({Not})";
    }
}
