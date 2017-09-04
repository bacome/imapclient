using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void UIDFetch(iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetch));
            var lTask = ZUIDFetchBodyAsync(pHandle, pUID, pSection, pDecoding, pStream, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        public Task UIDFetchAsync(iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync));
            return ZUIDFetchBodyAsync(pHandle, pUID, pSection, pDecoding, pStream, pConfiguration, lContext);
        }

        private async Task ZUIDFetchBodyAsync(iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchBodyAsync), pHandle, pUID, pSection, pDecoding);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));

            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lProgress = new cProgress();
                    var lWriteSizer = new cBatchSizer(mFetchBodyWriteConfiguration);
                    await lSession.UIDFetchBodyAsync(lMC, pHandle, pUID, pSection, pDecoding, pStream, lProgress, lWriteSizer, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lProgress = new cProgress(mSynchroniser, pConfiguration.Increment);
                var lWriteSizer = new cBatchSizer(pConfiguration.Write ?? mFetchBodyWriteConfiguration);
                await lSession.UIDFetchBodyAsync(lMC, pHandle, pUID, pSection, pDecoding, pStream, lProgress, lWriteSizer, lContext).ConfigureAwait(false);
            }
        }
    }
}