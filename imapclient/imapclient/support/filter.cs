using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal enum eFilterHandleRelativity { less, lessequal, greaterequal, greater }
    internal enum eFilterPart { bcc, body, cc, from, subject, text, to }
    internal enum eFilterDate { arrival, sent }
    internal enum eFilterDateCompare { before, on, since }
    internal enum eFilterSizeCompare { smaller, larger }
    internal enum eFilterEnd { first, last }

    internal class cFilterMSNRelativity : cFilter
    {
        public readonly iMessageHandle MessageHandle;
        public readonly eFilterEnd? End;
        public readonly int Offset;
        public readonly eFilterHandleRelativity Relativity;

        public cFilterMSNRelativity(iMessageHandle pMessageHandle, eFilterHandleRelativity pRelativity) : base(true, pMessageHandle.MessageCache.UIDValidity)
        {
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            End = null;
            Offset = 0;
            Relativity = pRelativity;
        }

        public cFilterMSNRelativity(cFilterMSNOffset pOffset, eFilterHandleRelativity pRelativity)
        {
            End = pOffset.End;
            MessageHandle = pOffset.MessageHandle;
            Offset = pOffset.Offset;
            Relativity = pRelativity;
        }

        public override string ToString() => $"{nameof(cFilterMSNRelativity)}({UIDValidity},{MessageHandle},{End},{Offset},{Relativity})";
    }

    internal class cFilterUIDIn : cFilter
    {
        public readonly cSequenceSet SequenceSet;
        public cFilterUIDIn(uint pUIDValidity, cSequenceSet pSequenceSet) : base(pUIDValidity) { SequenceSet = pSequenceSet ?? throw new ArgumentNullException(nameof(pSequenceSet)); }
        public override string ToString() => $"{nameof(cFilterUIDIn)}({UIDValidity},{SequenceSet})";
    }

    internal class cFilterFlagsContain : cFilter
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

    internal class cFilterPartContains : cFilter
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

    internal class cFilterDateCompare : cFilter
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

    internal class cFilterHeaderFieldContains : cFilter
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

    internal class cFilterSizeCompare : cFilter
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

    internal class cFilterAnd : cFilter
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
                if (lTerm == null) throw new ArgumentOutOfRangeException(nameof(pTerms), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
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

    internal class cFilterOr : cFilter
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

    internal class cFilterNot : cFilter
    {
        public readonly cFilter Not;

        public cFilterNot(cFilter pNot) : base(pNot.ContainsMessageHandles, pNot.UIDValidity)
        {
            Not = pNot ?? throw new ArgumentNullException(nameof(pNot));
        }

        public override string ToString() => $"{nameof(cFilterNot)}({Not})";
    }
}