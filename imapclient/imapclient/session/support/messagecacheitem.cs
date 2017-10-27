using System;
using System.Collections.Generic;
using System.Linq;
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
                private fCacheAttributes mAttributes;
                private cBodyPart mBody = null;
                private ulong? mModSeq;

                public cMessageCacheItem(iMessageCache pCache, int pCacheSequence)
                {
                    mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                    mCacheSequence = pCacheSequence;

                    if (pCache.NoModSeq)
                    {
                        mAttributes = fCacheAttributes.modseq;
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
                public fCacheAttributes Attributes => mAttributes;
                public cBodyPart Body => mBody ?? BodyStructure;
                public cBodyPart BodyStructure { get; private set; } = null;
                public cEnvelope Envelope { get; private set; } = null;
                public cFetchableFlags Flags { get; private set; } = null;
                public ulong? ModSeq => mModSeq;
                public DateTime? Received { get; private set; } = null;
                public uint? Size { get; private set; } = null;
                public cUID UID { get; private set; } = null;
                public cHeaderFields HeaderFields { get; private set; } = cHeaderFields.None;
                public cBinarySizes BinarySizes { get; private set; } = cBinarySizes.None;

                public bool Contains(cCacheItems pItems) => (~mAttributes & pItems.Attributes) == 0 && HeaderFields.Contains(pItems.Names);
                public bool ContainsNone(cCacheItems pItems) => (~mAttributes & pItems.Attributes) == pItems.Attributes && HeaderFields.ContainsNone(pItems.Names);
                public cCacheItems Missing(cCacheItems pItems) => new cCacheItems(~mAttributes & pItems.Attributes, HeaderFields.Missing(pItems.Names));

                public void SetExpunged() => mExpunged = true;

                public void Update(cResponseDataFetch lFetch, out fCacheAttributes rAttributesSet, out fMessageProperties rPropertiesChanged)
                {
                    rAttributesSet = ~mAttributes & lFetch.Attributes;
                    rPropertiesChanged = 0;

                    if ((rAttributesSet & fCacheAttributes.flags) != 0) Flags = lFetch.Flags;
                    else if (lFetch.Flags != null)
                    {
                        foreach (var lFlag in Flags.SymmetricDifference(lFetch.Flags))
                        {
                            rAttributesSet |= fCacheAttributes.flags;
                            rPropertiesChanged |= fMessageProperties.flags | LMessageProperty(lFlag);
                        }

                        Flags = lFetch.Flags;
                    }

                    if ((rAttributesSet & fCacheAttributes.envelope) != 0) Envelope = lFetch.Envelope;
                    if ((rAttributesSet & fCacheAttributes.received) != 0) Received = lFetch.Received;
                    if ((rAttributesSet & fCacheAttributes.size) != 0) Size = lFetch.Size;
                    if ((rAttributesSet & fCacheAttributes.body) != 0) mBody = lFetch.Body;
                    if ((rAttributesSet & fCacheAttributes.bodystructure) != 0) BodyStructure = lFetch.BodyStructure;
                    if ((rAttributesSet & fCacheAttributes.uid) != 0 && mCache.UIDValidity != 0) UID = new cUID(mCache.UIDValidity, lFetch.UID.Value);

                    if (!mCache.NoModSeq)
                    {
                        if ((rAttributesSet & fCacheAttributes.modseq) != 0) mModSeq = lFetch.ModSeq;
                        else if (lFetch.ModSeq != null && lFetch.ModSeq != mModSeq)
                        {
                            rAttributesSet |= fCacheAttributes.modseq;
                            rPropertiesChanged |= fMessageProperties.modseq;
                            mModSeq = lFetch.ModSeq;
                        }
                    }

                    HeaderFields += lFetch.HeaderFields;
                    BinarySizes += lFetch.BinarySizes;

                    mAttributes |= lFetch.Attributes;

                    fMessageProperties LMessageProperty(string pFlag)
                    {
                        if (pFlag.Equals(kMessageFlagName.Answered, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.answered;
                        if (pFlag.Equals(kMessageFlagName.Flagged, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.flagged;
                        if (pFlag.Equals(kMessageFlagName.Deleted, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.deleted;
                        if (pFlag.Equals(kMessageFlagName.Seen, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.seen;
                        if (pFlag.Equals(kMessageFlagName.Draft, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.draft;
                        if (pFlag.Equals(kMessageFlagName.Recent, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.recent;
                        // see comments elsewhere as to why this is commented out
                        //if (pFlag.Equals(kMessageFlagName.MDNSent, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.mdnsent;
                        if (pFlag.Equals(kMessageFlagName.Forwarded, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.forwarded;
                        if (pFlag.Equals(kMessageFlagName.SubmitPending, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.submitpending;
                        if (pFlag.Equals(kMessageFlagName.Submitted, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.submitted;
                        return 0;
                    }
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence})";
            }
        }
    }
}