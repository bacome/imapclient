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
                public cHeaders Headers { get; private set; } = null;
                public cBinarySizes BinarySizes { get; private set; } = null;
                public ulong? ModSeq { get; private set; } = null;

                public void SetExpunged() => mExpunged = true;

                public void Update(uint pUIDValidity, bool pNoModSeq, cResponseDataFetch lFetch, out fFetchAttributes rAttributesSet, out fKnownMessageFlags rKnownMessageFlagsSet, out fMessageProperties rDifferences)
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
                    if ((rAttributesSet & fFetchAttributes.uid) != 0 && pUIDValidity != 0) UID = new cUID(pUIDValidity, lFetch.UID.Value);
                    if ((rAttributesSet & fFetchAttributes.references) != 0) References = lFetch.References;

                    if (BinarySizes == null) BinarySizes = lFetch.BinarySizes;
                    else if (lFetch.BinarySizes != null) BinarySizes = BinarySizes + lFetch.BinarySizes;

                    if (!pNoModSeq)
                    {
                        if ((rAttributesSet & fFetchAttributes.modseq) != 0) ModSeq = lFetch.ModSeq;
                        else if (lFetch.ModSeq != null && lFetch.ModSeq != ModSeq)
                        {
                            rAttributesSet |= fFetchAttributes.modseq;
                            rDifferences |= fMessageProperties.modseq;
                            ModSeq = lFetch.ModSeq;
                        }
                    }

                    mAttributes |= lFetch.Attributes;
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence})";
            }
        }
    }
}