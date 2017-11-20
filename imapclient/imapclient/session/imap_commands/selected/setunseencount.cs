﻿using System;
using System.Linq;
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
            private static readonly cCommandPart kSetUnseenCountCommandPart = new cTextCommandPart("SEARCH UNSEEN");

            public async Task<cMessageHandleList> SetUnseenCountAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetUnseenCountAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, null);

                    lBuilder.Add(await mSearchExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // search commands must be single threaded (so we can tell which result is which)

                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSetUnseenCountCommandPart);

                    var lHook = new cSetUnseenCountCommandHook(lSelectedMailbox);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("setunseencount success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(0, "results not received on a successful setunseencount", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed setunseencount");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cSetUnseenCountCommandHook : cCommandHookBaseSearch
            {
                private int mMessageCount;

                public cSetUnseenCountCommandHook(cSelectedMailbox pSelectedMailbox) : base(pSelectedMailbox) { }

                public cMessageHandleList Handles { get; private set; } = null;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    mMessageCount = mSelectedMailbox.Cache.Count;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSetUnseenCountCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eCommandResultType.ok && mMSNs != null) Handles = mSelectedMailbox.SetUnseenCount(mMessageCount, new cUIntList(mMSNs.Distinct()), lContext);
                }
            }
        }
    }
}