using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Fetch(iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchConfiguration pFC)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchBodyAsync(pHandle, pSection, pDecoding, pStream, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task FetchAsync(iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchConfiguration pFC)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchBodyAsync(pHandle, pSection, pDecoding, pStream, pFC, lContext);
        }

        private async Task ZFetchBodyAsync(iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchConfiguration pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchBodyAsync), pHandle, pSection, pDecoding, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));

            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            if (pFC == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lProgress = new cFetchProgress();
                    var lWriteSizer = new cFetchSizer(mFetchBodyWriteConfiguration);
                    await lSession.FetchBodyAsync(lMC, pHandle, pSection, pDecoding, pStream, lProgress, lWriteSizer, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pFC.Timeout, pFC.CancellationToken);
                var lProgress = new cFetchProgress(mEventSynchroniser, pFC.IncrementProgress);
                var lWriteSizer = new cFetchSizer(pFC.WriteConfiguration ?? mFetchBodyWriteConfiguration);
                await lSession.FetchBodyAsync(lMC, pHandle, pSection, pDecoding, pStream, lProgress, lWriteSizer, lContext).ConfigureAwait(false);
            }
        }
    }
}