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
        public void Fetch(iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchBodyAsync(pHandle, pSection, pDecoding, pStream, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task FetchAsync(iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC)
        {
            // note: if it fails bytes could have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchBodyAsync(pHandle, pSection, pDecoding, pStream, pFC, lContext);
        }

        private async Task ZFetchBodyAsync(iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC, cTrace.cContext pParentContext)
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
                using (var lMC = mCancellationManager.GetMethodControl(lContext))
                {
                    var lFBMC = new cFetchBodyMethodControl(lMC, null, null, mFetchBodyWriteConfiguration);
                    await lSession.FetchBodyAsync(lFBMC, pHandle, pSection, pDecoding, pStream, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lFBMC = new cFetchBodyMethodControl(new cMethodControl(pFC.Timeout, pFC.CancellationToken), mEventSynchroniser, pFC.IncrementProgress, pFC.WriteConfiguration ?? mFetchBodyWriteConfiguration);
                await lSession.FetchBodyAsync(lFBMC, pHandle, pSection, pDecoding, pStream, lContext).ConfigureAwait(false);
            }
        }
    
        private class cFetchBodyMethodControl
        {
            public readonly cMethodControl MC;
            private readonly cEventSynchroniser mEventSynchroniser;
            private readonly Action<int> mIncrementProgress;
            public readonly cFetchSizer WriteSizer;

            public cFetchBodyMethodControl(cMethodControl pMC, cEventSynchroniser pEventSynchroniser, Action<int> pIncrementProgress, cFetchSizeConfiguration pWriteConfiguration)
            {
                MC = pMC ?? throw new ArgumentNullException(nameof(pMC));
                if (pWriteConfiguration == null) throw new ArgumentNullException(nameof(pWriteConfiguration));
                mEventSynchroniser = pEventSynchroniser;
                mIncrementProgress = pIncrementProgress;
                WriteSizer = new cFetchSizer(pWriteConfiguration);
            }

            public void IncrementProgress(int pValue, cTrace.cContext pParentContext) => mEventSynchroniser?.FireIncrementProgress(mIncrementProgress, pValue, pParentContext);

            public override string ToString() => $"{nameof(cFetchBodyMethodControl)}({MC},{WriteSizer})";
        }
    }
}