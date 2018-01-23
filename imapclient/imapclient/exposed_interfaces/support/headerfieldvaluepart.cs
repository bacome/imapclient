using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace work.bacome.imapclient.support
{
    public abstract class cHeaderFieldValuePart
    {
        internal abstract void GetBytes(cHeaderFieldBytes pBytes);
    }

    internal enum eQEncodingRestriction { none, comment, phrase }

    public class cHeaderFieldBytes
    {
        internal static readonly ReadOnlyCollection<char> SingleSpace = new ReadOnlyCollection<char>(new List<char>(new char[] { ' ' }));

        private static readonly ReadOnlyCollection<char> kEmpty = new ReadOnlyCollection<char>(new List<char>(new char[] { }));

        private static readonly cBytes kEQUALSQUESTIONMARK = new cBytes("=?");
        private static readonly cBytes kQUESTIONMARKEQUALS = new cBytes("?=");
        private static readonly cBytes kCRLF = new cBytes("\r\n");
        private static readonly cBytes kCRLFSPACE = new cBytes("\r\n ");

        internal readonly Encoding Encoding;
        private readonly List<byte> mCharset;

        private readonly List<byte> mBytes = new List<byte>();

        private int mCurrentLineByteCount;
        private int mCurrentLineCharCount;

        private bool mCurrentLineHasEncodedWords = false;
        private bool mLastWordWasAnEncodedWord = false;

        internal cHeaderFieldBytes(string pFieldName, Encoding pEncoding)
        {
            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));

            foreach (char lChar in pFieldName) mBytes.Add((byte)lChar);
            mBytes.Add(cASCII.COLON);
            mCurrentLineByteCount = pFieldName.Length + 1;
            mCurrentLineCharCount = mCurrentLineByteCount;

            Encoding = pEncoding;
            if (pEncoding == null) mCharset = null;
            else mCharset = cTools.CharsetNameBytes(pEncoding);
        }

        internal bool LastWordWasAnEncodedWord => mLastWordWasAnEncodedWord;

        internal void AddNonEncodedWord(IList<char> pLeadingWSP, int pWordCharCount, IList<byte> pWordBytes)
        {
            if (pLeadingWSP == null) throw new ArgumentNullException(nameof(pLeadingWSP));
            if (pWordCharCount == 0) throw new ArgumentOutOfRangeException(nameof(pWordCharCount));
            if (pWordBytes == null) throw new ArgumentNullException(nameof(pWordBytes));
            if (pWordBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pWordBytes));

            IList<char> lLeadingWSP;
            if (pLeadingWSP.Count == 0) lLeadingWSP = SingleSpace;
            else lLeadingWSP = pLeadingWSP;

            bool lAddLeadingWSP;

            if (mCurrentLineHasEncodedWords)
            {
                if (mCurrentLineByteCount + lLeadingWSP.Count + pWordBytes.Count > 76)
                {
                    mCurrentLineHasEncodedWords = false;
                    lAddLeadingWSP = false;
                }
                else lAddLeadingWSP = true;
            }
            else
            {
                if (mCurrentLineCharCount + lLeadingWSP.Count + pWordCharCount > 78) lAddLeadingWSP = false;
                else lAddLeadingWSP = true;
            }

            if (lAddLeadingWSP)
            {
                foreach (char lChar in lLeadingWSP) mBytes.Add((byte)lChar);
                mCurrentLineByteCount += lLeadingWSP.Count;
                mCurrentLineCharCount += lLeadingWSP.Count;
            }
            else
            {
                mBytes.AddRange(kCRLFSPACE);
                mCurrentLineByteCount = 1;
                mCurrentLineCharCount = 1;
            }

            mBytes.AddRange(pWordBytes);
            mCurrentLineByteCount += pWordBytes.Count;
            mCurrentLineCharCount += pWordCharCount;
            mLastWordWasAnEncodedWord = false;
        }

        internal void AddEncodedWords(List<char> pLeadingWSP, List<char> pWordChars, eQEncodingRestriction pRestriction)
        {
            if (Encoding == null) throw new InvalidOperationException();

            if (pLeadingWSP == null) throw new ArgumentNullException(nameof(pLeadingWSP));
            if (pWordChars == null) throw new ArgumentNullException(nameof(pWordChars));
            if (pWordChars.Count == 0) throw new ArgumentOutOfRangeException(nameof(pWordChars));

            IList<char> lLeadingWSP;
            if (pLeadingWSP.Count == 0) lLeadingWSP = SingleSpace;
            else lLeadingWSP = pLeadingWSP;

            StringInfo lString = new StringInfo(new string(pWordChars.ToArray()));

            int lMaxEncodedByteCount = 75 - 7 - mCharset.Count;

            List<byte> lResult = new List<byte>();

            int lFromTextElement = 0;
            int lTextElementCount = 1;

            int lLastTextElementCount = 0;
            byte lLastEncoding = cASCII.NUL;
            List<byte> lLastEncodedText = null;

            while (lFromTextElement + lTextElementCount <= lString.LengthInTextElements)
            {
                var lBytes = Encoding.GetBytes(lString.SubstringByTextElements(lFromTextElement, lTextElementCount));

                var lQEncodedText = ZQEncode(lBytes, pRestriction);
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
                    ZAddEncodedWord(lLeadingWSP, lLastEncoding, lLastEncodedText, true);
                    lFromTextElement = lFromTextElement + lLastTextElementCount;
                    lTextElementCount = 1;
                    lLeadingWSP = kEmpty;
                }
                else lTextElementCount++;
            }

            if (lFromTextElement < lString.LengthInTextElements) ZAddEncodedWord(lLeadingWSP, lLastEncoding, lLastEncodedText, false);

            mLastWordWasAnEncodedWord = true;
        }

        internal void AddNewLine()
        {
            mBytes.AddRange(kCRLF);
            mCurrentLineHasEncodedWords = false;
            mCurrentLineByteCount = 0;
            mCurrentLineCharCount = 0;
        }

        internal IList<byte> Bytes => mBytes;

        private List<byte> ZQEncode(byte[] pBytes, eQEncodingRestriction pRestriction)
        {
            List<byte> lResult = new List<byte>();

            foreach (var lByte in pBytes)
            {
                bool lEncode;

                if (lByte <= cASCII.SPACE || lByte == cASCII.EQUALS || lByte == cASCII.QUESTIONMARK || lByte == cASCII.UNDERSCORE || lByte >= cASCII.DEL) lEncode = true;
                else if (pRestriction == eQEncodingRestriction.comment) lEncode = lByte == cASCII.LPAREN || lByte == cASCII.RPAREN || lByte == cASCII.BACKSL;
                else if (pRestriction == eQEncodingRestriction.phrase)
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

        private void ZAddEncodedWord(IList<char> pLeadingWSP, byte pEncoding, List<byte> pEncodedText, bool pMoreToCome)
        {
            if (mCurrentLineByteCount + pLeadingWSP.Count + 7 + mCharset.Count + pEncodedText.Count > 76)
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
            mBytes.AddRange(mCharset);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.Add(pEncoding);
            mBytes.Add(cASCII.QUESTIONMARK);
            mBytes.AddRange(pEncodedText);
            mBytes.AddRange(kQUESTIONMARKEQUALS);

            if (pMoreToCome)
            {
                mBytes.AddRange(kCRLFSPACE);
                mCurrentLineByteCount = 1;
            }
            else
            {
                mCurrentLineHasEncodedWords = true;
                mCurrentLineByteCount = mCurrentLineByteCount + 7 + mCharset.Count + pEncodedText.Count;
            }
        }
    }
}