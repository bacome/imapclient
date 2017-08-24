﻿using System;
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

            public async Task<List<iMailboxHandle>> ListExtendedAsync(cMethodControl pMC, eListExtendedSelect pSelect, bool pRemote, string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, bool pStatus, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListExtendedAsync), pMC, pSelect, pRemote, pListMailbox, pDelimiter, pPattern, pStatus);

                // caller needs to determine if list status is supported

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
                if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

                if (!mCommandPartFactory.TryAsListMailbox(pListMailbox, pDelimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!mCapabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.BeginList(eListBracketing.none);

                    lBuilder.Add(kListExtendedCommandPartList);

                    lBuilder.BeginList(eListBracketing.ifany);

                    if (pSelect == eListExtendedSelect.subscribed) lBuilder.Add(kListExtendedCommandPartSubscribed);
                    else if (pSelect == eListExtendedSelect.subscribedrecursive)
                    {
                        lBuilder.Add(kListExtendedCommandPartSubscribed);
                        lBuilder.Add(kListExtendedCommandPartRecursiveMatch);
                    }

                    if (pRemote) lBuilder.Add(kListExtendedCommandPartRemote);

                    lBuilder.EndList();

                    lBuilder.Add(kListExtendedCommandPartMailbox);
                    lBuilder.Add(lListMailboxCommandPart);

                    // return options

                    lBuilder.BeginList(eListBracketing.ifany, kListExtendedCommandPartReturn);

                    if ((mMailboxCacheData & fMailboxCacheData.subscribed) != 0) lBuilder.Add(kListExtendedCommandPartSubscribed);
                    if ((mMailboxCacheData & fMailboxCacheData.children) != 0) lBuilder.Add(kListExtendedCommandPartChildren);
                    if ((mMailboxCacheData & fMailboxCacheData.specialuse) != 0 && mCapabilities.SpecialUse) lBuilder.Add(kListExtendedCommandPartSpecialUse);

                    if (pStatus)
                    {
                        lBuilder.Add(kListExtendedCommandPartStatus);
                        lBuilder.AddStatusAttributes(mStatusAttributes);
                    }

                    lBuilder.EndList();
                    lBuilder.EndList();

                    var lHook = new cListExtendedCommandHook(mMailboxCache, pSelect, pPattern, pStatus);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

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
                private readonly cMailboxPathPattern mPattern;
                private readonly bool mStatus;
                private readonly List<cMailboxName> mMailboxes = new List<cMailboxName>();
                private int mSequence;

                private readonly Dictionary<cMailboxName, iMailboxHandle> mHandles = new Dictionary<cMailboxName, iMailboxHandle>();

                public cListExtendedCommandHook(cMailboxCache pCache, eListExtendedSelect pSelect, cMailboxPathPattern pPattern, bool pStatus)
                {
                    mCache = pCache;
                    mSelect = pSelect;
                    mPattern = pPattern;
                    mStatus = pStatus;
                }

                public List<iMailboxHandle> Handles { get; private set; } = null;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(CommandStarted));
                    mSequence = mCache.Sequence;
                }

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListExtendedCommandHook), nameof(ProcessData));

                    if (!(pData is cResponseDataListMailbox lListMailbox)) return eProcessDataResult.notprocessed;
                    if (!mPattern.Matches(lListMailbox.MailboxName.Path)) return eProcessDataResult.notprocessed;

                    switch (mSelect)
                    {
                        case eListExtendedSelect.exists:

                            // if it is non-existent it may be being reported because it has children
                            if ((lListMailbox.Flags & fListFlags.nonexistent) == 0 || (lListMailbox.Flags & fListFlags.subscribed) == 0 || (lListMailbox.Flags & fListFlags.haschildren) != 0)
                            {
                                mMailboxes.Add(lListMailbox.MailboxName);
                                return eProcessDataResult.observed;
                            }

                            return eProcessDataResult.notprocessed;

                        case eListExtendedSelect.subscribed:

                            if ((lListMailbox.Flags & fListFlags.subscribed) != 0)
                            {
                                mMailboxes.Add(lListMailbox.MailboxName);
                                return eProcessDataResult.observed;
                            }

                            return eProcessDataResult.notprocessed;

                        case eListExtendedSelect.subscribedrecursive:

                            if ((lListMailbox.Flags & fListFlags.subscribed) != 0 || lListMailbox.HasSubscribedChildren)
                            {
                                mMailboxes.Add(lListMailbox.MailboxName);
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

                    if (mSelect == eListExtendedSelect.exists) mCache.ResetExists(mPattern, mSequence, lContext);
                    if (mSelect == eListExtendedSelect.subscribed || mSelect == eListExtendedSelect.subscribedrecursive) mCache.ResetLSubFlags(mPattern, mSequence, lContext);
                    if (mStatus) mCache.ResetStatus(mPattern, mSequence, lContext);

                    Handles = mCache.GetHandles(mMailboxes);
                }
            }
        }
    }
}