using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient;

namespace testharness2
{
    public abstract class cAppendDataSource
    {
        public static cAppendDataSource CurrentData = null;
    }

    public class cAppendDataSourceMessage : cAppendDataSource
    {
        public readonly cMessage Message;

        public cAppendDataSourceMessage(cMessage pMessage)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
        }

        public override string ToString() => Message.ToString(); 
    }

    public class cAppendDataSourceMessagePart : cAppendDataSource
    {
        public readonly cMessage Message;
        public readonly cSinglePartBody Part;

        public cAppendDataSourceMessagePart(cMessage pMessage, cSinglePartBody pPart)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));
        }

        public override string ToString() => Message.ToString() + " " + Part.Section;
    }

    public class cAppendDataSourceString : cAppendDataSource
    {
        public readonly string String;

        public cAppendDataSourceString(string pString)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
        }

        public override string ToString() => String;
    }

    public class cAppendDataSourceFile : cAppendDataSource
    {
        public readonly string Path;
        public readonly bool Base64Encode;
        public readonly bool AsStream;

        public cAppendDataSourceFile(string pPath, bool pBase64Encode, bool pAsStream)
        {
            Path = pPath ?? throw new ArgumentNullException(nameof(pPath));
            Base64Encode = pBase64Encode;
            AsStream = pAsStream;
        }

        public override string ToString() => $"{Path},{Base64Encode},{AsStream}";
    }

    public class cAppendDataSourceStream : cAppendDataSource
    {
        // NOTE: the stream needs to be disposed: but only after it has been 'read'
        public readonly cMessageDataStream Stream;
        public readonly uint Length;
        public readonly bool Base64Encode;

        public cAppendDataSourceStream(cMessageDataStream pStream, uint pLength, bool pBase64Encode)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            Length = pLength;
            Base64Encode = pBase64Encode;
        }

        public override string ToString() => $"{Stream.ToString()},{Length},{Base64Encode}";
    }

    public class cAppendDataSourceAttachment : cAppendDataSource
    {
        public readonly cAttachment Attachment;

        public cAppendDataSourceAttachment(cAttachment pAttachment)
        {
            Attachment = pAttachment ?? throw new ArgumentNullException(nameof(pAttachment));
        }

        public override string ToString() => Attachment.ToString();
    }

    public class cAppendDataSourceUIDSection : cAppendDataSource
    {
        public readonly cMailbox Mailbox;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly uint Length;

        public cAppendDataSourceUIDSection(cMailbox pMailbox, cUID pUID, cSection pSection, uint pLength)
        {
            Mailbox = pMailbox ?? throw new ArgumentNullException(nameof(pMailbox));
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Length = pLength;
        }

        public override string ToString() => $"{Mailbox},{UID},{Section}";
    }

    public class cAppendDataSourceMultiPart : cAppendDataSource
    {
        public readonly ReadOnlyCollection<cAppendDataSource> Parts;

        public cAppendDataSourceMultiPart(IList<cAppendDataSource> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));
            if (pParts.Count == 0) throw new ArgumentOutOfRangeException(nameof(pParts));
            Parts = new ReadOnlyCollection<cAppendDataSource>(pParts);
        }

        public override string ToString() => $"{nameof(cAppendDataSourceMultiPart)}({Parts.Count})";
    }
}