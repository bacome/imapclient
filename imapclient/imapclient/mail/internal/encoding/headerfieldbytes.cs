using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal enum eHeaderFieldTextContext { structured, unstructured, comment, phrase }

    internal class cHeaderFieldBytes
    {
        private static readonly ReadOnlyCollection<char> kNoWSP = new ReadOnlyCollection<char>(new List<char>(new char[] { }));
        //private static readonly ReadOnlyCollection<char> kSingleWSP = new ReadOnlyCollection<char>(new List<char>(new char[] { ' ' }));

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
        private bool mCurrentLineHasNonWSP = true;
        private bool mCurrentLineHasEncodedWords = false;
        private bool mCanAddText = true;

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

        public bool TryAddText(string pText, eHeaderFieldTextContext pContext)
        {
            // the restrictions are;
            //  no lines can be generated that are just white space
            //  no lines with encoded words longer than 76 bytes can be generated
            //  no lines longer than 998 bytes can be generated
            //
            // desired is no lines longer than 78 chars ... as this is for display purposes, white space at the end of a line is deliberately allowed to go over this limit
            //
            // allowed is;
            //  folding
            //  use of encoded words in unstructured, comment and phrase
            //  use of quoted-string in phrase

            // note that ptext should normally be trimmed, trailing spaces are particularly problematic

            // addspecial must be called between calls to this
            //  the unsolvable problem is that if one call ends with enc-word WSP and the next call starts with an enc-word then the WSP will be lost

            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) return false;

            char lChar;
            List<char> lLeadingWSP = new List<char>();
            List<char> lWordChars = new List<char>();
            List<char> lEncodedWordLeadingWSP = new List<char>();
            List<char> lEncodedWordWordChars = new List<char>();
            List<char> lTemp = new List<char>();

            // loop through the words

            int lPosition = 0;

            while (lPosition < pText.Length)
            {
                lLeadingWSP.Clear();
                lWordChars.Clear();

                if (pContext == eHeaderFieldTextContext.phrase)
                {
                    // phrases are a list of atoms and quoted-strings
                    //  when the phrase is reassembled each atom/qs has a single space inserted between (the actually present WSP (leading, separating and trailing) is not significant)
                    //   so to convert an arbitrary string to a phrase is different to converting the same string to unstructured or comment (as in unstructured and comment the white space is significant)
                    //    note the special handling a single trailing space (it is included in the last word)

                    // extract separator
                    //
                    if (lPosition > 0) lLeadingWSP.Add(' ');

                    bool lFoundNonSpace = false;

                    // extract non-separator
                    //
                    do
                    {
                        lChar = pText[lPosition++];

                        if (lChar == ' ' && lPosition < pText.Length)
                        {
                            if (lFoundNonSpace) break;
                        }
                        else lFoundNonSpace = true;

                        lWordChars.Add(lChar);
                    }
                    while (lPosition < pText.Length);
                }
                else
                {
                    // extract leading WSP (if any)
                    //
                    while (lPosition < pText.Length)
                    {
                        lChar = pText[lPosition];

                        if (lChar != '\t' && lChar != ' ') break;

                        lLeadingWSP.Add(lChar);
                        lPosition++;
                    }

                    // extract word (if any)
                    //
                    while (lPosition < pText.Length)
                    {
                        lChar = pText[lPosition];

                        if (lChar == '\t' || lChar == ' ') break;

                        lWordChars.Add(lChar);
                        lPosition++;
                    }
                }

                var lContainsNonASCII = cTools.ContainsNonASCII(lWordChars);

                if (pContext == eHeaderFieldTextContext.structured)
                {
                    if (lContainsNonASCII && !mUTF8Headers) return false;
                }
                else if (ZLooksLikeAnEncodedWord(lWordChars) || (lContainsNonASCII && !mUTF8Headers))
                {
                    if (lEncodedWordWordChars.Count > 0) lEncodedWordWordChars.AddRange(lLeadingWSP);
                    else lEncodedWordLeadingWSP.AddRange(lLeadingWSP);

                    lEncodedWordWordChars.AddRange(lWordChars);
                    continue;
                }
                
                if (lContainsNonASCII) mUsedUTF8 = true;

                if (lEncodedWordWordChars.Count > 0)
                {
                    if (!ZTryAddEncodedWords(lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext)) return false;
                    lEncodedWordLeadingWSP.Clear();
                    lEncodedWordWordChars.Clear();
                }

                char[] lNonEncodedWordChars;

                if (pContext == eHeaderFieldTextContext.phrase)
                {
                    if (cCharset.AText.ContainsAll(lWordChars)) lNonEncodedWordChars = lWordChars.ToArray();
                    else lNonEncodedWordChars = cTools.Enquote(lWordChars).ToCharArray();
                }
                else if (pContext == eHeaderFieldTextContext.comment)
                {
                    lTemp.Clear();

                    foreach (var lWordChar in lWordChars)
                    {
                        if (lWordChar == '(' || lWordChar == ')' || lWordChar == '\\') lTemp.Add('\\');
                        lTemp.Add(lWordChar);
                    }

                    lNonEncodedWordChars = lTemp.ToArray();
                }
                else lNonEncodedWordChars = lWordChars.ToArray();

                if (!ZTryAddNonEncodedWord(lLeadingWSP, Encoding.UTF8.GetBytes(lNonEncodedWordChars), lNonEncodedWordChars.Length)) return false;
            }

            // output the cached encoded word chars if any
            //
            if (lEncodedWordWordChars.Count > 0 && !ZTryAddEncodedWords(lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext)) return false;

            // done
            mCanAddText = false;
            return true;
        }

        /*
        public bool TryAddDotAtom(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (!cTools.IsDotAtom(pText)) return false;

            var lContainsNonASCII = cTools.ContainsNonASCII(pText);
            if (lContainsNonASCII && !mUTF8Headers) return false;

            ;?;
            if (!ZTryAddNonEncodedWord(kNoWSP, Encoding.UTF8.GetBytes(pText), pText.Length)) return false;
            if (lContainsNonASCII) mUsedUTF8 = true;

            return true;
        } 

        public bool TryAddQuotedString(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) return false;

            var lContainsNonASCII = cTools.ContainsNonASCII(pText);
            if (lContainsNonASCII && !mUTF8Headers) return false;

            if (!ZTryAddFoldableText(cTools.Enquote(pText))) return false;
            if (lContainsNonASCII) mUsedUTF8 = true;

            return true;
        } 

        public bool TryAddNonEncodableText(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) return false;

            var lContainsNonASCII = cTools.ContainsNonASCII(pText);
            if (lContainsNonASCII && !mUTF8Headers) return false;

            if (!ZTryAddNonEncodableText(pText)) return false;
            if (lContainsNonASCII) mUsedUTF8 = true;

            return true;
        } */

        public void AddSpecial(byte pByte)
        {
            if (!cCharset.Specials.Contains(pByte)) throw new ArgumentOutOfRangeException(nameof(pByte));

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
            mCurrentLineHasNonWSP = true;
            mCanAddText = true;
        }

        public bool TryGetBytes(out cBytes rBytes, out )
        {
            if (mcur)


            ;?; // must check that the last line isn't completely spaces. (
            ;?; //  must do this before adding any CRLF => this should be a common

            // add the new line before passing out
            new cBytes(mBytes);
        }

        public fMessageDataFormat Format => mUsedUTF8 ? fMessageDataFormat.utf8headers : 0;

        private bool ZLooksLikeAnEncodedWord(List<char> pWordChars)
        {
            if (pWordChars.Count < 9) return false;
            if (pWordChars[0] == '=' && pWordChars[1] == '?' && pWordChars[pWordChars.Count - 2] == '?' && pWordChars[pWordChars.Count - 1] == '=') return true;
            return false;
        }

        private bool ZTryAddNonEncodedWord(List<char> pLeadingWSP, IList<byte> pWordBytes, int pWordCharCount)
        {
            List<char> lLeadingWSP;

            // fold if possible and required
            if (pLeadingWSP.Count > 0 && mCurrentLineHasNonWSP)
            {
                if (mCurrentLineHasEncodedWords)
                {
                    if (mCurrentLineByteCount + pLeadingWSP.Count + pWordBytes.Count > 76) lLeadingWSP = ZFold(pLeadingWSP, 76);
                    else lLeadingWSP = pLeadingWSP;
                }
                else
                {
                    if (mCurrentLineCharCount + pLeadingWSP.Count + pWordCharCount > 78) lLeadingWSP = ZFold(pLeadingWSP, 998);
                    else lLeadingWSP = pLeadingWSP;
                }
            }
            else lLeadingWSP = pLeadingWSP;

            // check if adding the text will violate the restrictions
            //
            if (mCurrentLineHasEncodedWords)
            {
                if (mCurrentLineByteCount + lLeadingWSP.Count + pWordBytes.Count > 76) return false;
            }
            else
            {
                if (mCurrentLineByteCount + lLeadingWSP.Count + pWordBytes.Count > 998) return false;
            }

            // add the white space
            foreach (char lChar in lLeadingWSP) mBytes.Add((byte)lChar);
            mCurrentLineByteCount += lLeadingWSP.Count;
            mCurrentLineCharCount += lLeadingWSP.Count;

            // add non-wsp
            mBytes.AddRange(pWordBytes);
            mCurrentLineByteCount += pWordBytes.Count;
            mCurrentLineCharCount += pWordCharCount;

            // update the flag
            if (pWordBytes.Count > 0) mCurrentLineHasNonWSP = true;

            // done
            return true;
        }

        private bool ZTryAddEncodedWords(List<char> pLeadingWSP, List<char> pWordChars, eHeaderFieldTextContext pContext)
        {
            byte lEncoding;
            List<byte> lEncodedText;
            IList<char> lLeadingWSP;

            var lWordString = new string(pWordChars.ToArray());

            // encode the entire word
            ZEncodeWord(pContext, lWordString, out lEncoding, out lEncodedText);

            // fold if possible and required
            if (pLeadingWSP.Count > 0 && mCurrentLineHasNonWSP)
            {
                if (mCurrentLineByteCount + pLeadingWSP.Count + 7 + mCharsetNameBytes.Count + lEncodedText.Count > 76)
                {
                    if (mCurrentLineHasEncodedWords) lLeadingWSP = ZFold(pLeadingWSP, 76);
                    else lLeadingWSP = ZFold(pLeadingWSP, 998);
                }
                else lLeadingWSP = pLeadingWSP;
            }
            else lLeadingWSP = pLeadingWSP;

            // see if can add the entire word
            if (mCurrentLineByteCount + lLeadingWSP.Count + 7 + mCharsetNameBytes.Count + lEncodedText.Count < 77)
            {
                ZAddEncodedWord(lLeadingWSP, lEncoding, lEncodedText, lLeadingWSP.Count + pWordChars.Count);
                return true;
            }

            StringInfo lWordStringInfo = new StringInfo(lWordString);

            // see if can add it at all

            ZEncodeWord(pContext, lWordStringInfo.SubstringByTextElements(0, 1), out lEncoding, out lEncodedText);
            if (mCurrentLineByteCount + lLeadingWSP.Count + 7 + mCharsetNameBytes.Count + lEncodedText.Count > 76) return false;

            // add it in chunks

            int lLastTextElementCount = 1;
            byte lLastEncoding = lEncoding;
            List<byte> lLastEncodedText = lEncodedText;
            int lLastCharCount = lLeadingWSP.Count + 1;

            int lFromTextElement = 0;
            int lTextElementCount = 2;

            while (lFromTextElement + lTextElementCount <= lWordStringInfo.LengthInTextElements)
            {
                ZEncodeWord(pContext, lWordStringInfo.SubstringByTextElements(lFromTextElement, lTextElementCount), out lEncoding, out lEncodedText);

                if (mCurrentLineByteCount + lLeadingWSP.Count + 7 + mCharsetNameBytes.Count + lEncodedText.Count > 76)
                {
                    // add the previous good chunk
                    ZAddEncodedWord(lLeadingWSP, lLastEncoding, lLastEncodedText, lLastCharCount);

                    // add CRLFspace
                    mBytes.AddRange(kCRLFSPACE);
                    mCurrentLineByteCount = 1;
                    mCurrentLineCharCount = 1;

                    // start the next chunk
                    lLeadingWSP = kNoWSP;

                    lLastTextElementCount = 1;
                    ZEncodeWord(pContext, lWordStringInfo.SubstringByTextElements(lFromTextElement, 1), out lLastEncoding, out lLastEncodedText);
                    lLastCharCount = 1;

                    lFromTextElement = lFromTextElement + lLastTextElementCount;
                    lTextElementCount = 2;
                }
                else
                {
                    lLastTextElementCount = lTextElementCount;
                    lLastEncoding = lEncoding;
                    lLastEncodedText = lEncodedText;
                    lLastCharCount++;

                    lTextElementCount++;
                }
            }

            // add the final chunk
            ZAddEncodedWord(lLeadingWSP, lLastEncoding, lLastEncodedText, lLastCharCount);

            // done
            return true;
        }

        private List<char> ZFold(List<char> pLeadingWSP, int pLimit)
        {
            int lWSPCharsOnThisLine = Math.Max(pLimit - mCurrentLineByteCount, pLeadingWSP.Count - 1);

            List<char> lRemainingWSP;

            if (lWSPCharsOnThisLine == 0) lRemainingWSP = pLeadingWSP;
            else
            {
                for (int i = 0; i < lWSPCharsOnThisLine; i++) mBytes.Add((byte)pLeadingWSP[i]);
                lRemainingWSP = new List<char>();
                for (int i = lWSPCharsOnThisLine; i < pLeadingWSP.Count; i++) lRemainingWSP.Add(pLeadingWSP[i]);
            }

            mBytes.AddRange(kCRLF);
            mCurrentLineByteCount = 0;
            mCurrentLineCharCount = 0;
            mCurrentLineHasNonWSP = false;
            mCurrentLineHasEncodedWords = false;

            return lRemainingWSP;
        }

        private void ZEncodeWord(eHeaderFieldTextContext pContext, string pString, out byte rEncoding, out List<byte> rEncodedText)
        {
            var lBytes = mEncoding.GetBytes(pString);

            var lQEncodedText = ZQEncode(lBytes, pContext);
            var lBEncodedText = cBase64.Encode(lBytes);

            if (lQEncodedText.Count < lBEncodedText.Count)
            {
                rEncoding = cASCII.q;
                rEncodedText = lQEncodedText;
            }
            else
            {
                rEncoding = cASCII.b;
                rEncodedText = lBEncodedText;
            }
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

        private void ZAddEncodedWord(IList<char> pLeadingWSP, byte pEncoding, List<byte> pEncodedText, int pCharCount)
        {
            foreach (char lChar in pLeadingWSP) mBytes.Add((byte)lChar);

            mBytes.AddRange(kEQUALSQUESTIONMARK);
            mBytes.AddRange(mCharsetNameBytes);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.Add(pEncoding);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.AddRange(pEncodedText);
            mBytes.AddRange(kQUESTIONMARKEQUALS);

            mCurrentLineByteCount = mCurrentLineByteCount + pLeadingWSP.Count + 7 + mCharsetNameBytes.Count + pEncodedText.Count;
            mCurrentLineCharCount = mCurrentLineCharCount + pCharCount;
            mCurrentLineHasNonWSP = true;
            mCurrentLineHasEncodedWords = true;
        }
    }
}
