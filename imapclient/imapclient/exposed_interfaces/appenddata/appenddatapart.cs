using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Mail;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cAppendDataPart
    {
        internal cAppendDataPart() { }
        internal abstract bool HasContent { get; }
        public static implicit operator cAppendDataPart(cMessage pMessage) => new cMessageAppendDataPart(pMessage);
        public static implicit operator cAppendDataPart(cAttachment pAttachment) => new cMessagePartAppendDataPart(pAttachment);
        public static implicit operator cAppendDataPart(Stream pStream) => new cStreamAppendDataPart(pStream);
    }

    public class cMessageAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly bool AllowCatenate;

        public cMessageAppendDataPart(cMessage pMessage, bool pAllowCatenate = true)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            AllowCatenate = pAllowCatenate;
        }

        internal override bool HasContent => true;

        public override string ToString() => $"{nameof(cMessageAppendDataPart)}({MessageHandle},{AllowCatenate})";
    }

    public class cMessagePartAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cSinglePartBody Part;
        public readonly bool AllowCatenate;

        public cMessagePartAppendDataPart(cMessage pMessage, cSinglePartBody pPart, bool pAllowCatenate = true)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            // check that the part is part of the message
            if (!pMessage.Contains(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

            AllowCatenate = pAllowCatenate;
        }

        public cMessagePartAppendDataPart(cAttachment pAttachment, bool pAllowCatenate = true)
        {
            if (pAttachment == null) throw new ArgumentNullException(nameof(pAttachment));

            Client = pAttachment.Client;
            MessageHandle = pAttachment.MessageHandle;
            Part = pAttachment.Part;
            AllowCatenate = pAllowCatenate;
        }

        internal override bool HasContent => Part.SizeInBytes != 0;

        public override string ToString() => $"{nameof(cMessagePartAppendDataPart)}({MessageHandle},{Part},{AllowCatenate})";
    }

    public class cUIDSectionAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly uint Length;
        public readonly bool AllowCatenate;

        public cUIDSectionAppendDataPart(cMailbox pMailbox, cUID pUID, cSection pSection, uint pLength, bool pAllowCatenate = true)
        {
            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));

            // check that the mailbox is selected (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            Length = pLength;

            AllowCatenate = pAllowCatenate;
        }

        internal override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cUIDSectionAppendDataPart)}({MailboxHandle},{UID},{Section},{Length},{AllowCatenate})";
    }

    public class cFileAppendDataPart : cAppendDataPart
    {
        public readonly string Path;
        public readonly uint Length;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cFileAppendDataPart(string pPath, cBatchSizerConfiguration pReadConfiguration = null)
        {
            if (string.IsNullOrWhiteSpace(pPath)) throw new ArgumentOutOfRangeException(nameof(pPath));

            var lFileInfo = new FileInfo(pPath);
            if (!lFileInfo.Exists || (lFileInfo.Attributes & FileAttributes.Directory) != 0 || lFileInfo.Length == 0 || lFileInfo.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pPath));

            Path = lFileInfo.FullName;
            Length = (uint)lFileInfo.Length;
            ReadConfiguration = pReadConfiguration;
        }

        internal override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cFileAppendDataPart)}({Path},{Length},{ReadConfiguration})";
    }

    public class cStreamAppendDataPart : cAppendDataPart
    {
        public readonly Stream Stream;
        public readonly uint Length;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cStreamAppendDataPart(Stream pStream, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));
            long lLength = pStream.Length - pStream.Position;
            if (lLength < 0 || lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (uint)lLength;
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendDataPart(Stream pStream, uint pLength, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = pLength;
            ReadConfiguration = pReadConfiguration;
        }

        internal override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cStreamAppendDataPart)}({Length},{ReadConfiguration})";
    }
}