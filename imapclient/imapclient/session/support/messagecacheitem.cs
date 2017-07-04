using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cMessageCacheItem : iMessageHandle
            {
                private readonly iMessageCache mCache;
                private readonly int mCacheSequence;
                private bool mExpunged = false;
                private fFetchAttributes mAttributes = 0;
                private cBodyPart mBody = null;

                public cMessageCacheItem(iMessageCache pCache, int pCacheSequence)
                {
                    mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                    mCacheSequence = pCacheSequence;
                }

                public iMessageCache Cache => mCache;
                public int CacheSequence => mCacheSequence;
                public bool Expunged => mExpunged;
                public fFetchAttributes Attributes => mAttributes;
                public cBodyPart Body => mBody ?? BodyStructure;
                public cBodyPart BodyStructure { get; private set; } = null;
                public cEnvelope Envelope { get; private set; } = null;
                public cMessageFlags Flags { get; private set; } = null;
                public DateTime? Received { get; private set; } = null;
                public uint? Size { get; private set; } = null;
                public cUID UID { get; private set; } = null;
                public cStrings References { get; private set; } = null;
                public cBinarySizes BinarySizes { get; private set; } = null;

                public void SetExpunged() => mExpunged = true;

                public fFetchAttributes Update(uint? pUIDValidity, cResponseDataFetch lFetch)
                {
                    var lAttributesSet = ~mAttributes & lFetch.Attributes;

                    if ((lAttributesSet & fFetchAttributes.body) != 0) mBody = lFetch.Body;
                    if ((lAttributesSet & fFetchAttributes.bodystructure) != 0) BodyStructure = lFetch.BodyStructure;
                    if ((lAttributesSet & fFetchAttributes.envelope) != 0) Envelope = lFetch.Envelope;
                    if ((lAttributesSet & fFetchAttributes.received) != 0) Received = lFetch.Received;
                    if ((lAttributesSet & fFetchAttributes.size) != 0) Size = lFetch.Size;
                    if ((lAttributesSet & fFetchAttributes.uid) != 0 && pUIDValidity != null) UID = new cUID(pUIDValidity.Value, lFetch.UID.Value);
                    if ((lAttributesSet & fFetchAttributes.references) != 0) References = lFetch.References;

                    if (lFetch.Flags != null && lFetch.Flags != Flags)
                    {
                        lAttributesSet |= fFetchAttributes.flags;
                        Flags = lFetch.Flags;
                    }

                    if (BinarySizes == null) BinarySizes = lFetch.BinarySizes;
                    else if (lFetch.BinarySizes != null) BinarySizes = BinarySizes + lFetch.BinarySizes;

                    mAttributes |= lFetch.Attributes;

                    return lAttributesSet;
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence})";
            }
        }
    }
}