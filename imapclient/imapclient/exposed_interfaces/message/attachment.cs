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
        public uint PartSizeInBytes => Part.SizeInBytes;
        public string MD5 => Part.ExtensionData?.MD5;
        public string FileName => Part.Disposition?.FileName;
        public DateTime? CreationDate => Part.Disposition?.CreationDate;
        public DateTime? ModificationDate => Part.Disposition?.ModificationDate;
        public DateTime? ReadDate => Part.Disposition?.ReadDate;
        public uint? Size => Part.Disposition?.Size;
        public cStrings Languages => Part.ExtensionData?.Languages;

        public uint FetchSizeInBytes() => Client.FetchSizeInBytes(Handle, Part);
        public Task<uint> FetchSizeInBytesAsync() => Client.FetchSizeInBytesAsync(Handle, Part);

        public void SaveAs(string pPath, cBodyFetchConfiguration pConfiguration = null)
        {
            using (FileStream lStream = new FileStream(pPath, FileMode.Create))
            {
                Client.Fetch(Handle, Part.Section, Part.DecodingRequired, lStream, pConfiguration);
            }
        }

        public async Task SaveAsAsync(string pPath, cBodyFetchConfiguration pConfiguration = null)
        {
            using (FileStream lStream = new FileStream(pPath, FileMode.Create))
            {
                await Client.FetchAsync(Handle, Part.Section, Part.DecodingRequired, lStream, pConfiguration).ConfigureAwait(false);
            }
        }

        public void Fetch(Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, Part.Section, Part.DecodingRequired, pStream, pConfiguration);
        public Task FetchAsync(Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, Part.Section, Part.DecodingRequired, pStream, pConfiguration);

        // debugging
        public override string ToString() => $"{nameof(cAttachment)}({Handle},{Part.Section})";
    }
}