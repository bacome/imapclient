using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            public async Task FetchAsync(cMethodControl pMC, cMailboxId pMailboxId, iMessageHandle pHandle, cSection pSection, cCapability pCapability, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchAsync), pMC, pMailboxId, pHandle, pSection, pCapability, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                // 
                // note: if the section.textpart <> all then you can't use binary

                ;?;
            }
        }
    }
}