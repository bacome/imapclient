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
                public readonly cUIntList UIDs;

                public cResponseDataVanished(bool pEarlier, cUIntList pUIDs)
                {
                    Earlier = pEarlier;
                    UIDs = pUIDs ?? throw new ArgumentNullException(nameof(pUIDs));
                }

                public override string ToString() => $"{nameof(cResponseDataVanished)}({Earlier},{UIDs})";
            }
        }
    }
}