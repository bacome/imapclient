using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private fStatusAttributes mStatusDefaultAttributes = fStatusAttributes.messages | fStatusAttributes.unseen;

        public fStatusAttributes StatusDefaultAttributes
        {
            get => mStatusDefaultAttributes;

            set
            {
                if ((value & fStatusAttributes.all) == 0) throw new ArgumentOutOfRangeException(); // must have something returned
                if ((value & fStatusAttributes.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mStatusDefaultAttributes = value;
            }
        }

        public cMailboxStatus Status(cMailboxId pMailboxId, fStatusAttributes pAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Status));
            var lTask = ZStatusAsync(pMailboxId, pAttributes, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cMailboxStatus> StatusAsync(cMailboxId pMailboxId, fStatusAttributes pAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(StatusAsync));
            return ZStatusAsync(pMailboxId, pAttributes, lContext);
        }

        private async Task<cMailboxStatus> ZStatusAsync(cMailboxId pMailboxId, fStatusAttributes pAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZStatusAsync), pMailboxId, pAttributes);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            fStatusAttributes lAttributes = pAttributes & fStatusAttributes.all;
            if ((pAttributes & fStatusAttributes.clientdefault) != 0) lAttributes |= mStatusDefaultAttributes;
            if (lAttributes == 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);
                return await lSession.StatusAsync(lMC, pMailboxId, lAttributes, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}