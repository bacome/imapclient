using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private sealed class cCommandDetailsBuilder : IDisposable
            {
                // search
                private static readonly cCommandPart kCommandPartCharsetSpace = new cCommandPart("CHARSET ");
                private static readonly cCommandPart kCommandPartUSASCIISpace = new cCommandPart("US-ASCII ");
                private static readonly cCommandPart kCommandPartUTF8Space = new cCommandPart("UTF-8 ");
                private static readonly cCommandPart kCommandPartAll = new cCommandPart("ALL");
                private static readonly cCommandPart kCommandPartUIDSpace = new cCommandPart("UID ");
                private static readonly cCommandPart kCommandPartAnswered = new cCommandPart("ANSWERED");
                private static readonly cCommandPart kCommandPartFlagged = new cCommandPart("FLAGGED");
                private static readonly cCommandPart kCommandPartDeleted = new cCommandPart("DELETED");
                private static readonly cCommandPart kCommandPartSeen = new cCommandPart("SEEN");
                private static readonly cCommandPart kCommandPartDraft = new cCommandPart("DRAFT");
                private static readonly cCommandPart kCommandPartRecent = new cCommandPart("RECENT");
                private static readonly cCommandPart kCommandPartUnanswered = new cCommandPart("UNANSWERED");
                private static readonly cCommandPart kCommandPartUnflagged = new cCommandPart("UNFLAGGED");
                private static readonly cCommandPart kCommandPartUndeleted = new cCommandPart("UNDELETED");
                private static readonly cCommandPart kCommandPartUnseen = new cCommandPart("UNSEEN");
                private static readonly cCommandPart kCommandPartUndraft = new cCommandPart("UNDRAFT");
                private static readonly cCommandPart kCommandPartOld = new cCommandPart("OLD");
                private static readonly cCommandPart kCommandPartKeywordSpace = new cCommandPart("KEYWORD ");
                private static readonly cCommandPart kCommandPartUnkeywordSpace = new cCommandPart("UNKEYWORD ");
                private static readonly cCommandPart kCommandPartBCCSpace = new cCommandPart("BCC ");
                private static readonly cCommandPart kCommandPartBodySpace = new cCommandPart("BODY ");
                private static readonly cCommandPart kCommandPartCCSpace = new cCommandPart("CC ");
                private static readonly cCommandPart kCommandPartFromSpace = new cCommandPart("FROM ");
                private static readonly cCommandPart kCommandPartSubjectSpace = new cCommandPart("SUBJECT ");
                private static readonly cCommandPart kCommandPartTextSpace = new cCommandPart("TEXT ");
                private static readonly cCommandPart kCommandPartToSpace = new cCommandPart("TO ");
                private static readonly cCommandPart kCommandPartBeforeSpace = new cCommandPart("BEFORE ");
                private static readonly cCommandPart kCommandPartOnSpace = new cCommandPart("ON ");
                private static readonly cCommandPart kCommandPartSinceSpace = new cCommandPart("SINCE ");
                private static readonly cCommandPart kCommandPartSentBeforeSpace = new cCommandPart("SENTBEFORE ");
                private static readonly cCommandPart kCommandPartSentOnSpace = new cCommandPart("SENTON ");
                private static readonly cCommandPart kCommandPartSentSinceSpace = new cCommandPart("SENTSINCE ");
                private static readonly cCommandPart kCommandPartHeaderSpace = new cCommandPart("HEADER ");
                private static readonly cCommandPart kCommandPartLargerSpace = new cCommandPart("LARGER ");
                private static readonly cCommandPart kCommandPartSmallerSpace = new cCommandPart("SMALLER ");
                private static readonly cCommandPart kCommandPartOrSpace = new cCommandPart("OR ");
                private static readonly cCommandPart kCommandPartNotSpace = new cCommandPart("NOT ");

                // sort
                private static readonly cCommandPart kCommandPartReverse = new cCommandPart("REVERSE");
                private static readonly cCommandPart kCommandPartArrival = new cCommandPart("ARRIVAL");
                private static readonly cCommandPart kCommandPartCC = new cCommandPart("CC");
                private static readonly cCommandPart kCommandPartDate = new cCommandPart("DATE");
                private static readonly cCommandPart kCommandPartFrom = new cCommandPart("FROM");
                private static readonly cCommandPart kCommandPartSize = new cCommandPart("SIZE");
                private static readonly cCommandPart kCommandPartSubject = new cCommandPart("SUBJECT");
                private static readonly cCommandPart kCommandPartTo = new cCommandPart("TO");
                private static readonly cCommandPart kCommandPartDisplayFrom = new cCommandPart("DISPLAYFROM");
                private static readonly cCommandPart kCommandPartDisplayTo = new cCommandPart("DISPLAYTO");

                // fetch
                private static readonly cCommandPart kCommandPartFlags = new cCommandPart("FLAGS");
                private static readonly cCommandPart kCommandPartEnvelope = new cCommandPart("ENVELOPE");
                private static readonly cCommandPart kCommandPartInternalDate = new cCommandPart("INTERNALDATE");
                private static readonly cCommandPart kCommandPartrfc822size = new cCommandPart("RFC822.SIZE");
                private static readonly cCommandPart kCommandPartBody = new cCommandPart("BODY");
                private static readonly cCommandPart kCommandPartBodyStructure = new cCommandPart("BODYSTRUCTURE");
                private static readonly cCommandPart kCommandPartUID = new cCommandPart("UID");
                private static readonly cCommandPart kCommandPartModSeq = new cCommandPart("MODSEQ");
                private static readonly cCommandPart kCommandPartBodyPeekLBracketHeaderFieldsSpace = new cCommandPart("BODY.PEEK[HEADER.FIELDS ");

                // fetch body
                private static readonly cCommandPart kCommandPartHeader = new cCommandPart("HEADER");
                private static readonly cCommandPart kCommandPartHeaderFieldsSpaceLParen = new cCommandPart("HEADER.FIELDS (");
                private static readonly cCommandPart kCommandPartHeaderFieldsNotSpaceLParen = new cCommandPart("HEADER.FIELDS.NOT (");
                private static readonly cCommandPart kCommandPartText = new cCommandPart("TEXT");
                private static readonly cCommandPart kCommandPartMime = new cCommandPart("MIME");
                private static readonly cCommandPart kCommandPartLessThan = new cCommandPart("<");
                private static readonly cCommandPart kCommandPartGreaterThan = new cCommandPart(">");

                // status
                private static readonly cCommandPart kCommandPartMessages = new cCommandPart("MESSAGES");
                private static readonly cCommandPart kCommandPartUIDNext = new cCommandPart("UIDNEXT");
                private static readonly cCommandPart kCommandPartUIDValidity = new cCommandPart("UIDVALIDITY");
                private static readonly cCommandPart kCommandPartHighestModSeq = new cCommandPart("HIGHESTMODSEQ");

                // members
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

                public void Add(cCacheItems pItems, bool pNoModSeq)
                {
                    if (mEmitted) throw new InvalidOperationException();

                    fCacheAttributes lAttributes = pItems.Attributes;
                    if ((lAttributes & (fCacheAttributes.flags | fCacheAttributes.modseq)) != 0) lAttributes |= fCacheAttributes.flags | fCacheAttributes.modseq;
                    if (pNoModSeq) lAttributes = lAttributes & ~fCacheAttributes.modseq;

                    mParts.BeginList(eListBracketing.ifmorethanone);

                    if ((lAttributes & fCacheAttributes.flags) != 0) mParts.Add(kCommandPartFlags);
                    if ((lAttributes & fCacheAttributes.envelope) != 0) mParts.Add(kCommandPartEnvelope);
                    if ((lAttributes & fCacheAttributes.received) != 0) mParts.Add(kCommandPartInternalDate);
                    if ((lAttributes & fCacheAttributes.size) != 0) mParts.Add(kCommandPartrfc822size);
                    if ((lAttributes & fCacheAttributes.body) != 0) mParts.Add(kCommandPartBody);
                    if ((lAttributes & fCacheAttributes.bodystructure) != 0) mParts.Add(kCommandPartBodyStructure);
                    if ((lAttributes & fCacheAttributes.uid) != 0) mParts.Add(kCommandPartUID);
                    if ((lAttributes & fCacheAttributes.modseq) != 0) mParts.Add(kCommandPartModSeq);

                    if (pItems.Names.Count > 0)
                    {
                        mParts.BeginList(eListBracketing.bracketed, kCommandPartBodyPeekLBracketHeaderFieldsSpace, cCommandPart.RBracket);
                        foreach (var lName in pItems.Names) mParts.Add(cCommandPartFactory.AsASCIIAString(lName));
                        mParts.EndList();
                    }

                    mParts.EndList();
                }

                public void AddStatusAttributes(fMailboxCacheData pAttributes)
                {
                    if (mEmitted) throw new InvalidOperationException();

                    mParts.BeginList(eListBracketing.bracketed);
                    if ((pAttributes & fMailboxCacheData.messagecount) != 0) mParts.Add(kCommandPartMessages);
                    if ((pAttributes & fMailboxCacheData.recentcount) != 0) mParts.Add(kCommandPartRecent);
                    if ((pAttributes & fMailboxCacheData.uidnext) != 0) mParts.Add(kCommandPartUIDNext);
                    if ((pAttributes & fMailboxCacheData.uidvalidity) != 0) mParts.Add(kCommandPartUIDValidity);
                    if ((pAttributes & fMailboxCacheData.unseencount) != 0) mParts.Add(kCommandPartUnseen);
                    if ((pAttributes & fMailboxCacheData.highestmodseq) != 0) mParts.Add(kCommandPartHighestModSeq);
                    mParts.EndList();
                }

                public void Add(cFilter pFilter, cSelectedMailbox pSelectedMailbox, bool pCharsetMandatory, cCommandPartFactory pFactory)
                {
                    if (mEmitted) throw new InvalidOperationException();

                    if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
                    if (pSelectedMailbox == null) throw new ArgumentNullException(nameof(pSelectedMailbox));
                    if (pFactory == null) throw new ArgumentNullException(nameof(pFactory));

                    if (pFilter.UIDValidity != null)
                    {
                        if (pFilter.UIDValidity != pSelectedMailbox.Cache.UIDValidity) throw new cUIDValidityChangedException();
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

                            if (lRelativity.Handle == null)
                            {
                                if (lRelativity.End == eFilterEnd.first) lMSN = 1;
                                else lMSN = pSelectedMailbox.Cache.Count; // could be zero
                            }
                            else
                            {
                                lMSN = pSelectedMailbox.GetMSN(lRelativity.Handle);
                                if (lMSN == 0) throw new cFilterMSNException(lRelativity.Handle); // may have been expunged
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
                                    else lParts.Add(new cCommandPart(new cSequenceSet(1, (uint)lMSN - 1)));

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
                                    else lParts.Add(new cCommandPart(new cSequenceSet(1, (uint)lMSN)));

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
                                    else lParts.Add(new cCommandPart(new cSequenceSet((uint)lMSN, uint.MaxValue)));

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
                                    else lParts.Add(new cCommandPart(new cSequenceSet((uint)lMSN + 1, uint.MaxValue)));

                                    return lParts.Parts;

                                default:

                                    throw new cInternalErrorException();
                            }

                        case cFilterUIDIn lUIDIn:

                            lParts.Add(kCommandPartUIDSpace, new cCommandPart(lUIDIn.SequenceSet));
                            return lParts.Parts;

                        case cFilterFlagsContain lFlagsContain:

                            lParts.BeginList(pBracketing);

                            foreach (var lFlag in lFlagsContain.Flags)
                            {
                                if (lFlag.Equals(kFlagName.Answered, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartAnswered);
                                else if (lFlag.Equals(kFlagName.Flagged, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartFlagged);
                                else if (lFlag.Equals(kFlagName.Deleted, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartDeleted);
                                else if (lFlag.Equals(kFlagName.Seen, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartSeen);
                                else if (lFlag.Equals(kFlagName.Draft, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartDraft);
                                else if (lFlag.Equals(kFlagName.Recent, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartRecent);
                                else lParts.Add(kCommandPartKeywordSpace, new cCommandPart(lFlag));
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

                            lParts.Add(new cCommandPart(lSizeCompare.WithSize));
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
                                    if (lFlag.Equals(kFlagName.Answered, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUnanswered);
                                    else if (lFlag.Equals(kFlagName.Flagged, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUnflagged);
                                    else if (lFlag.Equals(kFlagName.Deleted, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUndeleted);
                                    else if (lFlag.Equals(kFlagName.Seen, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUnseen);
                                    else if (lFlag.Equals(kFlagName.Draft, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartUndraft);
                                    else if (lFlag.Equals(kFlagName.Recent, StringComparison.InvariantCultureIgnoreCase)) lParts.Add(kCommandPartOld);
                                    else lParts.Add(kCommandPartUnkeywordSpace, new cCommandPart(lFlag));
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
                    if (mEmitted) throw new InvalidOperationException();

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
                    if (mEmitted) throw new InvalidOperationException();

                    if (pSection.Part != null)
                    {
                        mParts.Add(cCommandPartFactory.AsAtom(pSection.Part));
                        if (pSection.TextPart != eSectionPart.all) mParts.Add(cCommandPart.Dot);
                    }

                    switch (pSection.TextPart)
                    {
                        case eSectionPart.all:

                            break;

                        case eSectionPart.header:

                            mParts.Add(kCommandPartHeader);
                            break;

                        case eSectionPart.headerfields:

                            mParts.Add(kCommandPartHeaderFieldsSpaceLParen);
                            LAdd(pSection.Names);
                            mParts.Add(cCommandPart.RParen);
                            break;

                        case eSectionPart.headerfieldsnot:

                            mParts.Add(kCommandPartHeaderFieldsNotSpaceLParen);
                            LAdd(pSection.Names);
                            mParts.Add(cCommandPart.RParen);
                            break;

                        case eSectionPart.text:

                            mParts.Add(kCommandPartText);
                            break;

                        case eSectionPart.mime:

                            mParts.Add(kCommandPartMime);
                            break;

                        default:

                            throw new cInternalErrorException();
                    }

                    mParts.Add(cCommandPart.RBracket, kCommandPartLessThan, new cCommandPart(pOrigin), cCommandPart.Dot, new cCommandPart(pLength), kCommandPartGreaterThan);

                    void LAdd(cHeaderFieldNames pNames)
                    {
                        mParts.BeginList(eListBracketing.none);
                        foreach (var lName in pNames) mParts.Add(cCommandPartFactory.AsASCIIAString(lName));
                        mParts.EndList();
                    }
                }

                public void Add(cExclusiveAccess.cToken pToken)
                {
                    if (mEmitted) throw new InvalidOperationException();
                    mDisposables.Add(pToken);
                }

                public void Add(cExclusiveAccess.cBlock pBlock)
                {
                    if (mEmitted) throw new InvalidOperationException();
                    mDisposables.Add(pBlock);
                }

                public void Add(cSASLAuthentication pSASLAuthentication)
                {
                    if (mEmitted) throw new InvalidOperationException();
                    mDisposables.Add(pSASLAuthentication);
                }

                public void AddUIDValidity(uint pUIDValidity)
                {
                    if (mEmitted) throw new InvalidOperationException();
                    if (mUIDValidity == null) mUIDValidity = pUIDValidity;
                    if (pUIDValidity != mUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
                }

                public void Add(cCommandHook pHook)
                {
                    if (mEmitted) throw new InvalidOperationException();
                    if (mHook != null) throw new InvalidOperationException();
                    mHook = pHook ?? throw new ArgumentNullException(nameof(pHook));
                }

                public sCommandDetails EmitCommandDetails()
                {
                    if (mEmitted) throw new InvalidOperationException();
                    mEmitted = true;
                    return new sCommandDetails(Tag, mParts.Parts, mDisposables, mUIDValidity, mHook ?? cCommandHook.DoNothing);
                }

                public void Dispose()
                {
                    if (mEmitted) return;
                    mDisposables.Dispose();
                }

                public override string ToString() => $"{nameof(cCommandDetailsBuilder)}({Tag},{mParts},{mUIDValidity},{mEmitted})";

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandDetailsBuilder), nameof(_Tests));

                    cCallbackSynchroniser lES = new cCallbackSynchroniser(new object(), cTrace.cContext.Null);
                    cStrings lStrings = new cStrings(new List<string>());
                    cMailboxCache lMC = new cMailboxCache(lES, 0, cCommandPartFactory.Validation, new cCapabilities(lStrings, lStrings, 0), (eConnectionState pCS, cTrace.cContext pC) => { });
                    cSelectedMailbox lSelectedMailbox = new cSelectedMailbox(lES, new cMailboxCacheItem(lES, lMC, "fred"), false, true, 10, 5, 1111, 1, 0, cTrace.cContext.Null);
                    //cSelectedMailbox lSelectedMailbox2 = new cSelectedMailbox(lES, new cMailboxCacheItem(lES, lMC, "fred"), false, true, 10, 5, 1111, 2222, 0, cTrace.cContext.Null);


                    if (LMessageFilterCommandPartsTestsString(cFilter.UID < new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1:999") throw new cTestsException("ZMessageFilterCommandPartsTests UID.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID <= new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1:1000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID == new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID >= new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1000:*") throw new cTestsException("ZMessageFilterCommandPartsTests UID.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID > new cUID(1, 1000), lSelectedMailbox, false, false, null) != "UID 1001:*") throw new cTestsException("ZMessageFilterCommandPartsTests UID.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.UID > new cUID(1, 1000) & cFilter.UID < new cUID(1, 2000), lSelectedMailbox, false, false, null) != "UID 1001:* UID 1:1999") throw new cTestsException("ZMessageFilterCommandPartsTests UID.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, false, false, null) != "NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.7", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, false, null) != "US-ASCII NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.8", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, true, null) != "UTF-8 NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.9", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, false, Encoding.UTF32) != "US-ASCII NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.10", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), lSelectedMailbox, true, true, Encoding.UTF32) != "UTF-8 NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.11", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered, lSelectedMailbox, false, false, null) != "ANSWERED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged, lSelectedMailbox, false, false, null) != "ANSWERED FLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.2", lContext);

                    cFetchableFlagsList lFFlags = new cFetchableFlagsList();

                    lFFlags.Add(kFlagName.Draft);
                    lFFlags.Add(kFlagName.Recent);
                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged | cFilter.FlagsContain(lFFlags), lSelectedMailbox, false, false, null) != "OR (ANSWERED FLAGGED) (DRAFT RECENT)") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged & cFilter.FlagsContain(lFFlags), lSelectedMailbox, false, false, null) != "ANSWERED DRAFT FLAGGED RECENT") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.4", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged & cFilter.IsForwarded, lSelectedMailbox, false, false, null) != "KEYWORD $FORWARDED ANSWERED FLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(!(cFilter.IsAnswered & cFilter.IsFlagged & cFilter.IsForwarded), lSelectedMailbox, false, false, null) != "UNKEYWORD $FORWARDED UNANSWERED UNFLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.6", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred"), lSelectedMailbox, false, false, null) != "KEYWORD FRED") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred") | cFilter.FlagsContain("angus"), lSelectedMailbox, false, false, null) != "OR KEYWORD FRED KEYWORD ANGUS") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred") & cFilter.FlagsContain("angus"), lSelectedMailbox, false, false, null) != "KEYWORD ANGUS KEYWORD FRED") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.3", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.BCC.Contains("@bacome.work"), lSelectedMailbox, false, false, null) != "BCC @bacome.work") throw new cTestsException("ZMessageFilterCommandPartsTests BCC.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("imap client"), lSelectedMailbox, false, false, null) != "SUBJECT \"imap client\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Body.Contains("imap"), lSelectedMailbox, false, false, null) != "BODY imap") throw new cTestsException("ZMessageFilterCommandPartsTests Body.1", lContext);

                    if (LMessageFilterCommandPartsTestsString(!cFilter.To.Contains("bacome") & !cFilter.FlagsContain(kFlagName.Recent), lSelectedMailbox, false, false, null) != "NOT TO bacome OLD") throw new cTestsException("ZMessageFilterCommandPartsTests And.1", lContext);

                    bool lFailed;

                    lFailed = false;
                    try { if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, false, null) != "SUBJECT \"fr?d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.2", lContext); }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests Subject.2 - didn't fail as expected", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, true, Encoding.UTF32) != "SUBJECT \"fr«226»«130»«172»d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, false, Encoding.UTF8) != "CHARSET utf-8 SUBJECT {6}fr«226»«130»«172»d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, false, false, Encoding.UTF7) != "CHARSET utf-7 SUBJECT fr+IKw-d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.5", lContext);



                    lFailed = false;
                    try { if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, true, false, null) != "US-ASCII SUBJECT \"fr?d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6", lContext); }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6 - didn't fail as expected", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, true, true, Encoding.UTF32) != "UTF-8 SUBJECT \"fr«226»«130»«172»d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), lSelectedMailbox, true, false, Encoding.UTF8) != "utf-8 SUBJECT {6}fr«226»«130»«172»d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.7", lContext);
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
                    var lMH1 = new cMessage(lClient, lSelectedMailbox.Cache[0]);
                    var lMH2 = new cMessage(lClient, lSelectedMailbox.Cache[1]);

                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < lMH1, lSelectedMailbox, false, false, null) != "SEEN UNSEEN") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < lMH2, lSelectedMailbox, false, false, null) != "1:1") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= lMH1, lSelectedMailbox, false, false, null) != "1:1") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= lMH2, lSelectedMailbox, false, false, null) != "1:2") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= lMH2, lSelectedMailbox, false, false, null) != "2:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.5", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > lMH2, lSelectedMailbox, false, false, null) != "3:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.6", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.First.MSNOffset(-5), lSelectedMailbox, false, false, null) != "ALL") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.7", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "2:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.8", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "ALL") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.9", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.First.MSNOffset(2), lSelectedMailbox, false, false, null) != "3:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.10", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.First.MSNOffset(-5), lSelectedMailbox, false, false, null) != "SEEN UNSEEN") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.11", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "SEEN UNSEEN") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.12", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.First.MSNOffset(0), lSelectedMailbox, false, false, null) != "1:1") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.13", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.First.MSNOffset(2), lSelectedMailbox, false, false, null) != "1:3") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.14", lContext);



                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.Last.MSNOffset(-5), lSelectedMailbox, false, false, null) != "6:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.15", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN > cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "11:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.16", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "10:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.17", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN >= cFilter.Last.MSNOffset(2), lSelectedMailbox, false, false, null) != "12:*") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.18", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.Last.MSNOffset(-5), lSelectedMailbox, false, false, null) != "1:4") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.19", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN < cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "1:9") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.20", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.Last.MSNOffset(0), lSelectedMailbox, false, false, null) != "1:10") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.21", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.MSN <= cFilter.Last.MSNOffset(2), lSelectedMailbox, false, false, null) != "1:12") throw new cTestsException("ZMessageFilterCommandPartsTests MSN.22", lContext);


                    string LMessageFilterCommandPartsTestsString(cFilter pFilter, cSelectedMailbox pSelectedMailbox, bool pCharsetMandatory, bool pUTF8Enabled, Encoding pEncoding)
                    {
                        StringBuilder lBuilder = new StringBuilder();
                        var lCommandBuilder = new cCommandDetailsBuilder();
                        cCommandPartFactory lFactory = new cCommandPartFactory(pUTF8Enabled, pEncoding);
                        lCommandBuilder.Add(pFilter, pSelectedMailbox, pCharsetMandatory, lFactory);
                        var lDetails = lCommandBuilder.EmitCommandDetails();

                        foreach (var lPart in lDetails.Parts)
                        {
                            if (lPart.Type == eCommandPartType.literal)
                            {
                                lBuilder.Append("{");
                                lBuilder.Append(cTools.ASCIIBytesToString(lPart.LiteralLengthBytes));
                                lBuilder.Append("}");
                            }

                            lBuilder.Append(cTools.BytesToLoggableString(lPart.Bytes));
                        }

                        return lBuilder.ToString();
                    }
                }
            }
        }
    }
}