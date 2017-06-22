using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kListExtendedCommandPartList = new cCommandPart("LIST");
            private static readonly cCommandPart kListExtendedCommandPartRecursiveMatch = new cCommandPart("RECURSIVEMATCH");
            private static readonly cCommandPart kListExtendedCommandPartSubscribed = new cCommandPart("SUBSCRIBED");
            private static readonly cCommandPart kListExtendedCommandPartRemote = new cCommandPart("REMOTE");
            private static readonly cCommandPart kListExtendedCommandPartSpecialUse = new cCommandPart("SPECIAL-USE");
            private static readonly cCommandPart kListExtendedCommandPartMailbox = new cCommandPart("\"\"");
            private static readonly cCommandPart kListExtendedCommandPartReturn = new cCommandPart("RETURN");
            private static readonly cCommandPart kListExtendedCommandPartChildren = new cCommandPart("CHILDREN");
            private static readonly cCommandPart kListExtendedCommandPartStatus = new cCommandPart("STATUS");

            [Flags]
            public enum fListExtendedSelect
            {
                recursivematch = 1, // mod-opt
                subscribed = 1 << 1, // base-opt
                remote = 1 << 2, // independent-opt
                specialuse = 1 << 3 // independent-opt (!) [which means you can't ask for all special-use mailboxes no matter where they are?]
            }

            [Flags]
            public enum fListExtendedReturn
            {
                subscribed = 1,
                children = 1 << 1,
                specialuse = 1 << 2
            }

            public async Task<cMailboxList> ListExtendedAsync(cMethodControl pMC, fListExtendedSelect pSelect, cListPatterns pPatterns, fListExtendedReturn pReturn, fStatusAttributes pStatus, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListAsync), pMC, pPatterns);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                // if a mod-opt is selected
                if ((pSelect & fListExtendedSelect.recursivematch) != 0)
                {
                    // check that a base-opt is also selected
                    if ((pSelect & fListExtendedSelect.subscribed) == 0) throw new ArgumentOutOfRangeException(nameof(pSelect));
                }

                // capture the current value
                var lEnabledExtensions = EnabledExtensions;

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    ZListExtendedAddCommandParts(pSelect, pPatterns, pReturn, pStatus, lEnabledExtensions, lCommand);

                    cMailboxNamePatterns lMailboxNamePatterns = new cMailboxNamePatterns();
                    foreach (var lPattern in pPatterns) lMailboxNamePatterns.Add(lPattern.MailboxNamePattern);
                    var lHook = new cListExtendedCommandHook(lMailboxNamePatterns, _Capability, lEnabledExtensions);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("listextended success");
                        return lHook.MailboxList;
                    }

                    if (lHook.MailboxList.Count != 0) lContext.TraceError("received mailboxes on a failed listextended");

                    fCapabilities lTryIgnoring = 0;

                    if ((pSelect & fListExtendedSelect.specialuse) != 0 || (pReturn & fListExtendedReturn.specialuse) != 0) lTryIgnoring |= fCapabilities.SpecialUse;
                    if ((pStatus & fStatusAttributes.all) != 0) lTryIgnoring |= fCapabilities.ListStatus;
                    if (lTryIgnoring == 0) lTryIgnoring |= fCapabilities.ListExtended;

                    if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }

            private static void ZListExtendedAddCommandParts(fListExtendedSelect pSelect, cListPatterns pPatterns, fListExtendedReturn pReturn, fStatusAttributes pStatus, fEnableableExtensions pEnabledExtensions, cCommand pCommand)
            {
                pCommand.BeginList(cCommand.eListBracketing.none); // space separate each section

                pCommand.Add(kListExtendedCommandPartList);

                // select options

                pCommand.BeginList(cCommand.eListBracketing.ifany);
                if ((pSelect & fListExtendedSelect.recursivematch) != 0) pCommand.Add(kListExtendedCommandPartRecursiveMatch);
                if ((pSelect & fListExtendedSelect.subscribed) != 0) pCommand.Add(kListExtendedCommandPartSubscribed);
                if ((pSelect & fListExtendedSelect.remote) != 0) pCommand.Add(kListExtendedCommandPartRemote);
                if ((pSelect & fListExtendedSelect.specialuse) != 0) pCommand.Add(kListExtendedCommandPartSpecialUse);
                pCommand.EndList();

                // mailbox

                pCommand.Add(kListExtendedCommandPartMailbox);

                // patterns

                cCommandPart.cFactory lFactory = new cCommandPart.cFactory((pEnabledExtensions & fEnableableExtensions.utf8) != 0);

                pCommand.BeginList(cCommand.eListBracketing.ifmorethanone);

                foreach (var lPattern in pPatterns)
                {
                    if (!lFactory.TryAsListMailbox(lPattern.ListMailbox, lPattern.Delimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pPatterns));
                    pCommand.Add(lListMailboxCommandPart);
                }

                pCommand.EndList();

                // return options

                pCommand.BeginList(cCommand.eListBracketing.ifany, kListExtendedCommandPartReturn);
                if ((pReturn & fListExtendedReturn.subscribed) != 0) pCommand.Add(kListExtendedCommandPartSubscribed);
                if ((pReturn & fListExtendedReturn.children) != 0) pCommand.Add(kListExtendedCommandPartChildren);
                if ((pReturn & fListExtendedReturn.specialuse) != 0) pCommand.Add(kListExtendedCommandPartSpecialUse);
                pCommand.Add(pStatus, kListExtendedCommandPartStatus);
                pCommand.EndList();

                // done
                pCommand.EndList();
            }

            private class cListExtendedCommandHook : cCommandHook
            {
                private static readonly cBytes kListSpace = new cBytes("LIST ");
                private static readonly cBytes kStatusSpace = new cBytes("STATUS ");

                private readonly cMailboxNamePatterns mMailboxNamePatterns; // the first part of the mailboxname must match one of these patterns
                private readonly cCapability mCapability;
                private readonly fEnableableExtensions mEnabledExtensions;

                public readonly cMailboxList MailboxList = new cMailboxList();

                public cListExtendedCommandHook(cMailboxNamePatterns pMailboxNamePatterns, cCapability pCapability, fEnableableExtensions pEnabledExtensions)
                {
                    mMailboxNamePatterns = pMailboxNamePatterns;
                    mCapability = pCapability;
                    mEnabledExtensions = pEnabledExtensions;
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(ProcessData));

                    cResponseDataList lList;
                    cResponseDataStatus lStatus;

                    if (pCursor.Parsed)
                    {
                        lList = pCursor.ParsedAs as cResponseDataList;
                        lStatus = pCursor.ParsedAs as cResponseDataStatus;
                        if (lList == null && lStatus == null) return eProcessDataResult.notprocessed;
                    }
                    else
                    {
                        if (pCursor.SkipBytes(kListSpace))
                        {
                            if (!cResponseDataList.Process(pCursor, mCapability, mEnabledExtensions, out lList, lContext))
                            {
                                lContext.TraceWarning("likely malformed list response");
                                return eProcessDataResult.notprocessed;
                            }

                            lStatus = null;
                        }
                        else if (pCursor.SkipBytes(kStatusSpace))
                        {
                            if (!cResponseDataStatus.Process(pCursor, out lStatus, lContext))
                            {
                                lContext.TraceWarning("likely malformed status response");
                                return eProcessDataResult.notprocessed;
                            }

                            lList = null;
                        }
                        else return eProcessDataResult.notprocessed;
                    }

                    if (lList != null)
                    {
                        if (!mMailboxNamePatterns.Matches(lList.MailboxName.Name)) return eProcessDataResult.notprocessed;
                        MailboxList.Store(lList.EncodedMailboxName, lList.MailboxName, lList.MailboxFlags);
                        return eProcessDataResult.observed;
                    }

                    if (lStatus != null)
                    {
                        if (MailboxList.Store(lStatus.EncodedMailboxName, lStatus.Status)) return eProcessDataResult.observed;
                        return eProcessDataResult.notprocessed;
                    }

                    return eProcessDataResult.notprocessed;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(_Tests));

                    cCapabilities lCD = new cCapabilities();
                    lCD.Set("children");
                    lCD.Set("list-extended");
                    lCD.Set("list-status");
                    lCD.Set("special-use");
                    cCapability lC = new cCapability(lCD, new cCapabilities(), 0);

                    cMailboxNamePatterns lPatterns;
                    cListExtendedCommandHook lRDP;
                    cMailboxList lMailboxList;
                    cMailboxList.cItem lMailboxListItem;

                    // 1: test the LIST "" "" response
                    lPatterns = new cMailboxNamePatterns();
                    lPatterns.Add(new cMailboxNamePattern("", "", null));
                    lRDP = new cListExtendedCommandHook(lPatterns, lC, 0);
                    LProcess(lRDP, "LIST () \"/\" \"\"", true, "1.1");
                    LProcess(lRDP, "LIST () \"/\" fred", false, "1.2");
                    lMailboxList = lRDP.MailboxList;
                    if (lMailboxList.Count != 1) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.1.r1");
                    lMailboxListItem = lMailboxList.FirstItem();
                    if (lMailboxListItem.MailboxName.Name != "") throw new cTestsException($"{nameof(cListExtendedCommandHook)}.1.r2");
                    if (lMailboxListItem.MailboxName.Delimiter != '/') throw new cTestsException($"{nameof(cListExtendedCommandHook)}.1.r3");
                    if (lMailboxListItem.Flags != fMailboxFlags.rfc3501) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.1.r4");
                    if (lMailboxListItem.Status != null) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.1.r5");

                    // 2: test some flags, test co-dependant flags, sending of a mailbox twice, childinfo, 
                    lPatterns = new cMailboxNamePatterns();
                    lPatterns.Add(new cMailboxNamePattern("fred/", "%", '/'));
                    lRDP = new cListExtendedCommandHook(lPatterns, lC, 0);
                    LProcess(lRDP, "LIST (\\Marked \\NoInferiors) \"/\" \"inbox\"", false, "2.1");
                    LProcess(lRDP, "LIST (\\Subscribed \\NonExistent) \"/\" \"Fruit/Peach\"", false, "2.2");
                    LProcess(lRDP, "LIST (\\Subscribed \\NonExistent) \"/\" \"fred/Fruit/Peach\"", false, "2.3");
                    LProcess(lRDP, "LIST (\\Subscribed \\NonExistent) \"/\" \"fred/Peach\"", true, "2.4"); // subscribed, non-existent, => noselect - children unknown
                    LProcess(lRDP, "LIST (\\HasChildren) \"/\" \"fred/Fruit\"", true, "2.5"); // haschildren
                    LProcess(lRDP, "LIST (\\HasNoChildren) \"/\" \"fred/Tofu\"", true, "2.6"); // hasnochildren
                    LProcess(lRDP, "LIST (\\Marked) \"/\" \"fred/Tofu\"", true, "2.7"); // add marked to the above
                    LProcess(lRDP, "LIST () \"/\" \"fred/Bread\" (\"CHILDINFO\" (\"SUBSCRIBED\"))", true, "2.8"); // subscribed children
                    LProcess(lRDP, "LIST () \"/\" \"fred/Tea\" (\"CHILDINFO\" (\"x-feature\" \"y-feature\" \"SUBSCRIBED\" \"z-freature\"))", true, "2.9"); // subscribed children
                    LProcess(lRDP, "LIST () \"/\" \"fred/Coffee\" (tag1 1 tag2 0 tag3 1,2,3:7,5 tag4 (d (e) (f (g h i) j (k l)) m) \"CHiLDiNFO\" ((a b c) \"SUBSCRiBED\" (d e f)) tag6 6)", true, "2.10"); // subscribed children
                    LProcess(lRDP, "STATUS fred/Coffee (uidvalidity 12345678 MESSAGES 231 UIDNEXT 44292 UNseen 44)", true, "2.11"); // status
                    LProcess(lRDP, "STATUS fred/Tofu (uidvalidity 12345679 MESSAGES 233)", true, "2.12"); // status
                    LProcess(lRDP, "STATUS fred/Tofo (uidvalidity 12345679 MESSAGES 233)", false, "2.13");
                    LProcess(lRDP, "STATUS fred/Tofu (uidvaliditx 12345680 MESSAGES 234)", false, "2.14");
                    LProcess(lRDP, "STATUS fred/Tofu (uidvalidity 12345681 MESSAGES 235)", true, "2.15"); // status

                    List<cMailboxList.cItem> lItems = new List<cMailboxList.cItem>(lRDP.MailboxList);

                    // peach (sub, non-ex, nosel), fruit (has), tofu (hasno, marked, status (235, 12345681)), bread (sub child), tea (sub child), coffee (sub child, status (231, 12345678, 44)), 
                    if (lItems.Count != 6) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.2.r1");


                    lMailboxListItem = lItems[0];
                    if (lMailboxListItem.MailboxName.Name != "fred/Peach" || lMailboxListItem.Flags != (fMailboxFlags.rfc3501 | fMailboxFlags.subscribed | fMailboxFlags.nonexistent | fMailboxFlags.noselect) || lMailboxListItem.Status != null) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.2.r2");
                    // TO FINISH THIS 


                    // 3: the above example but with a *

                    // test the list of a mailbox with a * in the name

                    // test the list of a mailbox with a % in the name

                    // test children, without the capability

                    // check that inbox is converted to uppercase

                    // that a UTF7 encoded mailbox names are decoded
                    //  with and without segments

                    // that a UTF8 encoded mailbox decoded


                    // test lsub

                    void LProcess(cListExtendedCommandHook pRDP, string pResponse, bool pShouldBeProcessed, string pTestNumber)
                    {
                        if (!cBytesCursor.TryConstruct(pResponse, out var lCursor)) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.{pTestNumber}.p1");
                        var lResult = pRDP.ProcessData(lCursor, lContext);
                        if (pShouldBeProcessed && lResult != eProcessDataResult.observed) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.{pTestNumber}.p2");
                        if (!pShouldBeProcessed && lResult == eProcessDataResult.observed) throw new cTestsException($"{nameof(cListExtendedCommandHook)}.{pTestNumber}.p3");
                    }
                }
            }

            [Conditional("DEBUG")]
            private static void _Tests_ListExtendedCommandParts(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession),nameof(_Tests_ListExtendedCommandParts));

                cListPatterns lPatterns;

                lPatterns = new cListPatterns();
                lPatterns.Add(new cListPattern("fred%", null, new cMailboxNamePattern("fred", "%", null)));

                if (LListExtendedCommandPartsTestsString(0, lPatterns, 0, 0) != "LIST \"\" fred%") throw new cTestsException("list extended command 1", lContext);
                if (LListExtendedCommandPartsTestsString(fListExtendedSelect.remote, lPatterns, 0, 0) != "LIST (REMOTE) \"\" fred%") throw new cTestsException("list extended command 2", lContext);
                if (LListExtendedCommandPartsTestsString(fListExtendedSelect.remote | fListExtendedSelect.subscribed, lPatterns, 0, 0) != "LIST (SUBSCRIBED REMOTE) \"\" fred%") throw new cTestsException("list extended command 3", lContext);

                lPatterns.Add(new cListPattern("angus%", null, new cMailboxNamePattern("angus", "%", null)));

                if (LListExtendedCommandPartsTestsString(0, lPatterns, 0, 0) != "LIST \"\" (fred% angus%)") throw new cTestsException("list extended command 4", lContext);

                if (LListExtendedCommandPartsTestsString(0, lPatterns, fListExtendedReturn.subscribed, 0) != "LIST \"\" (fred% angus%) RETURN (SUBSCRIBED)") throw new cTestsException("list extended command 5", lContext);
                if (LListExtendedCommandPartsTestsString(0, lPatterns, fListExtendedReturn.subscribed | fListExtendedReturn.children, 0) != "LIST \"\" (fred% angus%) RETURN (SUBSCRIBED CHILDREN)") throw new cTestsException("list extended command 6", lContext);
                if (LListExtendedCommandPartsTestsString(0, lPatterns, 0, fStatusAttributes.recent) != "LIST \"\" (fred% angus%) RETURN (STATUS (RECENT))") throw new cTestsException("list extended command 7", lContext);
                if (LListExtendedCommandPartsTestsString(0, lPatterns, fListExtendedReturn.children, fStatusAttributes.recent) != "LIST \"\" (fred% angus%) RETURN (CHILDREN STATUS (RECENT))") throw new cTestsException("list extended command 8", lContext);

                string LListExtendedCommandPartsTestsString(fListExtendedSelect pSelect, cListPatterns pPatterns, fListExtendedReturn pReturn, fStatusAttributes pStatus)
                {
                    StringBuilder lBuilder = new StringBuilder();
                    var lCommand = new cCommand();
                    ZListExtendedAddCommandParts(pSelect, pPatterns, pReturn, pStatus, 0, lCommand);
                    foreach (var lPart in lCommand.Parts) lBuilder.Append(cTools.ASCIIBytesToString(lPart.Bytes));
                    return lBuilder.ToString();
                }
            }
        }
    }
}