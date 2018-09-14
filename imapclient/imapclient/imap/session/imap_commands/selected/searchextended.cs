﻿using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kSearchExtendedCommandPart = new cTextCommandPart("SEARCH RETURN () ");

            public async Task<cMessageHandleList> SearchExtendedAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SearchExtendedAsync), pMC, pMailboxHandle, pFilter);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pFilter.UIDValidity);

                    // special cases
                    if (ReferenceEquals(pFilter, cFilter.All)) return new cMessageHandleList(lSelectedMailbox.MessageCache);
                    if (ReferenceEquals(pFilter, cFilter.None)) return new cMessageHandleList();

                    if (pFilter.ContainsMessageHandles) lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSearchExtendedCommandPart);
                    lBuilder.Add(pFilter, lSelectedMailbox, false, mEncodingPartFactory);

                    var lHook = new cCommandHookSearchExtended(lBuilder.Tag, lSelectedMailbox, false);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended search success");
                        if (lHook.MessageHandles == null) throw new cUnexpectedIMAPServerActionException(lResult, kUnexpectedIMAPServerActionMessage.ResultsNotReceived, fIMAPCapabilities.esearch, lContext);
                        return lHook.MessageHandles;
                    }

                    if (lHook.MessageHandles != null) lContext.TraceError("results received on a failed extended search");

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, fIMAPCapabilities.esearch, lContext);
                    throw new cIMAPProtocolErrorException(lResult, fIMAPCapabilities.esearch, lContext);
                }
            }
        }
    }
}