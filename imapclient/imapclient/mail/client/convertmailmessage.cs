using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public partial class cMailClient
    {
        [Flags]
        public enum fConvertMailMessageOptions
        {
            excludebcc = 1
        }

        private enum eConvertMailMessageMessageDataPartType { none, message, messagepart, uidsection }

        private static readonly cBatchSizerConfiguration kConvertMailMessageMemoryStreamReadWriteConfiguration = new cBatchSizerConfiguration(10000, 10000, 1, 10000); // 10k chunks

        public cMessageData ConvertMailMessage(cConvertMailMessageDisposables pDisposables, MailMessage pMessage, fConvertMailMessageOptions pOptions, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cMailClient), nameof(ConvertMailMessage));
            var lTask = ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessage(pMessage), pOptions, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count != 1) throw new cInternalErrorException(lContext);
            return lResult[0];
        }

        public async Task<cMessageData> ConvertMailMessageAsync(cConvertMailMessageDisposables pDisposables, MailMessage pMessage, fConvertMailMessageOptions pOptions, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cMailClient), nameof(ConvertMailMessageAsync));
            var lResult = await ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessage(pMessage), pOptions, pConfiguration, lContext).ConfigureAwait(false);
            if (lResult.Count != 1) throw new cInternalErrorException(lContext);
            return lResult[0];
        }

        public List<cMessageData> ConvertMailMessages(cConvertMailMessageDisposables pDisposables, IEnumerable<MailMessage> pMessages, fConvertMailMessageOptions pOptions, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cMailClient), nameof(ConvertMailMessages));
            var lTask = ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessages(pMessages), pOptions, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMessageData>> ConvertMailMessagesAsync(cConvertMailMessageDisposables pDisposables, IEnumerable<MailMessage> pMessages, fConvertMailMessageOptions pOptions, cConvertMailMessageConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cMailClient), nameof(ConvertMailMessagesAsync));
            return ZConvertMailMessagesAsync(pDisposables, cMailMessageList.FromMessages(pMessages), pOptions, pConfiguration, lContext);
        }

        private async Task<List<cMessageData>> ZConvertMailMessagesAsync(cConvertMailMessageDisposables pDisposables, cMailMessageList pMessages, fConvertMailMessageOptions pOptions, cConvertMailMessageConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailClient), nameof(ZConvertMailMessagesAsync), pMessages, pOptions);

            if (mDisposed) throw new ObjectDisposedException(nameof(cMailClient));

            if (pDisposables == null) throw new ArgumentNullException(nameof(pDisposables));
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await YConvertMailMessagesAsync(lMC, pDisposables, pMessages, pOptions, null, null, mLocalStreamReadConfiguration , mLocalStreamWriteConfiguration, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await YConvertMailMessagesAsync(lMC, pDisposables, pMessages, pOptions, pConfiguration.SetMaximum, pConfiguration.Increment, pConfiguration.ReadConfiguration ?? mLocalStreamReadConfiguration, pConfiguration.WriteConfiguration ?? mLocalStreamWriteConfiguration, lContext).ConfigureAwait(false);
            }
        }

        internal async Task<List<cMessageData>> YConvertMailMessagesAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, cMailMessageList pMessages, fConvertMailMessageOptions pOptions, Action<long> pSetMaximum, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailClient), nameof(YConvertMailMessagesAsync), pMC, pMessages, pOptions, pReadConfiguration, pWriteConfiguration);

            long lToConvert = 0;
            foreach (var lMessage in pMessages) lToConvert += await ZConvertMailMessageValidateAsync(lMessage, lContext).ConfigureAwait(false);

            mSynchroniser.InvokeActionLong(pSetMaximum, lToConvert, lContext);

            var lData = new List<cMessageData>();
            foreach (var lMessage in pMessages) lData.Add(await ZConvertMailMessageAsync(pMC, pDisposables, lMessage, pOptions, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false));

            return lData;
        }

        private async Task<long> ZConvertMailMessageValidateAsync(MailMessage pMessage, cTrace.cContext pParentContext)
        {
            var lContext = mRootContext.NewMethod(nameof(cMailClient), nameof(ZConvertMailMessageValidateAsync));

            // check for unsupported features
            if (pMessage.ReplyTo != null) throw new cMailMessageFormException(pMessage, nameof(MailMessage.ReplyTo));

            ;?; // but note: I have to check that if specified the bodytrasferencoding matches the content of the attachment collections
            //  and if it isn't specified I have to set it base on that content
            // => this routine should output 

            // check for unsupported format
            if (pMessage.BodyTransferEncoding == TransferEncoding.EightBit && (SupportedFormats & fMessageDataFormat.eightbit) == 0) throw new cMailMessageFormException(pMessage, nameof(MailMessage.BodyTransferEncoding));

            ;?; 

            long lToConvert = 0;

            foreach (var lAlternateView in pMessage.AlternateViews)
            {
                lToConvert += await ZConvertMailMessageValidateAttachmentAsync(pMessage, lAlternateView, true).ConfigureAwait(false);
                foreach (var lLinkedResource in lAlternateView.LinkedResources) lToConvert += await ZConvertMailMessageValidateAttachmentAsync(pMessage, lLinkedResource, false).ConfigureAwait(false);
            }

            foreach (var lAttachment in pMessage.Attachments) lToConvert += await ZConvertMailMessageValidateAttachmentAsync(pMessage, lAttachment, false).ConfigureAwait(false);


            ;?; // note that I have to generate the content-transfer-encoding for the implied multipart parts of the whole message and the alternate view and the multipart related of each alternate view
            //  => if utf8 headers, binary or 8bit is required in any attachment that has to be accumulated and tested for

            return lToConvert;
        }

        private async Task<sConvertMailMessageAttachmentDetails> ZConvertMailMessageGetAttachmentDetailsAsync(MailMessage pMessage, AttachmentBase pAttachment)
        {
            if (pAttachment.ContentStream is cIMAPMessageDataStream lMessageDataStream)
            {
                var lDetails = await ZConvertMailMessageGetIMAPMessageDataStreamURLDetailsAsync(lMessageDataStream, pAttachment.TransferEncoding).ConfigureAwait(false);

                // if the attachment can be converted to a URL return those details
                if (lDetails.MessageDataPartType != eConvertMailMessageMessageDataPartType.none) return lDetails;

                // I have to stream the data => I need to know the format and length
                if (!lMessageDataStream.HasKnownFormatAndLength) throw new cMailMessageFormException(pMessage, pAttachment, kMailMessageFormExceptionMessage.MessageDataStreamUnknownFormatAndLength);

                // ensure that the values are available
                await lMessageDataStream.GetKnownFormatAndLengthAsync().ConfigureAwait(false);

                switch (pAttachment.TransferEncoding)
                {
                    case TransferEncoding.QuotedPrintable:

                        return new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.quotedprintable, fMessageDataFormat.sevenbit, lMessageDataStream.KnownLength);

                    case TransferEncoding.Base64:

                        return sConvertMailMessageAttachmentDetails.Base64;

                    case TransferEncoding.SevenBit:

                        if (lMessageDataStream.KnownFormat == fMessageDataFormat.sevenbit) return sConvertMailMessageAttachmentDetails.SevenBit;
                        throw new cMailMessageFormException(pMessage, pAttachment, nameof(AttachmentBase.TransferEncoding));

                    case TransferEncoding.EightBit:

                        if ((lMessageDataStream.KnownFormat & fMessageDataFormat.binary) != 0) throw new cMailMessageFormException(pMessage, pAttachment, nameof(AttachmentBase.TransferEncoding));
                        return new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.eightbit, lMessageDataStream.KnownFormat, 0);

                    case TransferEncoding.Unknown:

                        if (lMessageDataStream.KnownFormat == fMessageDataFormat.sevenbit) return sConvertMailMessageAttachmentDetails.SevenBit;

                        if (pAttachment.ContentType.MediaType.Equals("message/partial", StringComparison.InvariantCultureIgnoreCase)) throw new cMailMessageFormException(pMessage, pAttachment, nameof(ContentType.MediaType));

                        if (pAttachment.ContentType.MediaType.Equals("message/rfc822", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ;?; // these could be constants
                            eContentTransferEncoding lCTE;
                            if ((lMessageDataStream.KnownFormat & fMessageDataFormat.binary) != 0) lCTE = eContentTransferEncoding.binary;
                            else lCTE = eContentTransferEncoding.eightbit;

                            return new sConvertMailMessageAttachmentDetails(lCTE, lMessageDataStream.KnownFormat, 0);
                        }

                        ;?; // should be constant
                        if (pAttachment.ContentType.MediaType.StartsWith("text/", StringComparison.InvariantCultureIgnoreCase)) return new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.quotedprintableorbase64, fMessageDataFormat.sevenbit, lMessageDataStream.KnownLength);

                        return sConvertMailMessageAttachmentDetails.Base64;

                    default:

                        throw new cInternalErrorException(nameof(cMailAttachment), nameof(ZConvertMailMessageGetAttachmentDetailsAsync), 1);
                }
            }

            if (!pAttachment.ContentStream.CanSeek) throw new cMailMessageFormException(pMessage, pAttachment, kMailMessageFormExceptionMessage.StreamNotSeekable);

            switch (pAttachment.TransferEncoding)
            {
                case TransferEncoding.QuotedPrintable:

                    return new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.quotedprintable, fMessageDataFormat.sevenbit, pAttachment.ContentStream.Length);

                case TransferEncoding.Base64:

                    return sConvertMailMessageAttachmentDetails.Base64;

                case TransferEncoding.SevenBit:

                    return sConvertMailMessageAttachmentDetails.SevenBit;

                case TransferEncoding.EightBit:

                    return sConvertMailMessageAttachmentDetails.EightBit;

                case TransferEncoding.Unknown:

                    if (pAttachment.ContentType.MediaType.Equals("message/partial", StringComparison.InvariantCultureIgnoreCase)) return sConvertMailMessageAttachmentDetails.SevenBit;

                    if (pAttachment.ContentType.MediaType.Equals("message/rfc822", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var lFormat = ZConvertMailMessageRFC822Format(pAttachment.ContentStream); // examines the stream parsing for format

                        ;/; // these cuold be constants
                        eContentTransferEncoding lCTE;
                        if ((lFormat & fMessageDataFormat.binary) != 0) lCTE = eContentTransferEncoding.binary;
                        else if (lFormat & fMessageDataFormat.eightbit) lCTE = eContentTransferEncoding.eightbit;
                        else lCTE = eContentTransferEncoding.sevenbit;

                        return new sConvertMailMessageAttachmentDetails(lCTE, lFormat, 0);
                    }

                    if (pAttachment.ContentType.MediaType.StartsWith("text/", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ;?; // analyse the stream for 7bit or encoded AND use constant
                        return new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.quotedprintableorbase64, fMessageDataFormat.sevenbit, lMessageDataStream.KnownLength);
                    }

                    return sConvertMailMessageAttachmentDetails.Base64;

                default:

                    throw new cInternalErrorException(nameof(cMailAttachment), nameof(ZConvertMailMessageGetAttachmentDetailsAsync), 1);
            }
        }

        private async Task<sConvertMailMessageIMAPMessageDataStreamURLDetails> ZConvertMailMessageGetIMAPMessageDataStreamURLDetailsAsync(cIMAPMessageDataStream pMessageDataStream, TransferEncoding pTransferEncoding)
        {
            eConvertMailMessageMessageDataPartType lMessageDataPartType;

            if (pMessageDataStream.MessageHandle != null && pMessageDataStream.Part == null && pMessageDataStream.Section == cSection.All) lMessageDataPartType = eConvertMailMessageMessageDataPartType.message;
            else if (pMessageDataStream.MessageHandle != null && pMessageDataStream.Part != null) lMessageDataPartType = eConvertMailMessageMessageDataPartType.messagepart;
            else if (pMessageDataStream.MailboxHandle != null && pMessageDataStream.Decoding == eDecodingRequired.none && pMessageDataStream.HasKnownFormatAndLength) lMessageDataPartType = eConvertMailMessageMessageDataPartType.uidsection;
            else return sConvertMailMessageIMAPMessageDataStreamURLDetails.CannotBeRepresentedAsURL;

            switch (pMessageDataStream.Decoding)
            {
                case eDecodingRequired.none:

                    if (pTransferEncoding == TransferEncoding.Unknown || pTransferEncoding == TransferEncoding.SevenBit || pTransferEncoding == TransferEncoding.EightBit)
                    {
                        // check that there is a match between the transferencoding and the format of the data and/or choose the contenttransferencoding
                        //  also check that the

                        await pMessageDataStream.GetKnownFormatAndLengthAsync().ConfigureAwait(false);

                        if ((pMessageDataStream.KnownFormat & fMessageDataFormat.binary) != 0) 



                    }

                    return sConvertMailMessageIMAPMessageDataStreamURLDetails.CannotBeRepresentedAsURL;


                    // check that the format of the data in the stream can be used
                    //  binary is an automatic no, utf8 headers requires utf8headers in the client, 8bit requires that unknown or 8bit is the selected transfer encoding

                    await lMessageDataStream.GetKnownFormatAndLengthAsync().ConfigureAwait(false);

                    if (pTransferEncoding == TransferEncoding.Unknown)


                    if (pTransferEncoding == TransferEncoding.Unknown || pTransferEncoding == TransferEncoding.SevenBit || pTransferEncoding == TransferEncoding.EightBit)
                    {
                        if ()
                    }


                    ;?; // check that the message format matches i.e. if the message format uses binary it is an auto fail, if the message format uses utf8 or 8bit and 7bit encoding is specified it is a fail etc
                    if (pTransferEncoding == TransferEncoding.SevenBit || pTransferEncoding == TransferEncoding.EightBit)
                    {
                        ;?;
                        return new sConvertMailMessageDataStreamToAppendDataPartResult(lAppendDataPartType, pTransferEncoding);
                    }

                    return sConvertMailMessageIMAPMessageDataStreamConversionType.CantBeConverted;

                case eDecodingRequired.quotedprintable:

                    if (pTransferEncoding == TransferEncoding.Unknown || pTransferEncoding == TransferEncoding.QuotedPrintable) return new sConvertMailMessageIMAPMessageDataStreamURLDetails(lMessageDataPartType, eContentTransferEncoding.quotedprintable);
                    return sConvertMailMessageIMAPMessageDataStreamURLDetails.CannotBeRepresentedAsURL;

                case eDecodingRequired.base64:

                    if (pTransferEncoding == TransferEncoding.Unknown || pTransferEncoding == TransferEncoding.Base64) return new sConvertMailMessageIMAPMessageDataStreamURLDetails(lMessageDataPartType, eContentTransferEncoding.base64);
                    return sConvertMailMessageIMAPMessageDataStreamURLDetails.CannotBeRepresentedAsURL;
            }

            return sConvertMailMessageIMAPMessageDataStreamURLDetails.CannotBeRepresentedAsURL;
        }

        private async Task<cMessageData> ZConvertMailMessageAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, MailMessage pMessage, fConvertMailMessageOptions pOptions, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageAsync), pMC, pReadConfiguration, pWriteConfiguration);

            var lParts = new List<cAppendDataPart>();

            // convert the properties to parts 

            if (pMessage.Bcc.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("bcc", pMessage.Bcc));
            if (pMessage.CC.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("cc", pMessage.CC));
            if (pMessage.From != null) lParts.Add(new cHeaderFieldAppendDataPart("from", pMessage.From));
            lParts.Add(new cHeaderFieldAppendDataPart("importance", pMessage.Priority.ToString()));
            if (pMessage.ReplyToList.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("reply-to", pMessage.ReplyToList));
            if (pMessage.Sender != null) lParts.Add(new cHeaderFieldAppendDataPart("sender", pMessage.Sender));
            if (pMessage.Subject != null) lParts.Add(new cHeaderFieldAppendDataPart("subject", pMessage.Subject)); ?? // encoding
            if (pMessage.To.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("to", pMessage.To));

            // add custom headers

            ;?; // encoding
            for (int i = 0; i < pMessage.Headers.Count; i++)
            {
                var lName = pMessage.Headers.GetKey(i);
                var lValues = pMessage.Headers.GetValues(i);

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

            if (pMessage.Attachments.Count == 0) await ZConvertMailMessageViewsAsync(pMC, pDisposables, pMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lBoundarySource, lContext).ConfigureAwait(false);
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
                await ZConvertMailMessageViewsAsync(pMC, pDisposables, pMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lParts, lBoundarySource, lContext).ConfigureAwait(false);

                // attachments
                foreach (var lAttachment in pMessage.Attachments)
                {
                    lParts.Add(lDelimiter);

                    var lContentDisposition = new List<cHeaderFieldValuePart>();

                    if (lAttachment.ContentDisposition.Inline) lContentDisposition.Add("inline");
                    else if (lAttachment.ContentDisposition.DispositionType == null) lContentDisposition.Add("attachment");
                    else lContentDisposition.Add(lAttachment.ContentDisposition.DispositionType);

                    if (lAttachment.ContentDisposition.FileName != null) lContentDisposition.Add(new cHeaderFieldMIMEParameter("filename", lAttachment.ContentDisposition.FileName)); ??? // enoding
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

        private async Task ZConvertMailMessageViewsAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, MailMessage pMessage, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, List<cAppendDataPart> pParts, cConvertMailMessageBoundarySource pBoundarySource, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageViewsAsync), pMC, pReadConfiguration, pWriteConfiguration, pBoundarySource);

            if (pMessage.AlternateViews.Count > 0)
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
                await ZConvertMailMessageBodyAsync(pMC, pDisposables, pMessage, pParts, lContext).ConfigureAwait(false);

                foreach (var lAlternateView in pMessage.AlternateViews)
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
            else await ZConvertMailMessageBodyAsync(pMC, pDisposables, pMessage, pParts, lContext).ConfigureAwait(false);
        }

        private async Task ZConvertMailMessageBodyAsync(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, MailMessage pMessage, List<cAppendDataPart> pParts, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertMailMessageBodyAsync), pMC);

            Encoding lEncoding = pMessage.BodyEncoding ?? Encoding.ASCII;

            string lMediaType;
            if (pMessage.IsBodyHtml) lMediaType = "text/html";
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

            if (pMessage.BodyTransferEncoding == TransferEncoding.SevenBit || pMessage.BodyTransferEncoding == TransferEncoding.EightBit)
            {
                lTransferEncoding = pMessage.BodyTransferEncoding;
                lPart = new cLiteralAppendDataPart(lEncoding.GetBytes(pMessage.Body));
            }
            else if (pMessage.BodyTransferEncoding == TransferEncoding.QuotedPrintable || pMessage.BodyTransferEncoding == TransferEncoding.Unknown)
            {
                var lSource = pDisposables.GetMemoryStream(lEncoding.GetBytes(pMessage.Body));
                var lResult = await ZConvertMailMessageQuotedPrintableEncodeAsync(pMC, pDisposables, lSource, true, null, kConvertMailMessageMemoryStreamReadWriteConfiguration, kConvertMailMessageMemoryStreamReadWriteConfiguration, lContext).ConfigureAwait(false);

                if (pMessage.BodyTransferEncoding == TransferEncoding.Unknown)
                {
                    if (lResult.TempFileLength <= cBase64EncodingStream.EncodedLength(lSource.Length)) lTransferEncoding = TransferEncoding.QuotedPrintable;
                    else lTransferEncoding = TransferEncoding.Base64;
                }
                else lTransferEncoding = TransferEncoding.QuotedPrintable;

                if (lTransferEncoding == TransferEncoding.QuotedPrintable) lPart = new cFileAppendDataPart(lResult.TempFileName);
                else lPart = new cStreamAppendDataPart(lSource, true);
            }
            else if (pMessage.BodyTransferEncoding == TransferEncoding.Base64)
            {
                lTransferEncoding = TransferEncoding.Base64;
                lPart = new cStreamAppendDataPart(pDisposables.GetMemoryStream(lEncoding.GetBytes(pMessage.Body)), true);
            }
            else throw new cInternalErrorException(lContext);

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
                            if (lEncodeResult.TempFileLength <= cBase64EncodingStream.EncodedLength(lKnownLength)) lTransferEncoding = TransferEncoding.QuotedPrintable;
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
                    else throw new cInternalErrorException(lContext, 1);
                }
                else
                {
                    lTransferEncoding = lResult.TransferEncoding;

                    if (lResult.AppendDataPartType == eConvertMailMessageAppendDataPartType.message) lPart = new cMessageAppendDataPart(lMessageDataStream.Client, lMessageDataStream.MessageHandle);
                    else if (lResult.AppendDataPartType == eConvertMailMessageAppendDataPartType.messagepart) lPart = new cMessagePartAppendDataPart(lMessageDataStream.Client, lMessageDataStream.MessageHandle, lMessageDataStream.Part);
                    else if (lResult.AppendDataPartType == eConvertMailMessageAppendDataPartType.uidsection) lPart = new cUIDSectionAppendDataPart(lMessageDataStream.Client, lMessageDataStream.MailboxHandle, lMessageDataStream.UID, lMessageDataStream.Section, lMessageDataStream.GetKnownLength());
                    else throw new cInternalErrorException(lContext, 2);
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
                        if (lResult.TempFileLength <= cBase64EncodingStream.EncodedLength(pData.ContentStream.Length)) lTransferEncoding = TransferEncoding.QuotedPrintable;
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
                else throw new cInternalErrorException(lContext, 3);
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
            else throw new cInternalErrorException(lContext);

            pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", lTransferEncodingString));
            pParts.Add(cAppendDataPart.CRLF);
            pParts.Add(pPart);
        }

        private struct sConvertMailMessageAttachmentDetails
        {
            public static readonly sConvertMailMessageAttachmentDetails None = new sConvertMailMessageAttachmentDetails();
            public static readonly sConvertMailMessageAttachmentDetails SevenBit = new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.sevenbit, fMessageDataFormat.sevenbit, 0);
            public static readonly sConvertMailMessageAttachmentDetails EightBit = new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.eightbit, fMessageDataFormat.eightbit, 0);
            public static readonly sConvertMailMessageAttachmentDetails Base64 = new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.base64, fMessageDataFormat.sevenbit, 0);

            public readonly eConvertMailMessageMessageDataPartType MessageDataPartType;
            public readonly eContentTransferEncoding ContentTransferEncoding;
            public readonly fMessageDataFormat MessageDataFormat;
            public readonly long LengthToConvertToQuotedPrintable;

            public sConvertMailMessageAttachmentDetails(eConvertMailMessageMessageDataPartType pMessageDataPartType, eContentTransferEncoding pContentTransferEncoding, fMessageDataFormat pMessageDataFormat, long pLengthToConvertToQuotedPrintable)
            {
                ;?;
                MessageDataPartType = pMessageDataPartType;
                ContentTransferEncoding = pContentTransferEncoding;
                MessageDataFormat = pMessageDataFormat;
                LengthToConvertToQuotedPrintable = pLengthToConvertToQuotedPrintable;
            }

            public sConvertMailMessageAttachmentDetails(eContentTransferEncoding pContentTransferEncoding, fMessageDataFormat pMessageDataFormat, long pLengthToConvertToQuotedPrintable = 0)
            {
                MessageDataPartType = eConvertMailMessageMessageDataPartType.none;
                ContentTransferEncoding = pContentTransferEncoding;
                MessageDataFormat = pMessageDataFormat;
                LengthToConvertToQuotedPrintable = pLengthToConvertToQuotedPrintable;
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
 