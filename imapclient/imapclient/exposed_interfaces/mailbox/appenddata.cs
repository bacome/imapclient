using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Mail;

namespace work.bacome.imapclient
{
    ;?; // tostring, implicit conversions

    public abstract class cAppendData
    {
        public readonly cFetchableFlags Flags;
        public readonly DateTime? Received;

        public cAppendData(cFetchableFlags pFlags, DateTime? pReceived)
        {
            Flags = pFlags;
            Received = pReceived;
        }
    }

    public class cMessageAppendData : cAppendData
    {
        // note that the flags and the receveive should defalt to those of the message


        public readonly cMessage Message;

        public cMessageAppendData(cMessage pMessage, cFetchableFlags pFlags = null, DateTime? pReceived = null) : base(pFlags, pReceived)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(pMessage.MessageHandle.MessageCache.MailboxHandle, pMessage.Client.SelectedMailboxDetails.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), "message must be in a selected mailbox");
        }

        public override string ToString() => $"{nameof(cMessageAppendData)}({Flags},{Received},{Message})";

        public static implicit operator cMessageAppendData(cMessage pMessage) => new cMessageAppendData(pMessage);
    }

    public class cMessagePartAppendData : cAppendData
    {
        // note that the  receveive should defalt to that of the message



        public readonly cMessage Message;
        public readonly cMessageBodyPart Part;

        public cMessagePartAppendData(cMessage pMessage, cMessageBodyPart pPart, cFetchableFlags pFlags = null, DateTime? pReceived = null) : base(pFlags, pReceived)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(pMessage.MessageHandle.MessageCache.MailboxHandle, pMessage.Client.SelectedMailboxDetails.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), "message must be in a selected mailbox");

            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            // check that the part is part of the message
            if (!pMessage.Contains(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
        }

        public override string ToString() => $"{nameof(cMessagePartAppendData)}({Flags},{Received},{Message},{Part})";
    }

    public class cMailMessageAppendData : cAppendData
    {
        public readonly MailMessage Message;
        public readonly cBatchSizerConfiguration Configuration; // optional

        public cMailMessageAppendData(MailMessage pMessage, cFetchableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pConfiguration = null) : base(pFlags, pReceived)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            if (!cTools.MailMessageFormCanBeHandled(pMessage, out var lError)) throw new ArgumentOutOfRangeException(nameof(pMessage), lError);
            Configuration = pConfiguration;
        }

        public override string ToString() => $"{nameof(cMailMessageAppendData)}({Flags},{Received},{Message},{Configuration})";

        public static implicit operator cMailMessageAppendData(MailMessage pMessage) => new cMailMessageAppendData(pMessage);
    }

    public class cStringAppendData : cAppendData
    {
        public readonly string String;

        public cStringAppendData(string pString, cFetchableFlags pFlags = null, DateTime? pReceived = null) : base(pFlags, pReceived)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
            if (String.Length == 0) throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public override string ToString() => $"{nameof(cStringAppendData)}({Flags},{Received},{String})";

        public static implicit operator cStringAppendData(string pString) => new cStringAppendData(pString);
    }

    public class cStreamAppendData : cAppendData
    {
        public readonly Stream Stream;
        public readonly int Length;
        public readonly cBatchSizerConfiguration Configuration; // optional

        public cStreamAppendData(Stream pStream, cFetchableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pConfiguration = null) : base(pFlags, pReceived)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (int)pStream.Length;
            Configuration = pConfiguration;
        }

        public cStreamAppendData(Stream pStream, int pLength, cFetchableFlags pFlags = null, DateTime? pReceived = null, cBatchSizerConfiguration pConfiguration = null) : base(pFlags, pReceived)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            Length = pLength;
            Configuration = pConfiguration;
        }

        public override string ToString() => $"{nameof(cStreamAppendData)}({Flags},{Received},{Length},{Configuration})";

        public static implicit operator cStreamAppendData(Stream pStream) => new cStreamAppendData(pStream);
    }

    public class cMultiPartAppendData : cAppendData
    {
        public readonly ReadOnlyCollection<cAppendDataPart> Parts;

        public cMultiPartAppendData(IEnumerable<cAppendDataPart> pParts, cFetchableFlags pFlags = null, DateTime? pReceived = null) : base(pFlags, pReceived)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));
            Parts = new List<cAppendDataPart>(from p in pParts where p != null select p).AsReadOnly();
            if (Parts.Count == 0) throw new ArgumentOutOfRangeException(nameof(pParts));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cMultiPartAppendData));
            lBuilder.Append(Flags);
            lBuilder.Append(Received);
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }

        public static implicit operator cMultiPartAppendData(List<cAppendDataPart> pParts) => new cMultiPartAppendData(pParts);
    }

    public abstract class cAppendDataPart { }

    public class cMessageAppendDataPart : cAppendDataPart
    {
        public readonly cMessage Message;

        public cMessageAppendDataPart(cMessage pMessage)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(pMessage.MessageHandle.MessageCache.MailboxHandle, pMessage.Client.SelectedMailboxDetails.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), "message must be in a selected mailbox");
        }

        ;?;
    }

    public class cMessagePartAppendDataPart : cAppendDataPart
    {
        public readonly cMessage Message;
        public readonly cSinglePartBody Part;

        public cMessagePartAppendDataPart(cMessage pMessage, cSinglePartBody pPart)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!ReferenceEquals(pMessage.MessageHandle.MessageCache.MailboxHandle, pMessage.Client.SelectedMailboxDetails.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), "message must be in a selected mailbox");

            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            // check that the part is part of the message
            if (!pMessage.Contains(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
        }

        ;?;
    }

    public class cMailMessageAppendDataPart : cAppendDataPart
    {
        public readonly MailMessage MailMessage;
        public readonly cBatchSizerConfiguration Configuration; // optional

        public cMailMessageAppendDataPart(MailMessage pMailMessage, cBatchSizerConfiguration pConfiguration = null)
        {
            MailMessage = pMailMessage ?? throw new ArgumentNullException(nameof(pMailMessage));
            if (!cTools.MailMessageFormCanBeHandled(pMailMessage, out var lError)) throw new ArgumentOutOfRangeException(nameof(pMailMessage), lError);
            Configuration = pConfiguration;
        }

        ;?;
    }

    public class cStringAppendDataPart : cAppendDataPart
    {
        public readonly string String;

        public cStringAppendDataPart(string pString)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
        }

        ;?;
    }

    public class cStreamAppendDataPart : cAppendDataPart
    {
        public readonly Stream Stream;
        public readonly int Length;
        public readonly cBatchSizerConfiguration Configuration; // optional

        public cStreamAppendDataPart(Stream pStream, cBatchSizerConfiguration pConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (int)pStream.Length;
            Configuration = pConfiguration;
        }

        public cStreamAppendDataPart(Stream pStream, int pLength, cBatchSizerConfiguration pConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            Length = pLength;
            Configuration = pConfiguration;
        }

        ;?;
    }
}