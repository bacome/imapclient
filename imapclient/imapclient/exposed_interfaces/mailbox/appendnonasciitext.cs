using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cNonASCIITextAppendDataPart : cAppendDataPart
    {
        protected internal enum eQEncodingRestriction { none, comment, phrase }

        protected internal readonly Encoding mEncoding; // nullable (if null the multipart's encoding is used)

        internal cNonASCIITextAppendDataPart(Encoding pEncoding)
        {
            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            mEncoding = pEncoding;
        }

        // if utf8 is enabled then the default encoding will be null
        internal abstract IList<byte> GetBytes(Encoding pDefaultEncoding);
    }

    public abstract class cEncodedWordsAppendDataPart : cNonASCIITextAppendDataPart
    {
        // it is important that the bytes emitted before this do not end with CRLF or FWS
        //  because the bytes emitted by this may start with FWS (if the first word is an encoded word)

        protected internal readonly string mText;
        protected internal readonly bool mHasContent;

        internal cEncodedWordsAppendDataPart(string pText, bool pAllowToEndWithFWS, Encoding pEncoding) : base(pEncoding)
        {
            mText = pText ?? throw new ArgumentNullException(nameof(pText));
            mHasContent = ZTextValid(pAllowToEndWithFWS);
        }

        private bool ZTextValid(bool pAllowToEndWithFWS)
        {
            int lFWSStage = 0;
            bool lLastWasFWS = false;
            bool lHasContent = false;

            foreach (char lChar in mText)
            {
                switch (lFWSStage)
                {
                    case 0:

                        if (lChar == '\r')
                        {
                            lFWSStage = 1;
                            break;
                        }

                        if (lChar == '\t' || lChar == ' ') break;
                        if (lChar < ' ' || lChar == cChar.DEL) throw new ArgumentOutOfRangeException("pText");

                        lHasContent = true;
                        lLastWasFWS = false;

                        break;

                    case 1:

                        if (lChar != '\n') throw new ArgumentOutOfRangeException("pText");
                        lFWSStage = 2;
                        break;

                    case 2:

                        if (lChar != '\t' && lChar != ' ') throw new ArgumentOutOfRangeException("pText");
                        lFWSStage = 0;
                        lLastWasFWS = true;
                        break;
                }
            }

            if (lFWSStage != 0) throw new ArgumentOutOfRangeException("pText");
            if (lLastWasFWS && !pAllowToEndWithFWS) throw new ArgumentOutOfRangeException("pText");

            return lHasContent;
        }

        internal override bool HasContent => mText.Length > 0;

        protected internal abstract IList<byte> GetBytesForNonEncodedWord(List<char> pWordChars);

        protected internal IList<byte> YGetTextBytes(Encoding pDefaultEncoding, eQEncodingRestriction pQEncodingRestriction)
        {
            Encoding lEncoding;

            if (pDefaultEncoding == null) lEncoding = null;
            else lEncoding = mEncoding ?? pDefaultEncoding;

            cTextBytes lTextBytes = new cTextBytes(lEncoding, pQEncodingRestriction);

            char lChar;
            List<char> lLeadingWSPChars = new List<char>();
            List<char> lWordChars = new List<char>();
            List<char> lEncodedWordLeadingWSPChars = new List<char>();
            List<char> lEncodedWordWordChars = new List<char>();
            bool lLastWordWasAnEncodedWord = false;

            // loop through the lines

            int lStartIndex = 0;

            while (true)
            {
                int lIndex = mText.IndexOf("\r\n", lStartIndex);

                string lInput;

                if (lIndex == -1) lInput = mText.Substring(lStartIndex);
                else if (lIndex == lStartIndex) lInput = string.Empty;
                else lInput = mText.Substring(lStartIndex, lIndex - lStartIndex);

                // loop through the words

                int lPosition = 0;

                while (lPosition < lInput.Length)
                {
                    // extract optional white space

                    while (lPosition < lInput.Length)
                    {
                        lChar = lInput[lPosition];

                        if (lChar != '\t' && lChar != ' ') break;

                        lLeadingWSPChars.Add(lChar);
                        lPosition++;
                    }

                    // extract optional non-wsp

                    while (lPosition < lInput.Length)
                    {
                        lChar = lInput[lPosition];

                        if (lChar == '\t' || lChar == ' ') break;

                        lWordChars.Add(lChar);
                        lPosition++;
                    }

                    // process the white space and word

                    if (lEncoding != null && (ZWordContainsNonASCII(lWordChars) || ZLooksLikeAnEncodedWord(lWordChars)))
                    {
                        if (lEncodedWordWordChars.Count == 0) lEncodedWordLeadingWSPChars.AddRange(lLeadingWSPChars);
                        if (lEncodedWordWordChars.Count > 0 || lLastWordWasAnEncodedWord) lEncodedWordWordChars.Add(' ');
                        lEncodedWordWordChars.AddRange(lWordChars);
                    }
                    else
                    {
                        if (lEncodedWordWordChars.Count > 0)
                        {
                            lTextBytes.AddEncodedWords(lEncodedWordLeadingWSPChars, lEncodedWordWordChars);
                            lLastWordWasAnEncodedWord = true;
                            lEncodedWordLeadingWSPChars.Clear();
                            lEncodedWordWordChars.Clear();
                        }

                        lTextBytes.AddNonEncodedWord(lLeadingWSPChars, GetBytesForNonEncodedWord(lWordChars));

                        if (lWordChars.Count > 0) lLastWordWasAnEncodedWord = false;
                    }

                    // prepare for next wsp word pair

                    lLeadingWSPChars.Clear();
                    lWordChars.Clear();
                }

                // output the cached encoded word chars if any

                if (lEncodedWordWordChars.Count > 0)
                {
                    lTextBytes.AddEncodedWords(lEncodedWordLeadingWSPChars, lEncodedWordWordChars);
                    lLastWordWasAnEncodedWord = true;
                    lEncodedWordLeadingWSPChars.Clear();
                    lEncodedWordWordChars.Clear();
                }

                // line and loop termination

                if (lIndex == -1) break;

                lTextBytes.AddNewLine();

                lStartIndex = lIndex + 2;

                if (lStartIndex == mText.Length) break;
            }

            return lTextBytes.Bytes;
        }

        protected internal List<byte> YASCIICharsToBytes(List<char> pChars, string pQuoteTheseChars)
        {
            List<byte> lBytes = new List<byte>();

            foreach (char lChar in pChars)
            {
                if (pQuoteTheseChars.IndexOf(lChar) != -1) lBytes.Add(cASCII.BACKSL);
                lBytes.Add((byte)lChar);
            }

            return lBytes;
        }

        private bool ZWordContainsNonASCII(List<char> pWordChars)
        {
            foreach (var lChar in pWordChars) if (lChar >= cChar.DEL) return true;
            return false;
        }

        private bool ZLooksLikeAnEncodedWord(List<char> pWordChars)
        {
            if (pWordChars.Count < 9) return false;
            if (pWordChars[0] == '=' && pWordChars[1] == '?' && pWordChars[pWordChars.Count - 2] == '?' && pWordChars[pWordChars.Count - 1] == '=') return true;
            return false;
        }

        private class cTextBytes
        {
            private static readonly cBytes kEQUALSQUESTIONMARK = new cBytes("=?");
            private static readonly cBytes kQUESTIONMARKEQUALS = new cBytes("?=");
            private static readonly cBytes kCRLF = new cBytes("\r\n");
            private static readonly cBytes kCRLFSPACE = new cBytes("\r\n ");

            private readonly Encoding mEncoding;
            private readonly eQEncodingRestriction mQEncodingRestriction;
            private readonly List<byte> mCharsetNameBytes;

            private readonly List<byte> mBytes = new List<byte>();

            private bool mCurrentLineHasEncodedWords = false;
            private int mCurrentLineByteCount = 0;
            private bool mCurrentLineByteCountKnown = false;

            public cTextBytes(Encoding pEncoding, eQEncodingRestriction pQEncodingRestriction)
            {
                mEncoding = pEncoding;
                mQEncodingRestriction = pQEncodingRestriction;

                if (pEncoding == null) mCharsetNameBytes = null;
                else mCharsetNameBytes = cTools.CharsetNameBytes(pEncoding);
            }

            public void AddNonEncodedWord(IList<char> pLeadingWSPChars, IList<byte> pWordBytes)
            {
                if (mCurrentLineHasEncodedWords && mCurrentLineByteCount + pLeadingWSPChars.Count + pWordBytes.Count > 76)
                {
                    mBytes.AddRange(kCRLFSPACE);
                    mCurrentLineHasEncodedWords = false;
                    mCurrentLineByteCount = 1;
                }
                else
                {
                    foreach (char lChar in pLeadingWSPChars) mBytes.Add((byte)lChar);
                    mCurrentLineByteCount += pLeadingWSPChars.Count;
                }

                mBytes.AddRange(pWordBytes);
                mCurrentLineByteCount += pWordBytes.Count;
            }

            public void AddEncodedWords(List<char> pLeadingWSPChars, List<char> pWordChars)
            {
                StringInfo lString = new StringInfo(new string(pWordChars.ToArray()));

                int lMaxEncodedByteCount = 75 - 7 - mCharsetNameBytes.Count;

                List<byte> lResult = new List<byte>();

                int lFromTextElement = 0;
                int lTextElementCount = 1;

                int lLastTextElementCount = 0;
                byte lLastEncoding = cASCII.NUL;
                List<byte> lLastEncodedTextBytes = null;

                while (lFromTextElement + lTextElementCount <= lString.LengthInTextElements)
                {
                    var lBytes = mEncoding.GetBytes(lString.SubstringByTextElements(lFromTextElement, lTextElementCount));

                    var lQEncodedTextBytes = ZQEncode(lBytes);
                    var lBEncodedTextBytes = cBase64.Encode(lBytes);

                    if (lTextElementCount == 1 || lQEncodedTextBytes.Count <= lMaxEncodedByteCount || lBEncodedTextBytes.Count <= lMaxEncodedByteCount)
                    {
                        lLastTextElementCount = lTextElementCount;

                        if (lQEncodedTextBytes.Count < lBEncodedTextBytes.Count)
                        {
                            lLastEncoding = cASCII.q;
                            lLastEncodedTextBytes = lQEncodedTextBytes;
                        }
                        else
                        {
                            lLastEncoding = cASCII.b;
                            lLastEncodedTextBytes = lBEncodedTextBytes;
                        }
                    }

                    if (lQEncodedTextBytes.Count > lMaxEncodedByteCount && lBEncodedTextBytes.Count > lMaxEncodedByteCount)
                    {
                        ZAddEncodedWord(pLeadingWSPChars, lLastEncoding, lLastEncodedTextBytes, true);
                        lFromTextElement = lFromTextElement + lLastTextElementCount;
                        lTextElementCount = 1;
                        pLeadingWSPChars.Clear();
                    }
                    else lTextElementCount++;
                }

                if (lFromTextElement < lString.LengthInTextElements) ZAddEncodedWord(pLeadingWSPChars, lLastEncoding, lLastEncodedTextBytes, false);
            }

            public void AddNewLine()
            {
                mBytes.AddRange(kCRLF);
                mCurrentLineHasEncodedWords = false;
                mCurrentLineByteCount = 0;
                mCurrentLineByteCountKnown = true;
            }

            public IList<byte> Bytes => mBytes;

            private List<byte> ZQEncode(byte[] pBytes)
            {
                List<byte> lResult = new List<byte>();

                foreach (var lByte in pBytes)
                {
                    bool lEncode;

                    if (lByte <= cASCII.SPACE || lByte == cASCII.EQUALS || lByte == cASCII.QUESTIONMARK || lByte == cASCII.UNDERSCORE || lByte >= cASCII.DEL) lEncode = true;
                    else if (mQEncodingRestriction == eQEncodingRestriction.comment) lEncode = lByte == cASCII.LPAREN || lByte == cASCII.RPAREN || lByte == cASCII.BACKSL;
                    else if (mQEncodingRestriction == eQEncodingRestriction.phrase)
                    {
                        if (lByte == cASCII.EXCLAMATION || lByte == cASCII.ASTERISK || lByte == cASCII.PLUS || lByte == cASCII.HYPEN || lByte == cASCII.SLASH) lEncode = false;
                        else if (lByte < cASCII.ZERO) lEncode = true;
                        else if (lByte <= cASCII.NINE) lEncode = false;
                        else if (lByte < cASCII.A) lEncode = true;
                        else if (lByte <= cASCII.Z) lEncode = false;
                        else if (lByte < cASCII.a) lEncode = true;
                        else if (lByte <= cASCII.z) lEncode = false;
                        else lEncode = true;
                    }
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

            private void ZAddEncodedWord(List<char> pLeadingWSPChars, byte pEncoding, List<byte> pEncodedTextBytes, bool pMoreToCome)
            {
                if (!mCurrentLineByteCountKnown || mCurrentLineByteCount + pLeadingWSPChars.Count + 7 + mCharsetNameBytes.Count + pEncodedTextBytes.Count > 76)
                {
                    mBytes.AddRange(kCRLFSPACE);
                    mCurrentLineByteCount = 1;
                    mCurrentLineByteCountKnown = true;
                }
                else
                {
                    foreach (char lChar in pLeadingWSPChars) mBytes.Add((byte)lChar);
                    mCurrentLineByteCount += pLeadingWSPChars.Count;
                }

                mBytes.AddRange(kEQUALSQUESTIONMARK);
                mBytes.AddRange(mCharsetNameBytes);
                mBytes.Add(cASCII.QUESTIONMARK);
                mBytes.Add(pEncoding);
                mBytes.Add(cASCII.QUESTIONMARK);
                mBytes.AddRange(pEncodedTextBytes);
                mBytes.AddRange(kQUESTIONMARKEQUALS);

                if (pMoreToCome)
                {
                    mBytes.AddRange(kCRLFSPACE);
                    mCurrentLineHasEncodedWords = false;
                    mCurrentLineByteCount = 1;
                }
                else
                {
                    mCurrentLineHasEncodedWords = true;
                    mCurrentLineByteCount = mCurrentLineByteCount + 7 + mCharsetNameBytes.Count + pEncodedTextBytes.Count;
                }
            }
        }
    }

    public class cUnstructuredTextAppendDataPart : cEncodedWordsAppendDataPart
    {
        // the text must be valid RFC 5322/6532 unstructured (excluding obs-unstruct)
        //  converts the text to UTF8 or encoded words (where required) if UTF8 is not in use

        public cUnstructuredTextAppendDataPart(string pText, Encoding pEncoding = null) : base(pText, false, pEncoding) { }

        protected internal override IList<byte> GetBytesForNonEncodedWord(List<char> pWordChars) => YASCIICharsToBytes(pWordChars, string.Empty);

        internal override IList<byte> GetBytes(Encoding pDefaultEncoding)
        {
            if (pDefaultEncoding == null) return Encoding.UTF8.GetBytes(mText);
            return YGetTextBytes(pDefaultEncoding, eQEncodingRestriction.none);
        }

        public override string ToString() => $"{nameof(cUnstructuredTextAppendDataPart)}({mText},{mEncoding?.WebName})";









        internal static void _Tests(cTrace.cContext pParentContext)
        {
            ZTest("8.1.1", "Keld Jørn Simonsen", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen", null, "ISO-8859-1");
            ZTest("8.1.2", "Keld Jøørn Simonsen", "Keld =?iso-8859-1?b?Svj4cm4=?= Simonsen", null, "ISO-8859-1"); // should switch to base64
            ZTest("8.1.3", "Keld Jørn Simonsen", "Keld =?utf-8?b?SsO4cm4=?= Simonsen"); // should use utf8

            // adjacent words that need to be encoded are encoded together with one space between them
            ZTest("joins.1", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d    fr€d    fred    fred ", "    =?utf-8?b?QfCgiJNDIEHwoIiTQw==?= fred =?utf-8?b?ZnLigqxkIGZy4oKsZA==?= fred  =?utf-8?b?ZnLigqxkIGZy4oKsZA==?=    fred    fred ", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d fr€d    fred    fred ");

            // if a line ends with an encoded word and the next line begins with an encoded word, a space is added to the beginning of the second encoded word to prevent them being joined on decoding
            ZTest("spaces.1", "    A𠈓C\r\n A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ", null, "    A𠈓C A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ");

            // check that adjacent encoded words are in fact joined
            ZTest("long.1",
                " 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234\r\n 1234567890123456789012345678901234567890€12345678901\r\n 1234567890123456789012345678901234567890€123456789012",
                null,
                " 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234 1234567890123456789012345678901234567890€12345678901 1234567890123456789012345678901234567890€123456789012"
                );

            // check that each encoded word is a whole number of characters
            ZTest("charcounting.1", " 𠈓𠈓𠈓𠈓𠈓a𠈓𠈓𠈓𠈓𠈓𠈓 fred 𠈓𠈓𠈓𠈓𠈓ab𠈓𠈓𠈓𠈓𠈓𠈓\r\n \r\n", " =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNh8KCIk/CgiJPwoIiT8KCIk/CgiJPwoIiT?= fred =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNhYvCgiJPwoIiT8KCIk/CgiJPwoIiT?= =?utf-8?b?8KCIkw==?=\r\n \r\n");

            // q-encoding rule checks

            //  unstructured - e.g. subject
            ZTest("q.1", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J\"#$%&'(),.:;<>@[\\]^`{|}~=F8rn?= Simonsen", null, "ISO-8859-1");

            //  ccontent - in a comment
            //ZTest("q.2", eEncodedWordsLocation.ccontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            //  qcontent - in a quoted string
            //ZTest("q.3", eEncodedWordsLocation.qcontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J=22#$%&'(),.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            // check that a word that looks like an encoded word gets encoded
            //;?;




            //ZTest("8.1.1", eEncodedWordsLocation.qcontent, "Keld Jørn Simonsen", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen", null, "ISO-8859-1");


            //;?; // more tests to do
        }


        private static void ZTest(string pTestName, string pString, string pExpectedI = null, string pExpectedF = null, string pCharsetName = null)
        {
            Encoding lEncoding;
            if (pCharsetName == null) lEncoding = null;
            else lEncoding = Encoding.GetEncoding(pCharsetName);
            cUnstructuredTextAppendDataPart lEW = new cUnstructuredTextAppendDataPart(pString, lEncoding);
            var lBytes = lEW.GetBytes(Encoding.UTF8);

            cCulturedString lCS = new cCulturedString(lBytes);

            string lString;

            // for stepping through
            lString = cTools.ASCIIBytesToString(lBytes);

            lString = lCS.ToString();
            if (lString != (pExpectedF ?? pString)) throw new cTestsException($"{nameof(cUnstructuredTextAppendDataPart)}({pTestName}.f : {lString})");

            if (pExpectedI == null) return;

            lString = cTools.ASCIIBytesToString(lBytes);
            if (lString != pExpectedI) throw new cTestsException($"{nameof(cUnstructuredTextAppendDataPart)}({pTestName}.i : {lString})");
        }

    }

    public class cCommentTextAppendDataPart : cEncodedWordsAppendDataPart
    {
        // the text is the text of the comment, without the surrounding ()
        //  the text must be valid RFC 5322 FWS or 5234/6532 VCHARs; embedded comments (including the surrounding ()) should not be in the text (these should be separated out into other appenddata objects at a higher level) 
        //  adds 'quoted-pair's where required (for () and \ characters) and converts the text to UTF8 or encoded words (where required) if UTF8 is not in use

        public cCommentTextAppendDataPart(string pText, Encoding pEncoding = null) : base(pText, true, pEncoding) { }

        protected internal override IList<byte> GetBytesForNonEncodedWord(List<char> pWordChars) => YASCIICharsToBytes(pWordChars, "()\\");

        internal override IList<byte> GetBytes(Encoding pDefaultEncoding)
        {
            if (pDefaultEncoding == null) return ZUTF8Bytes();
            return YGetTextBytes(pDefaultEncoding, eQEncodingRestriction.comment);
        }

        private byte[] ZUTF8Bytes()
        {
            List<char> lChars = new List<char>();

            foreach (var lChar in mText)
            {
                if (lChar == '(' || lChar == ')' || lChar == '\\') lChars.Add('\\');
                lChars.Add(lChar);
            }

            return Encoding.UTF8.GetBytes(lChars.ToArray());
        }

        public override string ToString() => $"{nameof(cCommentTextAppendDataPart)}({mText},{mEncoding?.WebName})";
    }

    public class cPhraseTextAppendDataPart : cEncodedWordsAppendDataPart
    {
        // the text is the text of (part of) the phrase
        //  the text must be valid RFC 5322 FWS or 5234/6532 VCHARs; embedded comments (including the surrounding ()) should not be in the text (these should be separated out into other appenddata objects at a higher level) 
        //  converts the text into atoms, 'quoted-string's (with 'quoted-pair's where required for \ and " characters) in UTF8 or encoded word format if UTF8 is not in use
        // if the text has no VCHARS then the output will be an empty quoted-string

        private static readonly cBytes kDQUOTEDQUOTE = new cBytes("\"\"");

        public cPhraseTextAppendDataPart(string pText, Encoding pEncoding = null) : base(pText, true, pEncoding) { }

        protected internal override IList<byte> GetBytesForNonEncodedWord(List<char> pWordChars)
        {
            if (cCharset.AText.ContainsAll(pWordChars)) return YASCIICharsToBytes(pWordChars, string.Empty);

            List<byte> lBytes = new List<byte>();

            lBytes.Add(cASCII.DQUOTE);

            foreach (byte lByte in Encoding.UTF8.GetBytes(pWordChars.ToArray()))
            {
                if (lByte == cASCII.DQUOTE || lByte == cASCII.BACKSL) lBytes.Add(cASCII.BACKSL);
                lBytes.Add(lByte);
            }

            lBytes.Add(cASCII.DQUOTE);

            return lBytes;
        }

        internal override IList<byte> GetBytes(Encoding pDefaultEncoding)
        {
            if (!mHasContent) return kDQUOTEDQUOTE;
            if (pDefaultEncoding == null) return YGetTextBytes(null, eQEncodingRestriction.none);
            return YGetTextBytes(pDefaultEncoding, eQEncodingRestriction.phrase);
        }

        public override string ToString() => $"{nameof(cPhraseTextAppendDataPart)}({mText},{mEncoding?.WebName})";







    }

    public class cMIMEParameterAppendDataPart : cNonASCIITextAppendDataPart
    {
        public readonly string Attribute;
        public readonly string Value; // nullable (then the value must be encoded as a quoted-string
        public readonly Encoding Encoding; // nullable (if null the multipart's encoding is used)

        public cMimeParameterAppendDataPart(string pAttribute, string pValue, Encoding pEncoding = null) : base(
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



    public class cNonASCIITextAppendDataPartx : cAppendDataPart
    {
        // designed to handle UTF8 on or off, nothing else
        //  in particular: it does not make an arbitrary string safe to be used in the location specified: this is the responsibility of the composing code 
        //   i.e.
        //    in unstructuredtext all chars should be FWS, VCHAR or non-ascii
        //    in commenttext the leading and terminating ( ) chars should NOT be in the text (they should be appended using a string), any ascii that is not a ctext char should be quoted, and embedded comments are not handled (these should be split out as separate objects)
        //    in phrase 
        //    
        //  it is the caller's job to make sure that the ascii characters in the string are safe to use in the location 
        //  in particular, commenttext cannot include an embedded comment

        // for header field values
        private readonly eTextLocation mLocation;
        private readonly string mText;

        // for mime parameters
        private readonly string mAttribute;
        private readonly string mValue; // nullable (then the value must be encoded as a quoted-string

        // for both
        private readonly Encoding mEncoding; // nullable (if null the multipart's encoding is used)

        public cNonASCIITextAppendDataPart(eTextLocation pLocation, string pText, Encoding pEncoding = null)
        {
            mLocation = pLocation;

            mText = pText ?? throw new ArgumentNullException(nameof(pText));
            if (pText.Length < 1) throw new ArgumentOutOfRangeException(nameof(pText));

            mAttribute = null;
            mValue = null;

            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            mEncoding = pEncoding;
        }

        public cNonASCIITextAppendDataPart(string pAttribute, string pValue, Encoding pEncoding = null)
        {
            mLocation = 0;
            mText = null;

            if (pAttribute == null) throw new ArgumentNullException(nameof(pAttribute));
            if (pAttribute.Length == 0) throw new ArgumentOutOfRangeException(nameof(pAttribute));
            if (!cCharset.RFC2047Token.ContainsAll(pAttribute)) throw new ArgumentOutOfRangeException(nameof(pAttribute));

            mAttribute = pAttribute;
            mValue = pValue;

            if (pEncoding != null && !cCommandPartFactory.TryAsCharsetName(pEncoding, out _)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            mEncoding = pEncoding;
        }

        public override bool HasContent => true;

        internal IList<byte> GetBytes(bool pUTF8Enabled, Encoding pDefaultEncoding)
        {
            if (pDefaultEncoding == null) throw new ArgumentNullException(nameof(pDefaultEncoding));

            if (mAttribute == null)
            {
                if (pUTF8Enabled && (mLocation == eTextLocation.unstructuredtext || mLocation == eTextLocation.commenttext)) return Encoding.UTF8.GetBytes(mText);
                return ZGetTextBytes();
            }


            if (mAttribute == null) return ZGetBytesASCIIText();
            return ZGetBytesASCIIMIMEAttributeValue();
        }

        private List<byte> ZGetTextBytes()
        { 


            List<byte> lResult = new List<byte>();

            var lEncoding = mEncoding ?? pDefaultEncoding;
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
    }

    public class cMimeParameterAppendDataPartx : cNonASCIITextAppendDataPart
    {
    }
    */
}