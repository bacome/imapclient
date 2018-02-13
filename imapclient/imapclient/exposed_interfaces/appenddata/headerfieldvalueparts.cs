using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal enum eHeaderFieldValuePartContext { unstructured, comment, phrase }

    public abstract class cHeaderFieldValuePart
    {
        public static readonly cHeaderFieldValuePart COMMA = new cHeaderFieldSpecialValuePart(cASCII.COMMA);
        public static readonly cHeaderFieldValuePart At = new cHeaderFieldSpecialValuePart(cASCII.AT);
        public static readonly cHeaderFieldValuePart LBRACKET = new cHeaderFieldSpecialValuePart(cASCII.LBRACKET);
        public static readonly cHeaderFieldValuePart RBRACKET = new cHeaderFieldSpecialValuePart(cASCII.RBRACKET);
        public static readonly cHeaderFieldValuePart LESSTHAN = new cHeaderFieldSpecialValuePart(cASCII.LESSTHAN);
        public static readonly cHeaderFieldValuePart GREATERTHAN = new cHeaderFieldSpecialValuePart(cASCII.GREATERTHAN);
        public static readonly cHeaderFieldValuePart COLON = new cHeaderFieldSpecialValuePart(cASCII.COLON);
        public static readonly cHeaderFieldValuePart SEMICOLON = new cHeaderFieldSpecialValuePart(cASCII.SEMICOLON);

        internal abstract void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext);

        public static bool TryAsDotAtom(string pText, out cHeaderFieldValuePart rPart)
        {
            if (pText == null) { rPart = null; return false; }
            if (string.IsNullOrWhiteSpace(pText)) { rPart = null; return false; }
            var lAtoms = pText.Split('.');
            foreach (var lAtom in lAtoms) if (lAtom.Length == 0 || !cCharset.AText.ContainsAll(lAtom)) { rPart = null; return false; }
            rPart = new cHeaderFieldWordValuePart(pText);
            return true;
        }

        public static bool TryAsQuotedString(string pText, out cHeaderFieldValuePart rPart)
        {
            if (pText == null) { rPart = null; return false; }

            var lChars = new List<char>();

            lChars.Add('"');

            foreach (var lChar in pText)
            {
                if (lChar == '\t') lChars.Add(lChar);
                else
                {
                    if (lChar < ' ' || lChar == cChar.DEL) { rPart = null; return false; }

                    if (lChar == '"' || lChar == '\\') lChars.Add('\\');
                    lChars.Add(lChar);
                }
            }

            lChars.Add('"');

            rPart = new cHeaderFieldQuotedStringValuePart(lChars);

            return true;
        }

        public static bool TryAsAddrSpec(string pLocalPart, string pDomain, out cHeaderFieldValuePart rPart)
        {
            if (pLocalPart == null) { rPart = null; return false; }
            if (pDomain == null) { rPart = null; return false; }
            if (pDomain.Length == 0) { rPart = null; return false; }

            List<cHeaderFieldValuePart> lParts = new List<cHeaderFieldValuePart>();

            cHeaderFieldValuePart lLocalPart;

            if (!TryAsDotAtom(pLocalPart, out lLocalPart) && !TryAsQuotedString(pLocalPart, out lLocalPart))
            {
                rPart = null;
                return false;
            }

            lParts.Add(lLocalPart);
            lParts.Add(At);

            // TODO: punycode
            if (TryAsDotAtom(pDomain, out var lDomainPart))
            {
                lParts.Add(lDomainPart);
                rPart = new cHeaderFieldValueParts(lParts);
                return true;
            }

            if (ZIsNoFoldLiteral(pDomain, out var lDText))
            {
                lParts.Add(LBRACKET);
                lParts.Add(new cHeaderFieldWordValuePart(lDText));
                lParts.Add(RBRACKET);
                rPart = new cHeaderFieldValueParts(lParts);
                return true;
            }

            rPart = null;
            return false;
        }

        public static bool TryAsNameAddr(string pDisplayName, string pLocalPart, string pDomain, out cHeaderFieldValuePart rPart)
        {
            if (pLocalPart == null) { rPart = null; return false; }
            if (pDomain == null) { rPart = null; return false; }
            if (pDomain.Length == 0) { rPart = null; return false; }

            List<cHeaderFieldValuePart> lParts = new List<cHeaderFieldValuePart>();

            if (!string.IsNullOrWhiteSpace(pDisplayName))
            {
                if (cHeaderFieldTextValuePart.TryConstruct(pDisplayName, out var lDisplayName)) lParts.Add(new cHeaderFieldPhraseValuePart(new cHeaderFieldCommentOrTextValuePart[] { lDisplayName }));
                else
                {
                    rPart = null;
                    return false;
                }
            }

            if (TryAsAddrSpec(pLocalPart, pDomain, out var lAddrSpec))
            {
                lParts.Add(LESSTHAN);
                lParts.Add(lAddrSpec);
                lParts.Add(GREATERTHAN);

                rPart = new cHeaderFieldValueParts(lParts);
                return true;
            }

            rPart = null;
            return false;
        }

        public static bool TryAsMsgId(string pIdLeft, string pIdRight, out cHeaderFieldValuePart rPart)
        {
            if (TryAsDotAtom(pIdLeft, out _) && (TryAsDotAtom(pIdRight, out _) || ZIsNoFoldLiteral(pIdRight, out _)))
            {
                rPart = new cHeaderFieldWordValuePart("<" + pIdLeft + "@" + pIdRight + ">");
                return true;
            }
            else
            {
                rPart = null;
                return false;
            }
        }

        private static bool ZIsNoFoldLiteral(string pNoFoldLiteral, out string rDText)
        {
            if (pNoFoldLiteral == null || pNoFoldLiteral.Length < 3 || pNoFoldLiteral[0] != '[' || pNoFoldLiteral[pNoFoldLiteral.Length - 1] != ']')
            {
                rDText = null;
                return false;
            }

            rDText = pNoFoldLiteral.Substring(1, pNoFoldLiteral.Length - 2);
            foreach (var lChar in rDText) if (lChar <= ' ' || lChar == '[' || lChar == '\\' || lChar == ']' || lChar == cChar.DEL) return false;
            return true;
        }

        private class cHeaderFieldSpecialValuePart : cHeaderFieldValuePart
        {
            private readonly byte mByte;
            public cHeaderFieldSpecialValuePart(byte pByte) { mByte = pByte; }
            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext) => pBytes.AddSpecial(mByte);
            public override string ToString() => $"{nameof(cHeaderFieldSpecialValuePart)}({(char)mByte})";
        }

        private class cHeaderFieldWordValuePart : cHeaderFieldValuePart
        {
            private readonly cBytes mWordBytes;
            private readonly int mWordCharCount;

            public cHeaderFieldWordValuePart(string pWord)
            {
                if (pWord == null) throw new ArgumentNullException(nameof(pWord));
                mWordBytes = new cBytes(Encoding.UTF8.GetBytes(pWord));
                mWordCharCount = pWord.Length;
            }

            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext)
            {
                pBytes.AddNonEncodedWord(cHeaderFieldBytes.NoWSP, mWordBytes, mWordCharCount);
            }

            public override string ToString() => $"{nameof(cHeaderFieldWordValuePart)}({mWordBytes})";
        }

        private class cHeaderFieldQuotedStringValuePart : cHeaderFieldValuePart
        {
            private readonly ReadOnlyCollection<char> mChars;

            public cHeaderFieldQuotedStringValuePart(IList<char> pChars)
            {
                if (pChars == null) throw new ArgumentNullException(nameof(pChars));
                if (pChars.Count < 3) throw new ArgumentOutOfRangeException(nameof(pChars)); // must have at least ""
                mChars = new ReadOnlyCollection<char>(pChars);
            }

            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext)
            {
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
                    pBytes.AddNonEncodedWord(lLeadingWSP, Encoding.UTF8.GetBytes(lWordChars.ToArray()), lWordChars.Count);
                }
            }

            public override string ToString() => $"{nameof(cHeaderFieldQuotedStringValuePart)}({new string(mChars.ToArray())})";
        }

        private class cHeaderFieldValueParts : cHeaderFieldValuePart
        {
            private ReadOnlyCollection<cHeaderFieldValuePart> mParts;

            public cHeaderFieldValueParts(List<cHeaderFieldValuePart> pParts)
            {
                if (pParts == null) throw new ArgumentNullException(nameof(pParts));
                mParts = pParts.AsReadOnly();
            }

            internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext)
            {
                foreach (var lPart in mParts) lPart.GetBytes(pBytes, pContext);
            }

            public override string ToString()
            {
                cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldValueParts));
                foreach (var lPart in mParts) lBuilder.Append(lPart);
                return lBuilder.ToString();
            }
        }

        public static implicit operator cHeaderFieldValuePart(string pString) => new cHeaderFieldTextValuePart(pString);

        public static implicit operator cHeaderFieldValuePart(DateTime pDateTime)
        {
            string lSign;
            string lZone;

            if (pDateTime.Kind == DateTimeKind.Local)
            {
                var lOffset = TimeZoneInfo.Local.GetUtcOffset(pDateTime);

                if (lOffset < TimeSpan.Zero)
                {
                    lSign = "-";
                    lOffset = -lOffset;
                }
                else lSign = "+";

                lZone = lOffset.ToString("hhmm");
            }
            else if (pDateTime.Kind == DateTimeKind.Utc)
            {
                lSign = "+";
                lZone = "0000";
            }
            else
            {
                lSign = "-";
                lZone = "0000";
            }

            var lMonth = cRFCMonth.cName[pDateTime.Month - 1];

            string lDateTime = string.Format("{0:dd} {1} {0:yyyy} {0:HH}:{0:mm}:{0:ss} {2}{3}", pDateTime, lMonth, lSign, lZone);

            return new cHeaderFieldTextValuePart(lDateTime);
        }

        public static implicit operator cHeaderFieldValuePart(DateTimeOffset pDateTimeOffset)
        {
            string lSign;
            string lZone;

            var lOffset = pDateTimeOffset.Offset;

            if (lOffset < TimeSpan.Zero)
            {
                lSign = "-";
                lOffset = -lOffset;
            }
            else lSign = "+";

            lZone = lOffset.ToString("hhmm");

            var lMonth = cRFCMonth.cName[pDateTimeOffset.Month - 1];

            string lDateTime = string.Format("{0:dd} {1} {0:yyyy} {0:HH}:{0:mm}:{0:ss} {2}{3}", pDateTimeOffset, lMonth, lSign, lZone);

            return new cHeaderFieldTextValuePart(lDateTime);
        }



        internal static void _Tests(cTrace.cContext pParentContext)
        {
            if (TryAsDotAtom("", out _)) throw new cTestsException("dotatom.1", pParentContext);
            if (TryAsDotAtom(".fred", out _)) throw new cTestsException("dotatom.2", pParentContext);
            if (TryAsDotAtom("fred..fred", out _)) throw new cTestsException("dotatom.3", pParentContext);
            if (TryAsDotAtom("fred.", out _)) throw new cTestsException("dotatom.4", pParentContext);
            if (!TryAsDotAtom("fred.fred", out _)) throw new cTestsException("dotatom.5", pParentContext);

            ZTestAddrSpec("1", "non.existant", "bacome.work", "x:non.existant@bacome.work");
            ZTestAddrSpec("2", "non,existant", "bacome.work", "x:\"non,existant\"@bacome.work");
            ZTestAddrSpec("3", "non\0existant", "bacome.work", null);
            ZTestAddrSpec("4", "non.existant", "[bacome.work]", "x:non.existant@[bacome.work]");
            ZTestAddrSpec("5", "non.existant", "[bacome]work", null);

            ZTestNameAddr("1", null, "non.existant", "bacome.work", "x:<non.existant@bacome.work>");
            ZTestNameAddr("2", "", "non.existant", "bacome.work", "x:<non.existant@bacome.work>");
            ZTestNameAddr("3", " ", "non.existant", "bacome.work", "x:<non.existant@bacome.work>");
            ZTestNameAddr("4", "Keld Jørn Simonsen", "non.existant", "bacome.work", "x:Keld =?utf-8?b?SsO4cm4=?= Simonsen<non.existant@bacome.work>");

            TryAsMsgId("left", "right", out var lPart);
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes("x", null);

            for (int i = 0; i < 6; i++)
            {
                if (i != 0) COMMA.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
                lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
            }

            string lString = cTools.ASCIIBytesToString(lBytes.Bytes);
            if (lString != "x:<left@right>,<left@right>,<left@right>,<left@right>,<left@right>,\r\n <left@right>") throw new cTestsException("msgid");

            //  12345678901234567890123456789012345678901234567890123456789012345678901234567890
            // "x:<left@right>,<left@right>,<left@right>,<left@right>,<left@right>,<left@right>"
        }

        private static void ZTestAddrSpec(string pTestName, string pLocalPart, string pDomain, string pExpected)
        {
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes("x", null);

            if (!TryAsAddrSpec(pLocalPart, pDomain, out var lPart))
            {
                if (pExpected == null) return;
                throw new cTestsException($"addrspec.{pTestName}.f");
            }

            if (pExpected == null) throw new cTestsException($"addrspec.{pTestName}.s");

            lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
            string lString = cTools.ASCIIBytesToString(lBytes.Bytes);
            if (lString != pExpected) throw new cTestsException($"addrspec.{pTestName}.e({lString})");
        }

        private static void ZTestNameAddr(string pTestName, string pDisplayName, string pLocalPart, string pDomain, string pExpected)
        {
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes("x", Encoding.UTF8);

            if (!TryAsNameAddr(pDisplayName, pLocalPart, pDomain, out var lPart))
            {
                if (pExpected == null) return;
                throw new cTestsException($"nameaddr.{pTestName}.f");
            }

            if (pExpected == null) throw new cTestsException($"nameaddr.{pTestName}.s");

            lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
            string lString = cTools.ASCIIBytesToString(lBytes.Bytes);
            if (lString != pExpected) throw new cTestsException($"nameaddr.{pTestName}.e({lString})");
        }
    }

    public abstract class cHeaderFieldCommentOrTextValuePart : cHeaderFieldValuePart { }

    public class cHeaderFieldTextValuePart : cHeaderFieldCommentOrTextValuePart
    {
        private string mText;

        private cHeaderFieldTextValuePart(string pText, bool pValidated)
        {
            mText = pText;
        }

        public cHeaderFieldTextValuePart(string pText)
        {
            mText = pText ?? throw new ArgumentNullException(nameof(pText));
            if (!ZTextValid(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
        }

        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext)
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
                int lIndex = mText.IndexOf("\r\n", lStartIndex, StringComparison.Ordinal);

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
                            case eHeaderFieldValuePartContext.unstructured:

                                lNonEncodedWordChars = lWordChars;
                                break;

                            case eHeaderFieldValuePartContext.comment:

                                lNonEncodedWordChars = new List<char>();

                                foreach (var lWordChar in lWordChars)
                                {
                                    if (lWordChar == '(' || lWordChar == ')' || lWordChar == '\\') lNonEncodedWordChars.Add('\\');
                                    lNonEncodedWordChars.Add(lWordChar);
                                }

                                break;

                            case eHeaderFieldValuePartContext.phrase:

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

                        pBytes.AddNonEncodedWord(lLeadingWSP, Encoding.UTF8.GetBytes(lNonEncodedWordChars.ToArray()), lNonEncodedWordChars.Count);
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

        public override string ToString() => $"{nameof(cHeaderFieldTextValuePart)}({mText})";

        public static bool TryConstruct(string pText, out cHeaderFieldTextValuePart rPart)
        {
            if (pText == null || !ZTextValid(pText)) { rPart = null; return false; }
            rPart = new cHeaderFieldTextValuePart(pText, true);
            return true;
        }

        private static bool ZTextValid(string pText)
        {
            int lFWSStage = 0;

            foreach (char lChar in pText)
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

                        if (lChar < ' ' || lChar == cChar.DEL) return false;

                        break;

                    case 1:

                        if (lChar != '\n') return false;
                        lFWSStage = 2;
                        break;

                    case 2:

                        if (lChar != '\t' && lChar != ' ') return false;
                        lFWSStage = 0;
                        break;
                }
            }

            return lFWSStage == 0;
        }
    }

    public class cHeaderFieldPhraseValuePart : cHeaderFieldValuePart
    {
        private readonly ReadOnlyCollection<cHeaderFieldCommentOrTextValuePart> mParts;

        public cHeaderFieldPhraseValuePart(IEnumerable<cHeaderFieldCommentOrTextValuePart> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            List<cHeaderFieldCommentOrTextValuePart> lParts = new List<cHeaderFieldCommentOrTextValuePart>();

            foreach (var lPart in pParts)
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                else lParts.Add(lPart);

            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrTextValuePart>(lParts);
        }

        public cHeaderFieldPhraseValuePart(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));

            var lParts = new List<cHeaderFieldCommentOrTextValuePart>();
            lParts.Add(new cHeaderFieldTextValuePart(pText));
            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrTextValuePart>(lParts);
        }

        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext)
        {
            foreach (var lPart in mParts) lPart.GetBytes(pBytes, eHeaderFieldValuePartContext.phrase);
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldPhraseValuePart));
            foreach (var lPart in mParts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public class cHeaderFieldCommentValuePart : cHeaderFieldCommentOrTextValuePart
    {
        private readonly ReadOnlyCollection<cHeaderFieldCommentOrTextValuePart> mParts;

        public cHeaderFieldCommentValuePart(IEnumerable<cHeaderFieldCommentOrTextValuePart> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            List<cHeaderFieldCommentOrTextValuePart> lParts = new List<cHeaderFieldCommentOrTextValuePart>();

            foreach (var lPart in pParts)
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                else lParts.Add(lPart);

            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrTextValuePart>(lParts);
        }

        public cHeaderFieldCommentValuePart(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));

            var lParts = new List<cHeaderFieldCommentOrTextValuePart>();
            lParts.Add(new cHeaderFieldTextValuePart(pText));
            mParts = new ReadOnlyCollection<cHeaderFieldCommentOrTextValuePart>(lParts);
        }

        internal override void GetBytes(cHeaderFieldBytes pBytes, eHeaderFieldValuePartContext pContext)
        {
            pBytes.AddSpecial(cASCII.LPAREN);
            foreach (var lPart in mParts) lPart.GetBytes(pBytes, eHeaderFieldValuePartContext.comment);
            pBytes.AddSpecial(cASCII.RPAREN);
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldCommentValuePart));
            foreach (var lPart in mParts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}