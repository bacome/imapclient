using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void UIDFetch(iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetch));
            var lTask = ZUIDFetchBodyAsync(pHandle, pUID, pSection, pDecoding, pStream, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task UIDFetchAsync(iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync));
            return ZUIDFetchBodyAsync(pHandle, pUID, pSection, pDecoding, pStream, pFC, lContext);
        }

        private async Task ZUIDFetchBodyAsync(iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchBodyAsync), pHandle, pUID, pSection, pDecoding, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));

            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchBodyMethodControl lMC;
                if (pFC == null) lMC = new cFetchBodyMethodControl(mTimeout, CancellationToken, null, null, mFetchBodyWriteConfiguration);
                else lMC = new cFetchBodyMethodControl(pFC.Timeout, pFC.CancellationToken, mEventSynchroniser, pFC.IncrementProgress, pFC.WriteConfiguration ?? mFetchBodyWriteConfiguration);

                await lSession.UIDFetchBodyAsync(lMC, pHandle, pUID, pSection, pDecoding, pStream, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}