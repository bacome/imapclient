using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCatenateAppendData : cAppendData
            {
                public readonly ReadOnlyCollection<cAppendDataPart> Parts;

                public cCatenateAppendData(cStorableFlags pFlags, DateTime? pReceived, List<cAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));
                    if (pParts.Count == 0) throw new ArgumentOutOfRangeException(nameof(pParts));
                    Parts = pParts.AsReadOnly();
                }

                /// <inheritdoc />
                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cCatenateAppendData));
                    lBuilder.Append(Flags);
                    lBuilder.Append(Received);
                    foreach (var lPart in Parts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }
            }

            private class cURLAppendDataPart : cAppendDataPart
            {
                public readonly string URL;

                public cURLAppendDataPart(string pURL)
                {
                    URL = pURL ?? throw new ArgumentNullException(nameof(pURL));
                }
            }
        }
    }
}