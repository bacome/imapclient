using System;
using work.bacome.imapclient.support;

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
                private fFetchAttributes mAttributes;
                private cBodyPart mBody = null;
                private ulong? mModSeq;

                public cMessageCacheItem(iMessageCache pCache, int pCacheSequence)
                {
                    mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                    mCacheSequence = pCacheSequence;

                    if (pCache.NoModSeq)
                    {
                        mAttributes = fFetchAttributes.modseq;
                        mModSeq = 0;
                    }
                    else
                    {
                        mAttributes = 0;
                        mModSeq = null;
                    }
                }

                public iMessageCache Cache => mCache;
                public int CacheSequence => mCacheSequence;
                public bool Expunged => mExpunged;
                public fFetchAttributes Attributes => mAttributes;
                public cBodyPart Body => mBody ?? BodyStructure;
                public cBodyPart BodyStructure { get; private set; } = null;
                public cEnvelope Envelope { get; private set; } = null;
                public cMessageFlags Flags { get; private set; } = null;
                public ulong? ModSeq => mModSeq;
                public DateTime? Received { get; private set; } = null;
                public uint? Size { get; private set; } = null;
                public cUID UID { get; private set; } = null;
                public cHeaderFields HeaderFields { get; private set; } = null;
                public cBinarySizes BinarySizes { get; private set; } = null;

                public bool ContainsAll(cFetchAttributes pAttributes) => (~mAttributes & pAttributes.Attributes) == 0 && HeaderFields.ContainsAll(pAttributes.Names);
                public bool ContainsNone(cFetchAttributes pAttributes) => (~mAttributes & pAttributes.Attributes) == pAttributes.Attributes && HeaderFields.ContainsNone(pAttributes.Names);
                public cFetchAttributes Missing(cFetchAttributes pAttributes) => new cFetchAttributes(~mAttributes & pAttributes.Attributes, HeaderFields.Missing(pAttributes.Names));

                public void SetExpunged() => mExpunged = true;

                public void Update(cResponseDataFetch lFetch, out fFetchAttributes rAttributesSet, out fKnownMessageFlags rKnownMessageFlagsSet, out fMessageProperties rDifferences)
                {
                    rAttributesSet = ~mAttributes & lFetch.Attributes;
                    rKnownMessageFlagsSet = 0;
                    rDifferences = 0;

                    if ((rAttributesSet & fFetchAttributes.flags) != 0) Flags = lFetch.Flags;
                    else if (lFetch.Flags != null && lFetch.Flags != Flags)
                    {
                        rAttributesSet |= fFetchAttributes.flags;
                        rKnownMessageFlagsSet = lFetch.Flags.KnownMessageFlags ^ Flags.KnownMessageFlags;
                        rDifferences |= cMessageFlags.Differences(Flags, lFetch.Flags);
                        Flags = lFetch.Flags;
                    }

                    if ((rAttributesSet & fFetchAttributes.envelope) != 0) Envelope = lFetch.Envelope;
                    if ((rAttributesSet & fFetchAttributes.received) != 0) Received = lFetch.Received;
                    if ((rAttributesSet & fFetchAttributes.size) != 0) Size = lFetch.Size;
                    if ((rAttributesSet & fFetchAttributes.body) != 0) mBody = lFetch.Body;
                    if ((rAttributesSet & fFetchAttributes.bodystructure) != 0) BodyStructure = lFetch.BodyStructure;
                    if ((rAttributesSet & fFetchAttributes.uid) != 0 && mCache.UIDValidity != 0) UID = new cUID(mCache.UIDValidity, lFetch.UID.Value);

                    if (!Cache.NoModSeq)
                    {
                        if ((rAttributesSet & fFetchAttributes.modseq) != 0) mModSeq = lFetch.ModSeq;
                        else if (lFetch.ModSeq != null && lFetch.ModSeq != mModSeq)
                        {
                            rAttributesSet |= fFetchAttributes.modseq;
                            rDifferences |= fMessageProperties.modseq;
                            mModSeq = lFetch.ModSeq;
                        }
                    }

                    if (HeaderFields == null) HeaderFields = lFetch.HeaderFields;
                    else if (lFetch.HeaderFields != null) HeaderFields = HeaderFields + lFetch.HeaderFields;

                    if (BinarySizes == null) BinarySizes = lFetch.BinarySizes;
                    else if (lFetch.BinarySizes != null) BinarySizes = BinarySizes + lFetch.BinarySizes;

                    mAttributes |= lFetch.Attributes;
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence})";
            }
        }
    }
}