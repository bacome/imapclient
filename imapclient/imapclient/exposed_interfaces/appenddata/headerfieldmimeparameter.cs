using System;
using System.Collections.Generic;
using System.Globalization;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderFieldMIMEParameter : cHeaderFieldValuePart
    {
        private string mAttribute;
        private string mValue;

        public cHeaderFieldMIMEParameter(string pAttribute, string pValue)
        {
            mAttribute = pAttribute ?? throw new ArgumentNullException(nameof(pAttribute));
            if (!cCharset.RFC2045Token.ContainsAll(pAttribute)) throw new ArgumentOutOfRangeException(nameof(pAttribute));
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
            if (cCharset.RFC2045Token.ContainsAll(mValue) && mAttribute.Length + mValue.Length < 76)
            {
                var lWordChars = new List<char>();
                lWordChars.Add(';');
                lWordChars.AddRange(mAttribute);
                lWordChars.Add('=');
                lWordChars.AddRange(mValue);
                pBytes.AddNonEncodedWord(cHeaderFieldBytes.SingleSpace, lWordChars);
                return;
            }

            // if the value is a short quoted-string

            bool lValueContainsNonASCII = ZStringContainsNonASCII(mValue);

            if (pBytes.UTF8Allowed || !lValueContainsNonASCII)
            {
                var lQuotedStringValue = ZQuotedString(mValue, out _);

                if (mAttribute.Length + lQuotedStringValue.Length < 76)
                {
                    var lWordChars = new List<char>();
                    lWordChars.Add(';');
                    lWordChars.AddRange(mAttribute);
                    lWordChars.Add('=');
                    lWordChars.AddRange(lQuotedStringValue);
                    pBytes.AddNonEncodedWord(cHeaderFieldBytes.SingleSpace, lWordChars);
                    return;
                }
            }

            int lSectionNumber = 0;
            string lSectionNumberString = lSectionNumber.ToString();
            int lMaxSectionLength;

            StringInfo lValue = new StringInfo(mValue);

            int lFromTextElement = 0;
            int lTextElementCount = 1;

            int lLastTextElementCount = 0;
            string lLastSectionText = null;

            string lSectionText;
            int lSectionLength;

            // if the value is long but doesn't need to be encoded
            if (pBytes.UTF8Allowed || !lValueContainsNonASCII)
            {
                // break into sections

                lMaxSectionLength = 74 - mAttribute.Length - lSectionNumberString.Length;

                while (lFromTextElement + lTextElementCount <= lValue.LengthInTextElements)
                {
                    var lString = lValue.SubstringByTextElements(lFromTextElement, lTextElementCount);

                    if (cCharset.RFC2045Token.ContainsAll(lString))
                    {
                        lSectionText = lString;
                        lSectionLength = lTextElementCount;
                    }
                    else
                    {
                        lSectionText = ZQuotedString(lString, out var lQuotingCharCount);
                        lSectionLength = lTextElementCount + lQuotingCharCount;
                    }

                    if (lTextElementCount == 1 || lSectionLength <= lMaxSectionLength)
                    {
                        lLastTextElementCount = lTextElementCount;
                        lLastSectionText = lSectionText;
                    }

                    if (lSectionLength > lMaxSectionLength)
                    {
                        ZAddSection(pBytes, lSectionNumberString, lLastSectionText);

                        lSectionNumber++;
                        lSectionNumberString = lSectionNumber.ToString();
                        lMaxSectionLength = 74 - mAttribute.Length - lSectionNumberString.Length;

                        lFromTextElement = lFromTextElement + lLastTextElementCount;
                        lTextElementCount = 1;
                    }
                    else lTextElementCount++;
                }

                if (lFromTextElement < lValue.LengthInTextElements) ZAddSection(pBytes, lSectionNumberString, lLastSectionText);

                return;
            }

            // if the encoded value is short

            var lPercentEncodedValue = ZPercentEncoded(pBytes.Encoding, mValue);

            if (mAttribute.Length + pBytes.Encoding.WebName.Length + lPercentEncodedValue.count < 73)
            {
                var lWordChars = new List<char>();
                lWordChars.Add(';');
                lWordChars.AddRange(mAttribute);
                lWordChars.AddRange("*=");
                lWordChars.AddRange(pBytes.Encoding.WebName);
                lWordChars.AddRange("''");
                lWordChars.AddRange(lPercentEncodedValue);
                pBytes.AddNonEncodedWord(cHeaderFieldBytes.SingleSpace, lWordChars);
                return;
            }

            // worst scenario: needs encoding and long

            bool lLastSectionPercentEncoded = true;

            lMaxSectionLength = 72 - mAttribute.Length - lSectionNumberString.Length - pBytes.Encoding.WebName.Length;

            while (lFromTextElement + lTextElementCount <= lValue.LengthInTextElements)
            {
                var lString = lValue.SubstringByTextElements(lFromTextElement, lTextElementCount);

                bool lSectionPercentEncoded;

                if (lFromTextElement == 0 || ZStringContainsNonASCII(lString))
                {
                    lSectionText = ZPercentEncoded(pBytes.Encoding, lString);
                    lSectionLength = lSectionText.Length + 1; // for the * in the *=
                    lSectionPercentEncoded = true;
                }
                else if (cCharset.RFC2045Token.ContainsAll(lString))
                {
                    lSectionText = lString;
                    lSectionLength = lString.Length;
                    lSectionPercentEncoded = false;
                }
                else
                {
                    lSectionText = ZQuotedString(lString, out _);
                    lSectionLength = lSectionText.Length;
                    lSectionPercentEncoded = false;
                }

                if (lTextElementCount == 1 || lSectionLength <= lMaxSectionLength)
                {
                    lLastTextElementCount = lTextElementCount;
                    lLastSectionText = lSectionText;
                    lLastSectionPercentEncoded = lSectionPercentEncoded;
                }

                if (lSectionLength > lMaxSectionLength)
                {
                    ZAddSection(pBytes, lSectionNumberString, lSectionPercentEncoded, pBytes.Encoding.WebName, lLastSectionText);

                    lSectionNumber++;
                    lSectionNumberString = lSectionNumber.ToString();
                    lMaxSectionLength = 74 - mAttribute.Length - lSectionNumberString.Length;

                    lFromTextElement = lFromTextElement + lLastTextElementCount;
                    lTextElementCount = 1;
                }
                else lTextElementCount++;
            }

            if (lFromTextElement < lValue.LengthInTextElements) ZAddSection(pBytes, lSectionNumberString, lSectionPercentEncoded, pBytes.Encoding.WebName, lLastSectionText);
        }


    }
}