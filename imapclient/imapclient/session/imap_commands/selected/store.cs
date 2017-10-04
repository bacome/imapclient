using System;
using System.Collections.Generic;
using System.Linq;
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
            public async Task<cMessageHandleList> ZStoreAsync(cMethodControl pMC, cMessageHandleList pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZStoreAsync), pMC, pHandles, pOperation, pFlags, pIfUnchangedSinceModSeq);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pHandles);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    Dictionary<uint, cStoreFeedbackItem> lDictionary = new Dictionary<uint, cStoreFeedbackItem>();

                    bool lResolved = false;

                    foreach (var lHandle in pHandles)
                    {
                        var lMSN = lSelectedMailbox.GetMSN(lHandle);
                        lDictionary[lMSN] = new cStoreFeedbackItem(lHandle);
                        if (lMSN != 0) lResolved = true;
                    }

                    if (lResolved)
                    {
                        ;?;





                    }

                    return new cMessageHandleList(from i in lDictionary.Values where !i.Fetched || i.Modified select i.Handle);
                }
            }
        }
    }
}

