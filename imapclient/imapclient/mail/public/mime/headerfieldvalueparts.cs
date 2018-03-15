using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract class cHeaderFieldValuePart
    {



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
        }

        public override string ToString() => $"{nameof(cHeaderFieldTextValuePart)}({mText})";

        public static bool TryConstruct(string pText, out cHeaderFieldTextValuePart rPart)
        {
            if (pText == null || !ZTextValid(pText)) { rPart = null; return false; }
            rPart = new cHeaderFieldTextValuePart(pText, true);
            return true;
        }

        ;?;

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