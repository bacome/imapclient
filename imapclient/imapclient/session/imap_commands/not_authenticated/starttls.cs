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
            private static readonly cCommandPart kStartTLSCommandPart = new cCommandPart("STARTTLS");

            public async Task StartTLSAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StartTLSAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notauthenticated) throw new InvalidOperationException("must be not authenticated");
                if (mConnection.TLSInstalled) throw new InvalidOperationException("tls already installed");

                using (var lCommand = new cCommand())
                {
                    //  note the lack of locking - this is only called during connect

                    lCommand.Add(kStartTLSCommandPart);

                    var lHook = new cStartTLSCommandHook(mConnection);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

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
                private readonly cConnection mConnection;

                public cStartTLSCommandHook(cConnection pConnection) { mConnection = pConnection; }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStartTLSCommandHook), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) mConnection.InstallTLS(lContext);
                }
            }
        }
    }
}