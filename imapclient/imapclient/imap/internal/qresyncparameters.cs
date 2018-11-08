using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cQResyncParameters
        {
            public readonly uint UIDValidity;
            public readonly ulong HighestModSeq;
            public readonly cSequenceSet UIDs; // not null
            public readonly Action<int> Increment; // can be null

            public cQResyncParameters(uint pUIDValidity, ulong pHighestModSeq, cSequenceSet pUIDs, Action<int> pIncrement)
            {
                if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
                UIDValidity = pUIDValidity;
                if (pHighestModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pHighestModSeq));
                HighestModSeq = pHighestModSeq;
                UIDs = pUIDs ?? throw new ArgumentNullException(nameof(pUIDs));
                Increment = pIncrement;
            }

            public override string ToString() => $"{nameof(cQResyncParameters)}({UIDValidity},{HighestModSeq},{UIDs},{Increment != null})";
        }
    }
}