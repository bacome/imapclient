﻿using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kCapabilityCommandPart = new cTextCommandPart("CAPABILITY");

            public async Task CapabilityAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CapabilityAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notauthenticated && _ConnectionState != eConnectionState.authenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnecting);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kCapabilityCommandPart);

                    var lCapabilities = mPipeline.Capabilities;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType != eCommandResultType.ok) throw new cProtocolErrorException(lResult, 0, lContext);

                    lContext.TraceInformation("capability success");

                    if (ReferenceEquals(lCapabilities, mPipeline.Capabilities)) throw new cUnexpectedServerActionException(0, "capability not received", lContext);

                    ZSetCapabilities(mPipeline.Capabilities, mPipeline.AuthenticationMechanisms, lContext);
                }
            }
        }
    }
}