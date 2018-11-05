using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataFetch : cResponseData, iFlagDataItem, iHeaderDataItem
            {
                public readonly uint MSN;
                public readonly uint? UID;

                private readonly cModSeqFlags mModSeqFlags;

                private readonly cEnvelope mEnvelope;
                private readonly cTimestamp mReceived;
                private readonly uint? mSize;
                private readonly cBodyPart mBodyStructure;
                private readonly cHeaderFields mHeaderFields;
                private readonly cBinarySizes mBinarySizes;

                public readonly ReadOnlyCollection<cBody> Bodies;

                public readonly cBodyPart Body;

                public readonly cBytes RFC822; // un-parsed
                public readonly cBytes RFC822Header; // un-parsed
                public readonly cBytes RFC822Text; // un-parsed

                public cResponseDataFetch(
                    uint pMSN,
                    uint? pUID,
                    cModSeqFlags pModSeqFlags,
                    cEnvelope pEnvelope, cTimestamp pReceived, uint? pSize, cBodyPart pBodyStructure, cHeaderFields pHeaderFields, cBinarySizes pBinarySizes,
                    IList<cBody> pBodies,
                    cBodyPart pBody,
                    cBytes pRFC822, cBytes pRFC822Header, cBytes pRFC822Text)
                {
                    MSN = pMSN;
                    UID = pUID;

                    mModSeqFlags = pModSeqFlags;

                    mEnvelope = pEnvelope;
                    mReceived = pReceived;
                    mSize = pSize;
                    mBodyStructure = pBodyStructure;
                    mHeaderFields = pHeaderFields;
                    mBinarySizes = pBinarySizes;

                    Bodies = new ReadOnlyCollection<cBody>(pBodies);

                    Body = pBody;

                    RFC822 = pRFC822;
                    RFC822Header = pRFC822Header;
                    RFC822Text = pRFC822Text;
                }

                public cModSeqFlags ModSeqFlags => mModSeqFlags;

                public cEnvelope Envelope => mEnvelope;
                public cTimestamp Received => mReceived;
                public uint? Size => mSize;
                public cBodyPart BodyStructure => mBodyStructure;
                public cHeaderFields HeaderFields => mHeaderFields;
                public cBinarySizes BinarySizes => mBinarySizes;

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cResponseDataFetch));

                    lBuilder.Append(MSN);
                    lBuilder.Append(UID);

                    lBuilder.Append(mModSeqFlags);

                    lBuilder.Append(mEnvelope);
                    lBuilder.Append(mReceived);
                    lBuilder.Append(mSize);
                    lBuilder.Append(mBodyStructure);
                    lBuilder.Append(mHeaderFields);
                    lBuilder.Append(mBinarySizes);

                    cListBuilder lBodies = new cListBuilder(nameof(Bodies));
                    foreach (var lBody in Bodies) lBodies.Append(lBody);
                    lBuilder.Append(lBodies);

                    lBuilder.Append(Body);

                    lBuilder.Append(RFC822);
                    lBuilder.Append(RFC822Header);
                    lBuilder.Append(RFC822Text);

                    return lBuilder.ToString();
                }
            }
        }
    }
}

