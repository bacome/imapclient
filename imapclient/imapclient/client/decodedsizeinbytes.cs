﻿using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal uint? DecodedSizeInBytes(iMessageHandle pMessageHandle, cSinglePartBody pPart)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(DecodedSizeInBytes));
            var lTask = ZDecodedSizeInBytesAsync(pMessageHandle, pPart, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<uint?> DecodedSizeInBytesAsync(iMessageHandle pMessageHandle, cSinglePartBody pPart)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(DecodedSizeInBytesAsync));
            return ZDecodedSizeInBytesAsync(pMessageHandle, pPart, lContext);
        }

        private async Task<uint?> ZDecodedSizeInBytesAsync(iMessageHandle pMessageHandle, cSinglePartBody pPart, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZDecodedSizeInBytesAsync), pMessageHandle, pPart);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));

            if (pPart.DecodingRequired == eDecodingRequired.none) return pPart.SizeInBytes;
            if (!lSession.Capabilities.Binary) return null;
            if (pPart.Section.TextPart != eSectionTextPart.all) return null;

            uint lSizeInBytes;

            if (pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);

                if (pMessageHandle.UID == null) await lSession.FetchBinarySizeAsync(lMC, pMessageHandle, pPart.Section.Part, false, lContext).ConfigureAwait(false);
                else
                {
                    if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                    await lSession.UIDFetchBinarySizeAsync(lMC, pMessageHandle.MessageCache.MailboxHandle, pMessageHandle.UID, pPart.Section.Part, false, lContext).ConfigureAwait(false);
                }
            }

            if (pMessageHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return lSizeInBytes;

            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            return null;
        }
    }
}
