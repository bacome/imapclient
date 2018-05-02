using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Fetch(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, cSectionCache.cItem.cReaderWriter pReaderWriter, CancellationToken pCancellationToken)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchBodyAsync(pMessageHandle, pSection, pDecoding, pReaderWriter, pCancellationToken, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task FetchAsync(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, cSectionCache.cItem.cReaderWriter pReaderWriter, CancellationToken pCancellationToken)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchBodyAsync(pMessageHandle, pSection, pDecoding, pReaderWriter, pCancellationToken, lContext);
        }

        private async Task ZFetchBodyAsync(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, cSectionCache.cItem.cReaderWriter pReaderWriter, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchBodyAsync), pMessageHandle, pSection, pDecoding);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pReaderWriter == null) throw new ArgumentNullException(nameof(pReaderWriter));

            await lSession.FetchBodyAsync(pMessageHandle, pSection, pDecoding, pReaderWriter, pCancellationToken, lContext).ConfigureAwait(false);
        }
    }
}