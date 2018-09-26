using System;
using System.Collections.Generic;
using System.Linq;
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
        public DateTimeOffset? SentDateTimeOffset => Envelope.SentDateTimeOffset;

        /// <summary>
        /// Gets the sent date of the message from the <see cref="Envelope"/> (in local time if there is usable time zone information). May be <see langword="null"/>.
        /// </summary>
        public DateTime? SentDateTime => Envelope.SentDateTime;

        /// <summary>
        /// Gets the subject of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public cCulturedString Subject => Envelope.Subject;

        /// <summary>
        /// Gets the base subject of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The base subject is defined RFC 5256 and is the subject with the RE: FW: etc artifacts removed.
        /// </remarks>
        public string BaseSubject => Envelope.BaseSubject;

        /// <summary>
        /// Gets the 'from' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public cAddresses From => Envelope.From;

        /// <summary>
        /// Gets the 'sender' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public cAddresses Sender => Envelope.Sender;

        /// <summary>
        /// Gets the 'reply-to' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public cAddresses ReplyTo => Envelope.ReplyTo;

        /// <summary>
        /// Gets the 'to' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public cAddresses To => Envelope.To;

        /// <summary>
        /// Gets the 'CC' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public cAddresses CC => Envelope.CC;

        /// <summary>
        /// Gets the 'BCC' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public cAddresses BCC => Envelope.BCC;

        /// <summary>
        /// Gets the normalised 'in-reply-to' message-ids of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Normalised message-ids have the quoting, comments and white space removed.
        /// </remarks>
        public cStrings InReplyTo => Envelope.InReplyTo?.MessageIds;

        /// <summary>
        /// Gets the normalised message-id of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        public string MessageId => Envelope.MsgId?.MessageId;

        public abstract uint Size { get; }

        public abstract cBodyPart BodyStructure { get; }

        public abstract fMessageDataFormat Format { get; }

        public abstract IEnumerable<cMailAttachment> GetAttachments();

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
                foreach (var lPart in ZGetPlainTextParts(BodyStructure)) lSize += lPart.SizeInBytes;
                return lSize;
            }
        }

        public abstract cStrings References { get; }
        public abstract eImportance? Importance { get; }

        internal void CheckPart(cBodyPart pPart)
        {
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));
            var lBodyStructure = BodyStructure;
            ;?; // this ref equals is a problem
            if (ReferenceEquals(lBodyStructure, pPart)) return;
            if (!lBodyStructure.Contains(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
        }

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
        public string GetPlainText()
        {
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in ZGetPlainTextParts(BodyStructure)) lBuilder.Append(Fetch(lPart));
            return lBuilder.ToString();
        }

        /// <summary>
        /// Ansynchronously returns the plain text of the message.
        /// </summary>
        /// <inheritdoc cref="PlainText" select="returns|remarks"/>
        public virtual async Task<string> GetPlainTextAsync()
        {
            var lTasks = new List<Task<string>>(from lPart in ZGetPlainTextParts(BodyStructure) select FetchAsync(lPart));
            await Task.WhenAll(lTasks).ConfigureAwait(false);

            StringBuilder lBuilder = new StringBuilder();
            foreach (var lTask in lTasks) lBuilder.Append(lTask.Result);
            return lBuilder.ToString();
        }

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
        /// Returns a stream containing the data of the message.
        /// </summary>
        /// <remarks>
        /// The returned stream must be disposed when you are finished with it.
        /// </remarks>
        public abstract Stream GetMessageDataStream();

        /// <summary>
        /// Returns a stream containing the data of the specified <see cref="cSinglePartBody"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <remarks>
        /// Will throw if <paramref name="pPart"/> is not in <see cref="BodyStructure"/>.
        /// The data is decoded if required.
        /// The returned stream must be disposed when you are finished with it.
        /// </remarks>
        public abstract Stream GetMessageDataStream(cSinglePartBody pPart, bool pDecoded = true);

        /// <summary>
        /// Returns a stream containing the data of the specified <see cref="cSection"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <param name="pDecoding">The decoding that should be applied.</param>
        /// <remarks>
        /// If <see cref="cIMAPCapabilities.Binary"/> is in use and the entire body-part (<see cref="cSection.TextPart"/> is <see cref="eSectionTextPart.all"/>) is being fetched then
        /// unless <paramref name="pDecoding"/> is <see cref="eDecodingRequired.none"/> the server will do the decoding that it determines is required (i.e. the decoding specified is ignored).
        /// </remarks>
        public abstract Stream GetMessageDataStream(cSection pSection, eDecodingRequired pDecoding = eDecodingRequired.none);

        protected List<cSinglePartBody> YGetAttachmentParts(cBodyPart pPart)
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
                    var lParts = YGetAttachmentParts(lPart);
                    lResult.AddRange(lParts);
                    if (lParts.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }

        private List<cTextBodyPart> ZGetPlainTextParts(cBodyPart pPart)
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
                    var lParts = ZGetPlainTextParts(lPart);
                    lResult.AddRange(lParts);
                    if (lParts.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }
    }
}