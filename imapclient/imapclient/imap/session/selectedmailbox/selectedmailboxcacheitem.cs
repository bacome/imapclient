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
                    private bool mExpunged = false;
                    private fMessageCacheAttributes mAttributes;
                    private cMessageUID mMessageUID = null;
                    private cModSeqFlags mModSeqFlags = null;
                    private cBodyPart mBody = null;
                    private iHeaderCacheItem mHeaderCacheItem = new cNoUIDHeaderCacheItem();

                    private iFlagCacheItem mFlagCacheItem = null;

                    private bool? mSeen = null; // is this message seen (null = don't know)
                    public bool? Unseen = null; // is this message unseen (null = don't know)

                    public cItem(cSelectedMailboxCache pSelectedMailboxCache, int pCacheSequence)
                    {
                        mSelectedMailboxCache = pSelectedMailboxCache ?? throw new ArgumentNullException(nameof(pSelectedMailboxCache));
                        mCacheSequence = pCacheSequence;
                        if (pSelectedMailboxCache.mUIDValidity.IsNone) mAttributes = fMessageCacheAttributes.uid;
                        else mAttributes = 0;
                    }

                    public iMessageCache MessageCache => mSelectedMailboxCache;
                    public int CacheSequence => mCacheSequence;
                    public bool Expunged => mExpunged;
                    public fMessageCacheAttributes Attributes => mAttributes;
                    public cMessageUID MessageUID => mMessageUID;
                    public cModSeqFlags ModSeqFlags => mModSeqFlags;
                    public cBodyPart Body => mBody ?? mHeaderCacheItem.BodyStructure;
                    public cEnvelope Envelope => mHeaderCacheItem.Envelope;
                    public cTimestamp Received => mHeaderCacheItem.Received;
                    public uint? Size => mHeaderCacheItem.Size;
                    public cBodyPart BodyStructure => mHeaderCacheItem.BodyStructure;
                    public cHeaderFields HeaderFields => mHeaderCacheItem.HeaderFields;
                    public cBinarySizes BinarySizes => mHeaderCacheItem.BinarySizes;

                    public bool Contains(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == 0 && mHeaderCacheItem.HeaderFields.Contains(pItems.Names);
                    public bool ContainsNone(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == pItems.Attributes && mHeaderCacheItem.HeaderFields.ContainsNone(pItems.Names);
                    public cMessageCacheItems Missing(cMessageCacheItems pItems) => new cMessageCacheItems(~mAttributes & pItems.Attributes, mHeaderCacheItem.HeaderFields.GetMissing(pItems.Names));

                    public bool? Seen => mSeen;
                    public ulong ModSeq => mModSeqFlags?.ModSeq ?? 0;

                    public void SetExpunged() => mExpunged = true;

                    public void Update(cResponseDataFetch pFetch, out bool rMessageUIDWasSet, out fIMAPMessageProperties rMessagePropertiesChanged, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(Update), pFetch);

                        // uid

                        if ((mAttributes & fMessageCacheAttributes.uid) == 0 && pFetch.UID != null)
                        {
                            rMessageUIDWasSet = true; // to indicate that the item should be indexed by uid
                            mMessageUID = new cMessageUID(mSelectedMailboxCache.MailboxHandle.MailboxId, new cUID(mSelectedMailboxCache.mUIDValidity, pFetch.UID.Value), mSelectedMailboxCache.mUTF8Enabled);

                            if (mSelectedMailboxCache.mPersistentCache.TryGetHeaderCacheItem(mMessageUID, out var lHeaderCacheItem, lContext))
                            {
                                if (lHeaderCacheItem == null) throw new cUnexpectedPersistentCacheActionException(lContext, 1);
                                lHeaderCacheItem.Update(mHeaderCacheItem, lContext); // updates the cache from the values I have
                                mHeaderCacheItem = lHeaderCacheItem;
                            }

                            if (mSelectedMailboxCache.mPersistentCache.TryGetFlagCacheItem(mMessageUID, out mFlagCacheItem, lContext))
                            {
                                if (mFlagCacheItem == null) throw new cUnexpectedPersistentCacheActionException(lContext, 2);
                                if (mModSeqFlags != null) ZUpdateCacheWithModSeqFlags(lContext);
                            }
                        }
                        else rMessageUIDWasSet = false;

                        // modseq and flags

                        if (pFetch.ModSeqFlags == null) rMessagePropertiesChanged = 0;
                        else
                        {
                            if (mModSeqFlags == null) rMessagePropertiesChanged = 0;
                            else
                            {
                                if (mModSeqFlags.ModSeq == pFetch.ModSeqFlags.ModSeq) rMessagePropertiesChanged = 0;
                                else rMessagePropertiesChanged = fIMAPMessageProperties.modseqflags;

                                foreach (var lFlag in mModSeqFlags.Flags.SymmetricDifference(pFetch.ModSeqFlags.Flags)) rMessagePropertiesChanged |= fIMAPMessageProperties.flags | LMessageProperty(lFlag);
                            }

                            mModSeqFlags = pFetch.ModSeqFlags;
                            mSeen = pFetch.ModSeqFlags.Flags.Contains(kMessageFlag.Seen);

                            ZUpdateCacheWithModSeqFlags(lContext);
                        }

                        // body

                        if (mBody != null) mBody = pFetch.Body;

                        // header

                        mHeaderCacheItem.Update(pFetch, lContext);

                        // set the attributes

                        if 

                        // done
                        return;

                        fIMAPMessageProperties LMessageProperty(string pFlag)
                        {
                            if (pFlag.Equals(kMessageFlag.Answered, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.answered;
                            if (pFlag.Equals(kMessageFlag.Flagged, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.flagged;
                            if (pFlag.Equals(kMessageFlag.Deleted, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.deleted;
                            if (pFlag.Equals(kMessageFlag.Seen, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.seen;
                            if (pFlag.Equals(kMessageFlag.Draft, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.draft;
                            if (pFlag.Equals(kMessageFlag.Recent, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.recent;
                            if (pFlag.Equals(kMessageFlag.Forwarded, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.forwarded;
                            if (pFlag.Equals(kMessageFlag.SubmitPending, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.submitpending;
                            if (pFlag.Equals(kMessageFlag.Submitted, StringComparison.InvariantCultureIgnoreCase)) return fIMAPMessageProperties.submitted;
                            return 0;
                        }
                    }

                    public void ZUpdateCacheWithModSeqFlags(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZUpdateCacheWithModSeqFlags));
                        if (mModSeqFlags == null) throw new InvalidOperationException();
                        if (mFlagCacheItem == null) return;
                        if (mModSeqFlags.ModSeq == 0) mSelectedMailboxCache.mPersistentCache.NoModSeqFlagUpdate(mSelectedMailboxCache.mMailboxUID, lContext);
                        mFlagCacheItem.Update(mModSeqFlags, lContext);
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

