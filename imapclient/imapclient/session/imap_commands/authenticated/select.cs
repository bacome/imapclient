using System;
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
            private static readonly cCommandPart kSelectCommandPart = new cTextCommandPart("SELECT ");
            private static readonly cCommandPart kSelectCommandPartCondStore = new cTextCommandPart(" (CONDSTORE)");

            public async Task SelectAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SelectAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                var lItem = mMailboxCache.CheckHandle(pHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // get exclusive access to the selected mailbox
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kSelectCommandPart, lItem.MailboxNameCommandPart);
                    if (_Capabilities.CondStore) lBuilder.Add(kSelectCommandPartCondStore);

                    var lHook = new cCommandHookSelect(mMailboxCache, _Capabilities, pHandle, true);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("select success");
                        return;
                    }

                    fCapabilities lTryIgnoring;
                    if (_Capabilities.CondStore) lTryIgnoring = fCapabilities.condstore;
                    if (_Capabilities.QResync) lTryIgnoring = fCapabilities.qresync;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}