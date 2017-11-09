using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Provides an API that allows interaction with an IMAP attachment.
    /// </summary>
    public class cAttachment
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle Handle;

        /// <summary>
        /// The message body part that this attachment refers to.
        /// </summary>
        public readonly cSinglePartBody Part;

        public cAttachment(cIMAPClient pClient, iMessageHandle pHandle, cSinglePartBody pPart)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));
        }

        /// <summary>
        /// The MIME type of the attachment in text form.
        /// </summary>
        public string Type => Part.Type;

        /// <summary>
        /// The MIME type of the attachment in code form.
        /// </summary>
        public eBodyPartTypeCode TypeCode => Part.TypeCode;

        /// <summary>
        /// The MIME subtype of the attachment in text form.
        /// </summary>
        public string SubType => Part.SubType;

        /// <summary>
        /// The MIME type parameters of the attachment.
        /// </summary>
        public cBodyStructureParameters Parameters => Part.Parameters;

        /// <summary>
        /// The MIME content-id of the attachment.
        /// </summary>
        public string ContentId => Part.ContentId;

        /// <summary>
        /// The MIME content description of the attachment.
        /// </summary>
        public cCulturedString Description => Part.Description;

        /// <summary>
        /// The MIME content transfer encoding of the attachment in text form.
        /// </summary>
        public string ContentTransferEncoding => Part.ContentTransferEncoding;

        /// <summary>
        /// The MIME content transfer encoding of the attachment in code form.
        /// </summary>
        public eDecodingRequired DecodingRequired => Part.DecodingRequired;

        /// <summary>
        /// The size in bytes of the encoded attachement.
        /// </summary>
        public int PartSizeInBytes => (int)Part.SizeInBytes;

        public string MD5 => Part.ExtensionData?.MD5;

        /// <summary>
        /// The suggested filename if provided. May be null.
        /// </summary>
        public string FileName => Part.Disposition?.FileName;

        /// <summary>
        /// The creation date if provided. May be null.
        /// </summary>
        public DateTime? CreationDate => Part.Disposition?.CreationDate;

        /// <summary>
        /// The modification date if provided. May be null.
        /// </summary>
        public DateTime? ModificationDate => Part.Disposition?.ModificationDate;

        /// <summary>
        /// The last read date if provided. May be null.
        /// </summary>
        public DateTime? ReadDate => Part.Disposition?.ReadDate;

        /// <summary>
        /// The approximate size in bytes if provided. May be null.
        /// </summary>
        public int? ApproximateFileSizeInBytes => Part.Disposition?.Size;

        /// <summary>
        /// The language(s) of the attachment.
        /// </summary>
        public cStrings Languages => Part.ExtensionData?.Languages;

        /// <summary>
        /// <para>Gets the number of bytes that will have to come over the network to save this attachment.</para>
        /// <para>If the server can do the decoding this may be smaller than the <see cref="PartSizeInBytes"/>.</para>
        /// </summary>
        /// <returns>The number of bytes.</returns>
        public int SaveSizeInBytes() => Client.FetchSizeInBytes(Handle, Part);
        /**<summary>The async version of <see cref="SaveSizeInBytes"/>.</summary>*/
        public Task<int> SaveSizeInBytesAsync() => Client.FetchSizeInBytesAsync(Handle, Part);

        /// <summary>
        /// Saves the attachment to the specified file.
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

        /**<summary>The async version of <see cref="SaveAs(string, cBodyFetchConfiguration)"/>.</summary>*/
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

        // debugging
        public override string ToString() => $"{nameof(cAttachment)}({Handle},{Part.Section})";
    }
}