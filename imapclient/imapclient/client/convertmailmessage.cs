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
        private enum eConvertMailMessageAppendDataPartType { cantbeconverted, message, messagepart, uidsection }

        private static readonly cBatchSizerConfiguration kConvertMailMessageMemoryStreamReadWriteConfiguration = new cBatchSizerConfiguration(10000, 10000, 1, 10000); // 10k chunks

        public cAppendData ConvertMailMessage(cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessage));
            var lTask = ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessage(pMailMessage), pFlags, pReceived, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count != 1) throw new cInternalErrorException($"result count {lResult.Count}", lContext);
            return lResult[0];
        }

        public async Task<cAppendData> ConvertMailMessageAsync(cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertMailMessageAsync));
            var lResult = await ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessage(pMailMessage), pFlags, pReceived, pConfiguration, lContext).ConfigureAwait(false);
            if (lResult.Count != 1) throw new cInternalErrorException($"result count {lResult.Count}", lContext);
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

        private async Task<cAppendDataList> ZZConvertMailMessagesAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, bool pCheckClient, cMailMessageList pMailMessages, Action<long> pSetMaximum, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cStorableFlags pFlags, DateTime? pReceived, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZConvertMailMessagesAsync), pMC, pMailMessages, pReadConfiguration, pWriteConfiguration, pFlags, pReceived);

            long lToConvert = 0;
            foreach (var lMailMessage in pMailMessages) lToConvert += await ZConvertMailMessageValidateAsync(lMailMessage, pCheckClient, lContext).ConfigureAwait(false);
            mSynchroniser.InvokeActionLong(pSetMaximum, lToConvert, lContext);

            var lMessages = new cAppendDataList();

            foreach (var lMailMessage in pMailMessages)
            {
                var lParts = await ZConvertMailMessageAsync(pMC, pDisposables, lMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);
                lMessages.Add(new cMultiPartAppendData(pFlags, pReceived, lParts.AsReadOnly(), ZConvertMailMessageEncoding(lMailMessage)));
            }

            return lMessages;
        }

        private async Task<long> ZConvertMailMessageValidateAsync(MailMessage pMailMessage, bool pCheckClient, cTrace.cContext pParentContext)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageValidateAsync));

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
                lToConvert += await ZConvertMailMessageValidateDataAsync(lAlternateView, true, pCheckClient).ConfigureAwait(false);
                foreach (var lLinkedResource in lAlternateView.LinkedResources) lToConvert += await ZConvertMailMessageValidateDataAsync(lLinkedResource, false, pCheckClient).ConfigureAwait(false);
            }

            foreach (var lAttachment in pMailMessage.Attachments) lToConvert += await ZConvertMailMessageValidateDataAsync(lAttachment, false, pCheckClient).ConfigureAwait(false);

            return lToConvert;
        }

        private Encoding ZConvertMailMessageEncoding(MailMessage pMailMessage)
        {
            if (pMailMessage.HeadersEncoding != null) return pMailMessage.HeadersEncoding;
            if (pMailMessage.SubjectEncoding != null) return pMailMessage.SubjectEncoding;
            foreach (var lAttachment in pMailMessage.Attachments) if (lAttachment.NameEncoding != null) return lAttachment.NameEncoding;
            return null;
        }

        private async Task<long> ZConvertMailMessageValidateDataAsync(AttachmentBase pData, bool pText, bool pCheckClient)
        {
            // returns the length that needs to be converted to quoted printable

            if (pData.ContentStream is cMessageDataStream lMessageDataStream)
            {
                if (pCheckClient && ReferenceEquals(lMessageDataStream.Client, this)) throw new cMessageDataClientException();
                if (ZConvertMailMessageDataStreamToAppendDataPart(lMessageDataStream, pData.TransferEncoding).AppendDataPartType != eConvertMailMessageAppendDataPartType.cantbeconverted) return 0;
                if (!lMessageDataStream.HasKnownLength) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.MessageDataStreamUnknownLength);
                if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pText)) return await lMessageDataStream.GetKnownLengthAsync().ConfigureAwait(false);
                return 0;
            }

            if (!pData.ContentStream.CanSeek) throw new cMailMessageFormException(kMailMessageFormExceptionMessage.StreamNotSeekable);
            if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pText)) return pData.ContentStream.Length;
            return 0;
        }

        private sConvertMailMessageDataStreamToAppendDataPartResult ZConvertMailMessageDataStreamToAppendDataPart(cMessageDataStream pMessageDataStream, TransferEncoding pTransferEncoding)
        {
            eConvertMailMessageAppendDataPartType lAppendDataPartType;

            if (pMessageDataStream.MessageHandle != null && pMessageDataStream.Part == null && pMessageDataStream.Section == cSection.All) lAppendDataPartType = eConvertMailMessageAppendDataPartType.message;
            else if (pMessageDataStream.MessageHandle != null && pMessageDataStream.Part != null) lAppendDataPartType = eConvertMailMessageAppendDataPartType.messagepart;
            else if (pMessageDataStream.MailboxHandle != null && pMessageDataStream.Decoding == eDecodingRequired.none && pMessageDataStream.HasKnownLength) lAppendDataPartType = eConvertMailMessageAppendDataPartType.uidsection;
            else return sConvertMailMessageDataStreamToAppendDataPartResult.CantBeConverted;

            switch (pMessageDataStream.Decoding)
            {
                case eDecodingRequired.none:

                    if (pTransferEncoding == TransferEncoding.SevenBit || pTransferEncoding == TransferEncoding.EightBit) return new sConvertMailMessageDataStreamToAppendDataPartResult(lAppendDataPartType, pTransferEncoding);
                    return sConvertMailMessageDataStreamToAppendDataPartResult.CantBeConverted;

                case eDecodingRequired.quotedprintable:

                    if (pTransferEncoding == TransferEncoding.Unknown || pTransferEncoding == TransferEncoding.QuotedPrintable) return new sConvertMailMessageDataStreamToAppendDataPartResult(lAppendDataPartType, TransferEncoding.QuotedPrintable);
                    return sConvertMailMessageDataStreamToAppendDataPartResult.CantBeConverted;

                case eDecodingRequired.base64:

                    if (pTransferEncoding == TransferEncoding.Unknown || pTransferEncoding == TransferEncoding.Base64) return new sConvertMailMessageDataStreamToAppendDataPartResult(lAppendDataPartType, TransferEncoding.Base64);
                    return sConvertMailMessageDataStreamToAppendDataPartResult.CantBeConverted;
            }

            return sConvertMailMessageDataStreamToAppendDataPartResult.CantBeConverted;
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

            var lBoundarySource = new cConvertMailMessageBoundarySource();

            lParts.Add(new cHeaderFieldAppendDataPart("mime-version", "1.0"));
            lParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit", "8bit"));

            if (pMailMessage.Attachments.Count == 0) await ZConvertMailMessageViewsAsync(pMC, pDisposables, pMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lBoundarySource, lContext).ConfigureAwait(false);
            else
            {
                lParts.Add(
                    new cHeaderFieldAppendDataPart(
                        "content-type",
                        new cHeaderFieldValuePart[]
                        {
                        "multipart/mixed",
                        new cHeaderFieldMIMEParameter("boundary", lBoundarySource.Boundary(out var lDelimiter, out var lCloseDelimiter))
                        }));


                // add blank line and preamble
                lParts.Add("\r\nThis is a multi-part message in MIME format.");

                // opening delimiter
                lParts.Add(lDelimiter);

                // views
                await ZConvertMailMessageViewsAsync(pMC, pDisposables, pMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lBoundarySource, lContext).ConfigureAwait(false);

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
                    ZConvertMailMessageAddHeaderFieldMIMEParameters(lAttachment.ContentDisposition.Parameters, lContentDisposition, "filename", "creation-date", "modification-date", "read-date", "size");

                    lParts.Add(new cHeaderFieldAppendDataPart("content-disposition", lContentDisposition));

                    await ZConvertMailMessageConvertDataAsync(pMC, pDisposables, lAttachment, false, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lContext).ConfigureAwait(false);
                }

                // close-delimiter
                lParts.Add(lCloseDelimiter);
            }

            // done
            return lParts;
        }

        private async Task ZConvertMailMessageViewsAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, List<cAppendDataPart> pParts, cConvertMailMessageBoundarySource pBoundarySource, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageViewsAsync), pMC, pReadConfiguration, pWriteConfiguration, pBoundarySource);

            if (pMailMessage.AlternateViews.Count > 0)
            {
                pParts.Add(
                    new cHeaderFieldAppendDataPart(
                        "content-type",
                        new cHeaderFieldValuePart[]
                        {
                            "multipart/alternative",
                            new cHeaderFieldMIMEParameter("boundary", pBoundarySource.Boundary(out var lDelimiter, out var lCloseDelimiter))
                        }));

                // add blank line and preamble
                pParts.Add("\r\nThis is a multi-part message in MIME format.");

                // opening delimiter
                pParts.Add(lDelimiter);

                // add the body
                await ZConvertMailMessageBodyAsync(pMC, pDisposables, pMailMessage, pParts, lContext).ConfigureAwait(false);

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
                                    new cHeaderFieldMIMEParameter("boundary", pBoundarySource.Boundary(out var lRelatedDelimiter, out var lRelatedCloseDelimiter))
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
            else await ZConvertMailMessageBodyAsync(pMC, pDisposables, pMailMessage, pParts, lContext).ConfigureAwait(false);
        }

        private async Task ZConvertMailMessageBodyAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, MailMessage pMailMessage, List<cAppendDataPart> pParts, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageBodyAsync), pMC);

            Encoding lEncoding = pMailMessage.BodyEncoding ?? Encoding.ASCII;

            string lMediaType;
            if (pMailMessage.IsBodyHtml) lMediaType = "text/html";
            else lMediaType = "text/plain";

            pParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[]
                    {
                        lMediaType,
                        new cHeaderFieldMIMEParameter("charset", lEncoding.WebName)
                    }));

            TransferEncoding lTransferEncoding;
            cAppendDataPart lPart;

            if (pMailMessage.BodyTransferEncoding == TransferEncoding.SevenBit || pMailMessage.BodyTransferEncoding == TransferEncoding.EightBit)
            {
                lTransferEncoding = pMailMessage.BodyTransferEncoding;
                lPart = new cLiteralAppendDataPart(lEncoding.GetBytes(pMailMessage.Body));
            }
            else if (pMailMessage.BodyTransferEncoding == TransferEncoding.QuotedPrintable || pMailMessage.BodyTransferEncoding == TransferEncoding.Unknown)
            {
                var lSource = pDisposables.GetMemoryStream(lEncoding.GetBytes(pMailMessage.Body));
                var lResult = await ZConvertMailMessageQuotedPrintableEncodeAsync(pMC, pDisposables, lSource, true, null, kConvertMailMessageMemoryStreamReadWriteConfiguration, kConvertMailMessageMemoryStreamReadWriteConfiguration, lContext).ConfigureAwait(false);

                if (pMailMessage.BodyTransferEncoding == TransferEncoding.Unknown)
                {
                    if (lResult.TempFileLength <= cBase64Encoder.EncodedLength(lSource.Length)) lTransferEncoding = TransferEncoding.QuotedPrintable;
                    else lTransferEncoding = TransferEncoding.Base64;
                }
                else lTransferEncoding = TransferEncoding.QuotedPrintable;

                if (lTransferEncoding == TransferEncoding.QuotedPrintable) lPart = new cFileAppendDataPart(lResult.TempFileName);
                else lPart = new cStreamAppendDataPart(lSource, true);
            }
            else if (pMailMessage.BodyTransferEncoding == TransferEncoding.Base64)
            {
                lTransferEncoding = TransferEncoding.Base64;
                lPart = new cStreamAppendDataPart(pDisposables.GetMemoryStream(lEncoding.GetBytes(pMailMessage.Body)), true);
            }
            else throw new cInternalErrorException($"bodytransferencoding {pMailMessage.BodyTransferEncoding}", lContext);

            ZConvertMailMessageAddPart(lTransferEncoding, lPart, pParts, lContext);
        }

        private async Task ZConvertMailMessageConvertDataAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, AttachmentBase pData, bool pText, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, List<cAppendDataPart> pParts, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageConvertDataAsync), pMC, pText, pReadConfiguration, pWriteConfiguration);

            if (pData.ContentId != null) pParts.Add(new cHeaderFieldAppendDataPart("content-id", pData.ContentId));

            var lContentType = new List<cHeaderFieldValuePart>();

            lContentType.Add(pData.ContentType.MediaType);

            if (pData.ContentType.CharSet != null) lContentType.Add(new cHeaderFieldMIMEParameter("charset", pData.ContentType.CharSet));
            if (pData.ContentType.Name != null) lContentType.Add(new cHeaderFieldMIMEParameter("name", pData.ContentType.Name));
            ZConvertMailMessageAddHeaderFieldMIMEParameters(pData.ContentType.Parameters, lContentType, "charset", "name");

            pParts.Add(new cHeaderFieldAppendDataPart("content-type", lContentType));

            TransferEncoding lTransferEncoding;
            cAppendDataPart lPart;

            if (pData.ContentStream is cMessageDataStream lMessageDataStream)
            {
                var lResult = ZConvertMailMessageDataStreamToAppendDataPart(lMessageDataStream, pData.TransferEncoding);

                if (lResult.AppendDataPartType == eConvertMailMessageAppendDataPartType.cantbeconverted)
                {
                    if (pData.TransferEncoding == TransferEncoding.SevenBit || pData.TransferEncoding == TransferEncoding.EightBit)
                    {
                        lTransferEncoding = pData.TransferEncoding;
                        lPart = new cStreamAppendDataPart(pDisposables.GetMessageDataStream(lMessageDataStream));
                    }
                    else if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pText))
                    {
                        var lEncodeResult = await ZConvertMailMessageQuotedPrintableEncodeAsync(pMC, pDisposables, pDisposables.GetMessageDataStream(lMessageDataStream), pText, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);

                        if (pData.TransferEncoding == TransferEncoding.Unknown)
                        {
                            long lKnownLength = await lMessageDataStream.GetKnownLengthAsync().ConfigureAwait(false);
                            if (lEncodeResult.TempFileLength <= cBase64Encoder.EncodedLength(lKnownLength)) lTransferEncoding = TransferEncoding.QuotedPrintable;
                            else lTransferEncoding = TransferEncoding.Base64;
                        }
                        else lTransferEncoding = TransferEncoding.QuotedPrintable;

                        if (lTransferEncoding == TransferEncoding.QuotedPrintable) lPart = new cFileAppendDataPart(lEncodeResult.TempFileName);
                        else lPart = new cStreamAppendDataPart(pDisposables.GetMessageDataStream(lMessageDataStream), true);
                    }
                    else if (pData.TransferEncoding == TransferEncoding.Base64)
                    {
                        lTransferEncoding = TransferEncoding.Base64;
                        lPart = new cStreamAppendDataPart(pDisposables.GetMessageDataStream(lMessageDataStream), true);
                    }
                    else throw new cInternalErrorException($"cantbeconverted transferencoding {pData.TransferEncoding}", lContext);
                }
                else
                {
                    lTransferEncoding = lResult.TransferEncoding;

                    if (lResult.AppendDataPartType == eConvertMailMessageAppendDataPartType.message) lPart = new cMessageAppendDataPart(lMessageDataStream.Client, lMessageDataStream.MessageHandle);
                    else if (lResult.AppendDataPartType == eConvertMailMessageAppendDataPartType.messagepart) lPart = new cMessagePartAppendDataPart(lMessageDataStream.Client, lMessageDataStream.MessageHandle, lMessageDataStream.Part);
                    else if (lResult.AppendDataPartType == eConvertMailMessageAppendDataPartType.uidsection) lPart = new cUIDSectionAppendDataPart(lMessageDataStream.Client, lMessageDataStream.MailboxHandle, lMessageDataStream.UID, lMessageDataStream.Section, lMessageDataStream.GetKnownLength());
                    else throw new cInternalErrorException($"messagedatastream appenddataparttype {lResult.AppendDataPartType}", lContext);
                }
            }
            else
            {
                if (pData.TransferEncoding == TransferEncoding.SevenBit || pData.TransferEncoding == TransferEncoding.EightBit)
                {
                    lTransferEncoding = pData.TransferEncoding;
                    lPart = new cStreamAppendDataPart(pData.ContentStream);
                }
                else if (pData.TransferEncoding == TransferEncoding.QuotedPrintable || (pData.TransferEncoding == TransferEncoding.Unknown && pText))
                {
                    pData.ContentStream.Position = 0;
                    var lResult = await ZConvertMailMessageQuotedPrintableEncodeAsync(pMC, pDisposables, pData.ContentStream, pText, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);

                    if (pData.TransferEncoding == TransferEncoding.Unknown)
                    {
                        if (lResult.TempFileLength <= cBase64Encoder.EncodedLength(pData.ContentStream.Length)) lTransferEncoding = TransferEncoding.QuotedPrintable;
                        else lTransferEncoding = TransferEncoding.Base64;
                    }
                    else lTransferEncoding = TransferEncoding.QuotedPrintable;

                    if (lTransferEncoding == TransferEncoding.QuotedPrintable) lPart = new cFileAppendDataPart(lResult.TempFileName);
                    else lPart = new cStreamAppendDataPart(pData.ContentStream, true);
                }
                else if (pData.TransferEncoding == TransferEncoding.Base64)
                {
                    lTransferEncoding = TransferEncoding.Base64;
                    lPart = new cStreamAppendDataPart(pData.ContentStream, true);
                }
                else throw new cInternalErrorException($"normal stream transferencoding {pData.TransferEncoding}", lContext);
            }

            ZConvertMailMessageAddPart(lTransferEncoding, lPart, pParts, lContext);
        }

        private async Task<sConvertMailMessageQuotedPrintableEncodeResult> ZConvertMailMessageQuotedPrintableEncodeAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, Stream pSource, bool pText, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageQuotedPrintableEncodeAsync), pMC, pText, pReadConfiguration, pWriteConfiguration);

            eQuotedPrintableEncodeSourceType lSourceType;
            if (pText) lSourceType = kQuotedPrintableEncodeDefaultSourceType;
            else lSourceType = eQuotedPrintableEncodeSourceType.Binary;

            string lTempFileName = pDisposables.GetTempFileName();
            long lTempFileLength;

            using (var lTempFileStream = new FileStream(lTempFileName, FileMode.Truncate))
            {
                lTempFileLength = await ZZQuotedPrintableEncodeAsync(pMC, pSource, lSourceType, eQuotedPrintableEncodeQuotingRule.EBCDIC, lTempFileStream, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);
            }

            return new sConvertMailMessageQuotedPrintableEncodeResult(lTempFileName, lTempFileLength);
        }

        private void ZConvertMailMessageAddHeaderFieldMIMEParameters(StringDictionary pParameters, List<cHeaderFieldValuePart> pParts, params string[] pIgnoreKeys)
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

        private void ZConvertMailMessageAddPart(TransferEncoding pTransferEncoding, cAppendDataPart pPart, List<cAppendDataPart> pParts, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageAddPart), pTransferEncoding, pPart);

            string lTransferEncodingString;

            if (pTransferEncoding == TransferEncoding.SevenBit) lTransferEncodingString = "7bit";
            else if (pTransferEncoding == TransferEncoding.EightBit) lTransferEncodingString = "8bit";
            else if (pTransferEncoding == TransferEncoding.QuotedPrintable) lTransferEncodingString = "quoted-printable";
            else if (pTransferEncoding == TransferEncoding.Base64) lTransferEncodingString = "base64";
            else throw new cInternalErrorException($"transferencoding {pTransferEncoding}", lContext);

            pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", lTransferEncodingString));
            pParts.Add(cAppendDataPart.CRLF);
            pParts.Add(pPart);
        }

        private struct sConvertMailMessageDataStreamToAppendDataPartResult
        {
            public static readonly sConvertMailMessageDataStreamToAppendDataPartResult CantBeConverted = new sConvertMailMessageDataStreamToAppendDataPartResult();

            public readonly eConvertMailMessageAppendDataPartType AppendDataPartType;
            public readonly TransferEncoding TransferEncoding;

            public sConvertMailMessageDataStreamToAppendDataPartResult(eConvertMailMessageAppendDataPartType pAppendDataPartType, TransferEncoding pTransferEncoding)
            {
                AppendDataPartType = pAppendDataPartType;
                TransferEncoding = pTransferEncoding;
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

        private class cConvertMailMessageBoundarySource
        {
            private readonly string mUnique;
            private int mPart = 0;

            public cConvertMailMessageBoundarySource()
            {
                mUnique = Guid.NewGuid().ToString();
            }

            public string Boundary(out cLiteralAppendDataPart rDelimiter, out cLiteralAppendDataPart rCloseDelimiter)
            {
                string lBoundary = $"=_{mPart++}_{mUnique}";
                rDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "\r\n");
                rCloseDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "--\r\n");
                return lBoundary;
            }

            public override string ToString() => $"{nameof(cConvertMailMessageBoundarySource)}({mUnique},{mPart})";
        }
    }
}
 