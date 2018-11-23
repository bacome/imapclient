﻿using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<cSinglePartBody> GetSinglePartBodyAsync(cMethodControl pMC, iMessageHandle pMessageHandle, cSection pSection, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetSinglePartBodyAsync), pMC, pMessageHandle, pSection);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (!pSection.CouldDescribeASinglePartBody) throw new ArgumentOutOfRangeException(nameof(pSection));

            if (pMessageHandle.BodyStructure == null)
            {
                await lSession.FetchCacheItemsAsync(pMC, cMessageHandleList.FromMessageHandle(pMessageHandle), cMessageCacheItems.BodyStructure, null, lContext).ConfigureAwait(false);

                if (pMessageHandle.BodyStructure == null)
                {
                    if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                    throw new cRequestedIMAPDataNotReturnedException(pMessageHandle);
                }
            }

            if (pMessageHandle.BodyStructure.TryGetSinglePartBody(pSection, out var lSinglePartBody)) return lSinglePartBody;
            return null;
        }

        internal async Task<cSinglePartBody> GetSinglePartBodyAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetSinglePartBodyAsync), pMC, pMailboxHandle, pUID, pSection);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (!pSection.CouldDescribeASinglePartBody) throw new ArgumentOutOfRangeException(nameof(pSection));

            cBodyPart lBodyStructure;

            if (lSession.PersistentCache.TryGetHeaderCacheItem(new cMessageUID(pMailboxHandle.MailboxId, pUID, lSession.UTF8Enabled), out var lHeaderCacheItem, lContext) && lHeaderCacheItem.BodyStructure != null) lBodyStructure = lHeaderCacheItem.BodyStructure;
            else
            {
                var lMessageHandles = await lSession.UIDFetchCacheItemsAsync(pMC, pMailboxHandle, cUIDList.FromUID(pUID), cMessageCacheItems.BodyStructure, null, lContext).ConfigureAwait(false);

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

            if (lBodyStructure.TryGetSinglePartBody(pSection, out var lSinglePartBody)) return lSinglePartBody;
            return null;
        }
    }
}