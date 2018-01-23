﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Mail;
using System.Text;
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

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            AllowCatenate = pAllowCatenate;
        }

        public override string ToString() => $"{nameof(cMessageAppendData)}({Flags},{Received},{MessageHandle},{AllowCatenate})";
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

        public override string ToString() => $"{nameof(cMessagePartAppendData)}({Flags},{Received},{MessageHandle},{Part},{AllowCatenate})";
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
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));
            long lLength = pStream.Length - pStream.Position;
            if (lLength == 0 || lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (uint)(lLength);
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
    }

    public class cMultiPartAppendData : cAppendData
    {
        public readonly ReadOnlyCollection<cAppendDataPart> Parts;
        public readonly Encoding Encoding; // for encoded words and mime parameters, nullable (if null the client's encoding is used if required)

        public cMultiPartAppendData(IEnumerable<cAppendDataPart> pParts, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null) : base(pFlags, pReceived)
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
            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));

            Parts = lParts.AsReadOnly();
            Encoding = pEncoding;
        }

        public cMultiPartAppendData(MailMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null) : base(pFlags, pReceived)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            List<cAppendDataPart> lParts = new List<cAppendDataPart>();

            // todo ... convert pMessage to lParts
            throw new NotImplementedException();
            // if can't convert ... throw new ArgumentOutOfRangeException(nameof(pMessage)); 

            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));

            Parts = lParts.AsReadOnly();
            Encoding = pEncoding;
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cMultiPartAppendData));
            lBuilder.Append(Flags);
            lBuilder.Append(Received);
            if (Encoding != null) lBuilder.Append(Encoding.WebName);
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}