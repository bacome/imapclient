using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderFieldMIMEParameter : cHeaderFieldValuePart
    {
        private static readonly cBytes kAsteriskEquals = new cBytes("*=");
        private static readonly cBytes kQuoteQuote = new cBytes("''");

        private cBytes mAttribute;
        private string mValue;

        public cHeaderFieldMIMEParameter(string pAttribute, string pValue)
        {
            if (pAttribute == null) throw new ArgumentNullException(nameof(pAttribute));
            if (!cCharset.RFC2045Token.ContainsAll(pAttribute)) throw new ArgumentOutOfRangeException(nameof(pAttribute));
            mAttribute = new cBytes(pAttribute);
            mValue = pValue ?? throw new ArgumentNullException(nameof(pValue));

            foreach (var lChar in pValue)
            {
                if (lChar == '\t') continue;
                if (lChar < ' ') throw new ArgumentOutOfRangeException(nameof(pValue));
                if (lChar == cChar.DEL) throw new ArgumentOutOfRangeException(nameof(pValue));
            }
        }

        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext)
        {
            // if the value is a short token
            if (cCharset.RFC2045Token.ContainsAll(mValue) && mAttribute.Count + mValue.Length < 76)
            {
                var lWordBytes = new List<byte>();
                lWordBytes.Add(cASCII.SEMICOLON);
                lWordBytes.AddRange(mAttribute);
                lWordBytes.Add(cASCII.EQUALS);
                lWordBytes.AddRange(new cBytes(mValue));
                pBytes.AddNonEncodedWord(lWordBytes);
                return;
            }

            // if the value is a short quoted-string

            bool lValueContainsNonASCII = ZStringContainsNonASCII(mValue);

            if (pBytes.UTF8Allowed || !lValueContainsNonASCII)
            {
                var lQuotedStringValue = ZQuotedString(mValue, out var lQuotingCharCount);

                if (mAttribute.Count + mValue.Length + lQuotingCharCount < 76)
                {
                    var lWordBytes = new List<byte>();
                    lWordBytes.Add(cASCII.SEMICOLON);
                    lWordBytes.AddRange(mAttribute);
                    lWordBytes.Add(cASCII.EQUALS);
                    lWordBytes.AddRange(lQuotedStringValue);
                    pBytes.AddNonEncodedWord(lWordBytes);
                    return;
                }
            }

            int lSectionNumber = 0;
            var lSectionNumberBytes = cTools.IntToBytesReverse(lSectionNumber);
            lSectionNumberBytes.Reverse();
            int lMaxSectionLength;

            StringInfo lValue = new StringInfo(mValue);

            int lFromTextElement = 0;
            int lTextElementCount = 1;

            int lLastTextElementCount = 0;
            IList<byte> lLastSectionBytes = null;

            IList<byte> lSectionBytes;
            int lSectionLength;

            // if the value is long but doesn't need to be encoded
            if (pBytes.UTF8Allowed || !lValueContainsNonASCII)
            {
                // break into sections

                lMaxSectionLength = 74 - mAttribute.Count - lSectionNumberBytes.Count;

                while (lFromTextElement + lTextElementCount <= lValue.LengthInTextElements)
                {
                    var lString = lValue.SubstringByTextElements(lFromTextElement, lTextElementCount);

                    if (cCharset.RFC2045Token.ContainsAll(lString))
                    {
                        lSectionBytes = new cBytes(lString);
                        lSectionLength = lString.Length;
                    }
                    else
                    {
                        lSectionBytes = ZQuotedString(lString, out var lQuotingCharCount);
                        lSectionLength = lString.Length + lQuotingCharCount;
                    }

                    if (lTextElementCount == 1 || lSectionLength <= lMaxSectionLength)
                    {
                        lLastTextElementCount = lTextElementCount;
                        lLastSectionBytes = lSectionBytes;
                    }

                    if (lSectionLength > lMaxSectionLength)
                    {
                        ZAddSection(pBytes, lSectionNumberBytes, lLastSectionBytes);

                        lSectionNumberBytes = cTools.IntToBytesReverse(++lSectionNumber);
                        lSectionNumberBytes.Reverse();
                        lMaxSectionLength = 74 - mAttribute.Count - lSectionNumberBytes.Count;

                        lFromTextElement = lFromTextElement + lLastTextElementCount;
                        lTextElementCount = 1;
                    }
                    else lTextElementCount++;
                }

                if (lFromTextElement < lValue.LengthInTextElements) ZAddSection(pBytes, lSectionNumberBytes, lLastSectionBytes);

                return;
            }

            // if the encoded value is short

            var lPercentEncodedValue = ZPercentEncoded(pBytes.Encoding.GetBytes(mValue));

            if (mAttribute.Count + pBytes.CharsetNameBytes.Count + lPercentEncodedValue.Count < 73)
            {
                var lWordBytes = new List<byte>();
                lWordBytes.Add(cASCII.SEMICOLON);
                lWordBytes.AddRange(mAttribute);
                lWordBytes.AddRange(kAsteriskEquals);
                lWordBytes.AddRange(pBytes.CharsetNameBytes);
                lWordBytes.AddRange(kQuoteQuote);
                lWordBytes.AddRange(lPercentEncodedValue);
                pBytes.AddNonEncodedWord(lWordBytes);
                return;
            }

            // worst scenario: needs encoding and long

            bool lFirstSection = true;
            bool lLastSectionPercentEncoded = true;

            lMaxSectionLength = 72 - mAttribute.Count - lSectionNumberBytes.Count - pBytes.CharsetNameBytes.Count;

            while (lFromTextElement + lTextElementCount <= lValue.LengthInTextElements)
            {
                var lString = lValue.SubstringByTextElements(lFromTextElement, lTextElementCount);

                bool lSectionPercentEncoded;

                if (lFirstSection || ZStringContainsNonASCII(lString))
                {
                    lSectionBytes = ZPercentEncoded(pBytes.Encoding.GetBytes(lString));
                    lSectionLength = lSectionBytes.Count + 1; // extra one for the * in the *=
                    lSectionPercentEncoded = true;
                }
                else if (cCharset.RFC2045Token.ContainsAll(lString))
                {
                    lSectionBytes = new cBytes(lString);
                    lSectionLength = lString.Length;
                    lSectionPercentEncoded = false;
                }
                else
                {
                    lSectionBytes = ZQuotedString(lString, out _);
                    lSectionLength = lSectionBytes.Count;
                    lSectionPercentEncoded = false;
                }

                if (lTextElementCount == 1 || lSectionLength <= lMaxSectionLength)
                {
                    lLastTextElementCount = lTextElementCount;
                    lLastSectionBytes = lSectionBytes;
                    lLastSectionPercentEncoded = lSectionPercentEncoded;
                }

                if (lSectionLength > lMaxSectionLength)
                {
                    ZAddSection(pBytes, lSectionNumberBytes, lLastSectionPercentEncoded, lFirstSection, lLastSectionBytes);

                    lFirstSection = false;

                    lSectionNumberBytes = cTools.IntToBytesReverse(++lSectionNumber);
                    lSectionNumberBytes.Reverse();
                    lMaxSectionLength = 74 - mAttribute.Count - lSectionNumberBytes.Count;

                    lFromTextElement = lFromTextElement + lLastTextElementCount;
                    lTextElementCount = 1;
                }
                else lTextElementCount++;
            }

            if (lFromTextElement < lValue.LengthInTextElements) ZAddSection(pBytes, lSectionNumberBytes, lLastSectionPercentEncoded, lFirstSection, lLastSectionBytes);
        }

        private bool ZStringContainsNonASCII(string pString)
        {
            foreach (var lChar in pString) if (lChar > cASCII.DEL) return true;
            return false;
        }

        private List<byte> ZQuotedString(string pString, out int rQuotingCharCount)
        {
            rQuotingCharCount = 2;

            var lBytes = new List<byte>();

            lBytes.Add(cASCII.DQUOTE);

            foreach (var lByte in Encoding.UTF8.GetBytes(pString))
            {
                if (lByte == cASCII.DQUOTE || lByte == cASCII.BACKSL)
                {
                    lBytes.Add(cASCII.BACKSL);
                    rQuotingCharCount++;
                }

                lBytes.Add(lByte);
            }

            lBytes.Add(cASCII.DQUOTE);

            return lBytes;
        }

        private List<byte> ZPercentEncoded(byte[] pBytes)
        {
            var lBytes = new List<byte>();

            foreach (var lByte in pBytes)
            {
                if (cCharset.AttributeChar.Contains(lByte)) lBytes.Add(lByte);
                else 
                {
                    lBytes.Add(cASCII.PERCENT);
                    lBytes.AddRange(cTools.ByteToHexBytes(lByte));
                }
            }

            return lBytes;
        }

        private void ZAddSection(cHeaderFieldBytes pBytes, IList<byte> pSectionNumberBytes, IList<byte> pSectionBytes)
        {
            var lWordBytes = new List<byte>();
            lWordBytes.Add(cASCII.SEMICOLON);
            lWordBytes.AddRange(mAttribute);
            lWordBytes.Add(cASCII.ASTERISK);
            lWordBytes.AddRange(pSectionNumberBytes);
            lWordBytes.Add(cASCII.EQUALS);
            lWordBytes.AddRange(pSectionBytes);
            pBytes.AddNonEncodedWord(lWordBytes);
        }

        private void ZAddSection(cHeaderFieldBytes pBytes, IList<byte> pSectionNumberBytes, bool pSectionPercentEncoded, bool pFirstSection, IList<byte> pSectionBytes)
        {
            var lWordBytes = new List<byte>();

            lWordBytes.Add(cASCII.SEMICOLON);
            lWordBytes.AddRange(mAttribute);
            lWordBytes.Add(cASCII.ASTERISK);
            lWordBytes.AddRange(pSectionNumberBytes);

            if (pSectionPercentEncoded) lWordBytes.Add(cASCII.ASTERISK);
            lWordBytes.Add(cASCII.EQUALS);

            if (pFirstSection)
            {
                lWordBytes.AddRange(pBytes.CharsetNameBytes);
                lWordBytes.AddRange(kQuoteQuote);
            }

            lWordBytes.AddRange(pSectionBytes);

            pBytes.AddNonEncodedWord(lWordBytes);
        }
    }
}