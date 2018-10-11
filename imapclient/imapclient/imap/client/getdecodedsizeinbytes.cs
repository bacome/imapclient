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

            if (pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out var lSizeInBytes)) return lSizeInBytes;

            if (pMC == null)
            {
                using (var lToken = CancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    return await ZGetDecodedSizeInBytesAsync(lMC, lSession, pMessageHandle, pPart, lContext).ConfigureAwait(false);
                }
            }
            else return await ZGetDecodedSizeInBytesAsync(pMC, lSession, pMessageHandle, pPart, lContext).ConfigureAwait(false);
        }

        private async Task<uint?> ZGetDecodedSizeInBytesAsync(cMethodControl pMC, cSession pSession, iMessageHandle pMessageHandle, cSinglePartBody pPart, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetDecodedSizeInBytesAsync), pMC, pMessageHandle, pPart);

            if (pMessageHandle.MessageUID == null) await pSession.FetchBinarySizeAsync(pMC, pMessageHandle, pPart.Section.Part, false, lContext).ConfigureAwait(false);
            else
            {
                if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                await pSession.UIDFetchBinarySizeAsync(pMC, pMessageHandle.MessageCache.MailboxHandle, pMessageHandle.MessageUID.UID, pPart.Section.Part, false, lContext).ConfigureAwait(false);
            }

            if (pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out var lSizeInBytes)) return lSizeInBytes;

            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            return null;
        }
    }
}
