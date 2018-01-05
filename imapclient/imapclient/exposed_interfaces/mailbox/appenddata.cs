using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Mail;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cAppendData
    {
        public readonly cStorableFlags Flags;
        public readonly DateTime? Received;

        internal cAppendData(cStorableFlags pFlags, DateTime? pReceived)
        {
            Flags = pFlags;
            Received = pReceived;
        }

        public static implicit operator cAppendData(cMessage pMessage) => new cMessageAppendData(pMessage);
        public static implicit operator cAppendData(string pString) => new cStringAppendData(pString);
        public static implicit operator cAppendData(Stream pStream) => new cStreamAppendData(pStream);
        public static implicit operator cAppendData(List<cAppendDataPart> pParts) => new cMultiPartAppendData(pParts);
        public static implicit operator cAppendData(MailMessage pMessage) => new cMultiPartAppendData(pMessage);
    }

    public class cMessageAppendData : cAppendData
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly bool AllowCatenate;

        public cMessageAppendData(cMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, bool pAllowCatenate = true) : base(pFlags, pReceived)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);

            AllowCatenate = pAllowCatenate;
        }

        public override string ToString() => $"{nameof(cMessageAppendData)}({Flags},{Received},{MessageHandle},{AllowCatenate})";

        public static implicit operator cMessageAppendData(cMessage pMessage) => new cMessageAppendData(pMessage);
    }

    public class cMessagePartAppendData : cAppendData
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cMessageBodyPart Part;
        public readonly bool AllowCatenate;

        public cMessagePartAppendData(cMessage pMessage, cMessageBodyPart pPart, cStorableFlags pFlags = null, DateTime? pReceived = null, bool pAllowCatenate = true) : base(pFlags, pReceived)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);

            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            // check that the part is part of the message
            if (!pMessage.Contains(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

            AllowCatenate = pAllowCatenate;
        }

        public override string ToString() => $"{nameof(cMessagePartAppendData)}({Flags},{Received},{MessageHandle},{Part},{AllowCatenate})";
    }

    public class cStringAppendData : cAppendData
    {
        public readonly string String;

        public cStringAppendData(string pString, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pFlags, pReceived)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
            if (String.Length == 0) throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public override string ToString() => $"{nameof(cStringAppendData)}({Flags},{Received},{String})";

        public static implicit operator cStringAppendData(string pString) => new cStringAppendData(pString);
    }

    public class cFileAppendData : cAppendData
    {
        public readonly string Path;
        public readonly uint Length;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cFileAppendData(string pPath, cStorableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pReadConfiguration = null) : base(pFlags, pReceived)
        {
            if (string.IsNullOrWhiteSpace(pPath)) throw new ArgumentOutOfRangeException(nameof(pPath));

            var lFileInfo = new FileInfo(pPath);
            if (!lFileInfo.Exists || (lFileInfo.Attributes & FileAttributes.Directory) != 0 || lFileInfo.Length == 0 || lFileInfo.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pPath));

            Path = lFileInfo.FullName;
            Length = (uint)lFileInfo.Length;
            ReadConfiguration = pReadConfiguration;
        }

        public override string ToString() => $"{nameof(cFileAppendData)}({Flags},{Received},{Path},{Length},{ReadConfiguration})";
    }

    public class cStreamAppendData : cAppendData
    {
        public readonly Stream Stream;
        public readonly uint Length;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cStreamAppendData(Stream pStream, cStorableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pReadConfiguration = null) : base(pFlags, pReceived)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek || pStream.Length == 0 || pStream.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            pStream.Position = 0;
            Length = (uint)(pStream.Length);
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendData(Stream pStream, uint pLength, cStorableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pReadConfiguration = null) : base(pFlags, pReceived)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            Length = pLength;
            ReadConfiguration = pReadConfiguration;
        }

        public override string ToString() => $"{nameof(cStreamAppendData)}({Flags},{Received},{Length},{ReadConfiguration})";

        public static implicit operator cStreamAppendData(Stream pStream) => new cStreamAppendData(pStream);
    }

    public class cMultiPartAppendData : cAppendData
    {
        public readonly ReadOnlyCollection<cAppendDataPart> Parts;

        public cMultiPartAppendData(IEnumerable<cAppendDataPart> pParts, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pFlags, pReceived)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            List<cAppendDataPart> lParts = new List<cAppendDataPart>();

            bool lHasContent = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lParts.Add(lPart);
                if (lPart.HasContent) lHasContent = true;
            }

            if (!lHasContent) throw new ArgumentOutOfRangeException(nameof(pParts));

            Parts = lParts.AsReadOnly();
        }

        public cMultiPartAppendData(MailMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pFlags, pReceived)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // todo ...

            throw new NotImplementedException();

            throw new cMailMessageException();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cMultiPartAppendData));
            lBuilder.Append(Flags);
            lBuilder.Append(Received);
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }

        public static implicit operator cMultiPartAppendData(List<cAppendDataPart> pParts) => new cMultiPartAppendData(pParts);
        public static implicit operator cMultiPartAppendData(MailMessage pMessage) => new cMultiPartAppendData(pMessage);
    }

    public abstract class cAppendDataPart
    {
        internal cAppendDataPart() { }
        public abstract bool HasContent { get; }
        public static implicit operator cAppendDataPart(cMessage pMessage) => new cMessageAppendDataPart(pMessage);
        public static implicit operator cAppendDataPart(cAttachment pAttachment) => new cMessagePartAppendDataPart(pAttachment);
        public static implicit operator cAppendDataPart(string pString) => new cStringAppendDataPart(pString);
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

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);

            AllowCatenate = pAllowCatenate;
        }

        public override bool HasContent => true;

        public override string ToString() => $"{nameof(cMessageAppendDataPart)}({MessageHandle},{AllowCatenate})";

        public static implicit operator cMessageAppendDataPart(cMessage pMessage) => new cMessageAppendDataPart(pMessage);
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

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);

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

        public override bool HasContent => Part.SizeInBytes != 0;

        public override string ToString() => $"{nameof(cMessagePartAppendDataPart)}({MessageHandle},{Part},{AllowCatenate})";

        public static implicit operator cMessagePartAppendDataPart(cAttachment pAttachment) => new cMessagePartAppendDataPart(pAttachment);
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

            Client = pMailbox.Client;
            MailboxHandle = pMailbox.MailboxHandle;

            if (!ReferenceEquals(MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            Length = pLength;

            AllowCatenate = pAllowCatenate;
        }

        public override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cUIDSectionAppendDataPart)}({MailboxHandle},{UID},{Section},{Length},{AllowCatenate})";
    }

    public class cStringAppendDataPart : cAppendDataPart
    {
        public readonly string String;

        public cStringAppendDataPart(string pString)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
        }

        public override bool HasContent => String.Length > 0;

        public override string ToString() => $"{nameof(cStringAppendDataPart)}({String})";

        public static implicit operator cStringAppendDataPart(string pString) => new cStringAppendDataPart(pString);
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
            if (!lFileInfo.Exists || (lFileInfo.Attributes | FileAttributes.Directory) != 0 || lFileInfo.Length == 0 || lFileInfo.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pPath));

            Path = lFileInfo.FullName;
            Length = (uint)lFileInfo.Length;
            ReadConfiguration = pReadConfiguration;
        }

        public override bool HasContent => Length != 0;

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
            if (!pStream.CanRead || !pStream.CanSeek || pStream.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            pStream.Position = 0;
            Length = (uint)pStream.Length;
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendDataPart(Stream pStream, uint pLength, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = pLength;
            ReadConfiguration = pReadConfiguration;
        }

        public override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cStreamAppendDataPart)}({Length},{ReadConfiguration})";

        public static implicit operator cStreamAppendDataPart(Stream pStream) => new cStreamAppendDataPart(pStream);
    }
}