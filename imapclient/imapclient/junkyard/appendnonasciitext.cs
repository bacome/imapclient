using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /*
    internal abstract class cEncodedWordsxxxx : 
    {
        protected internal readonly string mText;
        protected internal readonly bool mHasContent = false;

        public cEncodedWords(string pText, bool pAllowToEndWithFWS)
        {
            mText = pText ?? throw new ArgumentNullException(nameof(pText));

            int lFWSStage = 0;
            bool lLastWasFWS = false;

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
                        if (lChar < ' ' || lChar == cChar.DEL) throw new ArgumentOutOfRangeException(nameof(pText));

                        mHasContent = true;
                        lLastWasFWS = false;

                        break;

                    case 1:

                        if (lChar != '\n') throw new ArgumentOutOfRangeException("pText");
                        lFWSStage = 2;
                        break;

                    case 2:

                        if (lChar != '\t' && lChar != ' ') throw new ArgumentOutOfRangeException(nameof(pText));
                        lFWSStage = 0;
                        lLastWasFWS = true;
                        break;
                }
            }

            if (lFWSStage != 0) throw new ArgumentOutOfRangeException(nameof(pText));
            if (lLastWasFWS && !pAllowToEndWithFWS) throw new ArgumentOutOfRangeException(nameof(pText));
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