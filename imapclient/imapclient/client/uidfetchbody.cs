using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void UIDFetch(iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetch));
            var lTask = ZUIDFetchBodyAsync(pMailboxHandle, pUID, pSection, pDecoding, pStream, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task UIDFetchAsync(iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync));
            return ZUIDFetchBodyAsync(pMailboxHandle, pUID, pSection, pDecoding, pStream, pConfiguration, lContext);
        }

        private async Task ZUIDFetchBodyAsync(iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchBodyAsync), pMailboxHandle, pUID, pSection, pDecoding);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));

            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lWriteSizer = new cBatchSizer(mFetchBodyWriteConfiguration);
                    await lSession.UIDFetchBodyAsync(lMC, pMailboxHandle, pUID, pSection, pDecoding, pStream, null, lWriteSizer, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lWriteSizer = new cBatchSizer(pConfiguration.WriteConfiguration ?? mFetchBodyWriteConfiguration);
                await lSession.UIDFetchBodyAsync(lMC, pMailboxHandle, pUID, pSection, pDecoding, pStream, pConfiguration.Increment, lWriteSizer, lContext).ConfigureAwait(false);
            }
        }
    }
}