using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cCopyFeedback Copy(iMessageHandle pSourceHandle, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Copy), 1);
            var lTask = ZCopyAsync(cMessageHandleList.FromHandle(pSourceHandle), pDestinationHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public cCopyFeedback Copy(IEnumerable<iMessageHandle> pSourceHandles, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Copy), 2);
            var lTask = ZCopyAsync(cMessageHandleList.FromHandles(pSourceHandles), pDestinationHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cCopyFeedback> CopyAsync(iMessageHandle pSourceHandle, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(CopyAsync), 1);
            return ZCopyAsync(cMessageHandleList.FromHandle(pSourceHandle), pDestinationHandle, lContext);
        }

        public Task<cCopyFeedback> CopyAsync(IEnumerable<iMessageHandle> pSourceHandles, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(CopyAsync), 2);
            return ZCopyAsync(cMessageHandleList.FromHandles(pSourceHandles), pDestinationHandle, lContext);
        }

        private async Task<cCopyFeedback> ZCopyAsync(cMessageHandleList pSourceHandles, iMailboxHandle pDestinationHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCopyAsync), pSourceHandles, pDestinationHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pSourceHandles == null) throw new ArgumentNullException(nameof(pSourceHandles));
            if (pDestinationHandle == null) throw new ArgumentNullException(nameof(pDestinationHandle));

            if (pSourceHandles.Count == 0) return null;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                return await lSession.CopyAsync(lMC, pSourceHandles, pDestinationHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}