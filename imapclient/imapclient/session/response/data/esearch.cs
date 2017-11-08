using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataESearch : cResponseData
            {
                public readonly IList<byte> Tag;
                public readonly bool UID;
                public readonly cSequenceSet SequenceSet;

                public cResponseDataESearch(IList<byte> pTag, bool pUID, cSequenceSet pSequenceSet)
                {
                    Tag = pTag;
                    UID = pUID;
                    SequenceSet = pSequenceSet;
                }

                public override string ToString() => $"{nameof(cResponseDataESearch)}({Tag},{UID},{SequenceSet})";
            }
        }
    }
}