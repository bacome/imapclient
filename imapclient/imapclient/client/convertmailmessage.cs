using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        private static readonly cBatchSizerConfiguration kConvertMailMessageMemoryStreamReadWriteConfiguration = new cBatchSizerConfiguration(10000, 10000, 1, 10000); // 10k chunks

        public cAppendData ConvertMailMessage(cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessage));
            var lTask = ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessage(pMailMessage), pFlags, pReceived, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count != 1) throw new cInternalErrorException();
            return lResult[0];
        }

        public async Task<cAppendData> ConvertMailMessageAsync(cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessageAsync));
            var lResult = await ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessage(pMailMessage), pFlags, pReceived, pConfiguration, lContext).ConfigureAwait(false);
            if (lResult.Count != 1) throw new cInternalErrorException();
            return lResult[0];
        }

        public List<cAppendData> ConvertMailMessages(cConvertMailMessageDisposables pDisposables, IEnumerable<MailMessage> pMailMessages, cStorableFlags pFlags = null, DateTime? pReceived = null, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessages));
            var lTask = ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessages(pMailMessages), pFlags, pReceived, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cAppendData>> ConvertMailMessagesAsync(cConvertMailMessageDisposables pDisposables, IEnumerable<MailMessage> pMailMessages, cStorableFlags pFlags = null, DateTime? pReceived = null, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessagesAsync));
            return ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessages(pMailMessages), pFlags, pReceived, pConfiguration, lContext);
        }

        private async Task<List<cAppendData>> ZConvertMailMessagesAsync(cConvertMailMessageDisposables pDisposables, cMailMessageList pMailMessages, cStorableFlags pFlags, DateTime? pReceived, cConvertMailMessageConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessagesAsync), pMailMessages, pFlags, pReceived);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pDisposables == null) throw new ArgumentNullException(nameof(pDisposables));
            if (pMailMessages == null) throw new ArgumentNullException(nameof(pMailMessages));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await ZZConvertMailMessagesAsync(lMC, pDisposables, false, pMailMessages, null, null, mQuotedPrintableEncodeReadWriteConfiguration, mQuotedPrintableEncodeReadWriteConfiguration, pFlags, pReceived, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await ZZConvertMailMessagesAsync(lMC, pDisposables, false, pMailMessages, pConfiguration.SetMaximum, pConfiguration.Increment, pConfiguration.ReadConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, pConfiguration.WriteConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, pFlags, pReceived, lContext).ConfigureAwait(false);
            }
        }

        private async Task<cAppendDataList> ZZConvertMailMessagesAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, bool pCheckClient, cMailMessageList pMailMessages, Action<long> pSetMaximum, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration,  cStorableFlags pFlags, DateTime? pReceived, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZConvertMailMessagesAsync), pMC, pMailMessages, pReadConfiguration, pWriteConfiguration, pFlags, pReceived);

            long lToConvert = 0;
            foreach (var lMailMessage in pMailMessages) lToConvert += ZConvertMailMessageValidate(lMailMessage, pCheckClient, lContext);
            mSynchroniser.InvokeActionLong(pSetMaximum, lToConvert, lContext);

            var lMessages = new cAppendDataList();

            foreach (var lMailMessage in pMailMessages)
            {
                var lParts = await ZConvertMailMessageAsync(pMC, pDisposables, lMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);
                lMessages.Add(new cMultiPartAppendData(pFlags, pReceived, lParts.AsReadOnly(), ZConvertMailMessageEncoding(lMailMessage)));
            }

            return lMessages;
        }

        private long ZConvertMailMessageValidate(MailMessage pMailMessage, bool pCheckClient, cTrace.cContext pParentContext)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageValidate));

            long lToConvert = 0;

            // check that there is only one encoding
            //  (note that email addresses include an encoding that should be checked, however there is no 'Encoding' property to use to do so 

            var lEncoding = ZConvertMailMessageEncoding(pMailMessage);

            if (lEncoding != null)
            {
                if (pMailMessage.HeadersEncoding != null && pMailMessage.HeadersEncoding != lEncoding) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.MixedEncodings);
                if (pMailMessage.SubjectEncoding != null && pMailMessage.SubjectEncoding != lEncoding) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.MixedEncodings);
                foreach (var lAttachment in pMailMessage.Attachments) if (lAttachment.NameEncoding != null && lAttachment.NameEncoding != lEncoding) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.MixedEncodings);
            }

            // check for unsupported features
            if (pMailMessage.ReplyTo != null) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.ReplyToNotSupported);

            foreach (var lAlternateView in pMailMessage.AlternateViews)
            {
                lToConvert += ZConvertMailMessageValidateData(lAlternateView, true, pCheckClient);
                foreach (var lLinkedResource in lAlternateView.LinkedResources) lToConvert += ZConvertMailMessageValidateData(lLinkedResource, false, pCheckClient);
            }

            foreach (var lAttachment in pMailMessage.Attachments) lToConvert += ZConvertMailMessageValidateData(lAttachment, false, pCheckClient);

            return lToConvert;
        }

        private Encoding ZConvertMailMessageEncoding(MailMessage pMailMessage)
        {
            if (pMailMessage.HeadersEncoding != null) return pMailMessage.HeadersEncoding;
            if (pMailMessage.SubjectEncoding != null) return pMailMessage.SubjectEncoding;
            foreach (var lAttachment in pMailMessage.Attachments) if (lAttachment.NameEncoding != null) return lAttachment.NameEncoding;
            return null;
        }

        private long ZConvertMailMessageValidateData(AttachmentBase pData, bool pAlternateView, bool pCheckClient)
        {
            if (pData.ContentStream is cMessageDataStream lMessageDataStream)
            {
                if (pCheckClient && ReferenceEquals(lMessageDataStream.Client, this)) throw new cMessageDataClientException();

                // this switch statement identifies cases where the data will not require a client-side encoding
                //  (i.e. where the data can be streamed or catenated direct from the source to the target)
                //
                switch (lMessageDataStream.Decoding)
                {
                    case eDecodingRequired.none:

                        if (pData.TransferEncoding == TransferEncoding.SevenBit || pData.TransferEncoding == TransferEncoding.EightBit) return 0;
                        break;

                    case eDecodingRequired.quotedprintable:

                        if (pData.TransferEncoding == TransferEncoding.Unknown || pData.TransferEncoding == TransferEncoding.QuotedPrintable) return 0;
                        break;

                    case eDecodingRequired.base64:

                        if (pData.TransferEncoding == TransferEncoding.Unknown || pData.TransferEncoding == TransferEncoding.Base64) return 0;
                        break;
                }

                if (!lMessageDataStream.HasKnownLength) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.MessageDataStreamUnknownLength);

                // we will only quoted-printable encode when it has been explicitly requested OR it is text and the choice of encoding has been left up to us
                //
                if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pAlternateView)) return lMessageDataStream.GetKnownLength();
                else return 0;
            }

            if (!pData.ContentStream.CanSeek) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.StreamNotSeekable);
            if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pAlternateView)) return pData.ContentStream.Length;
            else return 0;
        }

        private async Task<List<cAppendDataPart>> ZConvertMailMessageAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageAsync), pMC, pReadConfiguration, pWriteConfiguration);

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

            // add custom headers

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
            lParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit", "8bit"));

            if (pMailMessage.Attachments.Count == 0) await ZConvertMailMessageViewsAsync(pMC, pDisposables, pMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lBoundaryBase, lContext).ConfigureAwait(false);
            else
            {
                lParts.Add(
                    new cHeaderFieldAppendDataPart(
                        "content-type",
                        new cHeaderFieldValuePart[]
                        {
                        "multipart/mixed",
                        new cHeaderFieldMIMEParameter("boundary", ZConvertMailMessageBoundary(0, lBoundaryBase, out var lDelimiter, out var lCloseDelimiter))
                        }));


                // add blank line and preamble
                lParts.Add("\r\nThis is a multi-part message in MIME format.");

                // opening delimiter
                lParts.Add(lDelimiter);

                // views
                await ZConvertMailMessageViewsAsync(pMC, pDisposables, pMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lBoundaryBase, lContext).ConfigureAwait(false);

                // attachments
                foreach (var lAttachment in pMailMessage.Attachments)
                {
                    lParts.Add(lDelimiter);

                    var lContentDisposition = new List<cHeaderFieldValuePart>();

                    if (lAttachment.ContentDisposition.Inline) lContentDisposition.Add("inline");
                    else if (lAttachment.ContentDisposition.DispositionType == null) lContentDisposition.Add("attachment");
                    else lContentDisposition.Add(lAttachment.ContentDisposition.DispositionType);

                    if (lAttachment.ContentDisposition.FileName != null) lContentDisposition.Add(new cHeaderFieldMIMEParameter("filename", lAttachment.ContentDisposition.FileName));
                    if (lAttachment.ContentDisposition.CreationDate != DateTime.MinValue) lContentDisposition.Add(new cHeaderFieldMIMEParameter("creation-date", lAttachment.ContentDisposition.CreationDate));
                    if (lAttachment.ContentDisposition.ModificationDate != null) lContentDisposition.Add(new cHeaderFieldMIMEParameter("modification-date", lAttachment.ContentDisposition.ModificationDate));
                    if (lAttachment.ContentDisposition.ReadDate != null) lContentDisposition.Add(new cHeaderFieldMIMEParameter("read-date", lAttachment.ContentDisposition.ReadDate));
                    if (lAttachment.ContentDisposition.Size >= 0) lContentDisposition.Add(new cHeaderFieldMIMEParameter("size", lAttachment.ContentDisposition.Size));
                    ZAddHeaderFieldMIMEParameters(lAttachment.ContentDisposition.Parameters, lContentDisposition, "filename", "creation-date", "modification-date", "read-date", "size");

                    lParts.Add(new cHeaderFieldAppendDataPart("content-disposition", lContentDisposition));

                    await ZConvertMailMessageConvertDataAsync(pMC, pDisposables, lAttachment, false, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lContext).ConfigureAwait(false);
                }

                // close-delimiter
                lParts.Add(lCloseDelimiter);
            }

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

        private async Task ZConvertMailMessageViewsAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, List<cAppendDataPart> pParts, string pBoundaryBase, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageViewsAsync), pMC, pReadConfiguration, pWriteConfiguration, pBoundaryBase);

            if (pMailMessage.AlternateViews.Count > 0)
            {
                pParts.Add(
                    new cHeaderFieldAppendDataPart(
                        "content-type",
                        new cHeaderFieldValuePart[]
                        {
                            "multipart/alternative",
                            new cHeaderFieldMIMEParameter("boundary", ZConvertMailMessageBoundary(0, pBoundaryBase, out var lDelimiter, out var lCloseDelimiter))
                        }));

                // add blank line and preamble
                pParts.Add("\r\nThis is a multi-part message in MIME format.");

                // opening delimiter
                pParts.Add(lDelimiter);

                // add the body
                await ZConvertMailMessageBodyAsync(pMC, pMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, pParts, lContext).ConfigureAwait(false);

                foreach (var lAlternateView in pMailMessage.AlternateViews)
                {
                    pParts.Add(lDelimiter);

                    if (lAlternateView.LinkedResources.Count == 0)
                    {
                        if (lAlternateView.BaseUri != null) pParts.Add(new cHeaderFieldAppendDataPart("content-base", lAlternateView.BaseUri.ToString())); // TO test (esp. with punycode host)
                        await ZConvertMailMessageConvertDataAsync(pMC, pDisposables, lAlternateView, true, pIncrement, pReadConfiguration, pWriteConfiguration, pParts, lContext).ConfigureAwait(false);
                    }
                    else
                    {
                        pParts.Add(
                            new cHeaderFieldAppendDataPart(
                                "content-type",
                                new cHeaderFieldValuePart[]
                                {
                                    "multipart/related",
                                    new cHeaderFieldMIMEParameter("boundary", ZConvertMailMessageBoundary(0, pBoundaryBase, out var lRelatedDelimiter, out var lRelatedCloseDelimiter))
                                }));

                        // add blank line and preamble
                        pParts.Add("\r\nThis is a multi-part message in MIME format.");

                        // opening delimiter
                        pParts.Add(lRelatedDelimiter);

                        if (lAlternateView.BaseUri != null) pParts.Add(new cHeaderFieldAppendDataPart("content-base", lAlternateView.BaseUri.ToString())); // TO test (esp. with punycode host)
                        await ZConvertMailMessageConvertDataAsync(pMC, pDisposables, lAlternateView, true, pIncrement, pReadConfiguration, pWriteConfiguration, pParts, lContext).ConfigureAwait(false);

                        foreach (var lResource in lAlternateView.LinkedResources)
                        {
                            pParts.Add(lRelatedDelimiter);
                            if (lResource.ContentLink != null) pParts.Add(new cHeaderFieldAppendDataPart("content-location", lResource.ContentLink.ToString())); // TO test (esp. with punycode host)
                            await ZConvertMailMessageConvertDataAsync(pMC, pDisposables, lResource, false, pIncrement, pReadConfiguration, pWriteConfiguration, pParts, lContext).ConfigureAwait(false);
                        }

                        // close-delimiter
                        pParts.Add(lRelatedCloseDelimiter);
                    }
                }

                // close-delimiter
                pParts.Add(lCloseDelimiter);
            }
            else await ZConvertMailMessageBodyAsync(pMC, pMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, pParts, lContext).ConfigureAwait(false);
        }

        private async Task ZConvertMailMessageBodyAsync(cMethodControl pMC, MailMessage pMailMessage, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, List<cAppendDataPart> pParts, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageBodyAsync), pMC);

            Encoding lEncoding = pMailMessage.BodyEncoding ?? Encoding.ASCII;

            string lContentType;
            if (pMailMessage.IsBodyHtml) lContentType = "text/html";
            else lContentType = "text/plain";

            pParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[]
                    {
                        lContentType,
                        new cHeaderFieldMIMEParameter("charset", lEncoding.WebName)
                    }));

            bool lBase64;

            using (MemoryStream lInput = new MemoryStream(lEncoding.GetBytes(pMailMessage.Body)), lOutput = new MemoryStream())
            {
                if (pMailMessage.BodyTransferEncoding == TransferEncoding.Base64) lBase64 = true;
                else
                {
                    // note: the increment and configuration are NOT used here as we know this is an in-memory transformation
                    long lQuotedPrintableLength = await ZZQuotedPrintableEncodeAsync(pMC, lInput, kQuotedPrintableEncodeDefaultSourceType, eQuotedPrintableEncodeQuotingRule.EBCDIC, lOutput, null, kConvertMailMessageMemoryStreamReadWriteConfiguration, kConvertMailMessageMemoryStreamReadWriteConfiguration, lContext).ConfigureAwait(false);

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

                    using (var lBase64Encoder = new cBase64Encoder(lInput, kConvertMailMessageMemoryStreamReadWriteConfiguration))
                    {
                        await lBase64Encoder.CopyToAsync(lOutput).ConfigureAwait(false);
                    }
                }
                else pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "quoted-printable"));

                pParts.Add(cAppendDataPart.CRLF);
                pParts.Add(new cLiteralAppendDataPart(new cBytes(lOutput.ToArray())));
            }
        }

        private async Task ZConvertMailMessageConvertDataAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, AttachmentBase pData, bool pAlternateView, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, List<cAppendDataPart> pParts, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageConvertDataAsync), pMC, pAlternateView, pReadConfiguration, pWriteConfiguration);

            if (pData.ContentId != null) pParts.Add(new cHeaderFieldAppendDataPart("content-id", pData.ContentId));

            var lContentType = new List<cHeaderFieldValuePart>();

            lContentType.Add(pData.ContentType.MediaType);

            if (pData.ContentType.CharSet != null) lContentType.Add(new cHeaderFieldMIMEParameter("charset", pData.ContentType.CharSet));
            if (pData.ContentType.Name != null) lContentType.Add(new cHeaderFieldMIMEParameter("name", pData.ContentType.Name));
            ZAddHeaderFieldMIMEParameters(pData.ContentType.Parameters, lContentType, "charset", "name");

            pParts.Add(new cHeaderFieldAppendDataPart("content-type", lContentType));

            if (pData.ContentStream is cMessageDataStream lMessageDataStream)
            {




                switch (lMessageDataStream.Decoding)
                {
                    case eDecodingRequired.none:

                        if (pData.TransferEncoding == TransferEncoding.SevenBit || pData.TransferEncoding == TransferEncoding.EightBit)
                        {
                            return 0;
                        }

                        break;

                    case eDecodingRequired.quotedprintable:

                        if (pData.TransferEncoding == TransferEncoding.Unknown || pData.TransferEncoding == TransferEncoding.QuotedPrintable)
                        {
                            return 0;
                        }

                        break;

                    case eDecodingRequired.base64:

                        if (pData.TransferEncoding == TransferEncoding.Unknown || pData.TransferEncoding == TransferEncoding.Base64)
                        {
                            return 0;
                        }

                        break;
                }

                if (pData.TransferEncoding == TransferEncoding.SevenBit)
                {
                    pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit"));
                    pParts.Add(cAppendDataPart.CRLF);

                    ZConvertMailMessageAddCatOrStream(pDisposables, lMessageDataStream, pParts);

                    if (lMessageDataStream.Decoding == eDecodingRequired.none && )
                    {
                        // catenate
                    }
                    else pParts.Add(pDisposables.CloneMessageDataStream(lMessageDataStream));

                    return;
                }

                if (pData.TransferEncoding == TransferEncoding.EightBit)
                {
                    pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "8bit"));
                    pParts.Add(cAppendDataPart.CRLF);

                    ;?; ditto

                    pParts.Add(pDisposables.CloneMessageDataStream(lMessageDataStream));
                    return;
                }

                if ()


                if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pAlternateView))
                {
                    sConvertMailMessageQuotedPrintableEncodeResult lResult;

                    using (var lSource = new cMessageDataStream(lMessageDataStream))
                    {
                        lResult = await ZConvertMailMessageQuotedPrintableEncodeAsync(pMC, pDisposables, lSource, pAlternateView, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);
                    }

                    bool lQuotedPrintable;
                    if (pData.TransferEncoding == TransferEncoding.Unknown) lQuotedPrintable = lResult.TempFileLength <= cBase64Encoder.EncodedLength(lMessageDataStream.GetKnownLength());
                    else lQuotedPrintable = true;

                    if (lQuotedPrintable)
                    {
                        pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "quoted-printable"));
                        pParts.Add(cAppendDataPart.CRLF);
                        pParts.Add(new cFileAppendDataPart(lResult.TempFileName));
                        return;
                    }
                }

                pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "base64"));
                pParts.Add(cAppendDataPart.CRLF);
                pParts.Add(new cStreamAppendDataPart(pDisposables.CloneMessageDataStream(lMessageDataStream), true));
                return;
            }

            if (pData.TransferEncoding == TransferEncoding.SevenBit)
            {
                pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit"));
                pParts.Add(cAppendDataPart.CRLF);
                pParts.Add(pData.ContentStream);
                return;
            }

            if (pData.TransferEncoding == TransferEncoding.EightBit)
            {
                pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "8bit"));
                pParts.Add(cAppendDataPart.CRLF);
                pParts.Add(pData.ContentStream);
                return;
            }

            if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pAlternateView))
            {
                pData.ContentStream.Position = 0;
                var lResult = await ZConvertMailMessageQuotedPrintableEncodeAsync(pMC, pDisposables, pData.ContentStream, pAlternateView, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);

                bool lQuotedPrintable;
                if (pData.TransferEncoding == TransferEncoding.Unknown) lQuotedPrintable = lResult.TempFileLength <= cBase64Encoder.EncodedLength(pData.ContentStream.Length);
                else lQuotedPrintable = true;

                if (lQuotedPrintable)
                {
                    pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "quoted-printable"));
                    pParts.Add(cAppendDataPart.CRLF);
                    pParts.Add(new cFileAppendDataPart(lResult.TempFileName));
                    return;
                }
            }

            pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "base64"));
            pParts.Add(cAppendDataPart.CRLF);
            pParts.Add(new cStreamAppendDataPart(pData.ContentStream, true));
            return;
        }

        private void ZConvertMailMessageAddCatOrStream(cConvertMailMessageDisposables pDisposables, cMessageDataStream pMessageDataStream, List<cAppendDataPart> pParts)
        {
            ;?; // note that if the decoding matches the encoding then the decoding has to be removed from the stream included in the appenddata

            if (pMessageDataStream.MessageHandle != null && pMessageDataStream.Part == null && pMessageDataStream.Section == cSection.All)
            {
                pParts.Add(new cMessageAppendDataPart(pMessageDataStream.Client, pMessageDataStream.MessageHandle));
                return;
            }

            ;?;
            if (pMessageDataStream.MessageHandle != null && pMessageDataStream.Part != null)
            {
                pParts.Add(new cMessagePartAppendDataPart(pMessageDataStream.Client, pMessageDataStream.MessageHandle, pMessageDataStream.part));
                return;
            }

            if (pMessageDataStream.MailboxHandle != null && pMessageDataStream.HasKnownLength && (pMessageDataStream.Section.TextPart == eSectionTextPart.all || pMessageDataStream.Section.TextPart == eSectionTextPart.text || pMessageDataStream.Section.TextPart == eSectionTextPart.header))
            {
                pParts.Add(new cUIDSectionAppendDataPart(pMessageDataStream.Client, pMessageDataStream.MailboxHandle, pMessageDataStream.UID, pMessageDataStream.Section, pMessageDataStream.GetKnownLength());
                return;
            }

        }


        private async Task<sConvertMailMessageQuotedPrintableEncodeResult> ZConvertMailMessageQuotedPrintableEncodeAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, Stream pSource, bool pAlternateView, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageQuotedPrintableEncodeAsync), pMC, pAlternateView, pReadConfiguration, pWriteConfiguration);

            eQuotedPrintableEncodeSourceType lSourceType;
            if (pAlternateView) lSourceType = kQuotedPrintableEncodeDefaultSourceType;
            else lSourceType = eQuotedPrintableEncodeSourceType.Binary;

            string lTempFileName = pDisposables.GetTempFileName();
            long lTempFileLength;

            using (var lTempFileStream = new FileStream(lTempFileName, FileMode.Truncate))
            {
                lTempFileLength = await ZZQuotedPrintableEncodeAsync(pMC, pSource, lSourceType, eQuotedPrintableEncodeQuotingRule.EBCDIC, lTempFileStream, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);
            }

            return new sConvertMailMessageQuotedPrintableEncodeResult(lTempFileName, lTempFileLength);
        }

        private void ZAddHeaderFieldMIMEParameters(StringDictionary pParameters, List<cHeaderFieldValuePart> pParts, params string[] pIgnoreKeys)
        {
            foreach (DictionaryEntry lEntry in pParameters)
            {
                string lKey = lEntry.Key as string;

                if (lKey == null) continue;
                foreach (var lIgnoreKey in pIgnoreKeys) if (lKey.Equals(lIgnoreKey, StringComparison.InvariantCultureIgnoreCase)) continue;

                string lValue = lEntry.Value as string;

                pParts.Add(new cHeaderFieldMIMEParameter(lKey, lValue ?? string.Empty));
            }
        }

        private struct sConvertMailMessageQuotedPrintableEncodeResult
        {
            public readonly string TempFileName;
            public readonly long TempFileLength;

            public sConvertMailMessageQuotedPrintableEncodeResult(string pTempFileName, long pTempFileLength)
            {
                TempFileName = pTempFileName;
                TempFileLength = pTempFileLength;
            }
        }
    }
}
 