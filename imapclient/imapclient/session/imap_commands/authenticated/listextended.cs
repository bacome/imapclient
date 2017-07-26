using System;
using System.Collections.Generic;
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

            public async Task<List<iMailboxHandle>> ListExtendedAsync(cMethodControl pMC, bool pSubscribedOnly, bool pRemote, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pStatus, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListExtendedAsync), pMC, pSubscribedOnly, pRemote, pListMailbox, pDelimiter, pPattern, pStatus);

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

                    if (pSubscribedOnly)
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

                    var lHook = new cListExtendedCommandHook(mMailboxCache, pSubscribedOnly, pPattern, pStatus);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("listextended success");
                        return lHook.Handles;
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
                private readonly bool mSubscribedOnly;
                private readonly cMailboxNamePattern mPattern;
                private readonly bool mStatus;
                private readonly int mSequence;

                public cListExtendedCommandHook(cMailboxCache pCache, bool pSubscribedOnly, cMailboxNamePattern pPattern, bool pStatus)
                {
                    mCache = pCache;
                    mSubscribedOnly = pSubscribedOnly;
                    mPattern = pPattern;
                    mStatus = pStatus;
                    mSequence = pCache.Sequence;
                }

                public List<iMailboxHandle> Handles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok)
                    {
                        if (mSubscribedOnly) Handles = mCache.LSub(mPattern, mStatus, mSequence, lContext);
                        else Handles = mCache.List(mPattern, mStatus, mSequence, lContext);
                    }
                }
            }
        }
    }
}