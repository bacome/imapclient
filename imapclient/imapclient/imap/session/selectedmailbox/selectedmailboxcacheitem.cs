using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailboxCache
            {
                private class cItem : iMessageHandle, IComparable<cItem> // icomparable is implemented for binary search
                {
                    private readonly cSelectedMailboxCache mSelectedMailboxCache;
                    private readonly int mCacheSequence;

                    private cUID mUID = null;
                    private iHeaderCacheItem mHeaderCacheItem = new cHeaderCacheItem();
                    private iFlagCacheItem mFlagCacheItem = new cFlagCacheItem;

                    private bool mExpunged = false;
                    private fMessageCacheAttributes mAttributes;
                    public bool? Unseen = null; // is this message unseen (null = don't know)

                    public cItem(cSelectedMailboxCache pSelectedMailboxCache, int pCacheSequence)
                    {
                        mSelectedMailboxCache = pSelectedMailboxCache ?? throw new ArgumentNullException(nameof(pSelectedMailboxCache));
                        mCacheSequence = pCacheSequence;

                        if (pSelectedMailboxCache.NoModSeq) mAttributes = fMessageCacheAttributes.modseq;
                        else mAttributes = 0;
                    }

                    public iMessageCache MessageCache => mSelectedMailboxCache;
                    public int CacheSequence => mCacheSequence;
                    public bool Expunged => mExpunged;
                    public fMessageCacheAttributes Attributes => mAttributes;

                    public cFetchableFlags Flags => mFlagCacheItem.Flags;
                    public cEnvelope Envelope => mHeaderCacheItem.Envelope;
                    public DateTimeOffset? ReceivedDateTimeOffset => mHeaderCacheItem.ReceivedDateTimeOffset;
                    public DateTime? ReceivedDateTime => mHeaderCacheItem.ReceivedDateTime;
                    public uint? Size => mHeaderCacheItem.Size;
                    public cBodyPart Body => mHeaderCacheItem.Body ?? mHeaderCacheItem.BodyStructure;
                    public cBodyPart BodyStructure => mHeaderCacheItem.BodyStructure;
                    public cUID UID => mUID;

                    public ulong? ModSeq
                    {
                        get
                        {
                            if (mSelectedMailboxCache.mNoModSeq) return 0;
                            return mFlagCacheItem.ModSeq;
                        }
                    }

                    public cHeaderFields HeaderFields => mHeaderCacheItem.HeaderFields;
                    public cBinarySizes BinarySizes => mHeaderCacheItem.BinarySizes;

                    public bool Contains(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == 0 && mHeaderCacheItem.HeaderFields.Contains(pItems.Names);
                    public bool ContainsNone(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == pItems.Attributes && mHeaderCacheItem.HeaderFields.ContainsNone(pItems.Names);
                    public cMessageCacheItems Missing(cMessageCacheItems pItems) => new cMessageCacheItems(~mAttributes & pItems.Attributes, mHeaderCacheItem.HeaderFields.GetMissing(pItems.Names));

                    public void SetExpunged() => mExpunged = true;

                    public void Update(cResponseDataFetch pFetch, out bool rUIDWasSet, out bool rFlagsWereSet, out fIMAPMessageProperties rPropertiesChanged)
                    {
                        rFlagsWereSet = false;

                        if (((mAttributes & fMessageCacheAttributes.uid) == 0) && mSelectedMailboxCache.mUIDValidity != 0 && ((pFetch.Attributes & fMessageCacheAttributes.uid) != 0))
                        {
                            // if we discover the UID

                            mUID = new cUID(mSelectedMailboxCache.mUIDValidity, pFetch.UID.Value);

                            var lMessageUID = new cMessageUID(mSelectedMailboxCache.MailboxHandle.MailboxId, mUID);

                            var lHeaderCacheItem = mSelectedMailboxCache.mPersistentCache.GetHeaderCacheItem(lMessageUID);

                            if (lHeaderCacheItem != null)
                            {
                                if (lHeaderCacheItem.Envelope == null)
                                {
                                    if (mHeaderCacheItem.Envelope != null) lHeaderCacheItem.Envelope = mHeaderCacheItem.Envelope;
                                }
                                else mAttributes |= fMessageCacheAttributes.envelope;

                                if (lHeaderCacheItem.ReceivedDateTimeOffset == null)
                                {
                                    if (mHeaderCacheItem.ReceivedDateTimeOffset != null) lHeaderCacheItem.SetReceivedDateTime(mHeaderCacheItem.ReceivedDateTime.Value, mHeaderCacheItem.ReceivedDateTime.Value);
                                }
                                else mAttributes |= fMessageCacheAttributes.received;

                                if (lHeaderCacheItem.Size == null)
                                {
                                    if (mHeaderCacheItem.Size != null) lHeaderCacheItem.Size = mHeaderCacheItem.Size;
                                }
                                else mAttributes |= fMessageCacheAttributes.size;

                                if (lHeaderCacheItem.Body == null)
                                {
                                    if (mHeaderCacheItem.Body != null) lHeaderCacheItem.Body = mHeaderCacheItem.Body;
                                }
                                else mAttributes |= fMessageCacheAttributes.body;

                                if (lHeaderCacheItem.BodyStructure == null)
                                {
                                    if (mHeaderCacheItem.BodyStructure != null) lHeaderCacheItem.BodyStructure = mHeaderCacheItem.BodyStructure;
                                }
                                else mAttributes |= fMessageCacheAttributes.bodystructure;

                                if (mHeaderCacheItem.HeaderFields != null) lHeaderCacheItem.AddHeaderFields(mHeaderCacheItem.HeaderFields);
                                if (mHeaderCacheItem.BinarySizes != null) lHeaderCacheItem.AddBinarySizes(mHeaderCacheItem.BinarySizes);

                                mHeaderCacheItem = lHeaderCacheItem;
                            }

                            var lFlagCacheItem = mSelectedMailboxCache.mPersistentCache.GetFlagCacheItem(lMessageUID);

                            if (lFlagCacheItem != null)
                            {
                                ;?; // the flags and modseq should be considered together
                                if (lFlagCacheItem.Flags == null)
                                {
                                    if (mFlagCacheItem.Flags != null) lFlagCacheItem.Update(mFlagCacheItem.Flags, mFlagCacheItem.ModSeq ?? 0);
                                }
                                else
                                {
                                    if (mFlagCacheItem.Flags != lFlagCacheItem.Flags) rFlagsWereSet = true;
                                    mAttributes |= fMessageCacheAttributes.flags;
                                }

                                ;?;
                                if (lFlagCacheItem.ModSeq == null)
                                {
                                    ;?; // this is an odd situation
                                    if (mFlagCacheItem.ModSeq != null) lFlagCacheItem.ModSeq = mFlagCacheItem.ModSeq;
                                }
                                else mAttributes |= fMessageCacheAttributes.modseq;

                                mFlagCacheItem = lFlagCacheItem;
                            }
                        }

                        var lAttributesSet = ~mAttributes & pFetch.Attributes;
                        rPropertiesChanged = 0;

                        if ((lAttributesSet & fMessageCacheAttributes.flags) != 0)
                        {
                            mFlagCacheItem.Flags = pFetch.Flags;
                            rFlagsWereSet = true;
                        }
                        else if (pFetch.Flags != null)
                        {
                            bool lFlagsWereSet = false;

                            foreach (var lFlag in mFlagCacheItem.Flags.SymmetricDifference(pFetch.Flags))
                            {
                                rFlagsWereSet = true;
                                rPropertiesChanged |= fIMAPMessageProperties.flags | LMessageProperty(lFlag);
                            }

                            if (lFlagsWereSet)

                                mFlagCacheItem.Flags = pFetch.Flags;
                        }


                        ;?;
                        if ((rAttributesSet & fMessageCacheAttributes.envelope) != 0) mHeaderCacheItem.Envelope = lFetch.Envelope;
                        if ((rAttributesSet & fMessageCacheAttributes.received) != 0) mHeaderCacheItem.SetReceivedDateTime(lFetch.ReceivedDateTimeOffset.Value, lFetch.ReceivedDateTime.Value);
                        if ((rAttributesSet & fMessageCacheAttributes.size) != 0) mHeaderCacheItem.Size = lFetch.Size;
                        if ((rAttributesSet & fMessageCacheAttributes.body) != 0) mHeaderCacheItem.Body = lFetch.Body;
                        if ((rAttributesSet & fMessageCacheAttributes.bodystructure) != 0) mHeaderCacheItem.BodyStructure = lFetch.BodyStructure;

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

                        ;?; // use the lattset
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

                    public int CompareTo(cItem pOther)
                    {
                        if (pOther == null) return 1;
                        return CacheSequence.CompareTo(pOther.CacheSequence);
                    }

                    public override string ToString() => $"{nameof(cItem)}({mSelectedMailboxCache},{mCacheSequence})";
                }
            }
        }
    }
}

