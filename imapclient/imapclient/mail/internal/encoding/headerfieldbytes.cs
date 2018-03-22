using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal enum eHeaderFieldTextContext { unstructured, comment, phrase, structured }

    internal class cHeaderFieldBytes
    {
        private static readonly ReadOnlyCollection<char> kNoWSP = new ReadOnlyCollection<char>(new List<char>(new char[] { }));
        private static readonly ReadOnlyCollection<char> kSingleWSP = new ReadOnlyCollection<char>(new List<char>(new char[] { ' ' }));

        private static readonly cBytes kEQUALSQUESTIONMARK = new cBytes("=?");
        private static readonly cBytes kQUESTIONMARKEQUALS = new cBytes("?=");
        private static readonly cBytes kCRLF = new cBytes("\r\n");
        private static readonly cBytes kCRLFSPACE = new cBytes("\r\n ");
        private static readonly cBytes kDQUOTEDQUOTE = new cBytes("\"\"");

        private readonly bool mUTF8Headers;
        private readonly Encoding mEncoding;
        private readonly cBytes mCharsetNameBytes;

        private readonly List<byte> mBytes = new List<byte>();

        private int mCurrentLineByteCount;
        private int mCurrentLineCharCount;
        private bool mCurrentLineHasNonWSP = true;
        private bool mCurrentLineHasEncodedWords = false;
        private bool mComplete = false;

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

        public bool TryAdd(string pText, eHeaderFieldTextContext pContext)
        {
            // the restrictions on the output are;
            //  no lines can be generated that are just white space
            //  lines with encoded words in them cannot be longer than 76 bytes
            //  no lines longer than 998 bytes can be generated
            //
            // desired is no output lines longer than 78 chars 
            //
            // allowed transformations of the input are;
            //  folding, including adding a single space at the beginning of the text so a fold can be inserted there
            //  use of encoded words in unstructured, comment and phrase
            //  use of quoted-string in phrase

            // note that ptext should normally be trimmed, trailing spaces are particularly problematic, and if it contains lots of contiguous WSP then it may fail to be added
            // each piece of text added must be delimited by specials;
            //   either the last char of the previous addition should be a special or
            //   the first char of the addition should be a special
            //   (the field header ends with a special (the ':') so the first addition can be anything)

            // note that the possible introduction of a fold at the beginning of the text is not technically valid for unstructured and comment text as spaces in the output are technically part of the text
            //  (which is why the code here goes to some effort to retain the spaces present in the ptext)
            // also note that white space at the end of an output line is deliberately allowed to take the line over the 78 char 'limit'
            // (words longer than 77 chars that don't need to be encoded will also take output lines over the 78 char limit)

            if (mComplete) throw new InvalidOperationException();
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) return false;
            if (!cCharset.Specials.Contains(mBytes[mBytes.Count - 1]) && pText.Length > 0 && !cCharset.Specials.Contains(pText[0])) return false;

            if (pText.Length == 0)
            {
                if (pContext != eHeaderFieldTextContext.phrase) return true;

                if (ZTryAddNonEncodedWord(true, kNoWSP, kDQUOTEDQUOTE, 2)) return true;

                mComplete = true;
                return false;
            }

            char lChar;
            List<char> lLeadingWSP = new List<char>();
            List<char> lWordChars = new List<char>();
            List<char> lEncodedWordLeadingWSP = new List<char>();
            List<char> lEncodedWordWordChars = new List<char>();
            List<char> lTemp = new List<char>();
            bool lFirstWord = true;

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
                    if (lContainsNonASCII && !mUTF8Headers)
                    {
                        mComplete = true;
                        return false;
                    }
                }
                else if (ZLooksLikeAnEncodedWord(lWordChars) || (lContainsNonASCII && !mUTF8Headers))
                {
                    // note that there is an option to put the thing that looks like an encoded word in quotes if we are in a phrase
                    //  however this leads to a decoding problem in IMAP because IMAP sometimes removes the quotes before reporting the value to the client
                    
                    // this code fires when UTF8 is on if the word looks like an encoded word
                    //  this is in violation of the SHOULD in RFC 6532 section 3.6
                    //   but I'm not sure what the option is ... if I don't encode it then it will be decoded when received

                    if (lEncodedWordWordChars.Count > 0) lEncodedWordWordChars.AddRange(lLeadingWSP);
                    else lEncodedWordLeadingWSP.AddRange(lLeadingWSP);

                    lEncodedWordWordChars.AddRange(lWordChars);
                    continue;
                }
                
                if (lContainsNonASCII) mUsedUTF8 = true;

                if (lEncodedWordWordChars.Count > 0)
                {
                    if (!ZTryAddEncodedWords(lFirstWord, lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext))
                    {
                        mComplete = true;
                        return false;
                    }

                    lFirstWord = false;
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

                if (!ZTryAddNonEncodedWord(lFirstWord, lLeadingWSP, Encoding.UTF8.GetBytes(lNonEncodedWordChars), lNonEncodedWordChars.Length))
                {
                    mComplete = true;
                    return false;
                }

                lFirstWord = false;
            }

            // output the cached encoded word chars if any
            //
            if (lEncodedWordWordChars.Count > 0 && !ZTryAddEncodedWords(lFirstWord, lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext))
            {
                mComplete = true;
                return false;
            }

            // done
            return true;
        }

        public cLiteralMessageDataPart GetMessageDataPart()
        {
            if (mComplete) throw new InvalidOperationException();
            mComplete = true;
            if (!mCurrentLineHasNonWSP) return null;
            mBytes.AddRange(kCRLF);
            return new cLiteralMessageDataPart(new cBytes(mBytes), mUsedUTF8 ? fMessageDataFormat.utf8headers : 0);
        }

        private bool ZLooksLikeAnEncodedWord(List<char> pWordChars)
        {
            if (pWordChars.Count < 9) return false;
            if (pWordChars[0] == '=' && pWordChars[1] == '?' && pWordChars[pWordChars.Count - 2] == '?' && pWordChars[pWordChars.Count - 1] == '=') return true;
            return false;
        }

        private bool ZTryAddNonEncodedWord(bool pFirstWord, IList<char> pLeadingWSP, IList<byte> pWordBytes, int pWordCharCount)
        {
            IList<char> lLeadingWSP;

            // fold if possible and required
            if ((pFirstWord || pLeadingWSP.Count > 0) && mCurrentLineHasNonWSP)
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

        private bool ZTryAddEncodedWords(bool pFirstWord, List<char> pLeadingWSP, List<char> pWordChars, eHeaderFieldTextContext pContext)
        {
            byte lEncoding;
            List<byte> lEncodedText;
            IList<char> lLeadingWSP;

            var lWordString = new string(pWordChars.ToArray());

            // encode the entire word
            ZEncodeWord(pContext, lWordString, out lEncoding, out lEncodedText);

            // fold if possible and required
            if ((pFirstWord || pLeadingWSP.Count > 0) && mCurrentLineHasNonWSP)
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

        private IList<char> ZFold(IList<char> pLeadingWSP, int pLimit)
        {
            IList<char> lRemainingWSP;

            if (pLeadingWSP.Count == 0) lRemainingWSP = kSingleWSP;
            else
            {
                int lWSPCharsOnThisLine = Math.Max(pLimit - mCurrentLineByteCount, pLeadingWSP.Count - 1);

                if (lWSPCharsOnThisLine == 0) lRemainingWSP = pLeadingWSP;
                else
                {
                    for (int i = 0; i < lWSPCharsOnThisLine; i++) mBytes.Add((byte)pLeadingWSP[i]);
                    lRemainingWSP = new List<char>();
                    for (int i = lWSPCharsOnThisLine; i < pLeadingWSP.Count; i++) lRemainingWSP.Add(pLeadingWSP[i]);
                }
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
