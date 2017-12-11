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
        internal void Fetch(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchBodyAsync(pMessageHandle, pSection, pDecoding, pStream, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task FetchAsync(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchBodyAsync(pMessageHandle, pSection, pDecoding, pStream, pConfiguration, lContext);
        }

        private async Task ZFetchBodyAsync(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchBodyAsync), pMessageHandle, pSection, pDecoding);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));

            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lWriteSizer = new cBatchSizer(mFetchBodyWriteConfiguration);
                    await lSession.FetchBodyAsync(lMC, pMessageHandle, pSection, pDecoding, pStream, null, lWriteSizer, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lWriteSizer = new cBatchSizer(pConfiguration.Write ?? mFetchBodyWriteConfiguration);
                await lSession.FetchBodyAsync(lMC, pMessageHandle, pSection, pDecoding, pStream, pConfiguration.Increment, lWriteSizer, lContext).ConfigureAwait(false);
            }
        }
    }
}