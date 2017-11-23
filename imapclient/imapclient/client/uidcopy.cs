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
        internal cCopyFeedback UIDCopy(iMailboxHandle pSourceMailboxHandle, cUID pSourceUID, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopy), 1);
            var lTask = ZUIDCopyAsync(pSourceMailboxHandle, cUIDList.FromUID(pSourceUID), pDestinationMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal cCopyFeedback UIDCopy(iMailboxHandle pSourceMailboxHandle, IEnumerable<cUID> pSourceUIDs, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopy), 2);
            var lTask = ZUIDCopyAsync(pSourceMailboxHandle, cUIDList.FromUIDs(pSourceUIDs), pDestinationMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cCopyFeedback> UIDCopyAsync(iMailboxHandle pSourceMailboxHandle, cUID pSourceUID, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopyAsync), 1);
            return ZUIDCopyAsync(pSourceMailboxHandle, cUIDList.FromUID(pSourceUID), pDestinationMailboxHandle, lContext);
        }

        internal Task<cCopyFeedback> UIDCopyAsync(iMailboxHandle pSourceMailboxHandle, IEnumerable<cUID> pSourceUIDs, iMailboxHandle pDestinationMailboxHandle)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(UIDCopyAsync), 2);
            return ZUIDCopyAsync(pSourceMailboxHandle, cUIDList.FromUIDs(pSourceUIDs), pDestinationMailboxHandle, lContext);
        }

        private async Task<cCopyFeedback> ZUIDCopyAsync(iMailboxHandle pSourceMailboxHandle, cUIDList pSourceUIDs, iMailboxHandle pDestinationMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCopyAsync), pSourceMailboxHandle, pSourceUIDs, pDestinationMailboxHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pSourceMailboxHandle == null) throw new ArgumentNullException(nameof(pSourceMailboxHandle));
            if (pSourceUIDs == null) throw new ArgumentNullException(nameof(pSourceUIDs));
            if (pDestinationMailboxHandle == null) throw new ArgumentNullException(nameof(pDestinationMailboxHandle));

            if (pSourceUIDs.Count == 0) return null;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                return await lSession.UIDCopyAsync(lMC, pSourceMailboxHandle, pSourceUIDs, pDestinationMailboxHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}