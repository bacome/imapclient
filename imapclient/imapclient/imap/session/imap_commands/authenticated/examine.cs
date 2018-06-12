using System;
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
            private static readonly cCommandPart kExamineCommandPart = new cTextCommandPart("EXAMINE ");
            private static readonly cCommandPart kExamineCommandPartCondStore = new cTextCommandPart(" (CONDSTORE)");

            public async Task<cSelectResult> ExamineAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cHeaderCache pHeaderCache, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ExamineAsync), pMC, pMailboxHandle, pHeaderCache);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

                var lItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // get exclusive access to the selected mailbox
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kExamineCommandPart, lItem.MailboxNameCommandPart);
                    if (_Capabilities.CondStore) lBuilder.Add(kExamineCommandPartCondStore);

                    var lHook = new cCommandHookSelect(mMailboxCache, _Capabilities, pMailboxHandle, false, pHeaderCache);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("examine success");
                        return lHook.result;
                    }

                    fIMAPCapabilities lTryIgnoring;
                    if (_Capabilities.CondStore) lTryIgnoring = fIMAPCapabilities.condstore;
                    if (_Capabilities.QResync) lTryIgnoring = fIMAPCapabilities.qresync;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cIMAPProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}