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
        public cCopyFeedback UIDCopy(iMailboxHandle pSourceHandle, cUID pSourceUID, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopy), 1);
            var lTask = ZUIDCopyAsync(pSourceHandle, cUIDList.FromUID(pSourceUID), pDestinationHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public cCopyFeedback UIDCopy(iMailboxHandle pSourceHandle, IEnumerable<cUID> pSourceUIDs, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopy), 2);
            var lTask = ZUIDCopyAsync(pSourceHandle, cUIDList.FromUIDs(pSourceUIDs), pDestinationHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cCopyFeedback> UIDCopyAsync(iMailboxHandle pSourceHandle, cUID pSourceUID, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopyAsync), 1);
            return ZUIDCopyAsync(pSourceHandle, cUIDList.FromUID(pSourceUID), pDestinationHandle, lContext);
        }

        public Task<cCopyFeedback> UIDCopyAsync(iMailboxHandle pSourceHandle, IEnumerable<cUID> pSourceUIDs, iMailboxHandle pDestinationHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopyAsync), 2);
            return ZUIDCopyAsync(pSourceHandle, cUIDList.FromUIDs(pSourceUIDs), pDestinationHandle, lContext);
        }

        private async Task<cCopyFeedback> ZUIDCopyAsync(iMailboxHandle pSourceHandle, cUIDList pSourceUIDs, iMailboxHandle pDestinationHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCopyAsync), pSourceHandle, pSourceUIDs, pDestinationHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pSourceHandle == null) throw new ArgumentNullException(nameof(pSourceHandle));
            if (pSourceUIDs == null) throw new ArgumentNullException(nameof(pSourceUIDs));
            if (pDestinationHandle == null) throw new ArgumentNullException(nameof(pDestinationHandle));

            if (pSourceUIDs.Count == 0) return null;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                return await lSession.UIDCopyAsync(lMC, pSourceHandle, pSourceUIDs, pDestinationHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}