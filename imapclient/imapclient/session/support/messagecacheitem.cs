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
                private cBodyStructure mBody = null;

                public cMessageCacheItem(iMessageCache pCache, int pCacheSequence)
                {
                    mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                    mCacheSequence = pCacheSequence;
                }

                public iMessageCache Cache => mCache;
                public int CacheSequence => mCacheSequence;
                public bool Expunged => mExpunged;
                public fMessageProperties Properties => mProperties;
                public cBodyStructure Body => mBody ?? BodyEx;
                public cBodyStructure BodyEx { get; private set; } = null;
                public cEnvelope Envelope { get; private set; } = null;
                public cMessageFlags Flags { get; private set; } = null;
                public DateTime? Received { get; private set; } = null;
                public uint? Size { get; private set; } = null;
                public cUID UID { get; private set; } = null;
                public cStrings References { get; private set; } = null;
                public cBinarySizes BinarySizes { get; private set; } = null;
                public string HandleString => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence},{mProperties},{mExpunged})";

                public void SetExpunged() => mExpunged = true;

                public void Update(uint? pUIDValidity, cResponseDataFetch lFetch, out fMessageProperties rSet)
                {
                    rSet = 0;

                    if (lFetch.Body != null && mBody == null)
                    {
                        mBody = lFetch.Body;
                        mProperties |= fMessageProperties.body;
                        rSet |= fMessageProperties.body;
                    }

                    if (lFetch.BodyEx != null && BodyEx == null)
                    {
                        BodyEx = lFetch.BodyEx;
                        mProperties |= fMessageProperties.body | fMessageProperties.bodyex;
                        rSet |= fMessageProperties.body | fMessageProperties.bodyex;
                    }

                    if (lFetch.Envelope != null && Envelope == null)
                    {
                        Envelope = lFetch.Envelope;
                        mProperties |= fMessageProperties.envelope;
                        rSet |= fMessageProperties.envelope;
                    }

                    if (lFetch.Flags != null && lFetch.Flags != Flags)
                    {
                        Flags = lFetch.Flags;
                        mProperties |= fMessageProperties.flags;
                        rSet |= fMessageProperties.flags;
                    }

                    if (lFetch.Received != null && Received == null)
                    {
                        Received = lFetch.Received;
                        mProperties |= fMessageProperties.received;
                        rSet |= fMessageProperties.received;
                    }

                    if (lFetch.Size != null && Size == null)
                    {
                        Size = lFetch.Size;
                        mProperties |= fMessageProperties.size;
                        rSet |= fMessageProperties.size;
                    }

                    if (pUIDValidity != null && lFetch.UID != null && UID == null)
                    {
                        UID = new cUID(pUIDValidity.Value, lFetch.UID.Value);
                        mProperties |= fMessageProperties.uid;
                        rSet |= fMessageProperties.uid;
                    }

                    if (lFetch.References != null && References == null)
                    {
                        References = lFetch.References;
                        mProperties |= fMessageProperties.references;
                        rSet |= fMessageProperties.references;
                    }

                    if (BinarySizes == null) BinarySizes = lFetch.BinarySizes;
                    else if (lFetch.BinarySizes != null) BinarySizes = BinarySizes + lFetch.BinarySizes;
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence},{mProperties},{mExpunged},{BodyEx ?? mBody},{Envelope},{Flags},{Received},{Size},{UID},{References},{BinarySizes})";
            }
        }
    }
}