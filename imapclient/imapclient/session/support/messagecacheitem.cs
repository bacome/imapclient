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
                private fMessageProperties mProperties = 0;
                private cBodyPart mBody = null;

                public cMessageCacheItem(iMessageCache pCache, int pCacheSequence)
                {
                    mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                    mCacheSequence = pCacheSequence;
                }

                public iMessageCache Cache => mCache;
                public int CacheSequence => mCacheSequence;
                public bool Expunged => mExpunged;
                public fMessageProperties Properties => mProperties;
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

                public fMessageProperties Update(uint? pUIDValidity, cResponseDataFetch lFetch)
                {
                    var lPropertiesSet = ~mProperties & lFetch.Properties;

                    if ((lPropertiesSet & fMessageProperties.body) != 0) mBody = lFetch.Body;
                    if ((lPropertiesSet & fMessageProperties.bodystructure) != 0) BodyStructure = lFetch.BodyStructure;
                    if ((lPropertiesSet & fMessageProperties.envelope) != 0) Envelope = lFetch.Envelope;
                    if ((lPropertiesSet & fMessageProperties.received) != 0) Received = lFetch.Received;
                    if ((lPropertiesSet & fMessageProperties.size) != 0) Size = lFetch.Size;
                    if ((lPropertiesSet & fMessageProperties.uid) != 0 && pUIDValidity != null) UID = new cUID(pUIDValidity.Value, lFetch.UID.Value);
                    if ((lPropertiesSet & fMessageProperties.references) != 0) References = lFetch.References;

                    if (lFetch.Flags != null && lFetch.Flags != Flags)
                    {
                        lPropertiesSet |= fMessageProperties.flags;
                        Flags = lFetch.Flags;
                    }

                    if (BinarySizes == null) BinarySizes = lFetch.BinarySizes;
                    else if (lFetch.BinarySizes != null) BinarySizes = BinarySizes + lFetch.BinarySizes;

                    mProperties |= lFetch.Properties;

                    return lPropertiesSet;
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence})";
            }
        }
    }
}