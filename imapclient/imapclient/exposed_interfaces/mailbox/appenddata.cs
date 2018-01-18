using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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

    public enum eEncodedWordsLocation { unstructured, ccontent, qcontent }

    public class cEncodedWordsAppendDataPart : cAppendDataPart
    {
        // designed to handle UTF8 on or off, nothing else
        //  in particular: it does not make an arbitrary string safe to be used in the location specified: this is the responsibility of the composing code
        //  all this class does is convert words containing non-ascii characters to either unadorned UTF8 (if UTF8 is in use) or encoded words
        //  it is the caller's job to make sure that the ascii characters in the string are safe to use in the location 

        public readonly eEncodedWordsLocation Location;
        public readonly string String;
        public readonly Encoding Encoding; // nullable (if null the multipart's encoding is used)

        public cEncodedWordsAppendDataPart(eEncodedWordsLocation pLocation, string pString, Encoding pEncoding = null)
        {
            Location = pLocation;
            String = pString ?? throw new ArgumentNullException(nameof(pString));
            if (pString.Length < 1) throw new ArgumentOutOfRangeException(nameof(pString));
            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            String = pString;
            Encoding = pEncoding;
        }

        public override bool HasContent => true;

        internal IList<byte> GetBytes(bool pUTF8Enabled, Encoding pDefaultEncoding)
        {
            if (pDefaultEncoding == null) throw new ArgumentNullException(nameof(pDefaultEncoding));

            if (pUTF8Enabled) return Encoding.UTF8.GetBytes(String);

            List<byte> lResult = new List<byte>();

            var lEncoding = Encoding ?? pDefaultEncoding;
            var lCharsetName = cTools.CharsetName(lEncoding);

            char lChar;
            List<char> lWSPChars = new List<char>();
            List<char> lWordChars = new List<char>();
            List<char> lEncodeableWordChars = new List<char>(); // the word chars less the quotes on quoted pairs
            bool lWordHasNonASCIIChars = false;
            List<char> lCharsToEncode = new List<char>();
            bool lLastWordWasAnEncodedWord = false;

            // loop through the lines

            int lStartIndex = 0;

            while (true)
            {
                int lIndex = String.IndexOf("\r\n", lStartIndex);

                string lLine;

                if (lIndex == -1) lLine = String.Substring(lStartIndex);
                else if (lIndex == lStartIndex) lLine = string.Empty;
                else lLine = String.Substring(lStartIndex, lIndex - lStartIndex);

                // loop through the words

                int lPosition = 0;

                while (lPosition < lLine.Length)
                {
                    // extract optional white space

                    while (lPosition < lLine.Length)
                    {
                        lChar = lLine[lPosition];

                        if (lChar != '\t' && lChar != ' ') break;

                        lWSPChars.Add(lChar);
                        lPosition++;
                    }

                    // extract optional word

                    bool lInQuotedPair = false;

                    while (lPosition < lLine.Length)
                    {
                        lChar = lLine[lPosition];

                        if (lInQuotedPair)
                        {
                            lWordChars.Add(lChar);
                            lEncodeableWordChars.Add(lChar);
                            lInQuotedPair = false;
                        }
                        else
                        {
                            if (lChar == '\t' || lChar == ' ') break;

                            lWordChars.Add(lChar);

                            if (lChar == '\\' && Location != eEncodedWordsLocation.unstructured) lInQuotedPair = true;
                            else lEncodeableWordChars.Add(lChar);
                        }

                        if (lChar > cChar.DEL) lWordHasNonASCIIChars = true;

                        lPosition++;
                    }

                    // process the white space and word

                    if (lWordHasNonASCIIChars || ZLooksLikeAnEncodedWord(lWordChars))
                    {
                        if (lCharsToEncode.Count == 0 && lWSPChars.Count > 0) lResult.AddRange(ZToASCIIBytes(lWSPChars));
                        if (lCharsToEncode.Count > 0 || lLastWordWasAnEncodedWord) lCharsToEncode.Add(' ');
                        lCharsToEncode.AddRange(lEncodeableWordChars);
                    }
                    else
                    {
                        if (lCharsToEncode.Count > 0)
                        {
                            lResult.AddRange(ZToEncodedWordBytes(lCharsToEncode, lEncoding, lCharsetName));
                            lLastWordWasAnEncodedWord = true;
                            lCharsToEncode.Clear();
                        }

                        if (lWSPChars.Count > 0) lResult.AddRange(ZToASCIIBytes(lWSPChars));

                        if (lWordChars.Count > 0)
                        {
                            lResult.AddRange(ZToASCIIBytes(lWordChars));
                            lLastWordWasAnEncodedWord = false;
                        }
                    }

                    // prepare for next wsp word pair

                    lWSPChars.Clear();
                    lWordChars.Clear();
                    lEncodeableWordChars.Clear();
                    lWordHasNonASCIIChars = false;
                }

                if (lCharsToEncode.Count > 0)
                {
                    lResult.AddRange(ZToEncodedWordBytes(lCharsToEncode, lEncoding, lCharsetName));
                    lLastWordWasAnEncodedWord = true;
                    lCharsToEncode.Clear();
                }

                // line and loop termination

                if (lIndex == -1) break;

                lResult.Add(cASCII.CR);
                lResult.Add(cASCII.LF);

                lStartIndex = lIndex + 2;

                if (lStartIndex == String.Length) break;
            }

            return lResult;
        }

        private bool ZLooksLikeAnEncodedWord(List<char> pChars)
        {
            if (pChars.Count < 9) return false;
            if (pChars[0] == '=' && pChars[1] == '?' && pChars[pChars.Count - 2] == '?' && pChars[pChars.Count - 1] == '=') return true;
            return false;
        }

        private List<byte> ZToASCIIBytes(List<char> pChars)
        {
            List<byte> lResult = new List<byte>(pChars.Count);
            foreach (char lChar in pChars) lResult.Add((byte)lChar);
            return lResult;
        }

        private List<byte> ZToEncodedWordBytes(List<char> pChars, Encoding pEncoding, List<byte> pCharsetName)
        {
            StringInfo lString = new StringInfo(new string(pChars.ToArray()));

            int lMaxEncodedByteCount = 75 - 7 - pCharsetName.Count;

            List<byte> lResult = new List<byte>();

            int lFromTextElement = 0;
            int lTextElementCount = 1;

            int lLastTextElementCount = 0;
            byte lLastEncoding = cASCII.NUL;
            List<byte> lLastEncodedText = null;

            while (lFromTextElement + lTextElementCount <= lString.LengthInTextElements)
            {
                var lBytes = pEncoding.GetBytes(lString.SubstringByTextElements(lFromTextElement, lTextElementCount));

                var lQEncodedText = ZQEncode(lBytes);
                var lBEncodedText = cBase64.Encode(lBytes);

                if (lTextElementCount == 1 || lQEncodedText.Count <= lMaxEncodedByteCount || lBEncodedText.Count <= lMaxEncodedByteCount)
                {
                    lLastTextElementCount = lTextElementCount;

                    if (lQEncodedText.Count < lBEncodedText.Count)
                    {
                        lLastEncoding = cASCII.q;
                        lLastEncodedText = lQEncodedText;
                    }
                    else
                    {
                        lLastEncoding = cASCII.b;
                        lLastEncodedText = lBEncodedText;
                    }
                }

                if (lQEncodedText.Count > lMaxEncodedByteCount && lBEncodedText.Count > lMaxEncodedByteCount)
                {
                    lResult.AddRange(ZToEncodedWord(lFromTextElement, pCharsetName, lLastEncoding, lLastEncodedText));

                    lFromTextElement = lFromTextElement + lLastTextElementCount;
                    lTextElementCount = 1;
                }
                else lTextElementCount++;
            }

            if (lFromTextElement < lString.LengthInTextElements) lResult.AddRange(ZToEncodedWord(lFromTextElement, pCharsetName, lLastEncoding, lLastEncodedText));

            return lResult;
        }

        private List<byte> ZQEncode(byte[] pBytes)
        {
            List<byte> lResult = new List<byte>();

            foreach (var lByte in pBytes)
            {
                bool lEncode;

                if (lByte <= cASCII.SPACE || lByte == cASCII.EQUALS || lByte == cASCII.QUESTIONMARK || lByte == cASCII.UNDERSCORE || lByte >= cASCII.DEL) lEncode = true;
                else if (Location == eEncodedWordsLocation.ccontent) lEncode = lByte == cASCII.LPAREN || lByte == cASCII.RPAREN || lByte == cASCII.BACKSL;
                else if (Location == eEncodedWordsLocation.qcontent) lEncode = lByte == cASCII.DQUOTE || lByte == cASCII.BACKSL;
                else lEncode = false;

                if (lEncode)
                {
                    lResult.Add(cASCII.EQUALS);
                    lResult.AddRange(cTools.ByteToHexBytes(lByte));
                }
                else lResult.Add(lByte);
            }

            return lResult;
        }

        private List<byte> ZToEncodedWord(int pFromTextElement, List<byte> pCharsetName, byte pEncoding, List<byte> pEncodedText)
        {
            List<byte> lBytes = new List<byte>(76);
            if (pFromTextElement > 0) lBytes.Add(cASCII.SPACE);
            lBytes.Add(cASCII.EQUALS);
            lBytes.Add(cASCII.QUESTIONMARK);
            lBytes.AddRange(pCharsetName);
            lBytes.Add(cASCII.QUESTIONMARK);
            lBytes.Add(pEncoding);
            lBytes.Add(cASCII.QUESTIONMARK);
            lBytes.AddRange(pEncodedText);
            lBytes.Add(cASCII.QUESTIONMARK);
            lBytes.Add(cASCII.EQUALS);
            return lBytes;
        }

        public override string ToString() => $"{nameof(cEncodedWordsAppendDataPart)}({Location},{String},{Encoding?.WebName})";

        internal static void _Tests(cTrace.cContext pParentContext)
        {
            ZTest("8.1.1", eEncodedWordsLocation.qcontent, "Keld Jørn Simonsen", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen", null, "ISO-8859-1");
            ZTest("8.1.2", eEncodedWordsLocation.qcontent, "Keld Jøørn Simonsen", "Keld =?iso-8859-1?b?Svj4cm4=?= Simonsen", null, "ISO-8859-1"); // should switch to base64
            ZTest("8.1.3", eEncodedWordsLocation.qcontent, "Keld Jørn Simonsen", "Keld =?utf-8?b?SsO4cm4=?= Simonsen"); // should use utf8

            // adjacent words that need to be encoded are encoded together with one space between them
            ZTest("joins.1", eEncodedWordsLocation.qcontent, "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d    fr€d    fred    fred ", "    =?utf-8?b?QfCgiJNDIEHwoIiTQw==?= fred =?utf-8?b?ZnLigqxkIGZy4oKsZA==?= fred  =?utf-8?b?ZnLigqxkIGZy4oKsZA==?=    fred    fred ", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d fr€d    fred    fred ");

            // if a line ends with an encoded word and the next line begins with an encoded word, a space is added to the beginning of the second encoded word to prevent them being joined on decoding
            ZTest("spaces.1", eEncodedWordsLocation.qcontent, "    A𠈓C\r\n A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ", null, "    A𠈓C A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ");

            // check that adjacent encoded words are in fact joined
            ZTest("long.1", eEncodedWordsLocation.qcontent,
                " 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234\r\n 1234567890123456789012345678901234567890€12345678901\r\n 1234567890123456789012345678901234567890€123456789012",
                null,
                " 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234 1234567890123456789012345678901234567890€12345678901 1234567890123456789012345678901234567890€123456789012"
                );

            // check that each encoded word is a whole number of characters
            ZTest("charcounting.1", eEncodedWordsLocation.qcontent, " 𠈓𠈓𠈓𠈓𠈓a𠈓𠈓𠈓𠈓𠈓𠈓 fred 𠈓𠈓𠈓𠈓𠈓ab𠈓𠈓𠈓𠈓𠈓𠈓\r\n \r\n", " =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNh8KCIk/CgiJPwoIiT8KCIk/CgiJPwoIiT?= fred =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNhYvCgiJPwoIiT8KCIk/CgiJPwoIiT?= =?utf-8?b?8KCIkw==?=\r\n \r\n");

            // q-encoding rule checks
            
            //  unstructured - e.g. subject
            ZTest("q.1", eEncodedWordsLocation.unstructured, "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J\"#$%&'(),.:;<>@[\\]^`{|}~=F8rn?= Simonsen", null, "ISO-8859-1");

            //  ccontent - in a comment
            ZTest("q.2", eEncodedWordsLocation.ccontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            //  qcontent - in a quoted string
            ZTest("q.3", eEncodedWordsLocation.qcontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J=22#$%&'(),.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            // check that a word that looks like an encoded word gets encoded
            ;?;




            //ZTest("8.1.1", eEncodedWordsLocation.qcontent, "Keld Jørn Simonsen", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen", null, "ISO-8859-1");


            //;?; // more tests to do
        }

        private static void ZTest(string pTestName, eEncodedWordsLocation pLocation, string pString, string pExpectedI = null, string pExpectedF = null, string pCharsetName = null)
        {
            Encoding lEncoding;
            if (pCharsetName == null) lEncoding = null;
            else lEncoding = Encoding.GetEncoding(pCharsetName);
            cEncodedWordsAppendDataPart lEW = new cEncodedWordsAppendDataPart(pLocation, pString, lEncoding);
            var lBytes = lEW.GetBytes(false, Encoding.UTF8);

            cCulturedString lCS = new cCulturedString(lBytes);

            string lString;

            // for stepping through
            lString = cTools.ASCIIBytesToString(lBytes);

            lString = lCS.ToString();
            if (lString != (pExpectedF ?? pString)) throw new cTestsException($"{nameof(cEncodedWordsAppendDataPart)}({pTestName}.f : {lString})");

            if (pExpectedI == null) return;

            lString = cTools.ASCIIBytesToString(lBytes);
            if (lString != pExpectedI) throw new cTestsException($"{nameof(cEncodedWordsAppendDataPart)}({pTestName}.i : {lString})");
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
            throw new NotImplementedException();
            // TODO!
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