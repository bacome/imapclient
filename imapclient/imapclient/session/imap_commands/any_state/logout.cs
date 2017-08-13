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
            private static readonly cCommandPart kLogoutCommandPart = new cCommandPart("LOGOUT");

            public async Task LogoutAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(LogoutAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState < eConnectionState.notauthenticated || _ConnectionState > eConnectionState.selected) throw new InvalidOperationException();

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kLogoutCommandPart);

                    var lHook = new cLogoutCommandHook(mResponseTextProcessor);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("logout success");
                        if (!lHook.GotBye) throw new cUnexpectedServerActionException(0, "bye not received", lContext);
                        Disconnect(lContext);
                        return;
                    }

                    if (lHook.GotBye) lContext.TraceError("received bye on a failed logout");

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cLogoutCommandHook : cCommandHook
            {
                private static readonly cBytes kByeSpace = new cBytes("BYE ");

                private readonly cResponseTextProcessor mResponseTextProcessor;

                public cLogoutCommandHook(cResponseTextProcessor pResponseTextProcessor)
                {
                    mResponseTextProcessor = pResponseTextProcessor;
                }

                public bool GotBye { get; private set; } = false;

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cLogoutCommandHook), nameof(ProcessData));

                    if (pCursor.SkipBytes(kByeSpace))
                    {
                        cResponseText lResponseText = mResponseTextProcessor.Process(pCursor, eResponseTextType.bye, null, lContext);
                        lContext.TraceVerbose("got bye: {0}", lResponseText);
                        GotBye = true;
                        return eProcessDataResult.processed;
                    }

                    return eProcessDataResult.notprocessed;
                }
            }
        }
    }
}