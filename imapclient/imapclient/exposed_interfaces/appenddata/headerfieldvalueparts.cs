﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal enum eHeaderValuePartContext { unstructured, comment, phrase }

    public abstract class cHeaderFieldValuePart
    {
        public static readonly cHeaderFieldValuePart Comma = new cHeaderFieldSpecial(cASCII.COMMA);

        internal abstract void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext);

        public bool TryAsDotAtom(string pText, out cHeaderFieldValuePart rPart)
        {
            if (pText == null) { rPart = null; return false; }
            if (string.IsNullOrWhiteSpace(pText)) { rPart = null; return false; }
            var lAtoms = pText.Split('.');
            foreach (var lAtom in lAtoms) if (lAtom.Length == 0 || !cCharset.AText.ContainsAll(lAtom)) { rPart = null; return false; }
            bool lContainsNonASCII = pText.Any((lChar) => lChar > cChar.DEL);
            rPart = new cHeaderFieldDotAtom(pText, lContainsNonASCII);
            return true;
        }

        public bool TryAsQuotedString(string pText, out cHeaderFieldValuePart rPart)
        {
            if (pText == null) { rPart = null; return false; }

            var lChars = new List<char>();
            bool lContainsNonASCII = false;

            lChars.Add('"');

            foreach (var lChar in pText)
            {
                if (lChar == '\t') lChars.Add(lChar);
                else
                {
                    if (lChar < ' ' || lChar == cChar.DEL) { rPart = null; return false; }

                    if (lChar == '"' || lChar == '\\') lChars.Add('\\');
                    lChars.Add(lChar);

                    if (lChar > cChar.DEL) lContainsNonASCII = true;
                }
            }

            lChars.Add('"');

            rPart = new cHeaderFieldQuotedString(lChars.AsReadOnly(), lContainsNonASCII);

            return true;
        }

        private class cHeaderFieldSpecial : cHeaderFieldValuePart
        {
            private readonly byte mByte;
            public cHeaderFieldSpecial(byte pByte) { mByte = pByte; }
            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext) => pBytes.AddSpecial(mByte);
        }

        private class cHeaderFieldDotAtom : cHeaderFieldValuePart
        {
            private readonly string mText;
            private readonly bool mContainsNonASCII;

            public cHeaderFieldDotAtom(string pText, bool pContainsNonASCII)
            {
                mText = pText ?? throw new ArgumentNullException(nameof(pText));
                mContainsNonASCII = pContainsNonASCII;
            }

            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext)
            {
                if (mContainsNonASCII && !pBytes.UTF8Allowed) throw new cUTF8RequiredException();
                pBytes.AddNonEncodedWord(mText);
            }
        }

        private class cHeaderFieldQuotedString : cHeaderFieldValuePart
        {
            private readonly ReadOnlyCollection<char> mChars;
            private readonly bool mContainsNonASCII;

            public cHeaderFieldQuotedString(ReadOnlyCollection<char> pChars, bool pContainsNonASCII)
            {
                mChars = pChars ?? throw new ArgumentNullException(nameof(pChars));
                mContainsNonASCII = pContainsNonASCII;
            }

            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderValuePartContext pContext)
            {
                if (mContainsNonASCII && !pBytes.UTF8Allowed) throw new cUTF8RequiredException();

                char lChar;
                List<char> lLeadingWSP = new List<char>();
                List<char> lWordChars = new List<char>();

                int lPosition = 0;

                while (lPosition < mChars.Count)
                {
                    // extract leading white space

                    lLeadingWSP.Clear();

                    while (lPosition < mChars.Count)
                    {
                        lChar = mChars[lPosition];

                        if (lChar != '\t' && lChar != ' ') break;

                        lLeadingWSP.Add(lChar);
                        lPosition++;
                    }

                    // extract word

                    lWordChars.Clear();

                    while (lPosition < mChars.Count)
                    {
                        lChar = mChars[lPosition];

                        if (lChar == '\t' || lChar == ' ') break;

                        lWordChars.Add(lChar);
                        lPosition++;
                    }

                    // add the wsp and word
                    pBytes.AddNonEncodedWord(lLeadingWSP, lWordChars);
                }
            }
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
            List<char> lLeadingWSP = new List<char>();
            List<char> lWordChars = new List<char>();
            List<char> lEncodedWordLeadingWSP = new List<char>();
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

                    // process the white space and word
                    //
                    if (ZLooksLikeAnEncodedWord(lWordChars) || (!pBytes.UTF8Allowed && ZWordContainsNonASCII(lWordChars)))
                    {
                        if (lEncodedWordWordChars.Count == 0) lEncodedWordLeadingWSP.AddRange(lLeadingWSP);
                        if (lEncodedWordWordChars.Count > 0 || pBytes.LastWordWasAnEncodedWord) lEncodedWordWordChars.Add(' ');
                        lEncodedWordWordChars.AddRange(lWordChars);
                    }
                    else
                    {
                        if (lEncodedWordWordChars.Count > 0)
                        {
                            pBytes.AddEncodedWords(lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext);
                            lEncodedWordLeadingWSP.Clear();
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

                        pBytes.AddNonEncodedWord(lLeadingWSP, lNonEncodedWordChars);
                    }
                }

                // output the cached encoded word chars if any
                //
                if (lEncodedWordWordChars.Count > 0)
                {
                    pBytes.AddEncodedWords(lEncodedWordLeadingWSP, lEncodedWordWordChars, pContext);
                    lEncodedWordLeadingWSP.Clear();
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