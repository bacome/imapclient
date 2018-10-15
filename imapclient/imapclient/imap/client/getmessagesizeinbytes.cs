using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<uint> GetMessageSizeInBytesAsync(cMethodControl pMC, iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetMessageSizeInBytesAsync), pMC, pMessageHandle);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));

            if (pMessageHandle.Size != null) return pMessageHandle.Size.Value;

            await lSession.FetchCacheItemsAsync(pMC, cMessageHandleList.FromMessageHandle(pMessageHandle), cMessageCacheItems.Size, null, lContext).ConfigureAwait(false);

            if (pMessageHandle.Size == null)
            {
                if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                throw new cRequestedIMAPDataNotReturnedException(pMessageHandle);
            }

            return pMessageHandle.Size.Value;
        }

        internal async Task<uint> GetMessageSizeInBytesAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cMessageUID pMessageUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetMessageSizeInBytesAsync), pMC, pMailboxHandle, pMessageUID);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            if (pMessageUID.MailboxId != pMailboxHandle.MailboxId) throw new ArgumentOutOfRangeException(nameof(pMessageUID));

            if (lSession.PersistentCache.TryGetHeaderCacheItem(pMessageUID, out var lHeaderCacheItem, lContext) && lHeaderCacheItem.Size != null) return lHeaderCacheItem.Size.Value;

            var lMessageHandles = await lSession.UIDFetchCacheItemsAsync(pMC, pMailboxHandle, cUIDList.FromUID(pMessageUID.UID), cMessageCacheItems.Size, null, lContext).ConfigureAwait(false);

            if (lMessageHandles.Count == 0) throw new cRequestedIMAPDataNotReturnedException(pMailboxHandle, pMessageUID.UID);
            if (lMessageHandles.Count != 1) throw new cInternalErrorException(lContext);

            var lMessageHandle = lMessageHandles[0];

            if (lMessageHandle.Size == null) throw new cRequestedIMAPDataNotReturnedException(pMailboxHandle, pMessageUID.UID);

            return lMessageHandle.Size.Value;
        }
    }
}
