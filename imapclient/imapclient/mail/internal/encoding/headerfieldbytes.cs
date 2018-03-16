using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal enum eHeaderFieldTextContext { unstructured, comment, phrase }

    internal class cHeaderFieldBytes
    {
        private static readonly ReadOnlyCollection<char> kNoWSP = new ReadOnlyCollection<char>(new List<char>(new char[] { }));
        private static readonly ReadOnlyCollection<char> kSingleWSP = new ReadOnlyCollection<char>(new List<char>(new char[] { ' ' }));

        private enum eWordType { special, encodedword, nothingspecial }

        private static readonly cBytes kEQUALSQUESTIONMARK = new cBytes("=?");
        private static readonly cBytes kQUESTIONMARKEQUALS = new cBytes("?=");
        private static readonly cBytes kCRLF = new cBytes("\r\n");
        private static readonly cBytes kCRLFSPACE = new cBytes("\r\n ");

        private readonly bool mUTF8Headers;
        private readonly Encoding mEncoding;
        private readonly cBytes mCharsetNameBytes;

        private readonly List<byte> mBytes = new List<byte>();

        private int mCurrentLineByteCount;
        private int mCurrentLineCharCount;

        private bool mCurrentLineHasEncodedWords = false;
        private eWordType mLastWordType = eWordType.special; // the colon

        private bool mUsedUTF8 = false;

        public cHeaderFieldBytes(bool pUTF8Headers, Encoding pEncoding, cBytes pCharsetNameBytes, string pFieldName)
        {
            mUTF8Headers = pUTF8Headers;
            mEncoding = pEncoding ?? throw new ArgumentNullException(nameof(pEncoding));
            mCharsetNameBytes = pCharsetNameBytes ?? throw new ArgumentNullException(nameof(pCharsetNameBytes));

            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            if (!cCharset.FText.ContainsAll(pFieldName)) throw new ArgumentOutOfRangeException(nameof(pFieldName));

            foreach (char lChar in pFieldName) mBytes.Add((byte)lChar);
            mBytes.Add(cASCII.COLON);
            mCurrentLineByteCount = pFieldName.Length + 1;
            mCurrentLineCharCount = mCurrentLineByteCount;
        }

        public void AddEncodableText(string pText, eHeaderFieldTextContext pContext)
        {
            char lChar;
            List<char> lLeadingWSP = new List<char>();
            List<char> lWordChars = new List<char>();
            List<char> lEncodedWordLeadingWSP = new List<char>();
            List<char> lEncodedWordWordChars = new List<char>();

            // loop through the lines

            int lStartIndex = 0;

            while (true)
            {
                int lIndex = pText.IndexOf("\r\n", lStartIndex, StringComparison.Ordinal);

                string lInput;

                if (lIndex == -1) lInput = pText.Substring(lStartIndex);
                else if (lIndex == lStartIndex) lInput = string.Empty;
                else lInput = pText.Substring(lStartIndex, lIndex - lStartIndex);

                // loop through the words

                int lPosition = 0;

                while (lPosition < lInput.Length)
                {
                    // extract leading white space (if any)

                    lLeadingWSP.Clear();

                    while (lPosition < lInput.Length)
                    {
                        lChar = lInput[lPosition];

                        if (lChar != '\t' && lChar != ' ') break;

                        lLeadingWSP.Add(lChar);
                        lPosition++;
                    }

                    // extract word (if there is one)

                    lWordChars.Clear();

                    while (lPosition < lInput.Length)
                    {
                        lChar = lInput[lPosition];

                        if (lChar == '\t' || lChar == ' ') break;

                        lWordChars.Add(lChar);
                        lPosition++;
                    }

                    // nothing left on this line
                    if (lWordChars.Count == 0) break;

                    var lContainsNonASCII = ZContainsNonASCII(lWordChars);

                    // process the white space and word
                    //
                    if (ZLooksLikeAnEncodedWord(lWordChars) || (!mUTF8Headers && lContainsNonASCII))
                    {
                        if (lEncodedWordWordChars.Count == 0) lEncodedWordLeadingWSP.AddRange(lLeadingWSP);
                        if (lEncodedWordWordChars.Count > 0 || mLastWordType == eWordType.encodedword) lEncodedWordWordChars.Add(' ');
                        lEncodedWordWordChars.AddRange(lWordChars);
                    }
                    else
                    {
                        if (lContainsNonASCII) mUsedUTF8 = true;

                        if (lEncodedWordWordChars.Count > 0)
                        {
                            ZAddEncodedWords(lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext);
                            lEncodedWordLeadingWSP.Clear();
                            lEncodedWordWordChars.Clear();
                        }

                        List<char> lNonEncodedWordChars;

                        switch (pContext)
                        {
                            case eHeaderFieldTextContext.unstructured:

                                lNonEncodedWordChars = lWordChars;
                                break;

                            case eHeaderFieldTextContext.comment:

                                lNonEncodedWordChars = new List<char>();

                                foreach (var lWordChar in lWordChars)
                                {
                                    if (lWordChar == '(' || lWordChar == ')' || lWordChar == '\\') lNonEncodedWordChars.Add('\\');
                                    lNonEncodedWordChars.Add(lWordChar);
                                }

                                break;

                            case eHeaderFieldTextContext.phrase:

                                if (cCharset.AText.ContainsAll(lWordChars)) lNonEncodedWordChars = lWordChars;
                                else
                                {
                                    lNonEncodedWordChars = new List<char>();

                                    lNonEncodedWordChars.Add('"');

                                    foreach (var lWordChar in lWordChars)
                                    {
                                        if (lWordChar == '"' || lWordChar == '\\') lNonEncodedWordChars.Add('\\');
                                        lNonEncodedWordChars.Add(lWordChar);
                                    }

                                    lNonEncodedWordChars.Add('"');
                                }

                                break;

                            default:

                                throw new cInternalErrorException($"{nameof(cHeaderFieldBytes)}.{nameof(AddEncodableText)}");
                        }

                        ZAddNonEncodedWord(lLeadingWSP, Encoding.UTF8.GetBytes(lNonEncodedWordChars.ToArray()), lNonEncodedWordChars.Count);
                    }
                }

                // output the cached encoded word chars if any
                //
                if (lEncodedWordWordChars.Count > 0)
                {
                    ZAddEncodedWords(lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext);
                    lEncodedWordLeadingWSP.Clear();
                    lEncodedWordWordChars.Clear();
                }

                // nothing left on this line
                if (lIndex == -1) break;

                // next line
                AddNewLine();
                lStartIndex = lIndex + 2;
            }
        }

        public bool TryAddDotAtom(string pText)
        {
            var lContainsNonASCII = ZContainsNonASCII(pText);
            if (lContainsNonASCII && !mUTF8Headers) return false;
            if (!cTools.IsDotAtom(pText)) return false;
            ZAddNonEncodedWord(kNoWSP, Encoding.UTF8.GetBytes(pText), pText.Length);
            if (lContainsNonASCII) mUsedUTF8 = true;
            return true;
        }

        public bool TryAddQuotedString(string pText)
        {
            var lContainsNonASCII = ZContainsNonASCII(pText);
            if (lContainsNonASCII && !mUTF8Headers) return false;
            if (!cCharset.WSPVChar.ContainsAll(pText)) return false;
            ZAddFoldableText(cTools.Enquote(pText));
            if (lContainsNonASCII) mUsedUTF8 = true;
            return true;
        }

        public bool TryAddFoldableText(string pText)
        {
            var lContainsNonASCII = ZContainsNonASCII(pText);
            if (lContainsNonASCII && !mUTF8Headers) return false;
            ZAddFoldableText(pText);
            if (lContainsNonASCII) mUsedUTF8 = true;
            return true;
        }

        public void AddSpecial(byte pByte)
        {
            bool lFold;

            if (mCurrentLineHasEncodedWords)
            {
                if (mCurrentLineByteCount > 75) lFold = true;
                else lFold = false;
            }
            else
            {
                if (mCurrentLineCharCount > 77) lFold = true;
                else lFold = false;
            }

            if (lFold)
            {
                mBytes.AddRange(kCRLFSPACE);
                mCurrentLineByteCount = 1;
                mCurrentLineCharCount = 1;
                mCurrentLineHasEncodedWords = false;
            }

            mBytes.Add(pByte);
            mCurrentLineByteCount++;
            mCurrentLineCharCount++;
            mLastWordType = eWordType.special;
        }

        public void AddNewLine()
        {
            if (mCurrentLineByteCount > 0)
            {
                mBytes.AddRange(kCRLF);
                mCurrentLineByteCount = 0;
                mCurrentLineCharCount = 0;
                mCurrentLineHasEncodedWords = false;
            }
        }

        //public bool LastWordWasAnEncodedWord => ;

        public cBytes Bytes => new cBytes(mBytes);
        public fMessageDataFormat Format => mUsedUTF8 ? fMessageDataFormat.utf8headers : 0;

        private bool ZContainsNonASCII(IEnumerable<char> pChars)
        {
            foreach (var lChar in pChars) if (lChar > cChar.DEL) return true;
            return false;
        }

        private bool ZLooksLikeAnEncodedWord(List<char> pWordChars)
        {
            if (pWordChars.Count < 9) return false;
            if (pWordChars[0] == '=' && pWordChars[1] == '?' && pWordChars[pWordChars.Count - 2] == '?' && pWordChars[pWordChars.Count - 1] == '=') return true;
            return false;
        }

        private void ZAddFoldableText(string pText)
        {
            char lChar;
            List<char> lLeadingWSP = new List<char>();
            List<char> lWordChars = new List<char>();

            int lPosition = 0;

            while (lPosition < pText.Length)
            {
                // extract leading white space

                lLeadingWSP.Clear();

                while (lPosition < pText.Length)
                {
                    lChar = pText[lPosition];

                    if (lChar != '\t' && lChar != ' ') break;

                    lLeadingWSP.Add(lChar);
                    lPosition++;
                }

                // extract word

                lWordChars.Clear();

                while (lPosition < pText.Length)
                {
                    lChar = pText[lPosition];

                    if (lChar == '\t' || lChar == ' ') break;

                    lWordChars.Add(lChar);
                    lPosition++;
                }

                // add the wsp and word
                ZAddNonEncodedWord(lLeadingWSP, Encoding.UTF8.GetBytes(lWordChars.ToArray()), lWordChars.Count);
            }
        }

        private void ZAddNonEncodedWord(IList<char> pLeadingWSP, IList<byte> pWordBytes, int pWordCharCount)
        {
            if (pLeadingWSP == null) throw new ArgumentNullException(nameof(pLeadingWSP));
            if (pWordBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pWordBytes));
            if (pWordCharCount <= 0) throw new ArgumentOutOfRangeException(nameof(pWordCharCount));

            IList<char> lLeadingWSP;
            if (pLeadingWSP.Count == 0 && mLastWordType != eWordType.special) lLeadingWSP = kSingleWSP;
            else lLeadingWSP = pLeadingWSP;

            bool lFold;

            if (mCurrentLineHasEncodedWords)
            {
                if (mCurrentLineByteCount + lLeadingWSP.Count + pWordBytes.Count > 76) lFold = true;
                else lFold = false;
            }
            else
            {
                if (mCurrentLineCharCount + lLeadingWSP.Count + pWordCharCount > 78) lFold = true;
                else lFold = false;
            }

            if (lFold)
            {
                mBytes.AddRange(kCRLF);
                mCurrentLineByteCount = 0;
                mCurrentLineCharCount = 0;
                mCurrentLineHasEncodedWords = false;
                if (lLeadingWSP.Count == 0) lLeadingWSP = kSingleWSP;
            }

            foreach (char lChar in lLeadingWSP) mBytes.Add((byte)lChar);
            mCurrentLineByteCount += lLeadingWSP.Count;
            mCurrentLineCharCount += lLeadingWSP.Count;

            mBytes.AddRange(pWordBytes);
            mCurrentLineByteCount += pWordBytes.Count;
            mCurrentLineCharCount += pWordCharCount;
            mLastWordType = eWordType.nothingspecial;
        }

        private void ZAddEncodedWords(List<char> pLeadingWSP, List<char> pWordChars, eHeaderFieldTextContext pContext)
        {
            if (pLeadingWSP == null) throw new ArgumentNullException(nameof(pLeadingWSP));
            if (pWordChars == null) throw new ArgumentNullException(nameof(pWordChars));
            if (pWordChars.Count == 0) throw new ArgumentOutOfRangeException(nameof(pWordChars));

            IList<char> lLeadingWSP;
            if (pLeadingWSP.Count == 0 && mLastWordType != eWordType.special) lLeadingWSP = kSingleWSP;
            else lLeadingWSP = pLeadingWSP;

            StringInfo lString = new StringInfo(new string(pWordChars.ToArray()));

            int lMaxEncodedByteCount = 75 - 7 - mCharsetNameBytes.Count;

            List<byte> lResult = new List<byte>();

            int lFromTextElement = 0;
            int lTextElementCount = 1;

            int lLastTextElementCount = 0;
            byte lLastEncoding = cASCII.NUL;
            List<byte> lLastEncodedText = null;

            while (lFromTextElement + lTextElementCount <= lString.LengthInTextElements)
            {
                var lBytes = mEncoding.GetBytes(lString.SubstringByTextElements(lFromTextElement, lTextElementCount));

                var lQEncodedText = ZQEncode(lBytes, pContext);
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
                    ZAddEncodedWord(lLeadingWSP, lLastEncoding, lLastEncodedText);
                    lFromTextElement = lFromTextElement + lLastTextElementCount;
                    lTextElementCount = 1;
                    lLeadingWSP = kNoWSP;
                }
                else lTextElementCount++;
            }

            if (lFromTextElement < lString.LengthInTextElements) ZAddEncodedWord(lLeadingWSP, lLastEncoding, lLastEncodedText);

            mLastWordType = eWordType.encodedword;
        }

        private List<byte> ZQEncode(byte[] pBytes, eHeaderFieldTextContext pContext)
        {
            List<byte> lResult = new List<byte>();

            foreach (var lByte in pBytes)
            {
                bool lEncode;

                if (lByte <= cASCII.SPACE || lByte == cASCII.EQUALS || lByte == cASCII.QUESTIONMARK || lByte == cASCII.UNDERSCORE || lByte >= cASCII.DEL) lEncode = true;
                else if (pContext == eHeaderFieldTextContext.comment) lEncode = lByte == cASCII.LPAREN || lByte == cASCII.RPAREN || lByte == cASCII.BACKSL;
                else if (pContext == eHeaderFieldTextContext.phrase)
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

        private void ZAddEncodedWord(IList<char> pLeadingWSP, byte pEncoding, List<byte> pEncodedText)
        {
            if (mCurrentLineByteCount + pLeadingWSP.Count + 7 + mCharsetNameBytes.Count + pEncodedText.Count > 76)
            {
                mBytes.AddRange(kCRLFSPACE);
                mCurrentLineByteCount = 1;
            }
            else
            {
                foreach (char lChar in pLeadingWSP) mBytes.Add((byte)lChar);
                mCurrentLineByteCount += pLeadingWSP.Count;
            }

            mBytes.AddRange(kEQUALSQUESTIONMARK);
            mBytes.AddRange(mCharsetNameBytes);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.Add(pEncoding);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.AddRange(pEncodedText);
            mBytes.AddRange(kQUESTIONMARKEQUALS);

            mCurrentLineByteCount = mCurrentLineByteCount + 7 + mCharsetNameBytes.Count + pEncodedText.Count;
            mCurrentLineHasEncodedWords = true;
        }
    }
}
