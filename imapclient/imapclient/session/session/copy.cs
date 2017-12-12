using System;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public Task<cCopyFeedback> CopyAsync(cMethodControl pMC, cMessageHandleList pSourceMessageHandles, iMailboxHandle pDestinationMailboxHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CopyAsync), pMC, pSourceMessageHandles, pDestinationMailboxHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pSourceMessageHandles == null) throw new ArgumentNullException(nameof(pSourceMessageHandles));
                if (pDestinationMailboxHandle == null) throw new ArgumentNullException(nameof(pDestinationMailboxHandle));

                if (pSourceMessageHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pSourceMessageHandles));

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pSourceMessageHandles); // to be repeated inside the select lock

                var lDestinationItem = mMailboxCache.CheckHandle(pDestinationMailboxHandle);

                if (pSourceMessageHandles.TrueForAll(h => h.UID != null))
                {
                    var lMessageHandle = pSourceMessageHandles.Find(h => h.Expunged);
                    if (lMessageHandle != null) throw new cMessageExpungedException(lMessageHandle);
                    return ZUIDCopyAsync(pMC, lSelectedMailbox.MailboxHandle, pSourceMessageHandles[0].UID.UIDValidity, new cUIntList(from h in pSourceMessageHandles select h.UID.UID), lDestinationItem, lContext);
                }
                else return ZCopyAsync(pMC, pSourceMessageHandles, lDestinationItem, lContext);
            }

            public Task<cCopyFeedback> UIDCopyAsync(cMethodControl pMC, iMailboxHandle pSourceMailboxHandle, cUIDList pSourceUIDs, iMailboxHandle pDestinationMailboxHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDCopyAsync), pMC, pSourceMailboxHandle, pSourceUIDs, pDestinationMailboxHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pSourceMailboxHandle == null) throw new ArgumentNullException(nameof(pSourceMailboxHandle));
                if (pSourceUIDs == null) throw new ArgumentNullException(nameof(pSourceUIDs));
                if (pDestinationMailboxHandle == null) throw new ArgumentNullException(nameof(pDestinationMailboxHandle));

                if (pSourceUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pSourceUIDs));

                uint lSourceUIDValidity = pSourceUIDs[0].UIDValidity;

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pSourceMailboxHandle, lSourceUIDValidity); // to be repeated inside the select lock

                var lDestinationItem = mMailboxCache.CheckHandle(pDestinationMailboxHandle);

                return ZUIDCopyAsync(pMC, pSourceMailboxHandle, lSourceUIDValidity, new cUIntList(from lUID in pSourceUIDs select lUID.UID), lDestinationItem, lContext);
            }
        }
    }
}