using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<cBodyPart> GetBodyPartAsync(cMethodControl pMC, iMessageHandle pMessageHandle, cSection pSection, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetBodyPartAsync), pMC, pMessageHandle, pSection);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (!pSection.CouldDescribeABodyPart) throw new ArgumentOutOfRangeException(nameof(pSection));

            if (pMessageHandle.BodyStructure == null)
            {
                await lSession.FetchCacheItemsAsync(pMC, cMessageHandleList.FromMessageHandle(pMessageHandle), cMessageCacheItems.BodyStructure, null, lContext).ConfigureAwait(false);

                if (pMessageHandle.BodyStructure == null)
                {
                    if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                    throw new cRequestedIMAPDataNotReturnedException(pMessageHandle);
                }
            }

            if (pMessageHandle.BodyStructure.TryGetBodyPart(pSection, out var lBodyPart)) return lBodyPart;
            return null;
        }

        internal async Task<cBodyPart> GetBodyPartAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cMessageUID pMessageUID, cSection pSection, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetBodyPartAsync), pMC, pMailboxHandle, pMessageUID, pSection);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            if (pMessageUID.MailboxId != pMailboxHandle.MailboxId) throw new ArgumentOutOfRangeException(nameof(pMessageUID));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (!pSection.CouldDescribeABodyPart) throw new ArgumentOutOfRangeException(nameof(pSection));

            cBodyPart lBodyStructure;

            if (lSession.PersistentCache.TryGetHeaderCacheItem(pMessageUID, out var lHeaderCacheItem, lContext) && lHeaderCacheItem.BodyStructure != null) lBodyStructure = lHeaderCacheItem.BodyStructure;
            else
            {
                var lMessageHandles = await lSession.UIDFetchCacheItemsAsync(pMC, pMailboxHandle, cUIDList.FromUID(pMessageUID.UID), cMessageCacheItems.BodyStructure, null, lContext).ConfigureAwait(false);

                if (lMessageHandles.Count == 0) return null;
                if (lMessageHandles.Count != 1) throw new cInternalErrorException(lContext);

                var lMessageHandle = lMessageHandles[0];

                if (lMessageHandle.BodyStructure == null)
                {
                    if (lMessageHandle.Expunged) throw new cMessageExpungedException(lMessageHandle);
                    throw new cRequestedIMAPDataNotReturnedException(lMessageHandle);
                }

                lBodyStructure = lMessageHandle.BodyStructure;
            }

            if (lBodyStructure.TryGetBodyPart(pSection, out var lBodyPart)) return lBodyPart;
            return null;
        }
    }
}
