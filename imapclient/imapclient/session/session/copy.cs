using System;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public Task<cCopyFeedback> CopyAsync(cMethodControl pMC, cMessageHandleList pSourceHandles, iMailboxHandle pDestinationHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CopyAsync), pMC, pSourceHandles, pDestinationHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pSourceHandles == null) throw new ArgumentNullException(nameof(pSourceHandles));
                if (pDestinationHandle == null) throw new ArgumentNullException(nameof(pDestinationHandle));

                if (pSourceHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pSourceHandles));

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pSourceHandles); // to be repeated inside the select lock

                var lDestinationItem = mMailboxCache.CheckHandle(pDestinationHandle);

                if (pSourceHandles.TrueForAll(h => h.UID != null)) return ZUIDCopyAsync(pMC, lSelectedMailbox.Handle, pSourceHandles[0].UID.UIDValidity, new cUIntList(from h in pSourceHandles select h.UID.UID), lDestinationItem, lContext);
                else return ZCopyAsync(pMC, pSourceHandles, lDestinationItem, lContext);
            }

            public Task<cCopyFeedback> UIDCopyAsync(cMethodControl pMC, iMailboxHandle pSourceHandle, cUIDList pSourceUIDs, iMailboxHandle pDestinationHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDCopyAsync), pMC, pSourceHandle, pSourceUIDs, pDestinationHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pSourceHandle == null) throw new ArgumentNullException(nameof(pSourceHandle));
                if (pSourceUIDs == null) throw new ArgumentNullException(nameof(pSourceUIDs));
                if (pDestinationHandle == null) throw new ArgumentNullException(nameof(pDestinationHandle));

                if (pSourceUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pSourceUIDs));

                uint lSourceUIDValidity = pSourceUIDs[0].UIDValidity;

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pSourceHandle, lSourceUIDValidity); // to be repeated inside the select lock

                var lDestinationItem = mMailboxCache.CheckHandle(pDestinationHandle);

                return ZUIDCopyAsync(pMC, pSourceHandle, lSourceUIDValidity, new cUIntList(from lUID in pSourceUIDs select lUID.UID), lDestinationItem, lContext);
            }
        }
    }
}