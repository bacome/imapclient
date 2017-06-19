﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookList : cCommandHook
            {
                private static readonly cBytes kListSpace = new cBytes("LIST ");

                private readonly cMailboxNamePattern mMailboxNamePattern; // the first part of the mailboxname must match this pattern
                private readonly cCapability mCapability;
                private readonly fEnableableExtensions mEnabledExtensions;

                public readonly cMailboxList MailboxList = new cMailboxList();

                public cCommandHookList(cMailboxNamePattern pMailboxNamePattern, cCapability pCapability, fEnableableExtensions pEnabledExtensions)
                {
                    mMailboxNamePattern = pMailboxNamePattern;
                    mCapability = pCapability;
                    mEnabledExtensions = pEnabledExtensions;
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookList), nameof(ProcessData));

                    cResponseDataList lList;

                    if (pCursor.Parsed)
                    {
                        lList = pCursor.ParsedAs as cResponseDataList;
                        if (lList == null) return eProcessDataResult.notprocessed;
                    }
                    else
                    {
                        if (!pCursor.SkipBytes(kListSpace)) return eProcessDataResult.notprocessed;

                        if (!cResponseDataList.Process(pCursor, mCapability, mEnabledExtensions, out lList, lContext))
                        {
                            lContext.TraceWarning("likely malformed list response");
                            return eProcessDataResult.notprocessed;
                        }
                    }

                    if (!mMailboxNamePattern.Matches(lList.MailboxName.Name)) return eProcessDataResult.notprocessed;

                    MailboxList.Store(lList.EncodedMailboxName, lList.MailboxName, lList.MailboxFlags);

                    return eProcessDataResult.observed;
                }










                public static class cTests
                {
                    [Conditional("DEBUG")]
                    public static void Tests(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewGeneric($"{nameof(cCommandHookList)}.{nameof(cTests)}.{nameof(Tests)}");

                        cCapabilities lCD = new cCapabilities();
                        lCD.Set("children");
                        lCD.Set("list-extended");
                        lCD.Set("list-status");
                        lCD.Set("special-use");
                        cCapability lC = new cCapability(lCD, new cCapabilities(), 0);


                        cCommandHookList lRDP;
                        cMailboxList lMailboxList;
                        cMailboxList.cItem lMailboxListItem;

                        // 1: test the LIST "" "" response
                        lRDP = new cCommandHookList(new cMailboxNamePattern("", "", null), lC, 0);
                        ZProcess(lRDP, "LIST () \"/\" \"\"", true, "1.1", lContext);
                        ZProcess(lRDP, "LIST () \"/\" fred", false, "1.2", lContext);
                        lMailboxList = lRDP.MailboxList;
                        if (lMailboxList.Count != 1) throw new cTestsException($"{nameof(cCommandHookList)}.1.r1");
                        lMailboxListItem = lMailboxList.FirstItem();
                        if (lMailboxListItem.MailboxName.Name != "") throw new cTestsException($"{nameof(cCommandHookList)}.1.r2");
                        if (lMailboxListItem.MailboxName.Delimiter != '/') throw new cTestsException($"{nameof(cCommandHookList)}.1.r3");
                        if (lMailboxListItem.Flags != 0) throw new cTestsException($"{nameof(cCommandHookList)}.1.r4");
                        if (lMailboxListItem.Status != null) throw new cTestsException($"{nameof(cCommandHookList)}.1.r5");

                        // 2: test some flags, test co-dependant flags, sending of a mailbox twice, childinfo, 
                        lRDP = new cCommandHookList(new cMailboxNamePattern("fred/", "%", '/'), lC, 0);
                        ZProcess(lRDP, "LIST (\\Marked \\NoInferiors) \"/\" \"inbox\"", false, "2.1", lContext);
                        ZProcess(lRDP, "LIST (\\Subscribed \\NonExistent) \"/\" \"Fruit/Peach\"", false, "2.2", lContext);
                        ZProcess(lRDP, "LIST (\\Subscribed \\NonExistent) \"/\" \"fred/Fruit/Peach\"", false, "2.3", lContext);
                        ZProcess(lRDP, "LIST (\\Subscribed \\NonExistent) \"/\" \"fred/Peach\"", true, "2.4", lContext); // subscribed, non-existent, => noselect - children unknown
                        ZProcess(lRDP, "LIST (\\HasChildren) \"/\" \"fred/Fruit\"", true, "2.5", lContext); // haschildren
                        ZProcess(lRDP, "LIST (\\HasNoChildren) \"/\" \"fred/Tofu\"", true, "2.6", lContext); // hasnochildren
                        ZProcess(lRDP, "LIST (\\Marked) \"/\" \"fred/Tofu\"", true, "2.7", lContext); // add marked to the above
                        ZProcess(lRDP, "LIST () \"/\" \"fred/Bread\" (\"CHILDINFO\" (\"SUBSCRIBED\"))", true, "2.8", lContext); // subscribed children
                        ZProcess(lRDP, "LIST () \"/\" \"fred/Tea\" (\"CHILDINFO\" (\"x-feature\" \"y-feature\" \"SUBSCRIBED\" \"z-freature\"))", true, "2.9", lContext); // subscribed children
                        ZProcess(lRDP, "LIST () \"/\" \"fred/Coffee\" (tag1 1 tag2 0 tag3 1,2,3:7,5 tag4 (d (e) (f (g h i) j (k l)) m) \"CHiLDiNFO\" ((a b c) \"SUBSCRiBED\" (d e f)) tag6 6)", true, "2.10", lContext); // subscribed children
                        ZProcess(lRDP, "STATUS fred/Coffee (uidvalidity 12345678 MESSAGES 231 UIDNEXT 44292 UNseen 44)", false, "2.11", lContext); // status
                        ZProcess(lRDP, "STATUS fred/Tofu (uidvalidity 12345679 MESSAGES 233)", false, "2.12", lContext); // status
                        ZProcess(lRDP, "STATUS fred/Tofo (uidvalidity 12345679 MESSAGES 233)", false, "2.13", lContext);
                        ZProcess(lRDP, "STATUS fred/Tofu (uidvaliditx 12345680 MESSAGES 234)", false, "2.14", lContext);
                        ZProcess(lRDP, "STATUS fred/Tofu (uidvalidity 12345681 MESSAGES 235)", false, "2.15", lContext); // status

                        List<cMailboxList.cItem> lItems = new List<cMailboxList.cItem>(lRDP.MailboxList);

                        // peach (sub, non-ex, nosel), fruit (has), tofu (hasno, marked, status (235, 12345681)), bread (sub child), tea (sub child), coffee (sub child, status (231, 12345678, 44)), 
                        if (lItems.Count != 6) throw new cTestsException($"{nameof(cCommandHookList)}.2.r1");


                        lMailboxListItem = lItems[0];
                        if (lMailboxListItem.MailboxName.Name != "fred/Peach" || lMailboxListItem.Flags != (fMailboxFlags.subscribed | fMailboxFlags.nonexistent | fMailboxFlags.noselect) || lMailboxListItem.Status != null) throw new cTestsException($"{nameof(cCommandHookList)}.2.r2");
                        // TO FINISH THIS 


                        // 3: debugging
                        lRDP = new cCommandHookList(new cMailboxNamePattern("INBOX.", "%", '.'), lC, 0);
                        ZProcess(lRDP, "LIST (\\HasNoChildren) \".\" INBOX.fr&IKw-d", true, "3.1", lContext);









                        // 3: the above example but with a *

                        // test the list of a mailbox with a * in the name

                        // test the list of a mailbox with a % in the name

                        // test children, without the capability

                        // check that inbox is converted to uppercase

                        // that a UTF7 encoded mailbox names are decoded
                        //  with and without segments

                        // that a UTF8 encoded mailbox decoded


                        // test lsub
                    }

                    private static void ZProcess(cCommandHookList pRDP, string pResponse, bool pShouldBeProcessed, string pTestNumber, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZProcess), pTestNumber);
                        if (!cBytesCursor.TryConstruct(pResponse, out var lCursor)) throw new cTestsException($"{nameof(cCommandHookList)}.{pTestNumber}.p1");
                        var lResult = pRDP.ProcessData(lCursor, lContext);
                        if (pShouldBeProcessed && lResult != eProcessDataResult.observed) throw new cTestsException($"{nameof(cCommandHookList)}.{pTestNumber}.p2");
                        if (!pShouldBeProcessed && lResult == eProcessDataResult.observed) throw new cTestsException($"{nameof(cCommandHookList)}.{pTestNumber}.p3");
                    }
                }



            }
        }
    }
}