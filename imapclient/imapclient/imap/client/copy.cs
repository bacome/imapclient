using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cCopyFeedback Copy(iMessageHandle pSourceMessageHandle, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Copy), 1);
            var lTask = ZCopyAsync(cMessageHandleList.FromMessageHandle(pSourceMessageHandle), pDestinationMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal cCopyFeedback Copy(IEnumerable<iMessageHandle> pSourceMessageHandles, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Copy), 2);
            var lTask = ZCopyAsync(cMessageHandleList.FromMessageHandles(pSourceMessageHandles), pDestinationMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cCopyFeedback> CopyAsync(iMessageHandle pSourceMessageHandle, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(CopyAsync), 1);
            return ZCopyAsync(cMessageHandleList.FromMessageHandle(pSourceMessageHandle), pDestinationMailboxHandle, lContext);
        }

        internal Task<cCopyFeedback> CopyAsync(IEnumerable<iMessageHandle> pSourceMessageHandles, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(CopyAsync), 2);
            return ZCopyAsync(cMessageHandleList.FromMessageHandles(pSourceMessageHandles), pDestinationMailboxHandle, lContext);
        }

        private async Task<cCopyFeedback> ZCopyAsync(cMessageHandleList pSourceMessageHandles, iMailboxHandle pDestinationMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCopyAsync), pSourceMessageHandles, pDestinationMailboxHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pSourceMessageHandles == null) throw new ArgumentNullException(nameof(pSourceMessageHandles));
            if (pDestinationMailboxHandle == null) throw new ArgumentNullException(nameof(pDestinationMailboxHandle));

            if (pSourceMessageHandles.Count == 0) return null;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                return await lSession.CopyAsync(lMC, pSourceMessageHandles, pDestinationMailboxHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}