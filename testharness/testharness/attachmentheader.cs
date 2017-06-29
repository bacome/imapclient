using System;
using work.bacome.imapclient;

namespace testharness
{
    public class cAttachmentHeader
    {
        public readonly cAttachment Attachment;

        public cAttachmentHeader(cAttachment pAttachment)
        {
            Attachment = pAttachment;
        }

        public string Type => Attachment.Type;
        public string SubType => Attachment.SubType;
        public string FileName => Attachment.FileName;
        public uint SizeInBytes => Attachment.SizeInBytes;
    }
}
