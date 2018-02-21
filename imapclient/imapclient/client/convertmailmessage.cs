using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private static readonly cBatchSizerConfiguration kConvertMailMessageMemoryStreamReadConfiguration = new cBatchSizerConfiguration(10000, 10000, 1, 10000); // 10k chunks

        public cMultiPartAppendData ConvertMailMessage(TempFileCollection pTempFileCollection, MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null, cQuotedPrintableEncodeConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessage));
            var lTask = ZConvertMailMessageAsync(pTempFileCollection, pMailMessage, pFlags, pReceived, pEncoding, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cMultiPartAppendData> ConvertMailMessageAsync(TempFileCollection pTempFileCollection, MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null, cQuotedPrintableEncodeConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(QuotedPrintableEncodeAsync), 1);
            return ZConvertMailMessageAsync(pTempFileCollection, pMailMessage, pFlags, pReceived, pEncoding, pConfiguration, lContext);
        }

        private async Task<cMultiPartAppendData> ZConvertMailMessageAsync(TempFileCollection pTempFileCollection, MailMessage pMailMessage, cStorableFlags pFlags, DateTime? pReceived, Encoding pEncoding, cQuotedPrintableEncodeConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageAsync), pFlags, pReceived, pConfiguration);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            // check required parameters

            if (pTempFileCollection == null) throw new ArgumentNullException(nameof(pTempFileCollection));
            if (pMailMessage == null) throw new ArgumentNullException(nameof(pMailMessage));

            // check encoding 
            //  while it is possible to specify an encoding when creating an address, it is not possible to find out what the specified encoding was (MailAddress has no 'Encoding' property)
            //   otherwise I'd check the address encodings also

            if (pMailMessage == null) throw new ArgumentNullException(nameof(pMailMessage));

            Encoding lEncoding = pMailMessage.HeadersEncoding ?? pMailMessage.SubjectEncoding ?? pEncoding;

            if (pMailMessage.HeadersEncoding != null && !pMailMessage.HeadersEncoding.Equals(lEncoding) ||
                pMailMessage.SubjectEncoding != null && !pMailMessage.SubjectEncoding.Equals(lEncoding) ||
                pEncoding != null && !pEncoding.Equals(lEncoding)) throw new ArgumentOutOfRangeException(nameof(pMailMessage), kArgumentOutOfRangeExceptionMessage.ContainsMixedEncodings);

            // convert

            List<cAppendDataPart> lParts;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    lParts = await ZZConvertMailMessageAsync(lMC, pTempFileCollection, pMailMessage, null, mQuotedPrintableEncodeReadWriteConfiguration, mQuotedPrintableEncodeReadWriteConfiguration, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                lParts = await ZZConvertMailMessageAsync(lMC, pTempFileCollection, pMailMessage, pConfiguration.Increment, pConfiguration.ReadConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, pConfiguration.WriteConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, lContext).ConfigureAwait(false);
            }

            // return

            return new cMultiPartAppendData(pFlags, pReceived, lParts.AsReadOnly(), pEncoding);
        }

        private async Task<List<cAppendDataPart>> ZZConvertMailMessageAsync(cMethodControl pMC, TempFileCollection pTempFileCollection, MailMessage pMailMessage, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZConvertMailMessageAsync), pMC);

            var lParts = new List<cAppendDataPart>();

            // convert the properties to parts 

            if (pMailMessage.Bcc.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("bcc", pMailMessage.Bcc));
            if (pMailMessage.CC.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("cc", pMailMessage.CC));
            if (pMailMessage.From != null) lParts.Add(new cHeaderFieldAppendDataPart("from", pMailMessage.From));
            lParts.Add(new cHeaderFieldAppendDataPart("importance", pMailMessage.Priority.ToString()));
            if (pMailMessage.ReplyToList.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("reply-to", pMailMessage.ReplyToList));
            if (pMailMessage.Sender != null) lParts.Add(new cHeaderFieldAppendDataPart("sender", pMailMessage.Sender));
            if (pMailMessage.Subject != null) lParts.Add(new cHeaderFieldAppendDataPart("subject", pMailMessage.Subject));
            if (pMailMessage.To.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("to", pMailMessage.To));

            // add customer headers

            for (int i = 0; i < pMailMessage.Headers.Count; i++)
            {
                var lName = pMailMessage.Headers.GetKey(i);
                var lValues = pMailMessage.Headers.GetValues(i);

                // this is the list from mailmessage.headers documentation (less date)
                if (!lName.Equals("bcc", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("cc", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("content-id", StringComparison.InvariantCultureIgnoreCase) &&
                    !lName.Equals("content-location", StringComparison.InvariantCultureIgnoreCase) &&
                    !lName.Equals("content-transfer-encoding", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("content-type", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("from", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("importance", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("mime-version", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("priority", StringComparison.InvariantCultureIgnoreCase) && // -
                    !lName.Equals("reply-to", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("sender", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("subject", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("to", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("x-priority", StringComparison.InvariantCultureIgnoreCase) // -
                    )
                {
                    foreach (var lValue in lValues) lParts.Add(new cHeaderFieldAppendDataPart(lName, lValue));
                }
            }

            // add mime headers

            string lBoundaryBase = Guid.NewGuid().ToString();

            lParts.Add(new cHeaderFieldAppendDataPart("mime-version", "1.0"));

            lParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[]
                    {
                        "multipart/mixed",
                        new cHeaderFieldMIMEParameter("boundary", ZConvertMailMessageBoundary(0, lBoundaryBase, out var lDelimiter, out var lCloseDelimiter))
                    }));

            lParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit", "8bit"));

            // add blank line and preamble
            lParts.Add("\r\nThis is a multi-part message in MIME format.");

            // opening delimiter
            lParts.Add(lDelimiter);

            if (pMailMessage.AlternateViews.Count > 0)
            {
                // multipart/alternative
                throw new NotImplementedException();
            }
            else await ZConvertMailMessagePlainTextAsync(pMC, pMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lContext).ConfigureAwait(false);

            // attachments

            if (pMailMessage.Attachments.Count > 0) throw new NotImplementedException();

            /*
            foreach (var lAttachment in pMessage.Attachments)
            {
                ;?;
            } */

            // close-delimiter
            lParts.Add(lCloseDelimiter);

            // done
            return lParts;
        }

        private string ZConvertMailMessageBoundary(int pPart, string pBoundaryBase, out cLiteralAppendDataPart rDelimiter, out cLiteralAppendDataPart rCloseDelimiter)
        {
            string lBoundary = $"=_{pPart}_{pBoundaryBase}";
            rDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "\r\n");
            rCloseDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "--\r\n");
            return lBoundary;
        }

        private async Task ZConvertMailMessagePlainTextAsync(cMethodControl pMC, MailMessage pMailMessage, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, List<cAppendDataPart> pParts, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessagePlainTextAsync), pMC);

            Encoding lEncoding = pMailMessage.BodyEncoding ?? Encoding.ASCII;

            pParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[]
                    {
                        "text/plain",
                        new cHeaderFieldMIMEParameter("charset", lEncoding.WebName)
                    }));

            bool lBase64;

            using (MemoryStream lInput = new MemoryStream(lEncoding.GetBytes(pMailMessage.Body)), lOutput = new MemoryStream())
            {
                if (pMailMessage.BodyTransferEncoding == TransferEncoding.Base64) lBase64 = true;
                else
                {
                    int lQuotedPrintableLength = await ZZQuotedPrintableEncodeAsync(pMC, lInput, kQuotedPrintableEncodeDefaultSourceType, eQuotedPrintableEncodeQuotingRule.EBCDIC, lOutput, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);

                    if (pMailMessage.BodyTransferEncoding == TransferEncoding.QuotedPrintable) lBase64 = false;
                    else
                    {
                        var lBase64Length = cBase64Encoder.EncodedLength(lInput.Length);

                        if (lBase64Length < lQuotedPrintableLength)
                        {
                            lBase64 = true;

                            lInput.Position = 0;
                            lOutput.Position = 0;
                            lOutput.SetLength(lBase64Length);
                        }
                        else lBase64 = false;
                    }
                }

                if (lBase64)
                {
                    pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "base64"));

                    using (var lBase64Encoder = new cBase64Encoder(lInput, kConvertMailMessageMemoryStreamReadConfiguration))
                    {
                        await lBase64Encoder.CopyToAsync(lOutput).ConfigureAwait(false);
                    }
                }
                else pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "quoted-printable"));

                pParts.Add(cAppendDataPart.CRLF);
                pParts.Add(new cLiteralAppendDataPart(new cBytes(lOutput.ToArray())));
            }
        }

    }
}
 