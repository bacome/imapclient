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
            private enum eListBracketing { none, bracketed, ifany, ifmorethanone }

            private class cCommandParts
            {
                private readonly Stack<cList> mLists = new Stack<cList>();
                private cList mList = null; // the current list

                public readonly List<cCommandPart> Parts = new List<cCommandPart>();

                public cCommandParts() { }

                public void Add(cCommandPart pPart)
                {
                    if (mList == null)
                    {
                        Parts.Add(pPart);
                        return;
                    }

                    mList.Add(pPart);
                }

                public void Add(IList<cCommandPart> pParts)
                {
                    if (mList == null)
                    {
                        Parts.AddRange(pParts);
                        return;
                    }

                    mList.Add(pParts);
                }

                public void Add(params cCommandPart[] pParts)
                {
                    if (mList == null)
                    {
                        Parts.AddRange(pParts);
                        return;
                    }

                    mList.Add(pParts);
                }

                public void BeginList(eListBracketing pBracketing, cCommandPart pListName = null)
                {
                    if (mList != null) mLists.Push(mList);
                    mList = new cList(pBracketing, pListName);
                }

                public void EndList()
                {
                    var lList = mList;

                    if (mLists.Count == 0) mList = null;
                    else mList = mLists.Pop();

                    if (lList.Bracketing == eListBracketing.bracketed || (lList.Bracketing == eListBracketing.ifany && lList.AddCount > 0) || (lList.Bracketing == eListBracketing.ifmorethanone && lList.AddCount > 1))
                    {
                        List<cCommandPart> lParts = new List<cCommandPart>();

                        if (lList.ListName != null)
                        {
                            lParts.Add(lList.ListName);
                            lParts.Add(cCommandPart.Space);
                        }

                        lParts.Add(cCommandPart.LParen);
                        lParts.AddRange(lList.Parts);
                        lParts.Add(cCommandPart.RParen);
                        Add(lParts);
                    }
                    else if (lList.Parts.Count > 0)
                    {
                        if (lList.ListName != null)
                        {
                            List<cCommandPart> lParts = new List<cCommandPart>();
                            lParts.Add(lList.ListName);
                            lParts.Add(cCommandPart.Space);
                            lParts.AddRange(lList.Parts);
                            Add(lParts);
                        }
                        else Add(lList.Parts);
                    }
                }

                public override string ToString()
                {
                    var lBuilder = new cListBuilder(nameof(cCommandParts));
                    foreach (var lPart in Parts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }

                private class cList
                {
                    public readonly eListBracketing Bracketing;
                    public readonly cCommandPart ListName;
                    private readonly List<cCommandPart> mParts;
                    private int mAddCount;
                    public readonly ReadOnlyCollection<cCommandPart> Parts;

                    public cList(eListBracketing pBracketing, cCommandPart pListName)
                    {
                        Bracketing = pBracketing;
                        ListName = pListName;
                        mParts = new List<cCommandPart>();
                        mAddCount = 0;
                        Parts = new ReadOnlyCollection<cCommandPart>(mParts);
                    }

                    public void Add(cCommandPart pPart)
                    {
                        if (mAddCount != 0) mParts.Add(cCommandPart.Space);
                        mParts.Add(pPart);
                        mAddCount++;
                    }

                    public void Add(IList<cCommandPart> pParts)
                    {
                        if (mAddCount != 0) mParts.Add(cCommandPart.Space);
                        mParts.AddRange(pParts);
                        mAddCount++;
                    }

                    public int AddCount => mAddCount;
                }
            }

            private class cCommand : cCommandParts, IDisposable
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
                private static readonly cCommandPart kCommandPartReferences = new cCommandPart("BODY.PEEK[HEADER.FIELDS (references)]");
                private static readonly cCommandPart kCommandPartModSeq = new cCommandPart("MODSEQ");

                // fetch body
                private static readonly cCommandPart kCommandPartHeader = new cCommandPart("HEADER");
                private static readonly cCommandPart kCommandPartHeaderFields = new cCommandPart("HEADER.FIELDS(");
                private static readonly cCommandPart kCommandPartHeaderFieldsNot = new cCommandPart("HEADER.FIELDS.NOT(");
                private static readonly cCommandPart kCommandPartText = new cCommandPart("TEXT");
                private static readonly cCommandPart kCommandPartMime = new cCommandPart("MIME");
                private static readonly cCommandPart kCommandPartLessThan = new cCommandPart("<");
                private static readonly cCommandPart kCommandPartGreaterThan = new cCommandPart(">");

                // status
                private static readonly cCommandPart kCommandPartMessages = new cCommandPart("MESSAGES");
                //private static readonly cCommandPart kCommandPartRecent = new cCommandPart("RECENT");
                private static readonly cCommandPart kCommandPartUIDNext = new cCommandPart("UIDNEXT");
                private static readonly cCommandPart kCommandPartUIDValidity = new cCommandPart("UIDVALIDITY");
                //private static readonly cCommandPart kCommandPartUnseen = new cCommandPart("UNSEEN");
                private static readonly cCommandPart kCommandPartHighestModSeq = new cCommandPart("HIGHESTMODSEQ");

                private bool mDisposed = false;
                private bool mDisposeOnCommandCompletion = false;

                public readonly cCommandTag Tag = new cCommandTag();

                // exclusive access tokens and blocks
                private readonly List<cExclusiveAccess.cToken> mTokens = new List<cExclusiveAccess.cToken>();
                private readonly List<cExclusiveAccess.cBlock> mBlocks = new List<cExclusiveAccess.cBlock>();
                private int mExclusiveAccessSequence = -1;

                // UIDValidity
                private uint? mUIDValidity = null;

                // authentication is disposable
                private cSASLAuthentication mAuthentication = null;

                // hook, hidden because this object needs to route the commandcompete calls to it so the disposes are done last
                private cCommandHook mHook = null;

                public cCommand() { }

                public void Add(fFetchAttributes pAttributes)
                {
                    BeginList(eListBracketing.ifmorethanone);
                    if ((pAttributes & fFetchAttributes.flags) != 0) Add(kCommandPartFlags);
                    if ((pAttributes & fFetchAttributes.envelope) != 0) Add(kCommandPartEnvelope);
                    if ((pAttributes & fFetchAttributes.received) != 0) Add(kCommandPartInternalDate);
                    if ((pAttributes & fFetchAttributes.size) != 0) Add(kCommandPartrfc822size);
                    if ((pAttributes & fFetchAttributes.body) != 0) Add(kCommandPartBody);
                    if ((pAttributes & fFetchAttributes.bodystructure) != 0) Add(kCommandPartBodyStructure);
                    if ((pAttributes & fFetchAttributes.uid) != 0) Add(kCommandPartUID);
                    if ((pAttributes & fFetchAttributes.references) != 0) Add(kCommandPartReferences);
                    if ((pAttributes & fFetchAttributes.modseq) != 0) Add(kCommandPartModSeq);
                    EndList();
                }

                public void AddStatusAttributes(fMailboxCacheData pAttributes)
                {
                    BeginList(eListBracketing.bracketed);
                    if ((pAttributes & fMailboxCacheData.messagecount) != 0) Add(kCommandPartMessages);
                    if ((pAttributes & fMailboxCacheData.recentcount) != 0) Add(kCommandPartRecent);
                    if ((pAttributes & fMailboxCacheData.uidnext) != 0) Add(kCommandPartUIDNext);
                    if ((pAttributes & fMailboxCacheData.uidvalidity) != 0) Add(kCommandPartUIDValidity);
                    if ((pAttributes & fMailboxCacheData.unseencount) != 0) Add(kCommandPartUnseen);
                    if ((pAttributes & fMailboxCacheData.highestmodseq) != 0) Add(kCommandPartHighestModSeq);
                    EndList();
                }

                public void Add(cFilter pFilter, bool pCharsetMandatory, cCommandPartFactory pFactory)
                {
                    if (pFilter?.UIDValidity != null) AddUIDValidity(pFilter.UIDValidity.Value);

                    var lFilterParts = ZFilterParts(pFilter, eListBracketing.none, pFactory);

                    if (pFactory.UTF8Enabled)
                    {
                        // rfc 6855 explicitly disallows charset on search when utf8 is enabled
                        if (pCharsetMandatory) Add(kCommandPartUTF8Space); // but for sort and thread it is mandatory
                    }
                    else
                    {
                        bool lEncodedParts = false;

                        foreach (var lPart in lFilterParts.Parts)
                        {
                            if (lPart.Encoded)
                            {
                                lEncodedParts = true;
                                break;
                            }
                        }

                        if (lEncodedParts)
                        {
                            if (!pCharsetMandatory) Add(kCommandPartCharsetSpace);
                            Add(pFactory.CharsetName);
                            Add(cCommandPart.Space);
                        }
                        else if (pCharsetMandatory) Add(kCommandPartUSASCIISpace); // have to put something for sort and thread
                    }

                    Add(lFilterParts.Parts);
                }

                private static cCommandParts ZFilterParts(cFilter pFilter, eListBracketing pBracketing, cCommandPartFactory pFactory)
                {
                    var lParts = new cCommandParts();

                    switch (pFilter)
                    {
                        case null:

                            lParts.Add(kCommandPartAll);
                            return lParts;

                        case cFilter.cUIDIn lUIDIn:

                            lParts.Add(kCommandPartUIDSpace, new cCommandPart(lUIDIn.SequenceSet));
                            return lParts;

                        case cFilter.cFlagsContain lFlagsContain:

                            lParts.BeginList(pBracketing);

                            foreach (var lFlag in lFlagsContain.Flags)
                            {
                                if (lFlag == cMessageFlags.Answered) lParts.Add(kCommandPartAnswered);
                                else if (lFlag == cMessageFlags.Flagged) lParts.Add(kCommandPartFlagged);
                                else if (lFlag == cMessageFlags.Deleted) lParts.Add(kCommandPartDeleted);
                                else if (lFlag == cMessageFlags.Seen) lParts.Add(kCommandPartSeen);
                                else if (lFlag == cMessageFlags.Draft) lParts.Add(kCommandPartDraft);
                                else if (lFlag == cMessageFlags.Recent) lParts.Add(kCommandPartRecent);
                                else lParts.Add(kCommandPartKeywordSpace, new cCommandPart(lFlag));
                            }

                            lParts.EndList();
                            return lParts;

                        case cFilter.cPartContains lPartContains:

                            switch (lPartContains.Part)
                            {
                                case cFilter.ePart.bcc:

                                    lParts.Add(kCommandPartBCCSpace);
                                    break;

                                case cFilter.ePart.body:

                                    lParts.Add(kCommandPartBodySpace);
                                    break;

                                case cFilter.ePart.cc:

                                    lParts.Add(kCommandPartCCSpace);
                                    break;

                                case cFilter.ePart.from:

                                    lParts.Add(kCommandPartFromSpace);
                                    break;

                                case cFilter.ePart.subject:

                                    lParts.Add(kCommandPartSubjectSpace);
                                    break;

                                case cFilter.ePart.text:

                                    lParts.Add(kCommandPartTextSpace);
                                    break;

                                case cFilter.ePart.to:

                                    lParts.Add(kCommandPartToSpace);
                                    break;

                                default:

                                    throw new ArgumentException("invalid part", nameof(pFilter));
                            }

                            lParts.Add(pFactory.AsAString(lPartContains.Contains));
                            return lParts;

                        case cFilter.cDateCompare lDateCompare:

                            if (lDateCompare.Date == cFilter.eDate.arrival)
                            {
                                if (lDateCompare.Compare == cFilter.eDateCompare.before) lParts.Add(kCommandPartBeforeSpace);
                                else if (lDateCompare.Compare == cFilter.eDateCompare.on) lParts.Add(kCommandPartOnSpace);
                                else lParts.Add(kCommandPartSinceSpace);
                            }
                            else
                            {
                                if (lDateCompare.Compare == cFilter.eDateCompare.before) lParts.Add(kCommandPartSentBeforeSpace);
                                else if (lDateCompare.Compare == cFilter.eDateCompare.on) lParts.Add(kCommandPartSentOnSpace);
                                else lParts.Add(kCommandPartSentSinceSpace);
                            }

                            lParts.Add(cCommandPartFactory.AsDate(lDateCompare.WithDate));
                            return lParts;

                        case cFilter.cHeaderFieldContains lHeaderFieldContains:

                            lParts.Add(kCommandPartHeaderSpace, cCommandPartFactory.AsRFC822HeaderField(lHeaderFieldContains.HeaderField), cCommandPart.Space, pFactory.AsAString(lHeaderFieldContains.Contains));
                            return lParts;

                        case cFilter.cSizeCompare lSizeCompare:

                            if (lSizeCompare.Compare == cFilter.eSizeCompare.larger) lParts.Add(kCommandPartLargerSpace);
                            else lParts.Add(kCommandPartSmallerSpace);

                            lParts.Add(new cCommandPart(lSizeCompare.WithSize));
                            return lParts;

                        case cFilter.cAnd lAnd:

                            lParts.BeginList(pBracketing);
                            foreach (var lTerm in lAnd.Terms) lParts.Add(ZFilterParts(lTerm, eListBracketing.none, pFactory).Parts);
                            lParts.EndList();
                            return lParts;

                        case cFilter.cOr lOr:

                            lParts.Add(kCommandPartOrSpace);
                            lParts.Add(ZFilterParts(lOr.A, eListBracketing.ifmorethanone, pFactory).Parts);
                            lParts.Add(cCommandPart.Space);
                            lParts.Add(ZFilterParts(lOr.B, eListBracketing.ifmorethanone, pFactory).Parts);
                            return lParts;

                        case cFilter.cNot lNot:

                            if (lNot.Not is cFilter.cFlagsContain lFlagsUncontain)
                            {
                                lParts.BeginList(pBracketing);

                                foreach (var lFlag in lFlagsUncontain.Flags)
                                {
                                    if (lFlag == cMessageFlags.Answered) lParts.Add(kCommandPartUnanswered);
                                    else if (lFlag == cMessageFlags.Flagged) lParts.Add(kCommandPartUnflagged);
                                    else if (lFlag == cMessageFlags.Deleted) lParts.Add(kCommandPartUndeleted);
                                    else if (lFlag == cMessageFlags.Seen) lParts.Add(kCommandPartUnseen);
                                    else if (lFlag == cMessageFlags.Draft) lParts.Add(kCommandPartUndraft);
                                    else if (lFlag == cMessageFlags.Recent) lParts.Add(kCommandPartOld);
                                    else lParts.Add(kCommandPartUnkeywordSpace, new cCommandPart(lFlag));
                                }

                                lParts.EndList();
                            }
                            else
                            {
                                lParts.Add(kCommandPartNotSpace);
                                lParts.Add(ZFilterParts(lNot.Not, eListBracketing.ifmorethanone, pFactory).Parts);
                            }

                            return lParts;

                        default:

                            throw new ArgumentException("invalid subtype", nameof(pFilter));
                    }
                }

                public void Add(cSort pSort)
                {
                    BeginList(eListBracketing.bracketed);

                    foreach (var lItem in pSort.Items)
                    {
                        if (lItem.Desc) Add(kCommandPartReverse);

                        switch (lItem.Type)
                        {
                            case cSortItem.eType.received:

                                Add(kCommandPartArrival);
                                break;

                            case cSortItem.eType.cc:

                                Add(kCommandPartCC);
                                break;

                            case cSortItem.eType.sent:

                                Add(kCommandPartDate);
                                break;

                            case cSortItem.eType.from:

                                Add(kCommandPartFrom);
                                break;

                            case cSortItem.eType.size:

                                Add(kCommandPartSize);
                                break;

                            case cSortItem.eType.subject:

                                Add(kCommandPartSubject);
                                break;

                            case cSortItem.eType.to:

                                Add(kCommandPartTo);
                                break;

                            case cSortItem.eType.displayfrom:

                                Add(kCommandPartDisplayFrom);
                                break;

                            case cSortItem.eType.displayto:

                                Add(kCommandPartDisplayTo);
                                break;

                            default:

                                throw new ArgumentException("unknown item", nameof(pSort));
                        }
                    }

                    EndList();
                }

                public void Add(cSection pSection, uint pOrigin, uint pLength)
                {
                    if (pSection.Part != null)
                    {
                        Add(cCommandPartFactory.AsAtom(pSection.Part));
                        if (pSection.TextPart != eSectionPart.all) Add(cCommandPart.Dot);
                    }

                    switch (pSection.TextPart)
                    {
                        case eSectionPart.all:

                            break;

                        case eSectionPart.header:

                            Add(kCommandPartHeader);
                            break;

                        case eSectionPart.headerfields:

                            Add(kCommandPartHeaderFields);
                            LAdd(pSection.HeaderFields);
                            Add(cCommandPart.RParen);
                            break;

                        case eSectionPart.headerfieldsnot:

                            Add(kCommandPartHeaderFieldsNot);
                            LAdd(pSection.HeaderFields);
                            Add(cCommandPart.RParen);
                            break;

                        case eSectionPart.text:

                            Add(kCommandPartText);
                            break;

                        case eSectionPart.mime:

                            Add(kCommandPartMime);
                            break;

                        default:

                            throw new cInternalErrorException();
                    }

                    Add(cCommandPart.RBracket, kCommandPartLessThan, new cCommandPart(pOrigin), cCommandPart.Dot, new cCommandPart(pLength), kCommandPartGreaterThan);

                    void LAdd(cStrings pStrings)
                    {
                        ;?;
                        BeginList(eListBracketing.none);
                        foreach (var lString in pStrings) Add(m.AsAString(lString));
                        EndList();
                    }
                }

                public void Add(cExclusiveAccess.cToken pToken)
                {
                    mTokens.Add(pToken);
                    if (pToken.Sequence <= mExclusiveAccessSequence) throw new ArgumentOutOfRangeException();
                    mExclusiveAccessSequence = pToken.Sequence;
                }

                public void Add(cExclusiveAccess.cBlock pBlock)
                {
                    mBlocks.Add(pBlock);
                    if (pBlock.Sequence <= mExclusiveAccessSequence) throw new ArgumentOutOfRangeException();
                    mExclusiveAccessSequence = pBlock.Sequence;
                }

                public void AddUIDValidity(uint? pUIDValidity)
                {
                    if (pUIDValidity == null) return;
                    if (mUIDValidity == null) mUIDValidity = pUIDValidity;
                    else if (pUIDValidity != mUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
                }

                public void Add(cSASLAuthentication pAuthentication)
                {
                    if (mAuthentication != null) throw new InvalidOperationException();
                    mAuthentication = pAuthentication ?? throw new ArgumentNullException(nameof(pAuthentication));
                }

                public void Add(cCommandHook pHook)
                {
                    if (mHook != null) throw new InvalidOperationException();
                    mHook = pHook ?? throw new ArgumentNullException(nameof(pHook));
                }

                public uint? UIDValidity => mUIDValidity;

                public cSASLAuthentication Authentication => mAuthentication;

                public void SetEnqueued() => mDisposeOnCommandCompletion = true;

                public void CommandStarted(cTrace.cContext pParentContext)
                {
                    if (mHook != null) mHook.CommandStarted(pParentContext);
                }

                public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    if (mHook == null) return eProcessDataResult.notprocessed;
                    return mHook.ProcessData(pData, pParentContext);
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    if (mHook == null) return eProcessDataResult.notprocessed;
                    return mHook.ProcessData(pCursor, pParentContext);
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    if (mHook == null) return false;
                    return mHook.ProcessTextCode(pCursor, pParentContext);
                }

                public void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(CommandCompleted), pResult, pException);
                    if (mHook != null) mHook.CommandCompleted(pResult, pException, lContext);
                    ZDispose();
                }

                public void Dispose()
                {
                    if (mDisposeOnCommandCompletion) return;
                    ZDispose();
                }

                private void ZDispose()
                {
                    if (mDisposed) return;

                    foreach (var lToken in mTokens)
                    {
                        try { lToken.Dispose(); }
                        catch { }
                    }

                    foreach (var lBlock in mBlocks)
                    {
                        try { lBlock.Dispose(); }
                        catch { }
                    }

                    if (mAuthentication != null)
                    {
                        try { mAuthentication.Dispose(); }
                        catch { }
                    }

                    mDisposed = true;
                }

                public override string ToString() => $"{nameof(cCommand)}({Tag},{UIDValidity},{base.ToString()})";

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(_Tests));

                    if (LMessageFilterCommandPartsTestsString(cFilter.UID < new cUID(1, 1000), false, false, null) != "UID 1:999") throw new cTestsException("ZMessageFilterCommandPartsTests UID.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID <= new cUID(1, 1000), false, false, null) != "UID 1:1000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID == new cUID(1, 1000), false, false, null) != "UID 1000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID >= new cUID(1, 1000), false, false, null) != "UID 1000:*") throw new cTestsException("ZMessageFilterCommandPartsTests UID.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID > new cUID(1, 1000), false, false, null) != "UID 1001:*") throw new cTestsException("ZMessageFilterCommandPartsTests UID.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.UID > new cUID(1, 1000) & cFilter.UID < new cUID(1, 2000), false, false, null) != "UID 1001:* UID 1:1999") throw new cTestsException("ZMessageFilterCommandPartsTests UID.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), false, false, null) != "NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.7", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), true, false, null) != "US-ASCII NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.8", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), true, true, null) != "UTF-8 NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.9", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), true, false, Encoding.UTF32) != "US-ASCII NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.10", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.UID != new cUID(1, 2000), true, true, Encoding.UTF32) != "UTF-8 NOT UID 2000") throw new cTestsException("ZMessageFilterCommandPartsTests UID.11", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered, false, false, null) != "ANSWERED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged, false, false, null) != "ANSWERED FLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.2", lContext);

                    cFetchableFlags lFFlags;
                    lFFlags = new cFetchableFlags();

                    lFFlags.IsDraft = true;
                    lFFlags.IsRecent = true;
                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged | cFilter.FlagsContain(lFFlags), false, false, null) != "OR (ANSWERED FLAGGED) (DRAFT RECENT)") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged & cFilter.FlagsContain(lFFlags), false, false, null) != "ANSWERED DRAFT FLAGGED RECENT") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.4", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.IsAnswered & cFilter.IsFlagged & cFilter.IsForwarded, false, false, null) != "KEYWORD $FORWARDED ANSWERED FLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(!(cFilter.IsAnswered & cFilter.IsFlagged & cFilter.IsForwarded), false, false, null) != "UNKEYWORD $FORWARDED UNANSWERED UNFLAGGED") throw new cTestsException("ZMessageFilterCommandPartsTests Flags.6", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred"), false, false, null) != "KEYWORD FRED") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred") | cFilter.FlagsContain("angus"), false, false, null) != "OR KEYWORD FRED KEYWORD ANGUS") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.FlagsContain("fred") & cFilter.FlagsContain("angus"), false, false, null) != "KEYWORD ANGUS KEYWORD FRED") throw new cTestsException("ZMessageFilterCommandPartsTests Keyword.3", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.BCC.Contains("@bacome.work"), false, false, null) != "BCC @bacome.work") throw new cTestsException("ZMessageFilterCommandPartsTests BCC.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("imap client"), false, false, null) != "SUBJECT \"imap client\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Body.Contains("imap"), false, false, null) != "BODY imap") throw new cTestsException("ZMessageFilterCommandPartsTests Body.1", lContext);

                    if (LMessageFilterCommandPartsTestsString(!cFilter.To.Contains("bacome") & !cFilter.FlagsContain(cMessageFlags.Recent), false, false, null) != "NOT TO bacome OLD") throw new cTestsException("ZMessageFilterCommandPartsTests And.1", lContext);

                    bool lFailed;

                    lFailed = false;
                    try { if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), false, false, null) != "SUBJECT \"fr?d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.2", lContext); }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests Subject.2 - didn't fail as expected", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), false, true, Encoding.UTF32) != "SUBJECT \"fr«226»«130»«172»d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), false, false, Encoding.UTF8) != "CHARSET utf-8 SUBJECT {6}fr«226»«130»«172»d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), false, false, Encoding.UTF7) != "CHARSET utf-7 SUBJECT fr+IKw-d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.5", lContext);



                    lFailed = false;
                    try { if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), true, false, null) != "US-ASCII SUBJECT \"fr?d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6", lContext); }
                    catch { lFailed = true; }
                    if (!lFailed) throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6 - didn't fail as expected", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), true, true, Encoding.UTF32) != "UTF-8 SUBJECT \"fr«226»«130»«172»d\"") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), true, false, Encoding.UTF8) != "utf-8 SUBJECT {6}fr«226»«130»«172»d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.7", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Subject.Contains("fr€d"), true, false, Encoding.UTF7) != "utf-7 SUBJECT fr+IKw-d") throw new cTestsException("ZMessageFilterCommandPartsTests Subject.8", lContext);

                    DateTime lDateTime = new DateTime(1968, 4, 4, 12, 34, 56);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent < lDateTime, false, false, null) != "SENTBEFORE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent <= lDateTime, false, false, null) != "SENTBEFORE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.2", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent == lDateTime, false, false, null) != "SENTON 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.3", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent >= lDateTime, false, false, null) != "SENTSINCE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.4", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent > lDateTime, false, false, null) != "SENTSINCE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.5", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Received < lDateTime, false, false, null) != "BEFORE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.6", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received <= lDateTime, false, false, null) != "BEFORE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.7", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received == lDateTime, false, false, null) != "ON 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.8", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received != lDateTime, false, false, null) != "NOT ON 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.9", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received >= lDateTime, false, false, null) != "SINCE 4-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.10", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Received > lDateTime, false, false, null) != "SINCE 5-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.11", lContext);

                    lDateTime = new DateTime(1968, 4, 14, 12, 34, 56);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Sent < lDateTime, false, false, null) != "SENTBEFORE 14-APR-1968") throw new cTestsException("ZMessageFilterCommandPartsTests date.12", lContext);


                    if (LMessageFilterCommandPartsTestsString(cFilter.HasHeaderField("references"), false, false, null) != "HEADER references \"\"") throw new cTestsException("ZMessageFilterCommandPartsTests header.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.HeaderFieldContains("references", "@bacome"), false, false, null) != "HEADER references @bacome") throw new cTestsException("ZMessageFilterCommandPartsTests header.2", lContext);

                    if (LMessageFilterCommandPartsTestsString(cFilter.Size < 1000, false, false, null) != "SMALLER 1000") throw new cTestsException("ZMessageFilterCommandPartsTests Size.1", lContext);
                    if (LMessageFilterCommandPartsTestsString(cFilter.Size > 1000, false, false, null) != "LARGER 1000") throw new cTestsException("ZMessageFilterCommandPartsTests Size.2", lContext);

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

                    string LMessageFilterCommandPartsTestsString(cFilter pFilter, bool pCharsetMandatory, bool pUTF8Enabled, Encoding pEncoding)
                    {
                        StringBuilder lBuilder = new StringBuilder();
                        var lCommand = new cCommand();
                        lCommand.Add(pFilter, pCharsetMandatory, pUTF8Enabled, pEncoding);

                        foreach (var lPart in lCommand.Parts)
                        {
                            if (lPart.Literal)
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