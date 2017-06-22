using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void UIDFetch(cMailboxId pMailboxId, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZUIDFetchAsync(pMailboxId, pUID, pSection, pDecoding, pStream, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task UIDFetchAsync(cMailboxId pMailboxId, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZUIDFetchAsync(pMailboxId, pUID, pSection, pDecoding, pStream, lContext);
        }

        private async Task ZUIDFetchAsync(cMailboxId pMailboxId, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchAsync), pMailboxId, pUID, pSection, pDecoding);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));

            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);
                await lSession.UIDFetchAsync(lMC, pMailboxId, pUID, pSection, pDecoding, pStream, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}