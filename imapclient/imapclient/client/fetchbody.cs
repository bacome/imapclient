using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Fetch(cMailboxId pMailboxId, iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchAsync(pMailboxId, pHandle, pSection, pDecoding, pStream, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task FetchAsync(cMailboxId pMailboxId, iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchAsync(pMailboxId, pHandle, pSection, pDecoding, pStream, lContext);
        }

        private async Task ZFetchAsync(cMailboxId pMailboxId, iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchAsync), pMailboxId, pHandle, pSection, pDecoding);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));

            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);
                await lSession.FetchAsync(lMC, pMailboxId, pHandle, pSection, pDecoding, pStream, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}