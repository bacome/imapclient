using System;
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

    public class cStringAppendData : cAppendData
    {
        public readonly string String;
        public readonly Encoding Encoding;

        public cStringAppendData(string pString, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null) : base(pFlags, pReceived)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
            if (String.Length == 0) throw new ArgumentOutOfRangeException(nameof(pString));
            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            Encoding = pEncoding;
        }

        public override string ToString() => $"{nameof(cStringAppendData)}({Flags},{Received},{String},{Encoding?.WebName})";
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

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            AllowCatenate = pAllowCatenate;
        }

        public override bool HasContent => true;

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

        public override bool HasContent => Part.SizeInBytes != 0;

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

        public override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cUIDSectionAppendDataPart)}({MailboxHandle},{UID},{Section},{Length},{AllowCatenate})";
    }

    public class cStringAppendDataPart : cAppendDataPart
    {
        public readonly string String;
        public readonly Encoding Encoding; // nullable (if null the multipart's encoding is used)

        public cStringAppendDataPart(string pString, Encoding pEncoding = null)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            Encoding = pEncoding;
        }

        public override bool HasContent => String.Length > 0; // the assumption here is that the encoding will produce something

        public override string ToString() => $"{nameof(cStringAppendDataPart)}({String},{Encoding?.WebName})";
    }

    public enum eEncodedWordLocation { text, comment, word }

    public class cEncodedWordAppendDataPart : cAppendDataPart
    {
        public readonly int BytesAlreadyOnLine;
        public readonly eEncodedWordLocation Location;
        public readonly string String;
        public readonly Encoding Encoding; // nullable (if null the multipart's encoding is used)

        public cEncodedWordAppendDataPart(int pBytesAlreadyOnLine, eEncodedWordLocation pLocation, string pString, Encoding pEncoding = null)
        {
            if (pBytesAlreadyOnLine < 0) throw new ArgumentOutOfRangeException(nameof(pBytesAlreadyOnLine));
            BytesAlreadyOnLine = pBytesAlreadyOnLine;
            String = pString ?? throw new ArgumentNullException(nameof(pString));
            if (pString.Length < 1) throw new ArgumentOutOfRangeException(nameof(pString));
            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            String = pString;
            Encoding = pEncoding;
        }

        public override bool HasContent => true;

        internal List<byte> GetBytes(bool pUTF8Enabled, Encoding pDefaultEncoding)
        {
            if (pDefaultEncoding == null) throw new ArgumentNullException(nameof(pDefaultEncoding));

            List<byte> lResult = new List<byte>();

            if (pUTF8Enabled)
            {
                ;?; // note that this can produce 'blank' lines, if the text ends with white space it will add it => could generate a blank line at end if it folds just before 

                var lStringBytes = Encoding.UTF8.GetBytes(String);

                int lBytesOnCurrentLine = BytesAlreadyOnLine;
                int lFirstByteOnLine = 0;
                int lLastWSP = -1;
                int lLastNonWSP = -1;

                for (int i = 0; i < lStringBytes.Length; i++)
                {
                    var lByte = lStringBytes[i];

                    if (lByte == cASCII.TAB || lByte == cASCII.SPACE) lLastWSP = i;
                    else llastnonWSP = i;

                    if (lBytesOnCurrentLine >= 78 && lLastWSP >= 0)
                    {
                        for (int j = lFirstByteOnLine; j < lLastWSP; j++) lResult.Add(lStringBytes[j]);

                        lResult.Add(cASCII.CR);
                        lResult.Add(cASCII.LF);

                        lBytesOnCurrentLine = 1;
                        lFirstByteOnLine = lLastWSP;
                        lLastWSP = -1;
                    }
                    else lBytesOnCurrentLine++;
                }

                for (int j = lFirstByteOnLine; j < lStringBytes.Length; j++) lResult.Add(lStringBytes[j]);

                return lResult;
            }

            var lEncoding = Encoding ?? pDefaultEncoding;

            ;?;
        }

        public override string ToString() => $"{nameof(cEncodedWordAppendDataPart)}({BytesAlreadyOnLine},{String},{Encoding?.WebName})";

        internal static void _Tests(cTrace.cContext pParentContext)
        {
            ZTestUTF8Enabled(0, "a", "a");
            ZTestUTF8Enabled(0, " ", " ");
            //                      abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz                         x                
            //                      abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz                        
            //ZTestUTF8Enabled(0,  "12345678901234567890123456789012345678901234567890123456789012345678901234567890")
            ZTestUTF8Enabled(  0 , "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz\r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(  23, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz\r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(  24, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz\r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(  25, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz\r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(  26, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz\r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz\r\n abcdefghijklmnopqrstuvwxyz");

            ;?; // probably going to coalese white space and trim from the end so these need revision

            ZTestUTF8Enabled(   0, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz  abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz \r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(   0, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz  abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz \r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(   0, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz   abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz  \r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(   0, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz                         abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz                        \r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(   0, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz                          abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz                         \r\n  abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");





            ZTestUTF8Enabled(   0, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz  abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz \r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");
            ZTestUTF8Enabled(   0, "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz  abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz \r\n abcdefghijklmnopqrstuvwxyz abcdefghijklmnopqrstuvwxyz");

        }
    }

    public class cMimeParameterAppendDataPart : cAppendDataPart
    {
        public readonly string Attribute;
        public readonly string Value; // nullable (then the value must be encoded as a quoted-string
        public readonly Encoding Encoding; // nullable (if null the multipart's encoding is used)

        public cMimeParameterAppendDataPart(string pAttribute, string pValue, Encoding pEncoding = null)
        {
            if (pAttribute == null) throw new ArgumentNullException(nameof(pAttribute));
            if (pAttribute.Length == 0) throw new ArgumentOutOfRangeException(nameof(pAttribute));
            if (!cCharset.RFC2047Token.ContainsAll(pAttribute)) throw new ArgumentOutOfRangeException(nameof(pAttribute));
            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            Attribute = pAttribute;
            Value = pValue;
            Encoding = pEncoding;
        }

        public override bool HasContent => true;

        internal List<byte> GetBytes(bool pUTF8Enabled, Encoding pDefaultEncoding)
        {

        }

        public override string ToString() => $"{nameof(cMimeParameterAppendDataPart)}({Attribute},{Value},{Encoding?.WebName})";
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

        public override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cStreamAppendDataPart)}({Length},{ReadConfiguration})";
    }
}