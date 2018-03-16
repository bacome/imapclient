using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.mailclient
{
    public abstract class cHeaderFieldCommentOrText
    {
        internal cHeaderFieldCommentOrText() { }
        internal abstract void AddTo(cHeaderFieldBytes pBytes, eHeaderFieldTextContext pContext);
    }

    public class cHeaderFieldText : cHeaderFieldCommentOrText
    {
        private string mText;

        public cHeaderFieldText(string pText)
        {
            mText = pText ?? throw new ArgumentNullException(nameof(pText));
            if (!cTools.IsValidHeaderFieldText(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
        }

        internal bool HasContent => !string.IsNullOrWhiteSpace(mText);

        internal override void AddTo(cHeaderFieldBytes pBytes, eHeaderFieldTextContext pContext) => pBytes.AddEncodableText(mText, pContext);

        public override string ToString() => $"{nameof(cHeaderFieldText)}({mText})";
    }

    public class cHeaderFieldComment : cHeaderFieldCommentOrText
    {
        private readonly ReadOnlyCollection<cHeaderFieldCommentOrText> mParts;

        public cHeaderFieldComment(IEnumerable<cHeaderFieldCommentOrText> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentOrText>();

            foreach (var lPart in pParts)
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                else lParts.Add(lPart);

            mParts = lParts.AsReadOnly();
        }

        public cHeaderFieldComment(string pText)
        {
            var lParts = new List<cHeaderFieldCommentOrText>();
            lParts.Add(new cHeaderFieldText(pText));
            mParts = lParts.AsReadOnly();
        }

        internal override void AddTo(cHeaderFieldBytes pBytes, eHeaderFieldTextContext pContext)
        {
            pBytes.AddSpecial(cASCII.LPAREN);
            foreach (var lPart in mParts) lPart.AddTo(pBytes, eHeaderFieldTextContext.comment);
            pBytes.AddSpecial(cASCII.RPAREN);
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldComment));
            foreach (var lPart in mParts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public class cHeaderFieldPhrase
    {
        private readonly ReadOnlyCollection<cHeaderFieldCommentOrText> mParts;

        public cHeaderFieldPhrase(IEnumerable<cHeaderFieldCommentOrText> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentOrText>();
            bool lHasContent = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (lPart is cHeaderFieldText lText && lText.HasContent) lHasContent = true;
                lParts.Add(lPart);
            }

            if (!lHasContent) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.HasNoContent);

            mParts = lParts.AsReadOnly();
        }

        public cHeaderFieldPhrase(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (string.IsNullOrWhiteSpace(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
            var lParts = new List<cHeaderFieldCommentOrText>();
            lParts.Add(new cHeaderFieldText(pText));
            mParts = lParts.AsReadOnly();
        }

        internal void AddTo(cHeaderFieldBytes pBytes)
        {
            foreach (var lPart in mParts) lPart.AddTo(pBytes, eHeaderFieldTextContext.phrase);
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldPhrase));
            foreach (var lPart in mParts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}
