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
            private async Task ZUIDFetchAttributesAsync(cMethodControl pMC, iMailboxHandle pHandle, uint pUIDValidity, cUIntList pUIDs, fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                // note that this will fail if the UIDValidity has changed (this is different to the behaviour of standard fetch)
                // note that the caller should have checked that pAttributes contains some attributes to fetch

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchAttributesAsync), pMC, pHandle, pUIDValidity, pUIDs, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
                if (pUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pUIDs));
                if (pAttributes == 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    mMailboxCache.CheckIsSelectedMailbox(pHandle);

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kFetchCommandPartUIDFetchSpace, new cCommandPart(pUIDs.ToSequenceSet()), cCommandPart.Space);
                    lCommand.Add(pAttributes);

                    lCommand.AddUIDValidity(pUIDValidity); // the command is sensitive to UIDValidity changes

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid fetch success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}