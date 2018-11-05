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
                    private cMessageUID mMessageUID = null;
                    private cModSeqFlags mLastModSeqFlags = null;
                    private iFlagCacheItem mFlagCacheItem = new cNoUIDFlagCacheItem();
                    private iHeaderCacheItem mHeaderCacheItem = new cNoUIDHeaderCacheItem();

                    private bool? mSeen = null; // is this message seen (null = don't know)
                    public bool? Unseen = null; // is this message unseen (null = don't know)

                    public cItem(cSelectedMailboxCache pSelectedMailboxCache, int pCacheSequence)
                    {
                        mSelectedMailboxCache = pSelectedMailboxCache ?? throw new ArgumentNullException(nameof(pSelectedMailboxCache));
                        mCacheSequence = pCacheSequence;
                    }

                    public iMessageCache MessageCache => mSelectedMailboxCache;
                    public int CacheSequence => mCacheSequence;
                    public bool Expunged => mExpunged;

                    public fMessageCacheAttributes Attributes
                    {
                        get
                        {
                            if (mMessageUID != null || mSelectedMailboxCache.mUIDValidity == 0) return fMessageCacheAttributes.uid | mFlagCacheItem.Attributes | mHeaderCacheItem.Attributes;
                            return mFlagCacheItem.Attributes | mHeaderCacheItem.Attributes;
                        }
                    }

                    public bool Contains(cMessageCacheItems pItems) => (Attributes & pItems.Attributes) == 0 && mHeaderCacheItem.HeaderFields.Contains(pItems.Names);
                    public bool ContainsNone(cMessageCacheItems pItems) => (Attributes & pItems.Attributes) == pItems.Attributes && mHeaderCacheItem.HeaderFields.ContainsNone(pItems.Names);
                    public cMessageCacheItems Missing(cMessageCacheItems pItems) => new cMessageCacheItems(Attributes & pItems.Attributes, mHeaderCacheItem.HeaderFields.GetMissing(pItems.Names));

                    public cMessageUID MessageUID => mMessageUID;

                    public cModSeqFlags ModSeqFlags => mLastModSeqFlags;

                    public cEnvelope Envelope => mHeaderCacheItem.Envelope;
                    public cTimestamp Received => mHeaderCacheItem.Received;
                    public uint? Size => mHeaderCacheItem.Size;
                    public cBodyPart BodyStructure => mHeaderCacheItem.BodyStructure;
                    public cHeaderFields HeaderFields => mHeaderCacheItem.HeaderFields;
                    public cBinarySizes BinarySizes => mHeaderCacheItem.BinarySizes;

                    public bool? Seen => mSeen;

                    public void SetExpunged() => mExpunged = true;

                    public void Update(cResponseDataFetch pFetch, out bool rMessageUIDWasSet, out fIMAPMessageProperties rMessagePropertiesChanged, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(Update), pFetch);

                        if (mMessageUID == null && mSelectedMailboxCache.mUIDValidity != 0 && pFetch.UID != null)
                        {
                            rMessageUIDWasSet = true; // to indicate that the item should be indexed by uid

                            mMessageUID = new cMessageUID(mSelectedMailboxCache.MailboxHandle.MailboxId, new cUID(mSelectedMailboxCache.mUIDValidity, pFetch.UID.Value), mSelectedMailboxCache.mUIDNotSticky, mSelectedMailboxCache.mUTF8Enabled);

                            var lFlagCacheItem = mSelectedMailboxCache.mPersistentCache.GetFlagCacheItem(mMessageUID, lContext);
                            if (lFlagCacheItem == null) throw new cUnexpectedPersistentCacheActionException(lContext, 1);
                            ZFlagCacheItemUpdate(lFlagCacheItem, mFlagCacheItem, lContext);
                            ZFlagCacheItemUpdate(lFlagCacheItem, pFetch, lContext);
                            mFlagCacheItem = lFlagCacheItem;

                            var lHeaderCacheItem = mSelectedMailboxCache.mPersistentCache.GetHeaderCacheItem(mMessageUID, lContext);
                            if (lHeaderCacheItem == null) throw new cUnexpectedPersistentCacheActionException(lContext, 2);
                            lHeaderCacheItem.Update(mHeaderCacheItem, lContext);
                            lHeaderCacheItem.Update(pFetch, lContext);
                            mHeaderCacheItem = lHeaderCacheItem;
                        }
                        else
                        {
                            rMessageUIDWasSet = false;
                            ZFlagCacheItemUpdate(mFlagCacheItem, pFetch, lContext);
                            mHeaderCacheItem.Update(pFetch, lContext);
                        }

                        rMessagePropertiesChanged = 0;

                        var lNewModSeqFlags = mFlagCacheItem.ModSeqFlags;

                        if (mLastModSeqFlags != null && lNewModSeqFlags != null)
                        {
                            if (lNewModSeqFlags.ModSeq != mLastModSeqFlags.ModSeq) rMessagePropertiesChanged |= fIMAPMessageProperties.modseqflags;
                            foreach (var lFlag in mLastModSeqFlags.Flags.SymmetricDifference(lNewModSeqFlags.Flags)) rMessagePropertiesChanged |= fIMAPMessageProperties.flags | LMessageProperty(lFlag);
                        }

                        if (lNewModSeqFlags != null) mSeen = lNewModSeqFlags.Flags.Contains(kMessageFlag.Seen);

                        mLastModSeqFlags = lNewModSeqFlags;

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

                    private void ZFlagCacheItemUpdate(iFlagCacheItem pFlagCacheItem, iFlagDataItem pFlagDataItem, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZFlagCacheItemUpdate), pFlagCacheItem, pFlagDataItem);
                        if (pFlagCacheItem == null) throw new ArgumentNullException(nameof(pFlagCacheItem));
                        if (pFlagDataItem == null) throw new ArgumentNullException(nameof(pFlagDataItem));
                        if (pFlagDataItem.ModSeqFlags == null) return;
                        if (pFlagDataItem.ModSeqFlags.ModSeq == 0) mSelectedMailboxCache.ZNoModSeqFlagUpdate(lContext);
                        pFlagCacheItem.Update(pFlagDataItem, lContext);
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

