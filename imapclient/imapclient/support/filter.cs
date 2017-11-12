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
        internal readonly iMessageHandle Handle;
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
}