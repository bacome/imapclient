using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal enum eHeaderValuePartContext { unstructured, comment, phrase }

    public abstract class cHeaderFieldValuePart
    {
        public static readonly cHeaderFieldValuePart Comma = new cHeaderFieldSpecial(cASCII.COMMA);

        internal abstract void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext);

        private class cHeaderFieldSpecial : cHeaderFieldValuePart
        {
            private readonly byte mByte;
            public cHeaderFieldSpecial(byte pByte) { mByte = pByte; }
            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext) => pBytes.AddSpecial(mByte);
        }
    }

    public abstract class cHeaderFieldCommentOrText : cHeaderFieldValuePart { }

    public class cHeaderFieldText : cHeaderFieldCommentOrText
    {
        private string mText;

        public cHeaderFieldText(string pText)
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
        }

        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext)
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
                            pBytes.AddEncodedWords(lEncodedWordLeadingWSPChars, lEncodedWordWordChars, pContext);
                            lEncodedWordLeadingWSPChars.Clear();
                            lEncodedWordWordChars.Clear();
                        }

                        List<char> lNonEncodedWordChars;

                        switch (pContext)
                        {
                            case eHeaderValuePartContext.unstructured:

                                lNonEncodedWordChars = lWordChars;
                                break;

                            case eHeaderValuePartContext.comment:

                                lNonEncodedWordChars = new List<char>();

                                foreach (var lWordChar in lWordChars)
                                {
                                    if (lWordChar == '(' || lWordChar == ')' || lWordChar == '\\') lNonEncodedWordChars.Add('\\');
                                    lNonEncodedWordChars.Add(lWordChar);
                                }

                                break;

                            case eHeaderValuePartContext.phrase:

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

                                throw new cInternalErrorException();
                        }

                        pBytes.AddNonEncodedWord(lLeadingWSPChars, lNonEncodedWordChars);
                    }
                }

                // output the cached encoded word chars if any
                //
                if (lEncodedWordWordChars.Count > 0)
                {
                    pBytes.AddEncodedWords(lEncodedWordLeadingWSPChars, lEncodedWordWordChars, pContext);
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

        public override string ToString() => $"{nameof(cHeaderFieldText)}({mText})";
    }

    public class cHeaderFieldPhrase : cHeaderFieldValuePart
    {
        private readonly ReadOnlyCollection<cHeaderFieldCommentOrText> mParts;

        public cHeaderFieldPhrase(IEnumerable<cHeaderFieldCommentOrText> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            List<cHeaderFieldCommentOrText> lParts = new List<cHeaderFieldCommentOrText>();

            foreach (var lPart in pParts)
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                else lParts.Add(lPart);

            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrText>(lParts);
        }

        public cHeaderFieldPhrase(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));

            var lParts = new List<cHeaderFieldCommentOrText>();
            lParts.Add(new cHeaderFieldText(pText));
            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrText>(lParts);
        }

        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext)
        {
            foreach (var lPart in mParts) lPart.GetBytes(pBytes, eHeaderValuePartContext.phrase);
        }
    }

    public class cHeaderFieldComment : cHeaderFieldCommentOrText
    {
        private readonly ReadOnlyCollection<cHeaderFieldCommentOrText> mParts;

        public cHeaderFieldComment(IEnumerable<cHeaderFieldCommentOrText> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            List<cHeaderFieldCommentOrText> lParts = new List<cHeaderFieldCommentOrText>();

            foreach (var lPart in pParts)
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                else lParts.Add(lPart);

            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrText>(lParts);
        }

        public cHeaderFieldComment(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));

            var lParts = new List<cHeaderFieldCommentOrText>();
            lParts.Add(new cHeaderFieldText(pText));
            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrText>(lParts);
        }

        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext)
        {
            pBytes.AddSpecial(cASCII.LPAREN);
            foreach (var lPart in mParts) lPart.GetBytes(pBytes, eHeaderValuePartContext.comment);
            pBytes.AddSpecial(cASCII.RPAREN);
        }
    }
}