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
                public cHeaderFields HeaderFields { get; private set; } = null;
                public cBinarySizes BinarySizes { get; private set; } = null;

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
                        ;?; // rewrite to two loops
                        foreach (var lFlag in lFetch.Flags.SymmetricDifference(Flags))
                        {
                            rAttributesSet |= fCacheAttributes.flags;
                            rPropertiesChanged |= fMessageProperties.flags;

                            if (lFlag.Equals(kFlagName.Answered, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.isanswered;
                            if (lFlag.Equals(kFlagName.Flagged, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.isflagged;
                            if (lFlag.Equals(kFlagName.Deleted, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.isdeleted;
                            if (lFlag.Equals(kFlagName.Seen, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.isseen;
                            if (lFlag.Equals(kFlagName.Draft, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.isdraft;
                            if (lFlag.Equals(kFlagName.Recent, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.isrecent;
                            if (lFlag.Equals(kFlagName.MDNSent, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.ismdnsent;
                            if (lFlag.Equals(kFlagName.Forwarded, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.isforwarded;
                            if (lFlag.Equals(kFlagName.SubmitPending, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.issubmitpending;
                            if (lFlag.Equals(kFlagName.Submitted, StringComparison.InvariantCultureIgnoreCase)) rPropertiesChanged |= fMessageProperties.issubmitted;
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

                    if (HeaderFields == null) HeaderFields = lFetch.HeaderFields;
                    else if (lFetch.HeaderFields != null) HeaderFields += lFetch.HeaderFields;

                    if (BinarySizes == null) BinarySizes = lFetch.BinarySizes;
                    else if (lFetch.BinarySizes != null) BinarySizes += lFetch.BinarySizes;

                    mAttributes |= lFetch.Attributes;
                }

                public override string ToString() => $"{nameof(cMessageCacheItem)}({mCache},{mCacheSequence})";
            }
        }
    }
}