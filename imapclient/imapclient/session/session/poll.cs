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
            public async Task PollAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(PollAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                using (var lBlock = await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false))
                {
                    if (_SelectedMailbox != null) await CheckAsync(pMC, _SelectedMailbox.MailboxId, lContext).ConfigureAwait(false);
                }

                await NoOpAsync(pMC, lContext).ConfigureAwait(false);
            }
        }
    }
}