using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandDetailsBuilder : IDisposable
            {
                // search
                private static readonly cCommandPart kCommandPartCharsetSpace = new cTextCommandPart("CHARSET ");
                private static readonly cCommandPart kCommandPartUSASCIISpace = new cTextCommandPart("US-ASCII ");
                private static readonly cCommandPart kCommandPartUTF8Space = new cTextCommandPart("UTF-8 ");
                private static readonly cCommandPart kCommandPartAll = new cTextCommandPart("ALL");
                private static readonly cCommandPart kCommandPartUIDSpace = new cTextCommandPart("UID ");
                private static readonly cCommandPart kCommandPartAnswered = new cTextCommandPart("ANSWERED");
                private static readonly cCommandPart kCommandPartFlagged = new cTextCommandPart("FLAGGED");
                private static readonly cCommandPart kCommandPartDeleted = new cTextCommandPart("DELETED");
                private static readonly cCommandPart kCommandPartSeen = new cTextCommandPart("SEEN");
                private static readonly cCommandPart kCommandPartDraft = new cTextCommandPart("DRAFT");
                private static readonly cCommandPart kCommandPartRecent = new cTextCommandPart("RECENT");
                private static readonly cCommandPart kCommandPartUnanswered = new cTextCommandPart("UNANSWERED");
                private static readonly cCommandPart kCommandPartUnflagged = new cTextCommandPart("UNFLAGGED");
                private static readonly cCommandPart kCommandPartUndeleted = new cTextCommandPart("UNDELETED");
                private static readonly cCommandPart kCommandPartUnseen = new cTextCommandPart("UNSEEN");
                private static readonly cCommandPart kCommandPartUndraft = new cTextCommandPart("UNDRAFT");
                private static readonly cCommandPart kCommandPartOld = new cTextCommandPart("OLD");
                private static readonly cCommandPart kCommandPartKeywordSpace = new cTextCommandPart("KEYWORD ");
                private static readonly cCommandPart kCommandPartUnkeywordSpace = new cTextCommandPart("UNKEYWORD ");
                private static readonly cCommandPart kCommandPartBCCSpace = new cTextCommandPart("BCC ");
                private static readonly cCommandPart kCommandPartBodySpace = new cTextCommandPart("BODY ");
                private static readonly cCommandPart kCommandPartCCSpace = new cTextCommandPart("CC ");
                private static readonly cCommandPart kCommandPartFromSpace = new cTextCommandPart("FROM ");
                private static readonly cCommandPart kCommandPartSubjectSpace = new cTextCommandPart("SUBJECT ");
                private static readonly cCommandPart kCommandPartTextSpace = new cTextCommandPart("TEXT ");
                private static readonly cCommandPart kCommandPartToSpace = new cTextCommandPart("TO ");
                private static readonly cCommandPart kCommandPartBeforeSpace = new cTextCommandPart("BEFORE ");
                private static readonly cCommandPart kCommandPartOnSpace = new cTextCommandPart("ON ");
                private static readonly cCommandPart kCommandPartSinceSpace = new cTextCommandPart("SINCE ");
                private static readonly cCommandPart kCommandPartSentBeforeSpace = new cTextCommandPart("SENTBEFORE ");
                private static readonly cCommandPart kCommandPartSentOnSpace = new cTextCommandPart("SENTON ");
                private static readonly cCommandPart kCommandPartSentSinceSpace = new cTextCommandPart("SENTSINCE ");
                private static readonly cCommandPart kCommandPartHeaderSpace = new cTextCommandPart("HEADER ");
                private static readonly cCommandPart kCommandPartLargerSpace = new cTextCommandPart("LARGER ");
                private static readonly cCommandPart kCommandPartSmallerSpace = new cTextCommandPart("SMALLER ");
                private static readonly cCommandPart kCommandPartOrSpace = new cTextCommandPart("OR ");
                private static readonly cCommandPart kCommandPartNotSpace = new cTextCommandPart("NOT ");

                // sort
                private static readonly cCommandPart kCommandPartReverse = new cTextCommandPart("REVERSE");
                private static readonly cCommandPart kCommandPartArrival = new cTextCommandPart("ARRIVAL");
                private static readonly cCommandPart kCommandPartCC = new cTextCommandPart("CC");
                private static readonly cCommandPart kCommandPartDate = new cTextCommandPart("DATE");
                private static readonly cCommandPart kCommandPartFrom = new cTextCommandPart("FROM");
                private static readonly cCommandPart kCommandPartSize = new cTextCommandPart("SIZE");
                private static readonly cCommandPart kCommandPartSubject = new cTextCommandPart("SUBJECT");
                private static readonly cCommandPart kCommandPartTo = new cTextCommandPart("TO");
                private static readonly cCommandPart kCommandPartDisplayFrom = new cTextCommandPart("DISPLAYFROM");
                private static readonly cCommandPart kCommandPartDisplayTo = new cTextCommandPart("DISPLAYTO");

                // fetch
                private static readonly cCommandPart kCommandPartFlags = new cTextCommandPart("FLAGS");
                private static readonly cCommandPart kCommandPartEnvelope = new cTextCommandPart("ENVELOPE");
                private static readonly cCommandPart kCommandPartInternalDate = new cTextCommandPart("INTERNALDATE");
                private static readonly cCommandPart kCommandPartrfc822size = new cTextCommandPart("RFC822.SIZE");
                private static readonly cCommandPart kCommandPartBody = new cTextCommandPart("BODY");
                private static readonly cCommandPart kCommandPartBodyStructure = new cTextCommandPart("BODYSTRUCTURE");
                private static readonly cCommandPart kCommandPartUID = new cTextCommandPart("UID");
                private static readonly cCommandPart kCommandPartModSeq = new cTextCommandPart("MODSEQ");
                private static readonly cCommandPart kCommandPartBodyPeekLBracketHeaderFieldsSpace = new cTextCommandPart("BODY.PEEK[HEADER.FIELDS ");

                // fetch body
                private static readonly cCommandPart kCommandPartHeader = new cTextCommandPart("HEADER");
                private static readonly cCommandPart kCommandPartHeaderFieldsSpaceLParen = new cTextCommandPart("HEADER.FIELDS (");
                private static readonly cCommandPart kCommandPartHeaderFieldsNotSpaceLParen = new cTextCommandPart("HEADER.FIELDS.NOT (");
                private static readonly cCommandPart kCommandPartText = new cTextCommandPart("TEXT");
                private static readonly cCommandPart kCommandPartMime = new cTextCommandPart("MIME");
                private static readonly cCommandPart kCommandPartLessThan = new cTextCommandPart("<");
                private static readonly cCommandPart kCommandPartGreaterThan = new cTextCommandPart(">");

                // status
                private static readonly cCommandPart kCommandPartMessages = new cTextCommandPart("MESSAGES");
                private static readonly cCommandPart kCommandPartUIDNext = new cTextCommandPart("UIDNEXT");
                private static readonly cCommandPart kCommandPartUIDValidity = new cTextCommandPart("UIDVALIDITY");
                private static readonly cCommandPart kCommandPartHighestModSeq = new cTextCommandPart("HIGHESTMODSEQ");

                // store
                private static readonly cCommandPart kCommandPartPlusFlagsSpace = new cTextCommandPart("+FLAGS ");
                private static readonly cCommandPart kCommandPartMinusFlagsSpace = new cTextCommandPart("-FLAGS ");
                private static readonly cCommandPart kCommandPartFlagsSpace = new cTextCommandPart("FLAGS ");

                // members
                private bool mDisposed = false;
                public readonly cCommandTag Tag = new cCommandTag();
                private readonly cCommandPartsBuilder mParts = new cCommandPartsBuilder();
                private readonly cCommandDisposables mDisposables = new cCommandDisposables();
                private uint? mUIDValidity = null;
                private cCommandHook mHook = null;
                private bool mEmitted = false;

                public cCommandDetailsBuilder() { }

                public void Add(cCommandPart pPart) => mParts.Add(pPart);
                public void Add(IEnumerable<cCommandPart> pParts) => mParts.Add(pParts);
                public void Add(params cCommandPart[] pParts) => mParts.Add(pParts);

                public void BeginList(eListBracketing pBracketing, cCommandPart pListName = null) => mParts.BeginList(pBracketing, pListName);
                public void EndList() => mParts.EndList();

                public void Add(cMessageCacheItems pItems, bool pNoModSeq)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);

                    fMessageCacheAttributes lAttributes = pItems.Attributes;
                    if ((lAttributes & (fMessageCacheAttributes.flags | fMessageCacheAttributes.modseq)) != 0) lAttributes |= fMessageCacheAttributes.flags | fMessageCacheAttributes.modseq;
                    if (pNoModSeq) lAttributes = lAttributes & ~fMessageCacheAttributes.modseq;

                    mParts.BeginList(eListBracketing.ifmorethanone);

                    if ((lAttributes & fMessageCacheAttributes.flags) != 0) mParts.Add(kCommandPartFlags);
                    if ((lAttributes & fMessageCacheAttributes.envelope) != 0) mParts.Add(kCommandPartEnvelope);
                    if ((lAttributes & fMessageCacheAttributes.received) != 0) mParts.Add(kCommandPartInternalDate);
                    if ((lAttributes & fMessageCacheAttributes.size) != 0) mParts.Add(kCommandPartrfc822size);
                    if ((lAttributes & fMessageCacheAttributes.body) != 0) mParts.Add(kCommandPartBody);
                    if ((lAttributes & fMessageCacheAttributes.bodystructure) != 0) mParts.Add(kCommandPartBodyStructure);
                    if ((lAttributes & fMessageCacheAttributes.uid) != 0) mParts.Add(kCommandPartUID);
                    if ((lAttributes & fMessageCacheAttributes.modseq) != 0) mParts.Add(kCommandPartModSeq);

                    if (pItems.Names.Count > 0)
                    {
                        mParts.BeginList(eListBracketing.bracketed, kCommandPartBodyPeekLBracketHeaderFieldsSpace, cCommandPart.RBracket);
                        foreach (var lName in pItems.Names) mParts.Add(cCommandPartFactory.AsASCIIAString(lName));
                        mParts.EndList();
                    }

                    mParts.EndList();
                }

                public void AddStatusAttributes(fMailboxCacheDataItems pAttributes)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);

                    mParts.BeginList(eListBracketing.bracketed);
                    if ((pAttributes & fMailboxCacheDataItems.messagecount) != 0) mParts.Add(kCommandPartMessages);
                    if ((pAttributes & fMailboxCacheDataItems.recentcount) != 0) mParts.Add(kCommandPartRecent);
                    if ((pAttributes & fMailboxCacheDataItems.uidnext) != 0) mParts.Add(kCommandPartUIDNext);
                    if ((pAttributes & fMailboxCacheDataItems.uidvalidity) != 0) mParts.Add(kCommandPartUIDValidity);
                    if ((pAttributes & fMailboxCacheDataItems.unseencount) != 0) mParts.Add(kCommandPartUnseen);
                    if ((pAttributes & fMailboxCacheDataItems.highestmodseq) != 0) mParts.Add(kCommandPartHighestModSeq);
                    mParts.EndList();
                }

                public void Add(cFilter pFilter, cSelectedMailbox pSelectedMailbox, bool pCharsetMandatory, cCommandPartFactory pFactory)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);

                    if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
                    if (pSelectedMailbox == null) throw new ArgumentNullException(nameof(pSelectedMailbox));
                    if (pFactory == null) throw new ArgumentNullException(nameof(pFactory));

                    if (pFilter.UIDValidity != null)
                    {
                        if (pFilter.UIDValidity != pSelectedMailbox.MessageCache.UIDValidity) throw new cUIDValidityException();
                        AddUIDValidity(pFilter.UIDValidity.Value);
                    }

                    var lFilterParts = ZFilterParts(pFilter, pSelectedMailbox, eListBracketing.none, pFactory);

                    if (pFactory.UTF8Enabled)
                    {
                        // rfc 6855 explicitly disallows charset on search when utf8 is enabled
                        if (pCharsetMandatory) mParts.Add(kCommandPartUTF8Space); // but for sort and thread it is mandatory
                    }
                    else
                    {
                        bool lEncodedParts = false;

                        foreach (var lPart in lFilterParts)
                        {
                            if (lPart.Encoded)
                            {
                                lEncodedParts = true;
                                break;
                            }
                        }

                        if (lEncodedParts)
                        {
                            if (!pCharsetMandatory) mParts.Add(kCommandPartCharsetSpace);
                            mParts.Add(pFactory.CharsetName);
                            mParts.Add(cCommandPart.Space);
                        }
                        else if (pCharsetMandatory) mParts.Add(kCommandPartUSASCIISpace); // have to put something for sort and thread
                    }

                    mParts.Add(lFilterParts);
                }

                private static ReadOnlyCollection<cCommandPart> ZFilterParts(cFilter pFilter, cSelectedMailbox pSelectedMailbox, eListBracketing pBracketing, cCommandPartFactory pFactory)
                {
                    var lParts = new cCommandPartsBuilder();

                    if (ReferenceEquals(pFilter, cFilter.All))
                    {
                        lParts.Add(kCommandPartAll);
                        return lParts.Parts;
                    }

                    switch (pFilter)
                    {
                        case cFilterMSNRelativity lRelativity:

                            long lMSN;

                            if (lRelativity.MessageHandle == null)
                            {
                                if (lRelativity.End == eFilterEnd.first) lMSN = 1;
                                else lMSN = pSelectedMailbox.MessageCache.Count; // could be zero
                            }
                            else
                            {
                                lMSN = pSelectedMailbox.GetMSN(lRelativity.MessageHandle);

                                if (lMSN == 0)
                                {
                                    if (lRelativity.MessageHandle.Expunged) throw new cMessageExpungedException(lRelativity.MessageHandle);
                                    else throw new ArgumentOutOfRangeException(nameof(pFilter));
                                }
                            }

                            lMSN = lMSN + lRelativity.Offset;

                            switch (lRelativity.Relativity)
                            {
                                case eFilterHandleRelativity.less:

                                    if (lMSN < 2)
                                    {
                                        lParts.BeginList(pBracketing);
                                        lParts.Add(kCommandPartSeen);
                                        lParts.Add(kCommandPartUnseen);
                                        lParts.EndList();
                                    }
                                    else if (lMSN > uint.MaxValue) lParts.Add(kCommandPartAll);
                                    else lParts.Add(new cTextCommandPart(new cSequenceSet(1, (uint)lMSN - 1)));

                                    return lParts.Parts;

                                case eFilterHandleRelativity.lessequal:

                                    if (lMSN < 1)
                                    {
                                        lParts.BeginList(pBracketing);
                                        lParts.Add(kCommandPartSeen);
                                        lParts.Add(kCommandPartUnseen);
                                        lParts.EndList();
                                    }
                                    else if (lMSN >= uint.MaxValue) lParts.Add(kCommandPartAll);
                                    else lParts.Add(new cTextCommandPart(new cSequenceSet(1, (uint)lMSN)));

                                    return lParts.Parts;

                                case eFilterHandleRelativity.greaterequal:

                                    if (lMSN < 2) lParts.Add(kCommandPartAll);
                                    else if (lMSN > uint.MaxValue)
                                    {
                                        lParts.BeginList(pBracketing);
                                        lParts.Add(kCommandPartSeen);
                                        lParts.Add(kCommandPartUnseen);
                                        lParts.EndList();
                                    }
                                    else lParts.Add(new cTextCommandPart(new cSequenceSet((uint)lMSN, uint.MaxValue)));

                                    return lParts.Parts;

                                case eFilterHandleRelativity.greater:

                                    if (lMSN < 1) lParts.Add(kCommandPartAll);
                                    else if (lMSN >= uint.MaxValue)
                                    {
                                        lParts.BeginList(pBracketing);
                                        lParts.Add(kCommandPartSeen);
                                        lParts.Add(kCommandPartUnseen);
                                        lParts.EndList();
                                    }
                                    else lParts.Add(new cTextCommandPart(new cSequenceSet((uint)lMSN + 1, uint.MaxValue)));

                                    return lParts.Parts;

                                default:

                                    throw new cInternalErrorException(nameof(cCommandDetailsBuilder), nameof(ZFilterParts));
                            }

                        case cFilterUIDIn lUIDIn:

                            lParts.Add(kCommandPartUIDSpace, new cTextCommandPart(lUIDIn.SequenceSet));
                            return lParts.Parts;

                        case cFilterFlagsContain lFlagsContain:

                            lParts.BeginList(pBracketing);

                            foreach (var lFlag in lFlagsContain.Flags)
                            {
                                if (lFlag.Equals(kMessageFlag.Answered, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartAnswered);
                                else if (lFlag.Equals(kMessageFlag.Flagged, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartFlagged);
                                else if (lFlag.Equals(kMessageFlag.Deleted, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartDeleted);
                                else if (lFlag.Equals(kMessageFlag.Seen, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartSeen);
                                else if (lFlag.Equals(kMessageFlag.Draft, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartDraft);
                                else if (lFlag.Equals(kMessageFlag.Recent, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartRecent);
                                else lParts.Add(kCommandPartKeywordSpace, new cTextCommandPart(lFlag));
                            }

                            lParts.EndList();
                            return lParts.Parts;

                        case cFilterPartContains lPartContains:

                            switch (lPartContains.Part)
                            {
                                case eFilterPart.bcc:

                                    lParts.Add(kCommandPartBCCSpace);
                                    break;

                                case eFilterPart.body:

                                    lParts.Add(kCommandPartBodySpace);
                                    break;

                                case eFilterPart.cc:

                                    lParts.Add(kCommandPartCCSpace);
                                    break;

                                case eFilterPart.from:

                                    lParts.Add(kCommandPartFromSpace);
                                    break;

                                case eFilterPart.subject:

                                    lParts.Add(kCommandPartSubjectSpace);
                                    break;

                                case eFilterPart.text:

                                    lParts.Add(kCommandPartTextSpace);
                                    break;

                                case eFilterPart.to:

                                    lParts.Add(kCommandPartToSpace);
                                    break;

                                default:

                                    throw new ArgumentException("invalid part", nameof(pFilter));
                            }

                            lParts.Add(pFactory.AsAString(lPartContains.Contains));
                            return lParts.Parts;

                        case cFilterDateCompare lDateCompare:

                            if (lDateCompare.Date == eFilterDate.arrival)
                            {
                                if (lDateCompare.Compare == eFilterDateCompare.before) lParts.Add(kCommandPartBeforeSpace);
                                else if (lDateCompare.Compare == eFilterDateCompare.on) lParts.Add(kCommandPartOnSpace);
                                else lParts.Add(kCommandPartSinceSpace);
                            }
                            else
                            {
                                if (lDateCompare.Compare == eFilterDateCompare.before) lParts.Add(kCommandPartSentBeforeSpace);
                                else if (lDateCompare.Compare == eFilterDateCompare.on) lParts.Add(kCommandPartSentOnSpace);
                                else lParts.Add(kCommandPartSentSinceSpace);
                            }

                            lParts.Add(cCommandPartFactory.AsDate(lDateCompare.WithDate));
                            return lParts.Parts;

                        case cFilterHeaderFieldContains lHeaderFieldContains:

                            lParts.Add(kCommandPartHeaderSpace, cCommandPartFactory.AsASCIIAString(lHeaderFieldContains.HeaderField), cCommandPart.Space, pFactory.AsAString(lHeaderFieldContains.Contains));
                            return lParts.Parts;

                        case cFilterSizeCompare lSizeCompare:

                            if (lSizeCompare.Compare == eFilterSizeCompare.larger) lParts.Add(kCommandPartLargerSpace);
                            else lParts.Add(kCommandPartSmallerSpace);

                            lParts.Add(new cTextCommandPart(lSizeCompare.WithSize));
                            return lParts.Parts;

                        case cFilterAnd lAnd:

                            lParts.BeginList(pBracketing);
                            foreach (var lTerm in lAnd.Terms) lParts.Add(ZFilterParts(lTerm, pSelectedMailbox, eListBracketing.none, pFactory));
                            lParts.EndList();
                            return lParts.Parts;

                        case cFilterOr lOr:

                            lParts.Add(kCommandPartOrSpace);
                            lParts.Add(ZFilterParts(lOr.A, pSelectedMailbox, eListBracketing.ifmorethanone, pFactory));
                            lParts.Add(cCommandPart.Space);
                            lParts.Add(ZFilterParts(lOr.B, pSelectedMailbox, eListBracketing.ifmorethanone, pFactory));
                            return lParts.Parts;

                        case cFilterNot lNot:

                            if (lNot.Not is cFilterFlagsContain lFlagsUncontain)
                            {
                                lParts.BeginList(pBracketing);

                                foreach (var lFlag in lFlagsUncontain.Flags)
                                {
                                    if (lFlag.Equals(kMessageFlag.Answered, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUnanswered);
                                    else if (lFlag.Equals(kMessageFlag.Flagged, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUnflagged);
                                    else if (lFlag.Equals(kMessageFlag.Deleted, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUndeleted);
                                    else if (lFlag.Equals(kMessageFlag.Seen, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUnseen);
                                    else if (lFlag.Equals(kMessageFlag.Draft, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUndraft);
                                    else if (lFlag.Equals(kMessageFlag.Recent, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartOld);
                                    else lParts.Add(kCommandPartUnkeywordSpace, new cTextCommandPart(lFlag));
                                }

                                lParts.EndList();
                            }
                            else
                            {
                                lParts.Add(kCommandPartNotSpace);
                                lParts.Add(ZFilterParts(lNot.Not, pSelectedMailbox, eListBracketing.ifmorethanone, pFactory));
                            }

                            return lParts.Parts;

                        default:

                            throw new ArgumentException("invalid subtype", nameof(pFilter));
                    }
                }

                public void Add(cSort pSort)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);

                    if (pSort == null) throw new ArgumentNullException(nameof(pSort));

                    mParts.BeginList(eListBracketing.bracketed);

                    foreach (var lItem in pSort.Items)
                    {
                        if (lItem.Desc) mParts.Add(kCommandPartReverse);

                        switch (lItem.Item)
                        {
                            case eSortItem.received:

                                mParts.Add(kCommandPartArrival);
                                break;

                            case eSortItem.cc:

                                mParts.Add(kCommandPartCC);
                                break;

                            case eSortItem.sent:

                                mParts.Add(kCommandPartDate);
                                break;

                            case eSortItem.from:

                                mParts.Add(kCommandPartFrom);
                                break;

                            case eSortItem.size:

                                mParts.Add(kCommandPartSize);
                                break;

                            case eSortItem.subject:

                                mParts.Add(kCommandPartSubject);
                                break;

                            case eSortItem.to:

                                mParts.Add(kCommandPartTo);
                                break;

                            case eSortItem.displayfrom:

                                mParts.Add(kCommandPartDisplayFrom);
                                break;

                            case eSortItem.displayto:

                                mParts.Add(kCommandPartDisplayTo);
                                break;

                            default:

                                throw new ArgumentException("unknown item", nameof(pSort));
                        }
                    }

                    mParts.EndList();
                }

                public void Add(cSection pSection, uint pOrigin, uint pLength)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);

                    if (pSection.Part != null)
                    {
                        mParts.Add(cCommandPartFactory.AsAtom(pSection.Part));
                        if (pSection.TextPart != eSectionTextPart.all) mParts.Add(cCommandPart.Dot);
                    }

                    switch (pSection.TextPart)
                    {
                        case eSectionTextPart.all:

                            break;

                        case eSectionTextPart.header:

                            mParts.Add(kCommandPartHeader);
                            break;

                        case eSectionTextPart.headerfields:

                            mParts.Add(kCommandPartHeaderFieldsSpaceLParen);
                            LAdd(pSection.Names);
                            mParts.Add(cCommandPart.RParen);
                            break;

                        case eSectionTextPart.headerfieldsnot:

                            mParts.Add(kCommandPartHeaderFieldsNotSpaceLParen);
                            LAdd(pSection.Names);
                            mParts.Add(cCommandPart.RParen);
                            break;

                        case eSectionTextPart.text:

                            mParts.Add(kCommandPartText);
                            break;

                        case eSectionTextPart.mime:

                            mParts.Add(kCommandPartMime);
                            break;

                        default:

                            throw new cInternalErrorException(nameof(cCommandDetailsBuilder), nameof(Add));
                    }

                    mParts.Add(cCommandPart.RBracket, kCommandPartLessThan, new cTextCommandPart(pOrigin), cCommandPart.Dot, new cTextCommandPart(pLength), kCommandPartGreaterThan);

                    void LAdd(cHeaderFieldNames pNames)
                    {
                        mParts.BeginList(eListBracketing.none);
                        foreach (var lName in pNames) mParts.Add(cCommandPartFactory.AsASCIIAString(lName));
                        mParts.EndList();
                    }
                }

                public void Add(eStoreOperation pOperation, cStorableFlags pFlags)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);

                    switch (pOperation)
                    {
                        case eStoreOperation.add:

                            mParts.Add(kCommandPartPlusFlagsSpace);
                            break;

                        case eStoreOperation.remove:

                            mParts.Add(kCommandPartMinusFlagsSpace);
                            break;

                        case eStoreOperation.replace:

                            mParts.Add(kCommandPartFlagsSpace);
                            break;

                        default:

                            throw new ArgumentOutOfRangeException(nameof(pOperation));
                    }

                    mParts.BeginList(eListBracketing.bracketed);
                    foreach (var lFlag in pFlags) mParts.Add(new cTextCommandPart(lFlag));
                    mParts.EndList();
                }

                public void Add(IDisposable pDisposable)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);
                    mDisposables.Add(pDisposable);
                }

                public void Add(cExclusiveAccess.cToken pToken)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);
                    mDisposables.Add(pToken);
                }

                public void Add(cExclusiveAccess.cBlock pBlock)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);
                    mDisposables.Add(pBlock);
                }

                public void Add(cSASLAuthentication pSASLAuthentication)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);
                    mDisposables.Add(pSASLAuthentication);
                }

                public void AddUIDValidity(uint pUIDValidity)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);
                    if (mUIDValidity == null) mUIDValidity = pUIDValidity;
                    if (pUIDValidity != mUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
                }

                public void Add(cCommandHook pHook)
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);
                    if (mHook != null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadySet);
                    mHook = pHook ?? throw new ArgumentNullException(nameof(pHook));
                }

                public sCommandDetails EmitCommandDetails()
                {
                    if (mEmitted) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEmitted);
                    mEmitted = true;
                    return new sCommandDetails(Tag, mParts.Parts, mDisposables, mUIDValidity, mHook ?? cCommandHook.DoNothing);
                }

                public void Dispose()
                {
                    Dispose(true);
                    GC.SuppressFinalize(this);
                }

                protected virtual void Dispose(bool pDisposing)
                {
                    if (mDisposed) return;

                    if (pDisposing)
                    {
                        if (mEmitted) return;
                        mDisposables.Dispose();

                    }

                    mDisposed = true;
                }

                public override string ToString() => $"{nameof(cCommandDetailsBuilder)}({Tag},{mParts},{mUIDValidity},{mEmitted})";

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandDetailsBuilder), nameof(_Tests));

                    cIMAPCallbackSynchroniser lES = new cIMAPCallbackSynchroniser();
                    cStrings lStrings = new cStrings(new List<string>());
                    cMailboxCache lMC = new cMailboxCache(lES, LExpunged, LUIDValidityDiscovered, 0, cCommandPartFactory.Validation, new cIMAPCapabilities(lStrings, lStrings, 0), new cAccountId("localhost", "fred"), (eIMAPConnectionState pCS, cTrace.cContext pC) => { });
                    cSelectedMailbox lSelectedMailbox = new cSelectedMailbox(lES, LExpunged, new cMailboxCacheItem(lES, LUIDValidityDiscovered, lMC, "fred"), false, true, 10, 5, 1111, 1, 0, cTrace.cContext.None);
                    //cSelectedMailbox lSelectedMailbox2 = new cSelectedMailbox(lES, new cMailboxCacheItem(lES, lMC, "fred"), false, true, 10, 5, 1111, 2222, 0, cTrace.cContext.Null);


                    if (LMessageFilterCommandPartsTestsString(cFilter.UID < new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1:999") throw new cTestsException("ZMessageFilterCommandPartsTests UID.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID <= new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1:1000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID == new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID >= new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1000:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests UID.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID > new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1001:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests UID.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.UID > new cUID(1, 1000) & cFilter.UID < new cUID(1, 2000), lSelectedMailbox, false, false, null) != "UID 1001:4294967295 UID 1:1999") throw new cTestsException("ZMessageFilterCommandPartsTests UID.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, false, false, null) != "NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.7", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, false, null) != "US-ASCII NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.8", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, true, null) != "UTF-8 NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.9", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, false, Encoding.UTF32) != "US-ASCII NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.10", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, true, Encoding.UTF32) != "UTF-8 NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.11", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.Answered, lSelectedMailbox, false, false, null) != "ANSWERED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Answered & cFilter.Flagged, lSelectedMailbox, false, false, null) != "ANSWERED FLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.2", lContext);

                    cFetchableFlagList lFFlags = new cFetchableFlagList();

                    lFFlags.Add(kMessageFlag.Draft);
                    lFFlags.Add(kMessageFlag.Recent);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Answered & cFilter.Flagged | cFilter.FlagsContain(lFFlags), lSelectedMailbox, false, false, null) != "OR (ANSWERED FLAGGED) (DRAFT RECENT)") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Answered & cFilter.Flagged & cFilter.FlagsContain(lFFlags), lSelectedMailbox, false, false, null) != "ANSWERED DRAFT FLAGGED RECENT") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.4", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Answered & cFilter.Flagged & cFilter.Forwarded, lSelectedMailbox, false, false, null) != "KEYWORD $Forwarded ANSWERED FLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(!(cFilter.Answered & cFilter.Flagged & cFilter.Forwarded), lSelectedMailbox, false, false, null) != "UNKEYWORD $Forwarded UNANSWERED UNFLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.6", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred"), lSelectedMailbox, false, false, null) != "KEYWORD fred") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred") | cFilter.FlagsContain("angus"), lSelectedMailbox, false, false, null) != "OR KEYWORD fred KEYWORD angus") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred") & cFilter.FlagsContain("angus"), lSelectedMailbox, false, false, null) != "KEYWORD angus KEYWORD fred") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.3", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.BCC.Contains("@bacome.work"), lSelectedMailbox, false, false, null) != "BCC @bacome.work") throw new cTestsException("ZMessageFilterCommandPartsTests BCC.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("imap client"), lSelectedMailbox, false, false, null) != "SUBJECT \"imap client\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Body.Contains("imap"), lSelectedMailbox, false, false, null) != "BODY imap") throw new cTestsException("ZMessageFilterCommandPartsTests Body.1", lContext);

                    if (LMessageFilterCommandPartsTestsString(!cFilter.To.Contains("bacome") & !cFilter.FlagsContain(kMessageFlag.Recent), lSelectedMailbox, false, false, null) != "NOT TO bacome OLD") throw new cTestsException("ZMessageFilterCommandPartsTests And.1", lContext);

                    bool lFailed;

                    lFailed = false;
                    try { if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, false, null) != "SUBJECT \"fr?d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.2", lContext); }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests Subject.2 - didn't fail as expected", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, true, Encoding.UTF32) != "SUBJECT \"fr`E2`82`ACd\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, false, Encoding.UTF8) != "CHARSET utf-8 SUBJECT {6}fr`E2`82`ACd") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, false, Encoding.UTF7) != "CHARSET utf-7 SUBJECT fr+IKw-d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.5", lContext);



                    lFailed = false;
                    try { if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, true, false, null) != "US-ASCII SUBJECT \"fr?d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6", lContext); }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6 - didn't fail as expected", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, true, true, Encoding.UTF32) != "UTF-8 SUBJECT \"fr`E2`82`ACd\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, true, false, Encoding.UTF8) != "utf-8 SUBJECT {6}fr`E2`82`ACd") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.7", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, true, false, Encoding.UTF7) != "utf-7 SUBJECT fr+IKw-d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.8", lContext);

                    DateTime lDateTime = new DateTime(1968, 4, 4, 12, 34, 56);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent < lDateTime, lSelectedMailbox, false, false, null) != "SENTBEFORE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent <= lDateTime, lSelectedMailbox, false, false, null) != "SENTBEFORE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent == lDateTime, lSelectedMailbox, false, false, null) != "SENTON 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent >= lDateTime, lSelectedMailbox, false, false, null) != "SENTSINCE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent > lDateTime, lSelectedMailbox, false, false, null) != "SENTSINCE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Received < lDateTime, lSelectedMailbox, false, false, null) != "BEFORE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received <= lDateTime, lSelectedMailbox, false, false, null) != "BEFORE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.7", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received == lDateTime, lSelectedMailbox, false, false, null) != "ON 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.8", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received != lDateTime, lSelectedMailbox, false, false, null) != "NOT ON 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.9", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received >= lDateTime, lSelectedMailbox, false, false, null) != "SINCE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.10", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received > lDateTime, lSelectedMailbox, false, false, null) != "SINCE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.11", lContext);

                    lDateTime = new DateTime(1968, 4, 14, 12, 34, 56);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent < lDateTime, lSelectedMailbox, false, false, null) != "SENTBEFORE 14-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.12", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.HasHeaderField("references"), lSelectedMailbox, false, false, null) != "HEADER references \"\"") throw new cTestsException("ZMessageFilterCommandPartsTests header.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.HeaderFieldContains("references", "@bacome"), lSelectedMailbox, false, false, null) != "HEADER references @bacome") throw new cTestsException("ZMessageFilterCommandPartsTests header.2", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Size < 1000, lSelectedMailbox, false, false, null) != "SMALLER 1000") throw new cTestsException("ZMessageFilterCommandPartsTests Size.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Size > 1000, lSelectedMailbox, false, false, null) != "LARGER 1000") throw new cTestsException("ZMessageFilterCommandPartsTests Size.2", lContext);

                    lFailed = false;
                    try { var lMF = cFilter.UID > new cUID(1, 1000) & cFilter.UID < new cUID(2, 2000); }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests UIDValidity.1 - didn't fail as expected", lContext);

                    var lMF1 = cFilter.UID > new cUID(1, 1000) | cFilter.Body.Contains("imap");
                    var lMF2 = cFilter.Body.Contains("imap") | !(cFilter.UID > new cUID(2, 1000));

                    lFailed = false;
                    try { var lMF = lMF1 & lMF2; }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests UIDValidity.2 - didn't fail as expected", lContext);

                    var lClient = new cIMAPClient();
                    var lMH1 = new cIMAPMessage(lClient, lSelectedMailbox.MessageCache[0]);
                    var lMH2 = new cIMAPMessage(lClient, lSelectedMailbox.MessageCache[1]);

                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < lMH1, lSelectedMailbox, false, false, null) != "SEEN UNSEEN") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < lMH2, lSelectedMailbox, false, false, null) != "1:1") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= lMH1, lSelectedMailbox, false, false, null) != "1:1") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= lMH2, lSelectedMailbox, false, false, null) != "1:2") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= lMH2, lSelectedMailbox, false, false, null) != "2:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.5", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > lMH2, lSelectedMailbox, false, false, null) != "3:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.6", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.First.MSNOffset(-5), lSelectedMailbox, false, false, null) != "ALL") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.7", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "2:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.8", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "ALL") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.9", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.First.MSNOffset(2), lSelectedMailbox, false, false, null) != "3:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.10", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.First.MSNOffset(-5), lSelectedMailbox, false, false, null) != "SEEN UNSEEN") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.11", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "SEEN UNSEEN") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.12", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "1:1") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.13", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.First.MSNOffset(2), lSelectedMailbox, false, false, null) != "1:3") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.14", lContext);



                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.Last.MSNOffset(-5), lSelectedMailbox, false, false, null) != "6:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.15", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "11:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.16", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "10:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.17", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.Last.MSNOffset(2), lSelectedMailbox, false, false, null) != "12:4294967295") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.18", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.Last.MSNOffset(-5), lSelectedMailbox, false, false, null) != "1:4") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.19", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "1:9") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.20", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "1:10") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.21", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.Last.MSNOffset(2), lSelectedMailbox, false, false, null) != "1:12") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.22", lContext);



                    ;?; // tests for coalescing sequence sets ...



                    string LMessageFilterCommandPartsTestsString(cFilter pFilter, cSelectedMailbox pSelectedMailbox, bool pCharsetMandatory, bool pUTF8Enabled, Encoding pEncoding)
                    {
                        StringBuilder lBuilder = new StringBuilder();
                        var lCommandBuilder = new cCommandDetailsBuilder();
                        cCommandPartFactory lFactory = new cCommandPartFactory(pUTF8Enabled, pEncoding);
                        lCommandBuilder.Add(pFilter, pSelectedMailbox, pCharsetMandatory, lFactory);
                        var lDetails = lCommandBuilder.EmitCommandDetails();

                        foreach (var lPart in lDetails.Parts)
                        {
                            switch (lPart)
                            {
                                case cTextCommandPart lText:

                                    lBuilder.Append(cTools.BytesToLoggableString(lText.Bytes));
                                    break;

                                case cLiteralCommandPart lLiteral:

                                    lBuilder.Append("{");
                                    lBuilder.Append(lLiteral.Bytes.Count);
                                    lBuilder.Append("}");

                                    lBuilder.Append(cTools.BytesToLoggableString(lLiteral.Bytes));

                                    break;
                            }
                        }

                        return lBuilder.ToString();
                    }

                    void LExpunged(iMessageHandle pMessageHandle, cTrace.cContext p2) { };
                    void LUIDValidityDiscovered(iMailboxHandle pMailboxHandle, cTrace.cContext p2) { };
                }
            }

            private class cAppendCommandDetailsBuilder : cCommandDetailsBuilder
            {
                public readonly bool UTF8;
                public readonly bool Binary;
                public readonly cBatchSizerConfiguration StreamReadConfiguration;
                public readonly Action<int> Increment;

                public cAppendCommandDetailsBuilder(bool pUTF8, bool pBinary, cBatchSizerConfiguration pStreamReadConfiguration, Action<int> pIncrement)
                {
                    UTF8 = pUTF8;
                    Binary = pBinary;
                    StreamReadConfiguration = pStreamReadConfiguration ?? throw new ArgumentNullException(nameof(pStreamReadConfiguration));
                    Increment = pIncrement;
                }

                // note that rfc 4469 has not been updated to allow binary literals in the text-literal
                //  in particular, on Dovecot use of binary literals in a catenate text-literal causes issues in some areas
                //   (notably the ;boundary="" parameter of the content-type gets mangled when it is on a separate line)
                //   (but it seems to work ok otherwise)
                //  so this property can not be used for generating command parts within a catenate clause
                //
                public bool AppendDataBinary => UTF8 || Binary;
            }
        }
    }
}