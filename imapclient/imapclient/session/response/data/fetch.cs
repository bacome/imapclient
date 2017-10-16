using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataFetch : cResponseData
            {
                public readonly uint MSN;
                public readonly fCacheAttributes Attributes;
                public readonly cFetchableFlags Flags;
                public readonly cEnvelope Envelope;
                public readonly DateTime? Received;
                public readonly cBytes RFC822; // un-parsed
                public readonly cBytes RFC822Header; // un-parsed
                public readonly cBytes RFC822Text; // un-parsed
                public readonly uint? Size;
                public readonly cBodyPart Body;
                public readonly cBodyPart BodyStructure;
                public readonly ReadOnlyCollection<cBody> Bodies;
                public readonly uint? UID;
                public readonly cHeaderFields HeaderFields;
                public readonly cBinarySizes BinarySizes;
                public readonly ulong? ModSeq;

                public cResponseDataFetch(uint pMSN, fCacheAttributes pAttributes, cFetchableFlags pFlags, cEnvelope pEnvelope, DateTime? pReceived, IList<byte> pRFC822, IList<byte> pRFC822Header, IList<byte> pRFC822Text, uint? pSize, cBodyPart pBody, cBodyPart pBodyStructure, IList<cBody> pBodies, uint? pUID, cHeaderFields pHeaderFields, IDictionary<string, uint> pBinarySizes, ulong? pModSeq)
                {
                    MSN = pMSN;
                    Attributes = pAttributes;
                    Flags = pFlags;
                    Envelope = pEnvelope;
                    Received = pReceived;
                    RFC822 = pRFC822 == null ? null : new cBytes(pRFC822);
                    RFC822Header = pRFC822Header == null ? null : new cBytes(pRFC822Header);
                    RFC822Text = pRFC822Text == null ? null : new cBytes(pRFC822Text);
                    Size = pSize;
                    Body = pBody;
                    BodyStructure = pBodyStructure;
                    Bodies = new ReadOnlyCollection<cBody>(pBodies);
                    UID = pUID;
                    HeaderFields = pHeaderFields;
                    BinarySizes = new cBinarySizes(pBinarySizes);
                    ModSeq = pModSeq;
                }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cResponseDataFetch));

                    lBuilder.Append(MSN);
                    lBuilder.Append(Attributes);
                    lBuilder.Append(Flags);
                    lBuilder.Append(Envelope);
                    lBuilder.Append(Received);
                    lBuilder.Append(RFC822);
                    lBuilder.Append(RFC822Header);
                    lBuilder.Append(RFC822Text);
                    lBuilder.Append(Size);
                    lBuilder.Append(Body);
                    lBuilder.Append(BodyStructure);

                    cListBuilder lBodies = new cListBuilder(nameof(Bodies));
                    foreach (var lBody in Bodies) lBodies.Append(lBody);
                    lBuilder.Append(lBodies);

                    lBuilder.Append(UID);
                    lBuilder.Append(HeaderFields);
                    lBuilder.Append(BinarySizes);
                    lBuilder.Append(ModSeq);

                    return lBuilder.ToString();
                }
            }
        }
    }
}

