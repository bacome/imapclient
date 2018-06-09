using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cMessageCacheItem : iMessageHandle
            {
                private readonly iMessageCache mMessageCache;
                private readonly int mCacheSequence;
                private bool mExpunged = false;
                private fMessageCacheAttributes mAttributes;
                private cBodyPart mBody = null;
                private ulong? mModSeq;

                public cMessageCacheItem(iMessageCache pMessageCache, int pCacheSequence)
                {
                    mMessageCache = pMessageCache ?? throw new ArgumentNullException(nameof(pMessageCache));
                    mCacheSequence = pCacheSequence;

                    if (pMessageCache.NoModSeq)
                    {
                        mAttributes = fMessageCacheAttributes.modseq;
                        mModSeq = 0;
                    }
                    else
                    {
                        mAttributes = 0;
                        mModSeq = null;
                    }
                }

                public iMessageCache MessageCache => mMessageCache;
                public int CacheSequence => mCacheSequence;
                public bool Expunged => mExpunged;
                public fMessageCacheAttributes Attributes => mAttributes;
                public cBodyPart Body => mBody ?? BodyStructure;
                public cBodyPart BodyStructure { get; private set; } = null;
                public cEnvelope Envelope { get; private set; } = null;
                public cFetchableFlags Flags { get; private set; } = null;
                public ulong? ModSeq => mModSeq;
                public DateTimeOffset? ReceivedDateTimeOffset { get; private set; } = null;
                public DateTime? ReceivedDateTime { get; private set; } = null;
                public uint? Size { get; private set; } = null;
                public cUID UID { get; private set; } = null;
                public cHeaderFields HeaderFields { get; private set; } = cHeaderFields.Empty;
                public cBinarySizes BinarySizes { get; private set; } = cBinarySizes.Empty;

                public bool Contains(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == 0 && HeaderFields.Contains(pItems.Names);
                public bool ContainsNone(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == pItems.Attributes && HeaderFields.ContainsNone(pItems.Names);
                public cMessageCacheItems Missing(cMessageCacheItems pItems) => new cMessageCacheItems(~mAttributes & pItems.Attributes, HeaderFields.GetMissing(pItems.Names));

                public void SetExpunged() => mExpunged = true;

                public void Update(cResponseDataFetch lFetch, out fMessageCacheAttributes rAttributesSet, out fIMAPMessageProperties rPropertiesChanged)
                {
                    rAttributesSet = ~mAttributes & lFetch.Attributes;
                    rPropertiesChanged = 0;

                    if ((rAttributesSet & fMessageCacheAttributes.flags) != 0) Flags = lFetch.Flags;
                    else if (lFetch.Flags != null)
                    {
                        foreach (var lFlag in Flags.SymmetricDifference(lFetch.Flags))
                        {
                            rAttributesSet |= fMessageCacheAttributes.flags;
                            rPropertiesChanged |= fIMAPMessageProperties.flags | LMessageProperty(lFlag);
                        }

                        Flags = lFetch.Flags;
                    }

                    if ((rAttributesSet & fMessageCacheAttributes.envelope) != 0) Envelope = lFetch.Envelope;

                    if ((rAttributesSet & fMessageCacheAttributes.received) != 0)
                    {
                        ReceivedDateTimeOffset = lFetch.ReceivedDateTimeOffset;
                        ReceivedDateTime = lFetch.ReceivedDateTime;
                    }

                    if ((rAttributesSet & fMessageCacheAttributes.size) != 0) Size = lFetch.Size;
                    if ((rAttributesSet & fMessageCacheAttributes.body) != 0) mBody = lFetch.Body;
                    if ((rAttributesSet & fMessageCacheAttributes.bodystructure) != 0) BodyStructure = lFetch.BodyStructure;
                    if ((rAttributesSet & fMessageCacheAttributes.uid) != 0 && mMessageCache.UIDValidity != 0) UID = new cUID(mMessageCache.UIDValidity, lFetch.UID.Value);

                    if (!mMessageCache.NoModSeq)
                    {
                        if ((rAttributesSet & fMessageCacheAttributes.modseq) != 0) mModSeq = lFetch.ModSeq;
                        else if (lFetch.ModSeq != null && lFetch.ModSeq != mModSeq)
                        {
                            rAttributesSet |= fMessageCacheAttributes.modseq;
                            rPropertiesChanged |= fIMAPMessageProperties.modseq;
                            mModSeq = lFetch.ModSeq;
                        }
                    }

                    HeaderFields += lFetch.HeaderFields;
                    BinarySizes += lFetch.BinarySizes;

                    mAttributes |= lFetch.Attributes;

                    fIMAPMessageProperties LMessageProperty(string pFlag)
                    {
                        if (pFlag.Equals(kMessageFlag.Answered, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.answered;
                        if (pFlag.Equals(kMessageFlag.Flagged, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.flagged;
                        if (pFlag.Equals(kMessageFlag.Deleted, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.deleted;
                        if (pFlag.Equals(kMessageFlag.Seen, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.seen;
                        if (pFlag.Equals(kMessageFlag.Draft, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.draft;
                        if (pFlag.Equals(kMessageFlag.Recent, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.recent;
                        // see comments elsewhere as to why this is commented out
                        //if (pFlag.Equals(kMessageFlagName.MDNSent, StringComparison.InvariantCultureIgnoreCase)) return fMessageProperties.mdnsent;
                        if (pFlag.Equals(kMessageFlag.Forwarded, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.forwarded;
                        if (pFlag.Equals(kMessageFlag.SubmitPending, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.submitpending;
                        if (pFlag.Equals(kMessageFlag.Submitted, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.submitted;
                        return 0;
                    }
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mMessageCache},{mCacheSequence})";
            }
        }
    }
}