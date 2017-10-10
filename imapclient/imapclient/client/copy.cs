using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cCopyFeedback Copy(iMessageHandle pHandle, iMailboxHandle pToHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Copy), 1);
            var lTask = ZCopyAsync(cMessageHandleList.FromHandle(pHandle), pToHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public cCopyFeedback Copy(IEnumerable<iMessageHandle> pHandles, iMailboxHandle pToHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Copy), 2);
            var lTask = ZCopyAsync(cMessageHandleList.FromHandles(pHandles), pToHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cCopyFeedback> CopyAsync(iMessageHandle pHandle, iMailboxHandle pToHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(CopyAsync), 1);
            return ZCopyAsync(cMessageHandleList.FromHandle(pHandle), pToHandle, lContext);
        }

        public Task<cCopyFeedback> CopyAsync(IEnumerable<iMessageHandle> pHandles, iMailboxHandle pToHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(CopyAsync), 2);
            return ZCopyAsync(cMessageHandleList.FromHandles(pHandles), pToHandle, lContext);
        }

        private async Task<cCopyFeedback> ZCopyAsync(cMessageHandleList pHandles, iMailboxHandle pToHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCopyAsync), pHandles, pToHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pToHandle == null) throw new ArgumentNullException(nameof(pToHandle));

            if (pHandles.Count == 0) return new cCopyFeedback();

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                ;?; 
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                if (pHandles.TrueForAll(h => h.UID != null)) return await lSession.UIDCopyAsync(cUIDList.FromUIDs(from h in pHandles select h.UID), pToHandle, lContext).ConfigureAwait(false);
                else return await lSession.CopyAsync(lMC, pHandles, pToHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}