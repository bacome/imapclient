using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<uint?> GetDecodedSizeInBytesAsync(cMethodControl pMC, iMessageHandle pMessageHandle, cSinglePartBody pPart, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetDecodedSizeInBytesAsync), pMC, pMessageHandle, pPart);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));

            if (pPart.DecodingRequired == eDecodingRequired.none) return pPart.SizeInBytes;
            if (!lSession.Capabilities.Binary) return null;
            if (pPart.Section.TextPart != eSectionTextPart.all) return null;

            uint lSizeInBytes;

            if (pMessageHandle.BinarySizes != null && pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

            if (pMessageHandle.MessageUID == null) await lSession.FetchBinarySizeAsync(pMC, pMessageHandle, pPart.Section.Part, false, lContext).ConfigureAwait(false);
            else await lSession.UIDFetchBinarySizeAsync(pMC, pMessageHandle.MessageCache.MailboxHandle, pMessageHandle.MessageUID.UID, pPart.Section.Part, false, lContext).ConfigureAwait(false);

            if (pMessageHandle.BinarySizes != null && pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

            return null;
        }

        internal async Task<uint?> GetDecodedSizeInBytesAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cMessageUID pMessageUID, cSinglePartBody pPart, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetDecodedSizeInBytesAsync), pMC, pMailboxHandle, pMessageUID, pPart);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            if (pMessageUID.MailboxId != pMailboxHandle.MailboxId) throw new ArgumentOutOfRangeException(nameof(pMessageUID));
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));
            if (pPart.DecodingRequired == eDecodingRequired.none) return pPart.SizeInBytes;
            if (!lSession.Capabilities.Binary) return null;
            if (pPart.Section.TextPart != eSectionTextPart.all) return null;

            uint lSizeInBytes;

            if (lSession.PersistentCache.TryGetHeaderCacheItem(pMessageUID, out var lHeaderCacheItem, lContext) && lHeaderCacheItem.BinarySizes != null && lHeaderCacheItem.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

            var lMessageHandle = await lSession.UIDFetchBinarySizeAsync(pMC, pMailboxHandle, pMessageUID.UID, pPart.Section.Part, false, lContext).ConfigureAwait(false);

            if (lMessageHandle != null && lMessageHandle.BinarySizes != null && lMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

            return null;
        }
    }
}
