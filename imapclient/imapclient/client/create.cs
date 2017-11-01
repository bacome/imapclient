﻿using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cMailbox Create(cMailboxName pMailboxName, bool pAsFutureParent)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Create));
            var lTask = ZCreateAsync(pMailboxName, pAsFutureParent, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cMailbox> CreateAsync(cMailboxName pMailboxName, bool pAsFutureParent)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(CreateAsync));
            return ZCreateAsync(pMailboxName, pAsFutureParent, lContext);
        }

        private async Task<cMailbox> ZCreateAsync(cMailboxName pMailboxName, bool pAsFutureParent, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCreateAsync), pMailboxName, pAsFutureParent);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                var lHandle = await lSession.CreateAsync(lMC, pMailboxName, pAsFutureParent, lContext).ConfigureAwait(false);
                return new cMailbox(this, lHandle);
            }
        }
    }
}