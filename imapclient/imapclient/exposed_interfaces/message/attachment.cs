using System;
using System.IO;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cAttachment
    {
        public readonly cIMAPClient Client;
        public readonly cMailboxId MailboxId;
        public readonly iMessageHandle Handle;
        public readonly cSinglePartBody Part;

        public cAttachment(cIMAPClient pClient, cMailboxId pMailboxId, iMessageHandle pHandle, cSinglePartBody pPart)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MailboxId = pMailboxId ?? throw new ArgumentNullException(nameof(pMailboxId));
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
        public uint SizeInBytes => Part.SizeInBytes;
        public string MD5 => Part.ExtensionData?.MD5;
        public string FileName => Part.Disposition?.FileName;
        public DateTime? CreationDate => Part.Disposition?.CreationDate;
        public DateTime? ModificationDate => Part.Disposition?.ModificationDate;
        public DateTime? ReadDate => Part.Disposition?.ReadDate;
        public uint? Size => Part.Disposition?.Size;
        public cStrings Languages => Part.ExtensionData?.Languages;

        public void Fetch(Stream pStream, cFetchControl pFC = null) => Client.Fetch(MailboxId, Handle, Part.Section, Part.DecodingRequired, pStream, pFC);
        public Task FetchAsync(Stream pStream, cFetchControl pFC = null) => Client.FetchAsync(MailboxId, Handle, Part.Section, Part.DecodingRequired, pStream, pFC);

        ;?; // saveas methods

        // debugging
        public override string ToString() => $"{nameof(cAttachment)}({MailboxId},{Handle},{Part.Section})";
    }
}