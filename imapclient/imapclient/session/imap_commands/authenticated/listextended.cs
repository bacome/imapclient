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
        private enum eListExtendedSelect { exists, subscribed, subscribedrecursive }
        // existing = mailboxes that exist
        // subscribed = subscribed mailboxes (some of which may not exist)
        // subscribedrecursive = subscribed + mailboxes that aren't subscribed but do have subscribed child mailboxes

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

            public async Task<List<iMailboxHandle>> ListExtendedAsync(cMethodControl pMC, eListExtendedSelect pSelect, bool pRemote, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pStatus, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListExtendedAsync), pMC, pSelect, pRemote, pListMailbox, pDelimiter, pPattern, pStatus);

                // caller needs to determine if list status is supported

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

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

                    if (pSelect == eListExtendedSelect.subscribed) lCommand.Add(kListExtendedCommandPartSubscribed);
                    else if (pSelect == eListExtendedSelect.subscribedrecursive)
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

                    if ((mMailboxCacheData & fMailboxCacheData.subscribed) != 0) lCommand.Add(kListExtendedCommandPartSubscribed);
                    if ((mMailboxCacheData & fMailboxCacheData.children) != 0) lCommand.Add(kListExtendedCommandPartChildren);
                    if ((mMailboxCacheData & fMailboxCacheData.specialuse) != 0 && mCapabilities.SpecialUse) lCommand.Add(kListExtendedCommandPartSpecialUse);

                    if (pStatus)
                    {
                        lCommand.Add(kListExtendedCommandPartStatus);
                        lCommand.AddStatusAttributes(mStatusAttributes);
                    }

                    lCommand.EndList();
                    lCommand.EndList();

                    var lHook = new cListExtendedCommandHook(mMailboxCache, pSelect, pPattern, pStatus);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("listextended success");
                        return lHook.Handles;
                    }

                    fKnownCapabilities lTryIgnoring = 0;

                    if ((mMailboxCacheData & fMailboxCacheData.specialuse) != 0 && mCapabilities.SpecialUse) lTryIgnoring |= fKnownCapabilities.specialuse;
                    if (pStatus) lTryIgnoring |= fKnownCapabilities.liststatus;
                    if (lTryIgnoring == 0) lTryIgnoring |= fKnownCapabilities.listextended;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }

            private class cListExtendedCommandHook : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly eListExtendedSelect mSelect;
                private readonly cMailboxNamePattern mPattern;
                private readonly bool mStatus;
                private readonly List<cMailboxName> mMailboxes = new List<cMailboxName>();
                private readonly int mSequence;

                private readonly Dictionary<cMailboxName, iMailboxHandle> mHandles = new Dictionary<cMailboxName, iMailboxHandle>();

                public cListExtendedCommandHook(cMailboxCache pCache, eListExtendedSelect pSelect, cMailboxNamePattern pPattern, bool pStatus)
                {
                    mCache = pCache;
                    mSelect = pSelect;
                    mPattern = pPattern;
                    mStatus = pStatus;
                    mSequence = pCache.Sequence;
                }

                public List<iMailboxHandle> Handles { get; private set; } = null;

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(ProcessData));

                    if (!(pData is cResponseDataList lList)) return eProcessDataResult.notprocessed;
                    if (!mPattern.Matches(lList.MailboxName.Name)) return eProcessDataResult.notprocessed;

                    switch (mSelect)
                    {
                        case eListExtendedSelect.exists:

                            // if it is non-existent it may be being reported because it has children
                            if ((lList.Flags & fListFlags.nonexistent) == 0 || (lList.Flags & fListFlags.subscribed) == 0 || (lList.Flags & fListFlags.haschildren) != 0)
                            {
                                mMailboxes.Add(lList.MailboxName);
                                return eProcessDataResult.observed;
                            }

                            return eProcessDataResult.notprocessed;

                        case eListExtendedSelect.subscribed:

                            if ((lList.Flags & fListFlags.subscribed) != 0)
                            {
                                mMailboxes.Add(lList.MailboxName);
                                return eProcessDataResult.observed;
                            }

                            return eProcessDataResult.notprocessed;

                        case eListExtendedSelect.subscribedrecursive:

                            if ((lList.Flags & fListFlags.subscribed) != 0 || lList.HasSubscribedChildren)
                            {
                                mMailboxes.Add(lList.MailboxName);
                                return eProcessDataResult.observed;
                            }

                            return eProcessDataResult.notprocessed;

                        default:

                            throw new cInternalErrorException(lContext);
                    }
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType != eCommandResultType.ok) return;

                    if (mSelect == eListExtendedSelect.exists) mCache.ResetListFlags(mPattern, mSequence, lContext);
                    if (mSelect == eListExtendedSelect.subscribed || mSelect == eListExtendedSelect.subscribedrecursive) mCache.ResetLSubFlags(mPattern, mSequence, lContext);
                    if (mStatus) mCache.ResetStatus(mPattern, mSequence, lContext);

                    Handles = mCache.GetHandles(mMailboxes);
                }
            }
        }
    }
}