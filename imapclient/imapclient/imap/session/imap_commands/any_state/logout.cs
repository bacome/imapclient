using System;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kLogoutCommandPart = new cTextCommandPart("LOGOUT");

            public async Task LogoutAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(LogoutAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState < eIMAPConnectionState.notauthenticated || _ConnectionState > eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kLogoutCommandPart);

                    var lHook = new cLogoutCommandHook(mPipeline);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("logout success");
                        if (!lHook.GotBye) throw new cUnexpectedIMAPServerActionException(lResult, "bye not received", 0, lContext);
                        Disconnect(lContext);
                        return;
                    }

                    if (lHook.GotBye) lContext.TraceError("received bye on a failed logout");

                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cLogoutCommandHook : cCommandHook
            {
                private static readonly cBytes kByeSpace = new cBytes("BYE ");

                private readonly cCommandPipeline mPipeline;

                public cLogoutCommandHook(cCommandPipeline pPipeline)
                {
                    mPipeline = pPipeline;
                }

                public bool GotBye { get; private set; } = false;

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cLogoutCommandHook), nameof(ProcessData));

                    if (pData is cResponseDataBye lBye)
                    {
                        GotBye = true;
                        return eProcessDataResult.processed;
                    }

                    return eProcessDataResult.notprocessed;
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cLogoutCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eIMAPCommandResultType.ok) mPipeline.RequestStop(lContext);
                }
            }
        }
    }
}