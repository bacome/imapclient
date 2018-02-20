using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private static readonly cBatchSizerConfiguration kConvertMailMessageMemoryStreamReadConfiguration = new cBatchSizerConfiguration(10000, 10000, 1, 10000); // 10k chunks

        public cMultiPartAppendData ConvertMailMessage(TempFileCollection pTempFileCollection, MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessage));
            var lTask = ZConvertMailMessageAsync(pMailMessage, pTempFileCollection, pFlags, pReceived, pEncoding, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        ;?;


        private async Task<cMultiPartAppendData> ZConvertMailMessageAsync(TempFileCollection pTempFileCollection, MailMessage pMailMessage, cStorableFlags pFlags, DateTime? pReceived, Encoding pEncoding, cConvertMailMessageConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageAsync), pFlags, pReceived, pConfiguration);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pTempFileCollection == null) throw new ArgumentNullException(nameof(pTempFileCollection));
            if (pMailMessage == null) throw new ArgumentNullException(nameof(pMailMessage));

            List<cAppendDataPart> lParts;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    lParts = await ZZConvertMailMessageAsync(lMC, pTempFileCollection, pMailMessage, , mQuotedPrintableEncodeBatchConfiguration, mQuotedPrintableEncodeBatchConfiguration, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                lParts = await ZZConvertMailMessageAsync(lMC, pSource, pSourceType, pQuotingRule, pTarget, pConfiguration.Increment, pConfiguration.ReadConfiguration ?? mQuotedPrintableEncodeBatchConfiguration, pConfiguration.WriteConfiguration ?? mQuotedPrintableEncodeBatchConfiguration, lContext).ConfigureAwait(false);
            }

            return new cMultiPartAppendData(pFlags, pReceived, lParts.AsReadOnly(), pEncoding);
        }

        private async Task<List<cAppendDataPart>> ZZConvertMailMessageAsync(cMethodControl pMC, TempFileCollection pTempFileCollection, MailMessage pMailMessage, Action<int> pIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZConvertMailMessageAsync), pMC);

            ;?;
        }


    }
}