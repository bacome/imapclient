using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cAttachment
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle Handle;
        public readonly cSinglePartBody Part;

        public cAttachment(cIMAPClient pClient, iMessageHandle pHandle, cSinglePartBody pPart)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));
        }

        public string Type => Part.Type;
        public eBodyPartTypeCode TypeCode => Part.TypeCode;
        public string SubType => Part.SubType;
        public cBodyPartParameters Parameters => Part.Parameters;
        public string ContentId => Part.ContentId;
        public cCulturedString Description => Part.Description;
        public string ContentTransferEncoding => Part.ContentTransferEncoding;
        public eDecodingRequired DecodingRequired => Part.DecodingRequired;
        public int PartSizeInBytes => (int)Part.SizeInBytes;
        public string MD5 => Part.ExtensionData?.MD5;
        public string FileName => Part.Disposition?.FileName;
        public DateTime? CreationDate => Part.Disposition?.CreationDate;
        public DateTime? ModificationDate => Part.Disposition?.ModificationDate;
        public DateTime? ReadDate => Part.Disposition?.ReadDate;
        public int? ApproximateFileSizeInBytes => Part.Disposition?.Size;
        public cStrings Languages => Part.ExtensionData?.Languages;

        public int SaveSizeInBytes() => Client.FetchSizeInBytes(Handle, Part);
        public Task<int> SaveSizeInBytesAsync() => Client.FetchSizeInBytesAsync(Handle, Part);

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