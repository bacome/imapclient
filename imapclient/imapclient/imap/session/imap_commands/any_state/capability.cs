﻿using System;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

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
                if (_ConnectionState != eIMAPConnectionState.notauthenticated && _ConnectionState != eIMAPConnectionState.authenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnecting);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kCapabilityCommandPart);

                    var lCapabilities = mPipeline.Capabilities;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType != eIMAPCommandResultType.ok) throw new cIMAPProtocolErrorException(lResult, 0, lContext);

                    lContext.TraceInformation("capability success");

                    if (ReferenceEquals(lCapabilities, mPipeline.Capabilities)) throw new cUnexpectedIMAPServerActionException(lResult, "capability not received", 0, lContext);

                    ZSetCapabilities(mPipeline.Capabilities, mPipeline.AuthenticationMechanisms, lContext);
                }
            }
        }
    }
}