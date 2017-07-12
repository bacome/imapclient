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
                recursivematch = 1 << 0, // mod-opt
                subscribed = 1 << 1, // base-opt
                remote = 1 << 2, // independent-opt
                specialuse = 1 << 3 // independent-opt (!) [which means you can't ask for all special-use mailboxes no matter where they are?]
            }

            [Flags]
            public enum fListExtendedReturn
            {
                subscribed = 1 << 0,
                children = 1 << 1,
                status = 1 << 2,
                specialuse = 1 << 3
            }

            public async Task ListExtendedAsync(cMethodControl pMC, fListExtendedSelect pSelect, cListPatterns pPatterns, fListExtendedReturn pReturn, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListExtendedAsync), pMC, pSelect, pPatterns, pReturn);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                // if a mod-opt is selected
                if ((pSelect & fListExtendedSelect.recursivematch) != 0)
                {
                    // check that a base-opt is also selected
                    if ((pSelect & fListExtendedSelect.subscribed) == 0) throw new ArgumentOutOfRangeException(nameof(pSelect));
                }

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    ZListExtendedAddCommandParts(pSelect, pPatterns, pReturn, lCommand);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("listextended success");
                        return;
                    }

                    fCapabilities lTryIgnoring = 0;

                    if ((pReturn & fListExtendedReturn.status) != 0) lTryIgnoring |= fCapabilities.ListStatus;
                    if ((pSelect & fListExtendedSelect.specialuse) != 0 || (pReturn & fListExtendedReturn.specialuse) != 0) lTryIgnoring |= fCapabilities.SpecialUse;
                    if (lTryIgnoring == 0) lTryIgnoring |= fCapabilities.ListExtended;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }

            private static void ZListExtendedAddCommandParts(fListExtendedSelect pSelect, cListPatterns pPatterns, fListExtendedReturn pReturn, cCommand pCommand)
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