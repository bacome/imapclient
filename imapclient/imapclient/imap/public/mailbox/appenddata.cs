using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    // NOTE that any changes here need to be reflected in the SMTP cSendData classes ...

        /*
    public abstract class cAppendData
    {
        public readonly fMessageDataFormat Format;
        public readonly cStorableFlags Flags;
        public readonly DateTime? Received;

        internal cAppendData(fMessageDataFormat pFormat, cStorableFlags pFlags, DateTime? pReceived)
        {
            Format = pFormat;
            Flags = pFlags;
            Received = pReceived;
        }

        public static implicit operator cAppendData(cIMAPMessage pMessage) => new cMessageAppendData(pMessage);
        public static implicit operator cAppendData(cMessageData pMessageData) => new cMessageDataAppendData(pMessageData);
    }

    public class cMessageAppendData : cAppendData
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;

        public cMessageAppendData(cIMAPMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pMessage.Format, pFlags, pReceived)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
        }

        public override string ToString() => $"{nameof(cMessageAppendData)}({Format},{Flags},{Received},{MessageHandle})";
    }

    public class cMessagePartAppendData : cAppendData
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cMessageBodyPart Part;

        public cMessagePartAppendData(cIMAPMessage pMessage, cMessageBodyPart pPart, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pPart.Format, pFlags, pReceived)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            pMessage.CheckPart(pPart);
            Part = pPart;
        }

        public override string ToString() => $"{nameof(cMessagePartAppendData)}({Format},{Flags},{Received},{MessageHandle},{Part})";
    }

    public class cLiteralAppendData : cAppendData
    {
        public readonly cBytes Bytes;

        public cLiteralAppendData(IEnumerable<byte> pBytes, fMessageDataFormat pFormat, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pFormat, pFlags, pReceived)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            var lBytes = new List<byte>(pBytes);
            if (lBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pBytes));
            Bytes = new cBytes(lBytes);
        }

        public cLiteralAppendData(string pString, fMessageDataFormat pFormat, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pFormat, pFlags, pReceived)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            var lBytes = Encoding.UTF8.GetBytes(pString);
            if (lBytes.Length == 0) throw new ArgumentOutOfRangeException(nameof(pString));
            Bytes = new cBytes(lBytes);
        }

        public override string ToString() => $"{nameof(cLiteralAppendData)}({Format},{Flags},{Received},{Bytes})";
    }

    public class cFileAppendData : cAppendData
    {
        public readonly string Path;
        public readonly uint Length;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cFileAppendData(string pPath, fMessageDataFormat pFormat, cStorableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pReadConfiguration = null) : base(pFormat, pFlags, pReceived)
        {
            if (string.IsNullOrWhiteSpace(pPath)) throw new ArgumentOutOfRangeException(nameof(pPath));

            var lFileInfo = new FileInfo(pPath);
            if (!lFileInfo.Exists || (lFileInfo.Attributes & FileAttributes.Directory) != 0 || lFileInfo.Length == 0 || lFileInfo.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pPath));

            Path = lFileInfo.FullName;
            Length = (uint)lFileInfo.Length;
            ReadConfiguration = pReadConfiguration;
        }

        public override string ToString() => $"{nameof(cFileAppendData)}({Format},{Flags},{Received},{Path},{Length},{ReadConfiguration})";
    }

    public class cStreamAppendData : cAppendData
    {
        public readonly Stream Stream;
        public readonly uint Length;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cStreamAppendData(Stream pStream, fMessageDataFormat pFormat, cStorableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pReadConfiguration = null) : base(pFormat, pFlags, pReceived)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek || pStream.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (uint)pStream.Length;
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendData(Stream pStream, uint pLength, fMessageDataFormat pFormat, cStorableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pReadConfiguration = null) : base(pFormat, pFlags, pReceived)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            Length = pLength;
            ReadConfiguration = pReadConfiguration;
        }

        public override string ToString() => $"{nameof(cStreamAppendData)}({Format},{Flags},{Received},{Length},{ReadConfiguration})";
    }

    public class cMessageDataAppendData : cAppendData
    {
        public readonly cMessageData MessageData;

        public cMessageDataAppendData(cMessageData pMessageData, cStorableFlags pFlags = null, DateTime? pReceived = null) : base(pMessageData.Format, pFlags, pReceived)
        {
            MessageData = pMessageData ?? throw new ArgumentNullException(nameof(pMessageData)); ;
        }

        public override string ToString() => $"{nameof(cMessageDataAppendData)}({Format},{Flags},{Received},{MessageData})";
    } */
}