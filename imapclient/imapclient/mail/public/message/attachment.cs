using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract class cMailAttachment
    {
        /**<summary>The body-part of the attachment.</summary>*/
        public readonly cSinglePartBody Part;

        internal cMailAttachment(cSinglePartBody pPart)
        {
            Part = pPart;
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
        /// Gets the size in bytes of the encoded attachment.
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
        public DateTimeOffset? CreationDateTimeOffset => Part.Disposition?.CreationDateTimeOffset;

        /// <summary>
        /// Gets the creation date of the attachment (in local time if there is usable time zone information). May be <see langword="null"/>.
        /// </summary>
        public DateTime? CreationDateTime => Part.Disposition?.CreationDateTime;

        /// <summary>
        /// Gets the modification date of the attachment. May be <see langword="null"/>.
        /// </summary>
        public DateTimeOffset? ModificationDateTimeOffset => Part.Disposition?.ModificationDateTimeOffset;

        /// <summary>
        /// Gets the modification date of the attachment (in local time if there is usable time zone information). May be <see langword="null"/>.
        /// </summary>
        public DateTime? ModificationDateTime => Part.Disposition?.ModificationDateTime;

        /// <summary>
        /// Gets the last read date of the attachment. May be <see langword="null"/>.
        /// </summary>
        public DateTimeOffset? ReadDateTimeOffset => Part.Disposition?.ReadDateTimeOffset;

        /// <summary>
        /// Gets the last read date of the attachment (in local time if there is usable time zone information). May be <see langword="null"/>.
        /// </summary>
        public DateTime? ReadDateTime => Part.Disposition?.ReadDateTime;

        /// <summary>
        /// Gets the approximate size of the attachment in bytes. May be <see langword="null"/>.
        /// </summary>
        public uint? ApproximateFileSizeInBytes => Part.Disposition?.Size;

        /// <summary>
        /// Gets the language(s) of the attachment. May be <see langword="null"/>.
        /// </summary>
        public cStrings Languages => Part.ExtensionData?.Languages;

        /// <summary>
        /// Returns a stream containing the data of the attachment.
        /// </summary>
        /// <remarks>
        /// The returned stream must be disposed when you are finished with it.
        /// </remarks>
        public abstract Stream GetMessageDataStream();

        /// <summary>
        /// Saves the attachment to the specified path.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        public abstract void SaveAs(string pPath, cSetMaximumConfiguration pConfiguration = null);

        /// <summary>
        /// Asynchronously saves the attachment to the specified path.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public abstract Task SaveAsAsync(string pPath, cSetMaximumConfiguration pConfiguration = null);

        protected void YSetFileTimes(string pPath, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailAttachment), nameof(YSetFileTimes), pPath);

            if (Part.Disposition == null) return;

            if (Part.Disposition.CreationDateTime != null)
            {
                try { File.SetCreationTime(pPath, Part.Disposition.CreationDateTime.Value); }
                catch (Exception e) { lContext.TraceException("failed to setcreationtime", e); }
            }

            if (Part.Disposition.ModificationDateTime != null)
            {
                try { File.SetLastWriteTime(pPath, Part.Disposition.ModificationDateTime.Value); }
                catch (Exception e) { lContext.TraceException("failed to setlastwritetime", e); }
            }

            if (Part.Disposition.ReadDateTime != null)
            {
                try { File.SetLastAccessTime(pPath, Part.Disposition.ReadDateTime.Value); }
                catch (Exception e) { lContext.TraceException("failed to setlastaccesstime", e); }
            }
        }
    }
}