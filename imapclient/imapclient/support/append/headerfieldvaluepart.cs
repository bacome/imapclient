using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal abstract class cHeaderFieldEncodedWordsPart : cHeaderFieldValuePart
    {
        private readonly string mText;
        private readonly eQEncodingRestriction mQEncodingRestriction;

        public cHeaderFieldEncodedWordsPart(string pText, eQEncodingRestriction pQEncodingRestriction)
        {
            mText = pText ?? throw new ArgumentNullException(nameof(pText));

            int lFWSStage = 0;

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

                        if (lChar == '\t') break;

                        if (lChar < ' ' || lChar == cChar.DEL) throw new ArgumentOutOfRangeException(nameof(pText));

                        break;

                    case 1:

                        if (lChar != '\n') throw new ArgumentOutOfRangeException(nameof(pText));
                        lFWSStage = 2;
                        break;

                    case 2:

                        if (lChar != '\t' && lChar != ' ') throw new ArgumentOutOfRangeException(nameof(pText));
                        lFWSStage = 0;
                        break;
                }
            }

            if (lFWSStage != 0) throw new ArgumentOutOfRangeException(nameof(pText));

            mQEncodingRestriction = pQEncodingRestriction;
        }

        protected abstract byte[] GetBytesForNonEncodedWord(List<char> pWordChars);

        internal override void GetBytes(cHeaderFieldBytes pBytes)
        {
            char lChar;
            List<char> lLeadingWSPChars = new List<char>();
            List<char> lWordChars = new List<char>();
            List<char> lEncodedWordLeadingWSPChars = new List<char>();
            List<char> lEncodedWordWordChars = new List<char>();

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
                    // extract leading white space (if any)

                    lLeadingWSPChars.Clear();

                    while (lPosition < lInput.Length)
                    {
                        lChar = lInput[lPosition];

                        if (lChar != '\t' && lChar != ' ') break;

                        lLeadingWSPChars.Add(lChar);
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

                    // process the white space and word
                    //
                    if (ZLooksLikeAnEncodedWord(lWordChars) || (!pBytes.UTF8Allowed && ZWordContainsNonASCII(lWordChars)))
                    {
                        if (lEncodedWordWordChars.Count == 0) lEncodedWordLeadingWSPChars.AddRange(lLeadingWSPChars);
                        if (lEncodedWordWordChars.Count > 0 || pBytes.LastWordWasAnEncodedWord) lEncodedWordWordChars.Add(' ');
                        lEncodedWordWordChars.AddRange(lWordChars);
                    }
                    else
                    {
                        if (lEncodedWordWordChars.Count > 0)
                        {
                            pBytes.AddEncodedWords(lEncodedWordLeadingWSPChars, lEncodedWordWordChars, mQEncodingRestriction);
                            lEncodedWordLeadingWSPChars.Clear();
                            lEncodedWordWordChars.Clear();
                        }

                        pBytes.AddNonEncodedWord(lLeadingWSPChars, lWordChars.Count, GetBytesForNonEncodedWord(lWordChars));
                    }
                }

                // output the cached encoded word chars if any
                //
                if (lEncodedWordWordChars.Count > 0)
                {
                    pBytes.AddEncodedWords(lEncodedWordLeadingWSPChars, lEncodedWordWordChars, mQEncodingRestriction);
                    lEncodedWordLeadingWSPChars.Clear();
                    lEncodedWordWordChars.Clear();
                }

                // nothing left on this line
                if (lIndex == -1) break;

                // next line
                pBytes.AddNewLine();
                lStartIndex = lIndex + 2;
            }
        }

        private bool ZWordContainsNonASCII(List<char> pWordChars)
        {
            foreach (var lChar in pWordChars) if (lChar > cChar.DEL) return true;
            return false;
        }

        private bool ZLooksLikeAnEncodedWord(List<char> pWordChars)
        {
            if (pWordChars.Count < 9) return false;
            if (pWordChars[0] == '=' && pWordChars[1] == '?' && pWordChars[pWordChars.Count - 2] == '?' && pWordChars[pWordChars.Count - 1] == '=') return true;
            return false;
        }

        public override string ToString() => $"{nameof(cHeaderFieldEncodedWordsPart)}({mText},{mQEncodingRestriction})";
    }

    internal class cHeaderFieldUnstructuredPart : cHeaderFieldEncodedWordsPart
    {
        public cHeaderFieldUnstructuredPart(string pText) : base(pText, eQEncodingRestriction.none) { }
        protected override byte[] GetBytesForNonEncodedWord(List<char> pWordChars) => Encoding.UTF8.GetBytes(pWordChars.ToArray());
    }

    internal class cHeaderFieldCommentPart : cHeaderFieldEncodedWordsPart
    {
        public cHeaderFieldCommentPart(string pText) : base(pText, eQEncodingRestriction.comment) { }

        protected override byte[] GetBytesForNonEncodedWord(List<char> pWordChars)
        {
            List<char> lWordChars = new List<char>();

            foreach (var lChar in pWordChars)
            {
                if (lChar == '(' || lChar == ')' || lChar == '\\') lWordChars.Add('\\');
                lWordChars.Add(lChar);
            }

            return Encoding.UTF8.GetBytes(lWordChars.ToArray());
        }
    }

    internal class cHeaderFieldPhrasePart : cHeaderFieldEncodedWordsPart
    {
        public cHeaderFieldPhrasePart(string pText) : base(pText, eQEncodingRestriction.phrase) { }

        protected override byte[] GetBytesForNonEncodedWord(List<char> pWordChars)
        {
            if (cCharset.AText.ContainsAll(pWordChars)) return Encoding.UTF8.GetBytes(pWordChars.ToArray());

            List<char> lWordChars = new List<char>();

            lWordChars.Add('"');

            foreach (var lChar in pWordChars)
            {
                if (lChar == '"' || lChar == '\\') lWordChars.Add('\\');
                lWordChars.Add(lChar);
            }

            lWordChars.Add('"');

            return Encoding.UTF8.GetBytes(lWordChars.ToArray());
        }
    }
}