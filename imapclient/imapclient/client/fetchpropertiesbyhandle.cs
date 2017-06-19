using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void FetchProperties(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchProperties));
            var lTask = ZFetchPropertiesAsync(pMailboxId, ZFetchHandles(pHandle), pProperties, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public void FetchProperties(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchProperties));
            var lTask = ZFetchPropertiesAsync(pMailboxId, ZFetchHandles(pHandles), pProperties, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task FetchPropertiesAsync(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchPropertiesAsync));
            return ZFetchPropertiesAsync(pMailboxId, ZFetchHandles(pHandle), pProperties, lContext);
        }

        public Task FetchPropertiesAsync(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchPropertiesAsync));
            return ZFetchPropertiesAsync(pMailboxId, ZFetchHandles(pHandles), pProperties, lContext);
        }

        private cHandleList ZFetchHandles(iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cHandleList(pHandle);
        }

        private cHandleList ZFetchHandles(IList<iMessageHandle> pHandles)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));

            iMessageCache lCache = null;

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains nulls");
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed caches");
            }

            return new cHandleList(pHandles);
        }

        private async Task ZFetchPropertiesAsync(cMailboxId pMailboxId, cHandleList pHandles, fMessageProperties pProperties, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchPropertiesAsync), pMailboxId, pHandles, pProperties);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            if (pHandles.Count == 0) return;

            // must have specified some properties to get, there is no default for fetch
            if ((pProperties & fMessageProperties.allmask) == 0 || (pProperties & fMessageProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException(nameof(pProperties));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);
                await lSession.FetchPropertiesAsync(lMC, pMailboxId, pHandles, pProperties, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

        /*







        ;?;



        public void FetchToStream(cMailboxId pMailboxId, iMessageProperties pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream)
        {
            // if it fails bytes could still have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchToStream));
            var lTask = ZFetchToStreamAsync(pMailboxId, pHandle, pSection, pDecoding, pStream, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task FetchToStreamAsync(cMailboxId pMailboxId, iMessageProperties pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream)
        {
            // if it fails bytes could still have been written to the stream
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchToStreamAsync));
            return ZFetchToStreamAsync(pMailboxId, pHandle, pSection, pDecoding, pStream, lContext);
        }

        private async Task ZFetchToStreamAsync(cMailboxId pMailboxId, iMessageProperties pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchToStreamAsync), pMailboxId, pHandle, pSection, pDecoding);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsSelected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            var lCapability = lSession.Capability;
            if (pDecoding == eDecodingRequired.unknown && !lCapability.Binary) throw new cContentTransferDecodingException(lContext);

            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);
                if (pHandle.UID != null) return await lSession.UIDFetchToStreamAsync(lMC, pMailboxId, pHandle.UID, pSection, pPart, lCapability, pDecoding, pStream, lContext).ConfigureAwait(false);
                return await lSession.FetchToStreamAsync(lMC, pMailboxId, pHandle, pSection, pPart, lCapability, pDecoding, pStream, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

    */
    }
}
