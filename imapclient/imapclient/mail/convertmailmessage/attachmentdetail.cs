using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using work.bacome.imapclient;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public partial class cMailClient
    {
        /* TEMP comment out for cachefile work
        private class cConvertMailMessageContentDetail
        {
            public static readonly cConvertMailMessageContentDetail SevenBit = new cConvertMailMessageContentDetail(eContentTransferEncoding.sevenbit, eMessageDataFormat.sevenbit, 0);
            public static readonly cConvertMailMessageContentDetail EightBit = new cConvertMailMessageContentDetail(eContentTransferEncoding.eightbit, eMessageDataFormat.eightbit, 0);
            public static readonly cConvertMailMessageContentDetail Binary = new cConvertMailMessageContentDetail(eContentTransferEncoding.binary, eMessageDataFormat.binary, 0);
            public static readonly cConvertMailMessageContentDetail UTF8Headers = new cConvertMailMessageContentDetail(eContentTransferEncoding.eightbit, eMessageDataFormat.utf8headers, 0);
            public static readonly cConvertMailMessageContentDetail BinaryAndUTF8Headers = new cConvertMailMessageContentDetail(eContentTransferEncoding.binary, eMessageDataFormat.binaryandutf8headers, 0);
            public static readonly cConvertMailMessageContentDetail QuotedPrintableEncoded = new cConvertMailMessageContentDetail(eContentTransferEncoding.quotedprintable, eMessageDataFormat.sevenbit, 0);
            public static readonly cConvertMailMessageContentDetail Base64Encoded = new cConvertMailMessageContentDetail(eContentTransferEncoding.base64, eMessageDataFormat.sevenbit, 0);

            public readonly eContentTransferEncoding ContentTransferEncoding;
            public readonly eMessageDataFormat Format;
            public readonly long LengthToEncode; // zero = already encoded

            public cConvertMailMessageContentDetail(eContentTransferEncoding pContentTransferEncoding, eMessageDataFormat pFormat, long pLengthToEncode)
            {
                ContentTransferEncoding = pContentTransferEncoding;
                Format = pFormat;
                LengthToEncode = pLengthToEncode;
            }
        }

        private class cConvertMailMessageAttachmentDetail
        {
            public readonly AttachmentBase Attachment;
            public readonly cConvertMailMessageContentDetail ContentDetail;
            public readonly string TempFileName;

            public cConvertMailMessageAttachmentDetail(AttachmentBase pAttachment, cConvertMailMessageContentDetail pContentDetail, string pTempFileName)
            {
                Attachment = pAttachment ?? throw new ArgumentNullException(nameof(pAttachment));
                ContentDetail = pContentDetail;
                TempFileName = pTempFileName;
            }
        }

        private class cConvertMailMessageAttachmentsDetail
        {
            private readonly cMethodControl mMC;
            private readonly cConvertMailMessageDisposables mDisposables;
            private readonly cBatchSizerConfiguration mLocalStreamReadConfiguration;
            private readonly List<cConvertMailMessageAttachmentDetail> mAttachmentDetail = new List<cConvertMailMessageAttachmentDetail>();

            public cConvertMailMessageAttachmentsDetail(cMethodControl pMC, cConvertMailMessageDisposables pDisposables, cBatchSizerConfiguration pLocalStreamReadConfiguration)
            {
                mMC = pMC;
                mDisposables = pDisposables;
                mLocalStreamReadConfiguration = pLocalStreamReadConfiguration;
            }

            public async Task<cConvertMailMessageAttachmentDetail> AddDetailAsync(MailMessage pMessage, AttachmentBase pAttachment)
            {
                const string kMessagePartial = "message/partial";
                const string kMessageRFC822 = "message/rfc822";
                const string kText = "text/";

                cConvertMailMessageContentDetail lContentDetail;
                string lTempFileName;

                if (pAttachment.ContentStream is cIMAPMessageDataStream lMessageDataStream)
                {
                    // the objective is to prevent download if the stream MAY be converted to a URL when the messagedata is used later
                    //  this can only be done if the encoding requested, the decoding requested and the encoding on the IMAP server are the same
                    //  messagedatastreams with a part (or for an entire message) are the only ones that I can tell what the encoding is on the IMAP server
                    //  in this code I take the decoding requested by the stream as the decoding on the server for other message data streams
                    // SO
                    //  also however consider that even if all these values are in alignment that the stream may not be able to be converted to a URL (because of the client capabilities or the connected account)
                    //  the issue here is the ignoring of the server's capability to do the decoding before the sending over the wire (which is really just an issue for quoted-printable to IMAP as I need to do the conversion before I start the upload)
                    // SO
                    //  what I'm begininng to think is that the problem stems from the decision to do accurate up-front feedback
                    //  I can't say how many bytes I need to download and convert to qp until the append/send is in progress
                    //  (tbh this applies to base64 also)
                    // SO
                    //  we do the encoding just before sending not here
                    //  we modify the feedback for append to include a new item to change the total up
                    //  if we know the fetch length of the stream, assume we will download it encode and upload it (guess 2x download size), otherwise mark it as unknown
                    ;?; // more here
                    //  if during append/send we can URL it, send an increment for the provided length
                    //  if during append/send we can't URL it, download it decrementing; encode to b64 or 


                    var lTransferEncoding = pAttachment.TransferEncoding;

                    switch (lMessageDataStream.Decoding)
                    {
                        case TransferEncoding.SevenBit:
                    }



                    lContentDetail = await ZURLContentDetail(pMessage, pAttachment).ConfigureAwait(false);

                    if (lContentDetail != null) lTempFileName = null;
                    else
                    {
                        long lLength;

                        if (lMessageDataStream.HasKnownFormatAndLength)
                        {
                            await lMessageDataStream.GetKnownFormatAndLengthAsync().ConfigureAwait(false);
                            lTempFileName = null;
                            lLength = lMessageDataStream.KnownLength;
                        }
                        else
                        {
                            lTempFileName = mDisposables.GetTempFileName();

                            var lSizer = new cBatchSizer()

                            using (var lTempMessageDataStream = new cIMAPMessageDataStream(lMessageDataStream))
                            using (var lTempFileStream = new FileStream(lTempFileName, FileMode.Truncate))
                            {
                                lTempMessageDataStream.ReadTimeout =


                                ;?; // copy with feedback
                            }


                        }

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

                                ;?;
                                if ((lMessageDataStream.KnownFormat & fMessageDataFormat.binary) != 0) throw new cMailMessageFormException(pMessage, pAttachment, nameof(AttachmentBase.TransferEncoding));
                                return new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.eightbit, lMessageDataStream.KnownFormat, 0);

                            case TransferEncoding.Unknown:

                                if (lMessageDataStream.KnownFormat == fMessageDataFormat.sevenbit) return sConvertMailMessageAttachmentDetails.SevenBit;

                                if (pAttachment.ContentType.MediaType.Equals(kMessagePartial, StringComparison.InvariantCultureIgnoreCase)) throw new cMailMessageFormException(pMessage, pAttachment, nameof(ContentType.MediaType));

                                if (pAttachment.ContentType.MediaType.Equals(kMessageRFC822, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    ;?;
                                    eContentTransferEncoding lCTE;
                                    if ((lMessageDataStream.KnownFormat & fMessageDataFormat.binary) != 0) lCTE = eContentTransferEncoding.binary;
                                    else lCTE = eContentTransferEncoding.eightbit;

                                    return new sConvertMailMessageAttachmentDetails(lCTE, lMessageDataStream.KnownFormat, 0);
                                }

                                if (pAttachment.ContentType.MediaType.StartsWith(kText, StringComparison.InvariantCultureIgnoreCase)) return new sConvertMailMessageAttachmentDetails(eContentTransferEncoding.text, fMessageDataFormat.sevenbit, lMessageDataStream.KnownLength);

                                return sConvertMailMessageAttachmentDetails.Base64;

                            default:

                                throw new cInternalErrorException(nameof(cMailAttachment), nameof(ZConvertMailMessageGetAttachmentDetailsAsync), 1);
                        }
                    }

                }
                else
                {
                    long lLength;

                    if (pAttachment.ContentStream.CanSeek)
                    {
                        lTempFileName = null;
                        lLength = pAttachment.ContentStream.Length;
                    }
                    else
                    {
                        lTempFileName = mDisposables.GetTempFileName();

                        lLength = 0;

                        using (var lWriteStream = new FileStream(lTempFileName, FileMode.Open, FileAccess.Write))
                        {
                            cBatchSizer lReadSizer = new cBatchSizer(mLocalStreamReadConfiguration);

                            byte[] lBuffer = null;
                            Stopwatch lStopwatch = new Stopwatch();

                            while (true)
                            {
                                // read some data

                                int lCurrent = lReadSizer.Current;
                                if (lBuffer == null || lCurrent > lBuffer.Length) lBuffer = new byte[lCurrent];

                                lStopwatch.Restart();

                                if (pAttachment.ContentStream.CanTimeout) pAttachment.ContentStream.ReadTimeout = mMC.Timeout;
                                else _ = mMC.Timeout; // this checks the timeout

                                int lBytesReadIntoBuffer = await pAttachment.ContentStream.ReadAsync(lBuffer, 0, lCurrent, mMC.CancellationToken).ConfigureAwait(false);

                                lStopwatch.Stop();

                                if (lBytesReadIntoBuffer == 0) break;

                                lReadSizer.AddSample(lBytesReadIntoBuffer, lStopwatch.ElapsedMilliseconds);

                                // write to the tempfile
                                await lWriteStream.WriteAsync(lBuffer, 0, lBytesReadIntoBuffer, mMC.CancellationToken).ConfigureAwait(false);

                                // increment length
                                lLength += lBytesReadIntoBuffer;
                            }
                        }
                    }

                    switch (pAttachment.TransferEncoding)
                    {
                        case TransferEncoding.QuotedPrintable:

                            lContentDetail = new cConvertMailMessageContentDetail(eContentTransferEncoding.quotedprintable, eMessageDataFormat.sevenbit, lLength);
                            break;

                        case TransferEncoding.Base64:

                            lContentDetail = new cConvertMailMessageContentDetail(eContentTransferEncoding.base64, eMessageDataFormat.sevenbit, lLength);
                            break;

                        case TransferEncoding.SevenBit:

                            lContentDetail = cConvertMailMessageContentDetail.SevenBit;
                            break;

                        case TransferEncoding.EightBit:

                            if (pAttachment.ContentType.MediaType.Equals(kMessageRFC822, StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (lTempFileName == null) lContentDetail = await ZContentDetail(pAttachment.ContentStream).ConfigureAwait(false);
                                else
                                {
                                    using (var lContentStream = new FileStream(lTempFileName, FileMode.Open))
                                    {
                                        lContentDetail = await ZContentDetail(lContentStream).ConfigureAwait(false);
                                    }
                                }
                            }
                            else lContentDetail = cConvertMailMessageContentDetail.EightBit;

                            break;

                        case TransferEncoding.Unknown:

                            if (pAttachment.ContentType.MediaType.Equals(kMessagePartial, StringComparison.InvariantCultureIgnoreCase)) lContentDetail = cConvertMailMessageContentDetail.SevenBit;
                            else if (pAttachment.ContentType.MediaType.Equals(kMessageRFC822, StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (lTempFileName == null) lContentDetail = await ZContentDetail(pAttachment.ContentStream).ConfigureAwait(false);
                                else
                                {
                                    using (var lContentStream = new FileStream(lTempFileName, FileMode.Open))
                                    {
                                        lContentDetail = await ZContentDetail(lContentStream).ConfigureAwait(false);
                                    }
                                }
                            }
                            else if (pAttachment.ContentType.MediaType.StartsWith(kText, StringComparison.InvariantCultureIgnoreCase)) lContentDetail = new cConvertMailMessageContentDetail(eContentTransferEncoding.text, null, lLength);
                            else lContentDetail = new cConvertMailMessageContentDetail(eContentTransferEncoding.base64, eMessageDataFormat.sevenbit, lLength);

                            break;

                        default:

                            throw new cInternalErrorException(nameof(cMailAttachment), nameof(ZConvertMailMessageGetAttachmentDetailsAsync), 1);
                    }
                }

                var lAttachmentDetail = new cConvertMailMessageAttachmentDetail(pAttachment, lContentDetail, lTempFileName);
                mAttachmentDetail.Add(lAttachmentDetail);
                return lAttachmentDetail;
            }



            public cConvertMailMessageAttachmentDetail Get(AttachmentBase pAttachment)
            {

            }

            private async Task<cConvertMailMessageContentDetail> ZContentDetail(Stream pStream)
            {
                using (var lMessage = await cStreamMessage.ParseAsync(mMC, pStream).ConfigureAwait(false))
                {
                    if (lMessage == null) return cConvertMailMessageContentDetail.BinaryAndUTF8Headers;

                    switch (lMessage.Format)
                    {
                        case eMessageDataFormat.sevenbit:

                            return cConvertMailMessageContentDetail.SevenBit;

                        case eMessageDataFormat.eightbit:

                            return cConvertMailMessageContentDetail.EightBit;

                        case eMessageDataFormat.binary:

                            return cConvertMailMessageContentDetail.Binary;

                        case eMessageDataFormat.utf8headers:

                            return cConvertMailMessageContentDetail.UTF8Headers;

                        case eMessageDataFormat.binaryandutf8headers:

                            return cConvertMailMessageContentDetail.BinaryAndUTF8Headers;

                        default:

                            throw new cInternalErrorException(nameof(cConvertMailMessageAttachmentsDetail), nameof(ZConvertMailMessageContentDetail));
                    }
                }
            }

            private async Task<cConvertMailMessageContentDetail> ZURLContentDetail(MailMessage pMessage, AttachmentBase pAttachment)
            {
                cIMAPMessageDataStream lStream = pAttachment.ContentStream as cIMAPMessageDataStream;
                if (lStream == null) return null;

                eConvertMailMessageMessageDataPartType lMessageDataPartType;

                if (lStream.MessageHandle != null && lStream.Part == null && lStream.Section == cSection.All) lMessageDataPartType = eConvertMailMessageMessageDataPartType.message;
                else if (lStream.MessageHandle != null && lStream.Part != null) lMessageDataPartType = eConvertMailMessageMessageDataPartType.messagepart;
                else if (lStream.MailboxHandle != null && lStream.Decoding == eDecodingRequired.none && lStream.HasKnownFormatAndLength) lMessageDataPartType = eConvertMailMessageMessageDataPartType.uidsection;
                else return null;

                var lTransferEncoding = pAttachment.TransferEncoding;

                switch (lStream.Decoding)
                {
                    case eDecodingRequired.none:

                        switch (lTransferEncoding)
                        {
                            case TransferEncoding.SevenBit:

                                await lStream.GetKnownFormatAndLengthAsync().ConfigureAwait(false);
                                if (lStream.KnownFormat == fMessageDataFormat.sevenbit) return new cConvertMailMessageContentDetail(lMessageDataPartType, eContentTransferEncoding.sevenbit, fMessageDataFormat.sevenbit);
                                throw new cMailMessageFormException(pMessage, pAttachment, nameof(AttachmentBase.TransferEncoding));

                            case TransferEncoding.EightBit:

                                await lStream.GetKnownFormatAndLengthAsync().ConfigureAwait(false);
                                ;?;
                                if ((lStream.KnownFormat & fMessageDataFormat.binary) != 0) throw new cMailMessageFormException(pMessage, pAttachment, nameof(AttachmentBase.TransferEncoding));
                                return new cConvertMailMessageContentDetail(lMessageDataPartType, eContentTransferEncoding.eightbit, lStream.KnownFormat);

                            case TransferEncoding.Unknown:

                                await lStream.GetKnownFormatAndLengthAsync().ConfigureAwait(false);

                                eContentTransferEncoding lCTE;
                                ;?;
                                if ((lStream.KnownFormat & fMessageDataFormat.binary) != 0) lCTE = eContentTransferEncoding.binary;
                                else if ((lStream.KnownFormat & fMessageDataFormat.eightbit) != 0) lCTE = eContentTransferEncoding.eightbit;
                                else lCTE = eContentTransferEncoding.sevenbit;

                                return new cConvertMailMessageContentDetail(lMessageDataPartType, lCTE, lStream.KnownFormat);

                            default:

                                return null;
                        }

                    case eDecodingRequired.quotedprintable:

                        if (lTransferEncoding == TransferEncoding.Unknown || lTransferEncoding == TransferEncoding.QuotedPrintable) return new cConvertMailMessageContentDetail(lMessageDataPartType, eContentTransferEncoding.quotedprintable, fMessageDataFormat.sevenbit);
                        return null;

                    case eDecodingRequired.base64:

                        if (lTransferEncoding == TransferEncoding.Unknown || lTransferEncoding == TransferEncoding.Base64) return new cConvertMailMessageContentDetail(lMessageDataPartType, eContentTransferEncoding.base64, fMessageDataFormat.sevenbit);
                        return null;
                }

                return null;
            }
        } */
    }
}