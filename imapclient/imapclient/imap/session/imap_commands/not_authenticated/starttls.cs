using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kStartTLSCommandPart = new cTextCommandPart("STARTTLS");

            public async Task StartTLSAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StartTLSAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notauthenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnauthenticated);
                if (mPipeline.TLSInstalled) throw new InvalidOperationException();

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kStartTLSCommandPart);

                    var lHook = new cStartTLSCommandHook(mPipeline);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceVerbose("starttls success");
                        return;
                    }

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cStartTLSCommandHook : cCommandHook
            {
                private readonly cCommandPipeline mPipeline;

                public cStartTLSCommandHook(cCommandPipeline pPipeline) { mPipeline = pPipeline; }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStartTLSCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eCommandResultType.ok) mPipeline.InstallTLS(lContext);
                }
            }
        }
    }
}