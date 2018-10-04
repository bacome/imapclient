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

                    private bool mReceivedUID = false;
                    private cMessageUID mMessageUID = null;

                    private iPersistentHeaderCacheItem mHeaderCacheItem = new cHeaderCacheItem();
                    private iPersistentFlagCacheItem mFlagCacheItem = ?;

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
                            fMessageCacheAttributes lAttributes = 0;

                            if (mSelectedMailboxCache.NoModSeq) lAttributes |= fMessageCacheAttributes.modseq;
                            if (mReceivedUID) lAttributes |= fMessageCacheAttributes.uid;
                            lAttributes |= mHeaderCacheItem.attributes;
                            if (mModSeqFlags != null) lAttributes |= fMessageCacheAttributes.flags | fMessageCacheAttributes.modseq;

                            return lAttributes;
                        }
                    }

                    ;/; // modseqflags

                    public cEnvelope Envelope
                    {
                        get
                        {
                            if (mHeaderCacheItem.Envelope == null) return null;
                            mHeaderCacheItem.RecordAccess();
                            return mHeaderCacheItem.Envelope;
                        }
                    }

                    ;?; // more property changes here
                    public cTimestamp Received => mHeaderCacheItem.Received;
                    public uint? Size => mHeaderCacheItem.Size;
                    public cBodyPart Body => mBody ?? mHeaderCacheItem.BodyStructure;
                    public cBodyPart BodyStructure => mHeaderCacheItem.BodyStructure;

                    ;?; // messageuid

                    public cHeaderFields HeaderFields => mHeaderCacheItem.HeaderFields;
                    public cBinarySizes BinarySizes => mHeaderCacheItem.BinarySizes;

                    public bool Contains(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == 0 && mHeaderCacheItem.HeaderFields.Contains(pItems.Names);
                    public bool ContainsNone(cMessageCacheItems pItems) => (~mAttributes & pItems.Attributes) == pItems.Attributes && mHeaderCacheItem.HeaderFields.ContainsNone(pItems.Names);
                    public cMessageCacheItems Missing(cMessageCacheItems pItems) => new cMessageCacheItems(~mAttributes & pItems.Attributes, mHeaderCacheItem.HeaderFields.GetMissing(pItems.Names));

                    public bool? Seen => mSeen;

                    public void SetExpunged() => mExpunged = true;

                    public void Update(cResponseDataFetch pFetch, out bool rUIDWasSet, out fIMAPMessageProperties rModSeqFlagPropertiesChanged, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(Update), pFetch);

                        // uid

                        rUIDWasSet = false; // to indicate that the item should be indexed by uid

                        if (!mReceivedUID  && pFetch.UID != null)
                        {
                            mReceivedUID = true;

                            if (mSelectedMailboxCache.mUIDValidity != 0)
                            {
                                rUIDWasSet = true;

                                mUID = new cUID(mSelectedMailboxCache.mUIDValidity, pFetch.UID.Value);
                                mMessageUID = new cMessageUID(mSelectedMailboxCache.MailboxHandle.MailboxId, mUID);

                                ;?; // ONLY get things from the cache if the UID is sticky => stikcy is a property of the mSelectedMailboxCache

                                ;?; // is this optional to return (NO) and the get should touch it
                                if (mSelectedMailboxCache.mPersistentCache.TryGetHeaderCacheItem(mMessageUID, out var lHeaderCacheItem, lContext))
                                {
                                    ;?; // don't use the getters here as they set the touched
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

                                var lFlagCacheItem = mSelectedMailboxCache.mPersistentCache.getflagcacheitem(mMessageUID, lContext);

                                if (lFlagCacheItem == null) throw new cUnexpectedPersistentCacheActionException(lContext, 2);

                                ;?; // do stuff


                                if (mModSeqFlags == null)
                                {
                                    ;?; // note: if I have condstore on but the value retrived from the cache has a zero modseq then I can't use it
                                        //  I can update the cache with values I get though


                                    if (mSelectedMailboxCache.mPersistentCache.TryGetModSeqFlags(mMessageUID, out var lModSeqFlags, lContext))
                                    {
                                        mSeen = lModSeqFlags.Flags.Contains(kMessageFlag.Seen);

                                        if (mSelectedMailboxCache.mNoModSeq && lModSeqFlags.ModSeq != 0) mModSeqFlags = new cModSeqFlags(lModSeqFlags.Flags, 0);
                                        else mModSeqFlags = lModSeqFlags;

                                        mAttributes |= fMessageCacheAttributes.flags | fMessageCacheAttributes.modseq;
                                    }
                                }
                                else mSelectedMailboxCache.mPersistentCache.SetModSeqFlags(mMessageUID, mModSeqFlags, lContext);
                            }
                        }

                        // modseq and flags

                        rModSeqFlagPropertiesChanged = 0;

                        if (pFetch.Flags != null) // note that we ignore fetches with just a modseq
                        {
                            ulong lModSeq;

                            if (mSelectedMailboxCache.mNoModSeq || pFetch.ModSeq == null) lModSeq = 0;
                            else lModSeq = pFetch.ModSeq.Value;

                            if (mModSeqFlags == null)
                            {
                                mSeen = pFetch.Flags.Contains(kMessageFlag.Seen);
                                mModSeqFlags = new cModSeqFlags(pFetch.Flags, lModSeq);
                                if (mMessageUID != null) mSelectedMailboxCache.mPersistentCache.SetModSeqFlags(mMessageUID, mModSeqFlags, lContext);
                            }
                            else
                            {
                                foreach (var lFlag in mModSeqFlags.Flags.SymmetricDifference(pFetch.Flags)) rModSeqFlagPropertiesChanged |= fIMAPMessageProperties.flags | LMessageProperty(lFlag);

                                if (rModSeqFlagPropertiesChanged != 0) mSeen = pFetch.Flags.Contains(kMessageFlag.Seen);

                                if (mModSeqFlags.ModSeq != lModSeq) rModSeqFlagPropertiesChanged |= fIMAPMessageProperties.modseq;

                                if (rModSeqFlagPropertiesChanged != 0)
                                {
                                    mModSeqFlags = new cModSeqFlags(pFetch.Flags, lModSeq);
                                    if (mMessageUID != null) mSelectedMailboxCache.mPersistentCache.SetModSeqFlags(mMessageUID, mModSeqFlags, lContext);
                                }
                            }

                            mAttributes |= fMessageCacheAttributes.flags | fMessageCacheAttributes.modseq;
                        }

                        // the static attibutes

                        var lStaticAttributesToSet = ~mAttributes & pFetch.Attributes & fMessageCacheAttributes.staticattributes;

                        if ((lStaticAttributesToSet & fMessageCacheAttributes.envelope) != 0) mHeaderCacheItem.Envelope = pFetch.Envelope;
                        if ((lStaticAttributesToSet & fMessageCacheAttributes.received) != 0) mHeaderCacheItem.SetReceivedDateTime(pFetch.ReceivedDateTimeOffset.Value, pFetch.ReceivedDateTime.Value);
                        if ((lStaticAttributesToSet & fMessageCacheAttributes.size) != 0) mHeaderCacheItem.Size = pFetch.Size;
                        if ((lStaticAttributesToSet & fMessageCacheAttributes.body) != 0) mHeaderCacheItem.Body = pFetch.Body;
                        if ((lStaticAttributesToSet & fMessageCacheAttributes.bodystructure) != 0) mHeaderCacheItem.BodyStructure = pFetch.BodyStructure;
                        
                        // the uid was set above, but this is where we record the fact that the uid was sent to us
                        mAttributes |= lStaticAttributesToSet;

                        // the header and binary sizes

                        if (pFetch.HeaderFields != null) mHeaderCacheItem.AddHeaderFields(pFetch.HeaderFields);
                        if (pFetch.BinarySizes != null) mHeaderCacheItem.AddBinarySizes(pFetch.BinarySizes);

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

