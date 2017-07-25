﻿using System;
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
            private static readonly cCommandPart kSearchCommandPart = new cCommandPart("SEARCH ");

            public async Task<cMessageHandleList> SearchAsync(cMethodControl pMC, iMailboxHandle pHandle, cFilter pFilter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SearchAsync), pMC, pHandle, pFilter);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.selected) throw new InvalidOperationException();

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = ZCheckHandle(pHandle);

                    lCommand.Add(await mSearchExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // search commands must be single threaded (so we can tell which result is which)

                    lCommand.Add(kSearchCommandPart);
                    lCommand.Add(pFilter, false, EnabledExtensions, mEncoding); // if the filter has UIDs in it, this makes the command sensitive to UIDValidity changes

                    var lHook = new cSearchCommandHook(lSelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("search success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(0, "results not received on a successful search", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed search");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cSearchCommandHook : cCommandHookBaseSearch
            {
                private cSelectedMailbox mSelectedMailbox;

                public cSearchCommandHook(cSelectedMailbox pSelectedMailbox)
                {
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                }

                public cMessageHandleList Handles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSearchCommandHook), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok && mMSNs != null)
                    {
                        cMessageHandleList lHandles = new cMessageHandleList();
                        foreach (var lMSN in mMSNs.ToSortedUniqueList()) lHandles.Add(mSelectedMailbox.GetHandle(lMSN));
                        Handles = lHandles;
                    }
                }
            }
        }
    }
}