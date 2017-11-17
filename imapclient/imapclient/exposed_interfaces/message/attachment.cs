using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a message attachment.
    /// </summary>
    public class cAttachment
    {
        /**<summary>The client that this instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The message that the attachment belongs to.</summary>*/
        public readonly iMessageHandle Handle;
        /**<summary>The body-part of the attachment.</summary>*/
        public readonly cSinglePartBody Part;

        internal cAttachment(cIMAPClient pClient, iMessageHandle pHandle, cSinglePartBody pPart)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));
        }

        /// <summary>
        /// Gets the MIME type of the attachment in text form.
        /// </summary>
        public string Type => Part.Type;

        /// <summary>
        /// Gets the MIME type of the attachment in code form.
        /// </summary>
        public eBodyPartTypeCode TypeCode => Part.TypeCode;

        /// <summary>
        /// Gets the MIME subtype of the attachment in text form.
        /// </summary>
        public string SubType => Part.SubType;

        /// <summary>
        /// Gets the MIME type parameters of the attachment. May be <see langword="null"/>.
        /// </summary>
        public cBodyStructureParameters Parameters => Part.Parameters;

        /// <summary>
        /// Gets the MIME content-id of the attachment. May be <see langword="null"/>.
        /// </summary>
        public string ContentId => Part.ContentId;

        /// <summary>
        /// Gets the MIME content description of the attachment. May be <see langword="null"/>.
        /// </summary>
        public cCulturedString Description => Part.Description;

        /// <summary>
        /// Gets the MIME content transfer encoding of the attachment in text form.
        /// </summary>
        public string ContentTransferEncoding => Part.ContentTransferEncoding;

        /// <summary>
        /// Gets the MIME content transfer encoding of the attachment in code form.
        /// </summary>
        public eDecodingRequired DecodingRequired => Part.DecodingRequired;

        /// <summary>
        /// Gets the size in bytes of the encoded attachement.
        /// </summary>
        public int PartSizeInBytes => (int)Part.SizeInBytes;

        /// <summary>
        /// Gets the MD5 value of the attachment. May be <see langword="null"/>.
        /// </summary>
        public string MD5 => Part.ExtensionData?.MD5;

        /// <summary>
        /// Gets the suggested filename of the attachment. May be <see langword="null"/>.
        /// </summary>
        public string FileName => Part.Disposition?.FileName;

        /// <summary>
        /// Gets the creation date of the attachment. May be <see langword="null"/>.
        /// </summary>
        public DateTime? CreationDate => Part.Disposition?.CreationDate;

        /// <summary>
        /// Gets the modification date of the attachment. May be <see langword="null"/>.
        /// </summary>
        public DateTime? ModificationDate => Part.Disposition?.ModificationDate;

        /// <summary>
        /// Gets the last read date of the attachment. May be <see langword="null"/>.
        /// </summary>
        public DateTime? ReadDate => Part.Disposition?.ReadDate;

        /// <summary>
        /// Gets the approximate size in bytes of the attachment. May be <see langword="null"/>.
        /// </summary>
        public int? ApproximateFileSizeInBytes => Part.Disposition?.Size;

        /// <summary>
        /// Gets the language(s) of the attachment. May be <see langword="null"/>.
        /// </summary>
        public cStrings Languages => Part.ExtensionData?.Languages;

        /// <summary>
        /// Gets the number of bytes that will have to come over the network from the IMAP server to save this attachment.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This may be smaller than <see cref="PartSizeInBytes"/> if <see cref="DecodingRequired"/>) isn't <see cref="eDecodingRequired.none"/> and <see cref="cCapabilities.Binary"/> is in use.
        /// The size may have to be fetched from the server, but once fetched it will be cached.
        /// </remarks>
        public int SaveSizeInBytes() => Client.FetchSizeInBytes(Handle, Part);

        /// <summary>
        /// Asynchronously gets the number of bytes that will have to come over the network from the IMAP server to save this attachment
        /// </summary>
        /// <inheritdoc cref="SaveSizeInBytes" select="returns|remarks"/>
        public Task<int> SaveSizeInBytesAsync() => Client.FetchSizeInBytesAsync(Handle, Part);

        /// <summary>
        /// Saves the attachment to the specified path.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        public void SaveAs(string pPath, cBodyFetchConfiguration pConfiguration = null)
        {
            using (FileStream lStream = new FileStream(pPath, FileMode.Create))
            {
                Client.Fetch(Handle, Part.Section, Part.DecodingRequired, lStream, pConfiguration);
            }

            if (Part.Disposition?.CreationDate != null) File.SetCreationTime(pPath, Part.Disposition.CreationDate.Value);
            if (Part.Disposition?.ModificationDate != null) File.SetLastWriteTime(pPath, Part.Disposition.ModificationDate.Value);
            if (Part.Disposition?.ReadDate != null) File.SetLastAccessTime(pPath, Part.Disposition.ReadDate.Value);
        }

        /// <summary>
        /// Asynchronously saves the attachment to the specified path.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public async Task SaveAsAsync(string pPath, cBodyFetchConfiguration pConfiguration = null)
        {
            using (FileStream lStream = new FileStream(pPath, FileMode.Create))
            {
                await Client.FetchAsync(Handle, Part.Section, Part.DecodingRequired, lStream, pConfiguration).ConfigureAwait(false);
            }

            if (Part.Disposition?.CreationDate != null) File.SetCreationTime(pPath, Part.Disposition.CreationDate.Value);
            if (Part.Disposition?.ModificationDate != null) File.SetLastWriteTime(pPath, Part.Disposition.ModificationDate.Value);
            if (Part.Disposition?.ReadDate != null) File.SetLastAccessTime(pPath, Part.Disposition.ReadDate.Value);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cAttachment)}({Handle},{Part.Section})";
    }
}