using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal class cHeaderFieldBytes
    {
        public static readonly ReadOnlyCollection<char> SingleSpace = new ReadOnlyCollection<char>(new List<char>(new char[] { ' ' }));

        private enum eWordType { special, encodedword, nothingspecial }

        private static readonly ReadOnlyCollection<char> kEmpty = new ReadOnlyCollection<char>(new List<char>(new char[] { }));

        private static readonly cBytes kEQUALSQUESTIONMARK = new cBytes("=?");
        private static readonly cBytes kQUESTIONMARKEQUALS = new cBytes("?=");
        private static readonly cBytes kCRLF = new cBytes("\r\n");
        private static readonly cBytes kCRLFSPACE = new cBytes("\r\n ");

        private readonly bool mUTF8Allowed;
        public readonly Encoding Encoding;
        public readonly cBytes CharsetNameBytes;

        private readonly List<byte> mBytes = new List<byte>();

        private int mCurrentLineByteCount;
        private int mCurrentLineCharCount;

        private bool mCurrentLineHasEncodedWords = false;
        private eWordType mLastWordType = eWordType.special; // the colon

        public cHeaderFieldBytes(string pFieldName, Encoding pEncoding)
        {
            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));

            mUTF8Allowed = (pEncoding == null);
            Encoding = pEncoding ?? Encoding.UTF8;
            CharsetNameBytes = new cBytes(cTools.CharsetNameBytes(Encoding));

            foreach (char lChar in pFieldName) mBytes.Add((byte)lChar);
            mBytes.Add(cASCII.COLON);
            mCurrentLineByteCount = pFieldName.Length + 1;
            mCurrentLineCharCount = mCurrentLineByteCount;
        }

        public bool UTF8Allowed => mUTF8Allowed;
        public bool LastWordWasAnEncodedWord => mLastWordType == eWordType.encodedword;

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

        public void AddNonEncodedWord(List<byte> pWordBytes, int pWordCharCount)
        {
            if (pWordBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pWordBytes));
            if (pWordCharCount <= 0) throw new ArgumentOutOfRangeException(nameof(pWordCharCount));

            int lLeadingSpaces;
            if (mLastWordType == eWordType.special) lLeadingSpaces = 0;
            else lLeadingSpaces = 1;

            bool lFold;

            if (mCurrentLineHasEncodedWords)
            {
                if (mCurrentLineByteCount + lLeadingSpaces + pWordBytes.Count > 76) lFold = true;
                else lFold = false;
            }
            else
            {
                if (mCurrentLineCharCount + lLeadingSpaces + pWordCharCount > 78) lFold = true;
                else lFold = false;
            }

            if (lFold)
            {
                mBytes.AddRange(kCRLFSPACE);
                mCurrentLineByteCount = 1;
                mCurrentLineCharCount = 1;
                mCurrentLineHasEncodedWords = false;
            }
            else if (mLastWordType != eWordType.special)
            {
                mBytes.Add(cASCII.SPACE);
                mCurrentLineByteCount++;
                mCurrentLineCharCount++;
            }

            mBytes.AddRange(pWordBytes);
            mCurrentLineByteCount += pWordBytes.Count;
            mCurrentLineCharCount += pWordCharCount;
            mLastWordType = eWordType.nothingspecial;
        }

        public void AddNonEncodedWord(string pWord)
        {
            if (pWord.Length == 0) throw new ArgumentOutOfRangeException(nameof(pWord));

            int lLeadingSpaces;
            if (mLastWordType == eWordType.special) lLeadingSpaces = 0;
            else lLeadingSpaces = 1;

            var lWordBytes = Encoding.UTF8.GetBytes(pWord);

            bool lFold;

            if (mCurrentLineHasEncodedWords)
            {
                if (mCurrentLineByteCount + lLeadingSpaces + lWordBytes.Length > 76) lFold = true;
                else lFold = false;
            }
            else
            {
                if (mCurrentLineCharCount + lLeadingSpaces + pWord.Length > 78) lFold = true;
                else lFold = false;
            }

            if (lFold)
            {
                mBytes.AddRange(kCRLFSPACE);
                mCurrentLineByteCount = 1;
                mCurrentLineCharCount = 1;
                mCurrentLineHasEncodedWords = false;
            }
            else if (mLastWordType != eWordType.special)
            {
                mBytes.Add(cASCII.SPACE);
                mCurrentLineByteCount++;
                mCurrentLineCharCount++;
            }

            mBytes.AddRange(lWordBytes);
            mCurrentLineByteCount += lWordBytes.Length;
            mCurrentLineCharCount += pWord.Length;
            mLastWordType = eWordType.nothingspecial;
        }

        public void AddNonEncodedWord(IList<char> pLeadingWSP, List<char> pWordChars)
        {
            if (pLeadingWSP == null) throw new ArgumentNullException(nameof(pLeadingWSP));
            if (pWordChars.Count == 0) throw new ArgumentOutOfRangeException(nameof(pWordChars));

            IList<char> lLeadingWSP;
            if (pLeadingWSP.Count == 0 && mLastWordType != eWordType.special) lLeadingWSP = SingleSpace;
            else lLeadingWSP = pLeadingWSP;

            var lWordBytes = Encoding.UTF8.GetBytes(pWordChars.ToArray());

            bool lFold;

            if (mCurrentLineHasEncodedWords)
            {
                if (mCurrentLineByteCount + lLeadingWSP.Count + lWordBytes.Length > 76) lFold = true;
                else lFold = false;
            }
            else
            {
                if (mCurrentLineCharCount + lLeadingWSP.Count + pWordChars.Count > 78) lFold = true;
                else lFold = false;
            }

            if (lFold)
            {
                mBytes.AddRange(kCRLF);
                mCurrentLineByteCount = 0;
                mCurrentLineCharCount = 0;
                mCurrentLineHasEncodedWords = false;
                if (lLeadingWSP.Count == 0) lLeadingWSP = SingleSpace;
            }

            foreach (char lChar in lLeadingWSP) mBytes.Add((byte)lChar);
            mCurrentLineByteCount += lLeadingWSP.Count;
            mCurrentLineCharCount += lLeadingWSP.Count;

            mBytes.AddRange(lWordBytes);
            mCurrentLineByteCount += lWordBytes.Length;
            mCurrentLineCharCount += pWordChars.Count;
            mLastWordType = eWordType.nothingspecial;
        }

        public void AddEncodedWords(List<char> pLeadingWSP, List<char> pWordChars, eHeaderValuePartContext pContext)
        {
            if (pLeadingWSP == null) throw new ArgumentNullException(nameof(pLeadingWSP));
            if (pWordChars == null) throw new ArgumentNullException(nameof(pWordChars));
            if (pWordChars.Count == 0) throw new ArgumentOutOfRangeException(nameof(pWordChars));

            IList<char> lLeadingWSP;
            if (pLeadingWSP.Count == 0 && mLastWordType != eWordType.special) lLeadingWSP = SingleSpace;
            else lLeadingWSP = pLeadingWSP;

            StringInfo lString = new StringInfo(new string(pWordChars.ToArray()));

            int lMaxEncodedByteCount = 75 - 7 - CharsetNameBytes.Count;

            List<byte> lResult = new List<byte>();

            int lFromTextElement = 0;
            int lTextElementCount = 1;

            int lLastTextElementCount = 0;
            byte lLastEncoding = cASCII.NUL;
            List<byte> lLastEncodedText = null;

            while (lFromTextElement + lTextElementCount <= lString.LengthInTextElements)
            {
                var lBytes = Encoding.GetBytes(lString.SubstringByTextElements(lFromTextElement, lTextElementCount));

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
                    lLeadingWSP = kEmpty;
                }
                else lTextElementCount++;
            }

            if (lFromTextElement < lString.LengthInTextElements) ZAddEncodedWord(lLeadingWSP, lLastEncoding, lLastEncodedText);

            mLastWordType = eWordType.encodedword;
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

        public IList<byte> Bytes => mBytes;

        private List<byte> ZQEncode(byte[] pBytes, eHeaderValuePartContext pContext)
        {
            List<byte> lResult = new List<byte>();

            foreach (var lByte in pBytes)
            {
                bool lEncode;

                if (lByte <= cASCII.SPACE || lByte == cASCII.EQUALS || lByte == cASCII.QUESTIONMARK || lByte == cASCII.UNDERSCORE || lByte >= cASCII.DEL) lEncode = true;
                else if (pContext == eHeaderValuePartContext.comment) lEncode = lByte == cASCII.LPAREN || lByte == cASCII.RPAREN || lByte == cASCII.BACKSL;
                else if (pContext == eHeaderValuePartContext.phrase)
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
            if (mCurrentLineByteCount + pLeadingWSP.Count + 7 + CharsetNameBytes.Count + pEncodedText.Count > 76)
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
            mBytes.AddRange(CharsetNameBytes);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.Add(pEncoding);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.AddRange(pEncodedText);
            mBytes.AddRange(kQUESTIONMARKEQUALS);

            mCurrentLineByteCount = mCurrentLineByteCount + 7 + CharsetNameBytes.Count + pEncodedText.Count;
            mCurrentLineHasEncodedWords = true;
        }
    }
}