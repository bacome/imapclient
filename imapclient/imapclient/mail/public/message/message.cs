using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace work.bacome.mailclient
{
    public abstract class cMailMessage
    {
        internal cMailMessage() { }

        /// <summary>
        /// Gets the envelope data of the message.
        /// </summary>
        public abstract cEnvelope Envelope { get; }

        /// <summary>
        /// Gets the sent date of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public abstract DateTimeOffset? SentDateTimeOffset { get; }

        /// <summary>
        /// Gets the sent date of the message from the <see cref="Envelope"/> (in local time if there is usable time zone information). May be <see langword="null"/>.
        /// </summary>
        public abstract DateTime? SentDateTime { get; }

        // note when doing help: see the the above worked first (for the IMAPMessage copying the summary from here and the remarks from there)
        public abstract cCulturedString Subject { get; }
        public abstract string BaseSubject { get; }
        public abstract cAddresses From { get; }
        public abstract cAddresses Sender { get; }
        public abstract cAddresses ReplyTo { get; }
        public abstract cAddresses To { get; }
        public abstract cAddresses CC { get; }
        public abstract cAddresses BCC { get; }
        public abstract cStrings InReplyTo { get; }
        public abstract string MessageId { get; }
        public abstract uint Size { get; }
        public abstract cBodyPart BodyStructure { get; }
        public abstract fMessageDataFormat Format { get; }
        public abstract List<cMailAttachment> Attachments { get; }

        /// <summary>
        /// Gets the size in bytes of the plain text of the message. May be zero.
        /// </summary>
        /// <remarks>
        /// The library defines plain text as being contained in message body-parts with a MIME type of text/plain and without a disposition of 'attachment'.
        /// If there are alternate versions of a body-part only one of the alternates is considered to be part of the plain text (the first one).
        /// The size returned is the encoded size of the body-parts.
        /// </remarks>
        public uint PlainTextSizeInBytes
        {
            get
            {
                uint lSize = 0;
                foreach (var lPart in YPlainTextParts(BodyStructure)) lSize += lPart.SizeInBytes;
                return lSize;
            }
        }

        public abstract cStrings References { get; }
        public abstract eImportance? Importance { get; }

        /// <summary>
        /// Returns the plain text of the message.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The library defines plain text as being contained in message body-parts with a MIME type of text/plain and without a disposition of 'attachment'.
        /// If there are alternate versions of a body-part only one of the alternates is considered to be part of the plain text (the first one).
        /// The required body-parts are fetched from the server and concatented to yield the result.
        /// The text returned is the decoded text.
        /// </remarks>
        public string PlainText()
        {
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in YPlainTextParts(BodyStructure)) lBuilder.Append(Fetch(lPart));
            return lBuilder.ToString();
        }

        /// <summary>
        /// Ansynchronously returns the plain text of the message.
        /// </summary>
        /// <inheritdoc cref="PlainText" select="returns|remarks"/>
        public abstract Task<string> PlainTextAsync();

        /// <summary>
        /// Returns the content of the specified <see cref="cTextBodyPart"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
        /// <remarks>
        /// The content is decoded if required.
        /// </remarks>
        public abstract string Fetch(cTextBodyPart pPart);

        /// <summary>
        /// Asynchronously returns the content of the specified <see cref="cTextBodyPart"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <inheritdoc cref="Fetch(cTextBodyPart)" select="returns|remarks"/>
        public abstract Task<string> FetchAsync(cTextBodyPart pPart);

        /// <summary>
        /// Returns the content of the specified <see cref="cSection"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <returns></returns>
        /// <remarks>
        /// The result is not decoded.
        /// </remarks>
        public abstract string Fetch(cSection pSection);

        /// <summary>
        /// Asynchronously returns the content of the specified <see cref="cSection"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <inheritdoc cref="Fetch(cSection)" select="returns|remarks"/>
        public abstract Task<string> FetchAsync(cSection pSection);

        /// <summary>
        /// Fetches the content of the specified <see cref="cSinglePartBody"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress-increment callback and write-size configuration.</param>
        /// <remarks>
        /// Will throw if <paramref name="pPart"/> is not in <see cref="BodyStructure"/>.
        /// The content is decoded if required.
        /// To calculate the number of bytes that have to be fetched, use <see cref="FetchSizeInBytes(cSinglePartBody)"/>. 
        /// (This may be useful if you are intending to display a progress bar.)
        /// </remarks>
        public abstract void Fetch(cSinglePartBody pPart, Stream pStream, cFetchConfiguration pConfiguration = null);

        /// <summary>
        /// Asynchronously fetches the content of the specified <see cref="cSinglePartBody"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress-increment callback and write-size configuration.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Fetch(cSinglePartBody, Stream, cFetchConfiguration)" select="remarks"/>
        public abstract Task FetchAsync(cSinglePartBody pPart, Stream pStream, cFetchConfiguration pConfiguration = null);

        /// <summary>
        /// Fetches the content of the specified <see cref="cSection"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <param name="pDecoding">The decoding that should be applied.</param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress-increment callback and write-size configuration.</param>
        /// <remarks>
        /// If <see cref="cIMAPCapabilities.Binary"/> is in use and the entire body-part (<see cref="cSection.TextPart"/> is <see cref="eSectionTextPart.all"/>) is being fetched then
        /// unless <paramref name="pDecoding"/> is <see cref="eDecodingRequired.none"/> the server will do the decoding that it determines is required (i.e. the decoding specified is ignored).
        /// </remarks>
        public abstract void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchConfiguration pConfiguration = null);

        /// <summary>
        /// Asynchronously fetches the content of the specified <see cref="cSection"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <param name="pDecoding"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress-increment callback and write-size configuration.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Fetch(cSection, eDecodingRequired, Stream, cFetchConfiguration)" select="remarks"/>
        public abstract Task FetchAsync(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchConfiguration pConfiguration = null);

        protected List<cSinglePartBody> YAttachmentParts(cBodyPart pPart)
        {
            // TODO: when we know what languages the user is interested in (on implementation of languages) choose from multipart/alternative options based on language tag

            var lResult = new List<cSinglePartBody>();

            if (pPart is cSinglePartBody lSinglePart)
            {
                if (lSinglePart.Disposition?.TypeCode == eDispositionTypeCode.attachment) lResult.Add(lSinglePart);
            }
            else if (pPart.Disposition?.TypeCode != eDispositionTypeCode.attachment && pPart is cMultiPartBody lMultiPart)
            {
                foreach (var lPart in lMultiPart.Parts)
                {
                    var lParts = YAttachmentParts(lPart);
                    lResult.AddRange(lParts);
                    if (lParts.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }

        protected List<cTextBodyPart> YPlainTextParts(cBodyPart pPart)
        {
            // TODO: when we know what languages the user is interested in (on implementation of languages) choose from multipart/alternative options based on language tag

            List<cTextBodyPart> lResult = new List<cTextBodyPart>();

            if (pPart.Disposition?.TypeCode == eDispositionTypeCode.attachment) return lResult;

            if (pPart is cTextBodyPart lTextPart)
            {
                if (lTextPart.SubTypeCode == eTextBodyPartSubTypeCode.plain) lResult.Add(lTextPart);
            }
            else if (pPart is cMultiPartBody lMultiPart)
            {
                foreach (var lPart in lMultiPart.Parts)
                {
                    var lParts = YPlainTextParts(lPart);
                    lResult.AddRange(lParts);
                    if (lParts.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }
    }
}