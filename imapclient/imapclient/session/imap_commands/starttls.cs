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

                if (mConnection.tlsinstalled) throw new InvalidOperationException();

                using (var lCommand = new cCommand())
                {
                    //  note the lack of locking - this is only called during connect

                    lCommand.Add(kStartTLSCommandPart);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceVerbose("starttls success");
                        mConnection.installtls();
                        return;
                    }

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}