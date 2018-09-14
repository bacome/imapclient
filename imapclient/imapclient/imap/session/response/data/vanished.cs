using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataVanished : cResponseData
            {
                public readonly bool Earlier;
                public readonly cSequenceSet KnownUIDs;

                public cResponseDataVanished(bool pEarlier, cSequenceSet pKnownUIDs)
                {
                    Earlier = pEarlier;
                    KnownUIDs = pKnownUIDs;
                }

                public override string ToString() => $"{nameof(cResponseDataVanished)}({Earlier},{KnownUIDs})";
            }
        }
    }
}