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
    /*
    public sealed class cMultiPartAppendDataFactory : IDisposable
    {

        private bool mDisposed = false;
        private readonly cStorableFlags mDefaultFlags;
        private readonly DateTime? mDefaultReceived;
        private readonly Encoding mDefaultEncoding;

        // the conversion may require the use of temporary files for quoted-printable encoding and for streams that can't seek 
        private readonly TempFileCollection mTempFileCollection = new TempFileCollection();

        public cMultiPartAppendDataFactory(cIMAPClient pClient, cStorableFlags pDefaultFlags = null, DateTime? pDefaultReceived = null, Encoding pDefaultEncoding = null)
        {
            mDefaultFlags = pDefaultFlags;
            mDefaultReceived = pDefaultReceived;
            mDefaultEncoding = pDefaultEncoding;
        }

        public cMultiPartAppendData Convert(MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null, cAppendConfiguration pConfiguration = null)
        {
            var lEncoding = ZEncoding(pMailMessage, pEncoding);
            ReadOnlyCollection<cAppendDataPart> lParts = ZConvertAsync(pMailMessage, new cConfiguration(false, pConfiguration)).Result;
            return new cMultiPartAppendData(pFlags, pReceived, lParts, lEncoding);
        }

        public async Task<cMultiPartAppendData> ConvertAsync(MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null, cAppendConfiguration pConfiguration = null)
        {
            var lEncoding = ZEncoding(pMailMessage, pEncoding);
            ReadOnlyCollection<cAppendDataPart> lParts = await ZConvertAsync(pMailMessage, new cConfiguration(true, pConfiguration)).ConfigureAwait(false);
            return new cMultiPartAppendData(pFlags, pReceived, lParts, lEncoding);
        }

        internal cMultiPartAppendData Convert(MailMessage pMailMessage, cStorableFlags pFlags, DateTime? pReceived, Encoding pEncoding, cAppendConfiguration.cMC pMC)
        {
            var lEncoding = ZEncoding(pMailMessage, pEncoding);
            ReadOnlyCollection<cAppendDataPart> lParts = ZConvertAsync(pMailMessage, new cConfiguration(false, pConfiguration)).Result;
            return new cMultiPartAppendData(pFlags, pReceived, lParts, lEncoding);
        }

        internal async Task<cMultiPartAppendData> ConvertAsync(MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null, cAppendConfiguration pConfiguration = null)
        {
            var lEncoding = ZEncoding(pMailMessage, pEncoding);
            ReadOnlyCollection<cAppendDataPart> lParts = await ZConvertAsync(pMailMessage, new cConfiguration(true, pConfiguration)).ConfigureAwait(false);
            return new cMultiPartAppendData(pFlags, pReceived, lParts, lEncoding);
        }

        public void Dispose()
        {
            if (mDisposed) return;
            if (mTempFileCollection != null) mTempFileCollection.Delete();
            mDisposed = true;
        }

        private Encoding ZEncoding(MailMessage pMailMessage, Encoding pEncoding)
        {
            // while it is possible to specify an encoding when creating an address, it is not possible to find out what the specified encoding was (MailAddress has no 'Encoding' property)
            //  otherwise I'd check the address encodings also

            if (pMailMessage == null) throw new ArgumentNullException(nameof(pMailMessage));

            Encoding lEncoding = pMailMessage.HeadersEncoding ?? pMailMessage.SubjectEncoding ?? pEncoding;

            if (pMailMessage.HeadersEncoding != null && !pMailMessage.HeadersEncoding.Equals(lEncoding) ||
                pMailMessage.SubjectEncoding != null && !pMailMessage.SubjectEncoding.Equals(lEncoding) ||
                pEncoding != null && !pEncoding.Equals(lEncoding)) throw new ArgumentOutOfRangeException(nameof(pMailMessage), kArgumentOutOfRangeExceptionMessage.ContainsMixedEncodings);

            return lEncoding;
        }

        private async Task<ReadOnlyCollection<cAppendDataPart>> ZConvertAsync(MailMessage pMailMessage, cConfiguration pConfiguration)
        {
            var lParts = new List<cAppendDataPart>();

            if (pMailMessage.Bcc.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("bcc", pMailMessage.Bcc));
            if (pMailMessage.CC.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("cc", pMailMessage.CC));
            if (pMailMessage.From != null) lParts.Add(new cHeaderFieldAppendDataPart("from", pMailMessage.From));
            lParts.Add(new cHeaderFieldAppendDataPart("importance", pMailMessage.Priority.ToString()));
            if (pMailMessage.ReplyToList.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("reply-to", pMailMessage.ReplyToList));
            if (pMailMessage.Sender != null) lParts.Add(new cHeaderFieldAppendDataPart("sender", pMailMessage.Sender));
            if (pMailMessage.Subject != null) lParts.Add(new cHeaderFieldAppendDataPart("subject", pMailMessage.Subject));
            if (pMailMessage.To.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("to", pMailMessage.To));

            var lDate = pMailMessage.Headers["date"];
            if (lDate != null) lParts.Add(new cHeaderFieldAppendDataPart("date", lDate));

            ZConvertCustomHeaders(pMailMessage.Headers, lParts);

            // mime headers

            string lBoundaryBase = Guid.NewGuid().ToString();

            lParts.Add(new cHeaderFieldAppendDataPart("mime-version", "1.0"));

            lParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[]
                    {
                        "multipart/mixed",
                        new cHeaderFieldMIMEParameter("boundary", ZBoundary(0, lBoundaryBase, out var lDelimiter, out var lCloseDelimiter))
                    }));

            lParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit", "8bit"));

            // blank line and preamble
            lParts.Add("\r\nThis is a multi-part message in MIME format.");

            // opening delimiter
            lParts.Add(lDelimiter);

            if (pMailMessage.AlternateViews.Count > 0)
            {
                // multipart/alternative
                throw new NotImplementedException();
            }
            else await ZConvertPlainTextAsync(pMailMessage, lParts, pConfiguration).ConfigureAwait(false);

            // attachments

            if (pMailMessage.Attachments.Count > 0) throw new NotImplementedException();

            /*
            foreach (var lAttachment in pMessage.Attachments)
            {
                ;?;
            } 

            // close-delimiter
            lParts.Add(lCloseDelimiter);

            // done
            return lParts.AsReadOnly();
        }

        private void ZConvertCustomHeaders(NameValueCollection pHeaders, List<cAppendDataPart> pParts)
        {
            for (int i = 0; i < pHeaders.Count; i++)
            {
                var lName = pHeaders.GetKey(i);
                var lValues = pHeaders.GetValues(i);

                // this is the list from mailmessage.headers documentation
                if (!lName.Equals("bcc", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("cc", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("content-id", StringComparison.InvariantCultureIgnoreCase) &&
                    !lName.Equals("content-location", StringComparison.InvariantCultureIgnoreCase) &&
                    !lName.Equals("content-transfer-encoding", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("content-type", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("date", StringComparison.InvariantCultureIgnoreCase) && // k
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
                    foreach (var lValue in lValues) pParts.Add(new cHeaderFieldAppendDataPart(lName, lValue));
                }
            }
        }

        private string ZBoundary(int pPart, string pBoundaryBase, out cLiteralAppendDataPart rDelimiter, out cLiteralAppendDataPart rCloseDelimiter)
        {
            string lBoundary = $"=_{pPart}_{pBoundaryBase}";
            rDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "\r\n");
            rCloseDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "--\r\n");
            return lBoundary;
        }

        private async Task ZConvertPlainTextAsync(MailMessage pMailMessage, List<cAppendDataPart> pParts, cConfiguration pConfiguration)
        {
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
                    int lQuotedPrintableLength;
                    if (pConfiguration.Async) lQuotedPrintableLength = await cQuotedPrintableEncoder.EncodeAsync(lInput, lOutput, pConfiguration.Timeout, pConfiguration.CancellationToken, pConfiguration.Increment).ConfigureAwait(false);
                    else lQuotedPrintableLength = cQuotedPrintableEncoder.Encode(lInput, lOutput, pConfiguration.Timeout, pConfiguration.Increment);

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

                    using (var lBase64Encoder = new cBase64Encoder(lInput, kMemoryStreamReadConfiguration))
                    {
                        if (pConfiguration.Async) await lBase64Encoder.CopyToAsync(lOutput).ConfigureAwait(false);
                        else lBase64Encoder.CopyTo(lOutput);
                    }
                }
                else pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "quoted-printable"));

                pParts.Add(cAppendDataPart.CRLF);
                pParts.Add(new cLiteralAppendDataPart(new cBytes(lOutput.ToArray())));
            }
        }
    } */
}