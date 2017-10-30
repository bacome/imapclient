using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public int FetchSizeInBytes(iMessageHandle pHandle, cSinglePartBody pPart)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchSizeInBytes));
            var lTask = ZFetchSizeInBytesAsync(pHandle, pPart, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<int> FetchSizeInBytesAsync(iMessageHandle pHandle, cSinglePartBody pPart)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchSizeInBytesAsync));
            return ZFetchSizeInBytesAsync(pHandle, pPart, lContext);
        }

        private async Task<int> ZFetchSizeInBytesAsync(iMessageHandle pHandle, cSinglePartBody pPart, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchSizeInBytesAsync), pHandle, pPart);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));

            if (lSession.Capabilities.Binary && pPart.Section.TextPart == eSectionTextPart.all && pPart.DecodingRequired != eDecodingRequired.none)
            {
                uint lSizeInBytes;

                if (pHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return (int)lSizeInBytes;

                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    if (pHandle.UID == null) await lSession.FetchBinarySizeAsync(lMC, pHandle, pPart.Section.Part, lContext).ConfigureAwait(false);
                    else await lSession.UIDFetchBinarySizeAsync(lMC, pHandle.Cache.MailboxHandle, pHandle.UID, pPart.Section.Part, lContext).ConfigureAwait(false);
                }

                if (pHandle.BinarySizes.TryGetValue(pPart.Section.Part, out lSizeInBytes)) return (int)lSizeInBytes;

                throw new InvalidOperationException(); // probably expunged
            }

            return (int)pPart.SizeInBytes;
        }
    }
}
