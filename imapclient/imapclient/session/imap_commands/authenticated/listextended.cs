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
            private static readonly cCommandPart kListExtendedCommandPartMailbox = new cCommandPart("\"\"");
            private static readonly cCommandPart kListExtendedCommandPartReturn = new cCommandPart("RETURN");
            private static readonly cCommandPart kListExtendedCommandPartChildren = new cCommandPart("CHILDREN");
            private static readonly cCommandPart kListExtendedCommandPartSpecialUse = new cCommandPart("SPECIAL-USE");
            private static readonly cCommandPart kListExtendedCommandPartStatus = new cCommandPart("STATUS");

            public async Task<List<cMailbox>> ListExtendedAsync(cMethodControl pMC, bool pSubscribed, bool pRemote, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pStatus, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListExtendedAsync), pMC, pSubscribed, pRemote, pListMailbox, pDelimiter, pPattern, pStatus);

                // caller needs to determine if status is supported

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();

                if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
                if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

                if (!mCommandPartFactory.TryAsListMailbox(pListMailbox, pDelimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.BeginList(eListBracketing.none);

                    lCommand.Add(kListExtendedCommandPartList);

                    lCommand.BeginList(eListBracketing.ifany);

                    if (pSubscribed)
                    {
                        lCommand.Add(kListExtendedCommandPartSubscribed);
                        lCommand.Add(kListExtendedCommandPartRecursiveMatch);
                    }

                    if (pRemote) lCommand.Add(kListExtendedCommandPartRemote);

                    lCommand.EndList();

                    lCommand.Add(kListExtendedCommandPartMailbox);
                    lCommand.Add(lListMailboxCommandPart);

                    // return options

                    lCommand.BeginList(eListBracketing.ifany, kListExtendedCommandPartReturn);

                    if ((mMailboxFlagSets & fMailboxFlagSets.subscribed) != 0) lCommand.Add(kListExtendedCommandPartSubscribed);
                    if ((mMailboxFlagSets & fMailboxFlagSets.children) != 0) lCommand.Add(kListExtendedCommandPartChildren);
                    if ((mMailboxFlagSets & fMailboxFlagSets.specialuse) != 0 && _Capability.SpecialUse) lCommand.Add(kListExtendedCommandPartSpecialUse);

                    if (pStatus)
                    {
                        lCommand.Add(kListExtendedCommandPartStatus);
                        lCommand.AddStatusAttributes(_Capability);
                    }

                    lCommand.EndList();
                    lCommand.EndList();

                    var lHook = new cListExtendedCommandHook(mMailboxCache, pSubscribed, pPattern, mMailboxCache.Sequence);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("listextended success");
                        return;
                    }

                    fCapabilities lTryIgnoring = 0;

                    if (pStatus) lTryIgnoring |= fCapabilities.ListStatus;
                    if ((mMailboxFlagSets & fMailboxFlagSets.specialuse) != 0) lTryIgnoring |= fCapabilities.SpecialUse;
                    if (lTryIgnoring == 0) lTryIgnoring |= fCapabilities.ListExtended;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }




            private class cListExtendedCommandHook : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly bool mSubscribed;
                private readonly cMailboxNamePattern mPattern;
                private readonly int mSequence;

                public cListExtendedCommandHook(cMailboxCache pCache, bool pSubscribed, cMailboxNamePattern pPattern, int pSequence)
                {
                    mCache = pCache;
                    mSubscribed = pSubscribed;
                    mPattern = pPattern;
                    mSequence = pSequence;
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok)
                    {
                        if (mSubscribed) ;?; // lsub processing
                        else mCache.Reset(mSelect, mPattern, mSequence, lContext); // list processing
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