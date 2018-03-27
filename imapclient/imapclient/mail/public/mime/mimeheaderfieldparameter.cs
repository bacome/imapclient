﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public class cMIMEHeaderFieldParameter
    {
        public readonly string Attribute;
        public readonly string Value;

        public cMIMEHeaderFieldParameter(string pAttribute, string pValue)
        {
            Attribute = pAttribute ?? throw new ArgumentNullException(nameof(pAttribute));
            if (!cCharset.AttributeChar.ContainsAll(pAttribute)) throw new ArgumentOutOfRangeException(nameof(pAttribute));
            Value = pValue ?? throw new ArgumentNullException(nameof(pValue));
            if (!cCharset.WSPVChar.ContainsAll(pValue)) throw new ArgumentOutOfRangeException(nameof(pValue));
        }

        public cMIMEHeaderFieldParameter(string pAttribute, DateTime pDateTime)
        {
            Attribute = pAttribute ?? throw new ArgumentNullException(nameof(pAttribute));
            if (!cCharset.AttributeChar.ContainsAll(pAttribute)) throw new ArgumentOutOfRangeException(nameof(pAttribute));
            Value = cTools.GetRFC822DateTimeString(pDateTime);
        }

        public cMIMEHeaderFieldParameter(string pAttribute, long pValue)
        {
            Attribute = pAttribute ?? throw new ArgumentNullException(nameof(pAttribute));
            if (!cCharset.AttributeChar.ContainsAll(pAttribute)) throw new ArgumentOutOfRangeException(nameof(pAttribute));
            Value = pValue.ToString();
        }

        public override string ToString() => $"{nameof(cMIMEHeaderFieldParameter)}({Attribute},{Value})";
    //}
//}


        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext)
        {
            // if the value is a non-zero-length short token
            if (mValue.Length != 0 && cCharset.RFC2045Token.ContainsAll(mValue) && 1 + mAttribute.Count + 1 + mValue.Length < 78)
            {
                var lWordBytes = new List<byte>();
                lWordBytes.Add(cASCII.SEMICOLON);
                lWordBytes.AddRange(mAttribute);
                lWordBytes.Add(cASCII.EQUALS);
                lWordBytes.AddRange(new cBytes(mValue));
                pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, lWordBytes, lWordBytes.Count);
                return;
            }

            // if the value is a short quoted-string

            bool lValueContainsNonASCII = cTools.ContainsNonASCII(mValue);

            if (pBytes.UTF8Allowed || !lValueContainsNonASCII)
            {
                var lQuotedStringValue = ZQuotedString(mValue, out var lQuotingCharCount);

                if (1 + mAttribute.Count + 1 + mValue.Length + lQuotingCharCount < 78)
                {
                    var lWordBytes = new List<byte>();
                    lWordBytes.Add(cASCII.SEMICOLON);
                    lWordBytes.AddRange(mAttribute);
                    lWordBytes.Add(cASCII.EQUALS);
                    lWordBytes.AddRange(lQuotedStringValue);
                    pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, lWordBytes, 2 + mAttribute.Count + mValue.Length + lQuotingCharCount);
                    return;
                }
            }

            int lSectionNumber = 0;
            var lSectionNumberBytes = cTools.IntToBytesReverse(lSectionNumber);
            lSectionNumberBytes.Reverse();

            StringInfo lValue = new StringInfo(mValue);

            int lFromTextElement = 0;
            int lTextElementCount = 1;

            int lLastTextElementCount = 0;

            // if the value is long but doesn't need to be encoded
            if (pBytes.UTF8Allowed || !lValueContainsNonASCII)
            {
                // break into sections

                sSection lSection;
                sSection lLastSection = new sSection();

                while (lFromTextElement + lTextElementCount <= lValue.LengthInTextElements)
                {
                    var lString = lValue.SubstringByTextElements(lFromTextElement, lTextElementCount);

                    if (cCharset.AttributeChar.ContainsAll(lString)) lSection = ZSection(lSectionNumberBytes, new cBytes(lString), lString.Length);
                    else lSection = ZSection(lSectionNumberBytes, ZQuotedString(lString, out var lQuotingCharCount), lString.Length + lQuotingCharCount);

                    if (lTextElementCount == 1 || lSection.CharCount < 78)
                    {
                        lLastTextElementCount = lTextElementCount;
                        lLastSection = lSection;
                    }

                    if (lSection.CharCount > 77)
                    {
                        pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, lLastSection.Bytes, lLastSection.CharCount);

                        lSectionNumberBytes = cTools.IntToBytesReverse(++lSectionNumber);
                        lSectionNumberBytes.Reverse();

                        lFromTextElement = lFromTextElement + lLastTextElementCount;
                        lTextElementCount = 1;
                    }
                    else lTextElementCount++;
                }

                if (lFromTextElement < lValue.LengthInTextElements) pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, lLastSection.Bytes, lLastSection.CharCount);

                return;
            }

            // if the encoded value is short

            var lPercentEncodedValue = ZPercentEncoded(pBytes.Encoding.GetBytes(mValue));

            if (1 + mAttribute.Count + kAsteriskEquals.Count + pBytes.CharsetNameBytes.Count + kQuoteQuote.Count + lPercentEncodedValue.Count < 78)
            {
                var lWordBytes = new List<byte>();
                lWordBytes.Add(cASCII.SEMICOLON);
                lWordBytes.AddRange(mAttribute);
                lWordBytes.AddRange(kAsteriskEquals);
                lWordBytes.AddRange(pBytes.CharsetNameBytes);
                lWordBytes.AddRange(kQuoteQuote);
                lWordBytes.AddRange(lPercentEncodedValue);
                pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, lWordBytes, lWordBytes.Count);
                return;
            }

            // worst scenario: needs encoding and long
            {
                bool lFirstSection = true;

                List<byte> lSection;
                List<byte> lLastSection = null;

                while (lFromTextElement + lTextElementCount <= lValue.LengthInTextElements)
                {
                    var lString = lValue.SubstringByTextElements(lFromTextElement, lTextElementCount);

                    if (lFirstSection || ZStringContainsNonASCII(lString)) lSection = ZSection(lSectionNumberBytes, true, lFirstSection, pBytes.CharsetNameBytes, ZPercentEncoded(pBytes.Encoding.GetBytes(lString)));
                    else if (cCharset.AttributeChar.ContainsAll(lString)) lSection = ZSection(lSectionNumberBytes, false, false, null, new cBytes(lString));
                    else lSection = ZSection(lSectionNumberBytes, false, false, null, ZQuotedString(lString, out _));

                    if (lTextElementCount == 1 || lSection.Count < 78)
                    {
                        lLastTextElementCount = lTextElementCount;
                        lLastSection = lSection;
                    }

                    if (lSection.Count > 77)
                    {
                        pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, lLastSection, lLastSection.Count);

                        lFirstSection = false;

                        lSectionNumberBytes = cTools.IntToBytesReverse(++lSectionNumber);
                        lSectionNumberBytes.Reverse();

                        lFromTextElement = lFromTextElement + lLastTextElementCount;
                        lTextElementCount = 1;
                    }
                    else lTextElementCount++;
                }

                if (lFromTextElement < lValue.LengthInTextElements) pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, lLastSection, lLastSection.Count);
            }
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

        private sSection ZSection(IList<byte> pSectionNumberBytes, IList<byte> pBytes, int pCharCount)
        {
            var lBytes = new List<byte>();

            lBytes.Add(cASCII.SEMICOLON);
            lBytes.AddRange(mAttribute);
            lBytes.Add(cASCII.ASTERISK);
            lBytes.AddRange(pSectionNumberBytes);
            lBytes.Add(cASCII.EQUALS);
            lBytes.AddRange(pBytes);

            return new sSection(lBytes, 1 + mAttribute.Count + 1 + pSectionNumberBytes.Count + 1 + pCharCount);
        }

        private List<byte> ZSection(IList<byte> pSectionNumberBytes, bool pSectionPercentEncoded, bool pFirstSection, IList<byte> pCharsetNameBytes, IList<byte> pBytes)
        {
            var lBytes = new List<byte>();

            lBytes.Add(cASCII.SEMICOLON);
            lBytes.AddRange(mAttribute);
            lBytes.Add(cASCII.ASTERISK);
            lBytes.AddRange(pSectionNumberBytes);

            if (pSectionPercentEncoded) lBytes.Add(cASCII.ASTERISK);

            lBytes.Add(cASCII.EQUALS);

            if (pFirstSection)
            {
                lBytes.AddRange(pCharsetNameBytes);
                lBytes.AddRange(kQuoteQuote);
            }

            lBytes.AddRange(pBytes);

            return lBytes;
        }

        private struct sSection
        {
            public readonly List<byte> Bytes;
            public readonly int CharCount;

            public sSection(List<byte> pBytes, int pCharCount)
            {
                Bytes = pBytes;
                CharCount = pCharCount;
            }
        }

    }
}