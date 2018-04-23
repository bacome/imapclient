using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal uint FetchSizeInBytes(iMessageHandle pMessageHandle, cSinglePartBody pPart)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchSizeInBytes));
            var lTask = ZFetchSizeInBytesAsync(pMessageHandle, pPart, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<uint> FetchSizeInBytesAsync(iMessageHandle pMessageHandle, cSinglePartBody pPart)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchSizeInBytesAsync));
            return ZFetchSizeInBytesAsync(pMessageHandle, pPart, lContext);
        }

        private async Task<uint> ZFetchSizeInBytesAsync(iMessageHandle pMessageHandle, cSinglePartBody pPart, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchSizeInBytesAsync), pMessageHandle, pPart);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));

            if (lSession.Capabilities.Binary && pPart.Section.TextPart == eSectionTextPart.all && pPart.DecodingRequired != eDecodingRequired.none)
            {
                uint lSizeInBytes;

                if (pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);

                    if (pMessageHandle.UID == null) await lSession.FetchBinarySizeAsync(lMC, pMessageHandle, pPart.Section.Part, true, lContext).ConfigureAwait(false);
                    else
                    {
                        if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                        await lSession.UIDFetchBinarySizeAsync(lMC, pMessageHandle.MessageCache.MailboxHandle, pMessageHandle.UID, pPart.Section.Part, true, lContext).ConfigureAwait(false);
                    }
                }

                if (pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

                if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                else throw new cRequestedIMAPDataNotReturnedException(pMessageHandle);
            }

            return pPart.SizeInBytes;
        }
    }
}
