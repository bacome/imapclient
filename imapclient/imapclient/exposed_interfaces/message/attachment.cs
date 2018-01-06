using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a message attachment.
    /// </summary>
    /// <remarks>
    /// Instances of this class are only valid whilst the mailbox that they are in remains selected. 
    /// Re-selecting the mailbox will not bring instances back to life.
    /// Instances of this class are only valid whilst the containing mailbox has the same UIDValidity.
    /// </remarks>
    /// <seealso cref="cMessage.Attachments"/>
    public class cAttachment : IEquatable<cAttachment>
    {
        /**<summary>The client that the instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The message that the attachment belongs to.</summary>*/
        public readonly iMessageHandle MessageHandle;
        /**<summary>The body-part of the attachment.</summary>*/
        public readonly cSinglePartBody Part;

        internal cAttachment(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));
        }

        /// <summary>
        /// Gets the MIME type of the attachment as a string.
        /// </summary>
        public string Type => Part.Type;

        /// <summary>
        /// Gets the MIME type of the attachment as a code.
        /// </summary>
        public eBodyPartTypeCode TypeCode => Part.TypeCode;

        /// <summary>
        /// Gets the MIME subtype of the attachment as a string.
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
        /// Gets the MIME content-transfer-encoding of the attachment as a string.
        /// </summary>
        public string ContentTransferEncoding => Part.ContentTransferEncoding;

        /// <summary>
        /// Gets the MIME content-transfer-encoding of the attachment as a code.
        /// </summary>
        public eDecodingRequired DecodingRequired => Part.DecodingRequired;

        /// <summary>
        /// Gets the size in bytes of the encoded attachement.
        /// </summary>
        public uint PartSizeInBytes => Part.SizeInBytes;

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
        /// Gets the approximate size of the attachment in bytes. May be <see langword="null"/>.
        /// </summary>
        public uint? ApproximateFileSizeInBytes => Part.Disposition?.Size;

        /// <summary>
        /// Gets the language(s) of the attachment. May be <see langword="null"/>.
        /// </summary>
        public cStrings Languages => Part.ExtensionData?.Languages;

        public bool IsValid() => ReferenceEquals(Client.SelectedMailboxDetails?.MessageCache, MessageHandle.MessageCache);

        /// <summary>
        /// Gets the number of bytes that will have to come over the network from the server to save the attachment.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This may be smaller than <see cref="PartSizeInBytes"/> if <see cref="DecodingRequired"/>) isn't <see cref="eDecodingRequired.none"/> and <see cref="cCapabilities.Binary"/> is in use.
        /// The size may have to be fetched from the server, but once fetched it will be cached.
        /// </remarks>
        public uint FetchSizeInBytes() => Client.FetchSizeInBytes(MessageHandle, Part);

        /// <summary>
        /// Asynchronously gets the number of bytes that will have to come over the network from the server to save the attachment
        /// </summary>
        /// <inheritdoc cref="FetchSizeInBytes" select="returns|remarks"/>
        public Task<uint> FetchSizeInBytesAsync() => Client.FetchSizeInBytesAsync(MessageHandle, Part);

        public uint? DecodedSizeInBytes() => Client.DecodedSizeInBytes(MessageHandle, Part);

        public Task<uint?> DecodedSizeInBytesAsync() => Client.DecodedSizeInBytesAsync(MessageHandle, Part);

        /// <summary>
        /// Saves the attachment to the specified path.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        public void SaveAs(string pPath, cBodyFetchConfiguration pConfiguration = null)
        {
            using (FileStream lStream = new FileStream(pPath, FileMode.Create))
            {
                Client.Fetch(MessageHandle, Part.Section, Part.DecodingRequired, lStream, pConfiguration);
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
                await Client.FetchAsync(MessageHandle, Part.Section, Part.DecodingRequired, lStream, pConfiguration).ConfigureAwait(false);
            }

            if (Part.Disposition?.CreationDate != null) File.SetCreationTime(pPath, Part.Disposition.CreationDate.Value);
            if (Part.Disposition?.ModificationDate != null) File.SetLastWriteTime(pPath, Part.Disposition.ModificationDate.Value);
            if (Part.Disposition?.ReadDate != null) File.SetLastAccessTime(pPath, Part.Disposition.ReadDate.Value);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cAttachment pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cAttachment;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode() 
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + MessageHandle.GetHashCode();
                lHash = lHash * 23 + Part.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cAttachment)}({MessageHandle},{Part.Section})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cAttachment pA, cAttachment pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Client.Equals(pB.Client) && pA.MessageHandle.Equals(pB.MessageHandle) && pA.Part.Equals(pB.Part);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cAttachment pA, cAttachment pB) => !(pA == pB);
    }
}