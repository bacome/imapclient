using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /* TODO: remove the copysectiontostreamconfiguration as well
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
        internal void CopySectionToStream(iSectionCacheItemReader pReader, Stream pStream, cCopySectionToStreamConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(CopySectionToStream));
            var lTask = ZCopySectionToStreamAsync(pReader, pStream, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task CopySectionToStreamAsync(iSectionCacheItemReader pReader, Stream pStream, cCopySectionToStreamConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(CopySectionToStreamAsync));
            return ZCopySectionToStreamAsync(pReader, pStream, pConfiguration, lContext);
        }

        private async Task ZCopySectionToStreamAsync(iSectionCacheItemReader pReader, Stream pStream, cCopySectionToStreamConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCopySectionToStreamAsync));

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pReader == null) throw new ArgumentNullException(nameof(pReader));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    await ZZCopySectionToStreamAsync(lMC, pReader, pStream, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lIncrementer = new cIncrementer(mSynchroniser, pConfiguration.Increment, pConfiguration.MaxCallbackFrequency);
                await ZZCopySectionToStreamAsync(lMC, pReader, pStream, lIncrementer, lContext);
                lIncrementer.Increment(lContext);
            }
        }

        private async Task ZZCopySectionToStreamAsync(cMethodControl pMC, iSectionCacheItemReader pReader, Stream pStream, cIncrementer pIncrementer, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZCopySectionToStreamAsync), pMC);

            byte[] lBuffer = new byte[mLocalStreamBufferSize];

            while (true)
            {
                var lBytesRead = await pReader.ReadAsync(pMC, lBuffer, 0, lBuffer.Length, lContext).ConfigureAwait(false);
                if (lBytesRead == 0) return;

                if (pStream.CanTimeout) pStream.WriteTimeout = pMC.Timeout;
                else _ = pMC.Timeout;

                await pStream.WriteAsync(lBuffer, 0, lBytesRead, pMC.CancellationToken);

                pIncrementer.Increment(lBytesRead, lContext);
            }
        } */
    }
}