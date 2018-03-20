using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract class cHeaderCommentTextOrPhraseValue
    {
        internal cHeaderCommentTextOrPhraseValue() { }
    }

    public abstract class cHeaderCommentOrTextValue : cHeaderCommentTextOrPhraseValue
    {
        internal cHeaderCommentOrTextValue() { }
    }

    public class cHeaderTextValue : cHeaderCommentOrTextValue
    {
        public readonly string Text;

        public cHeaderTextValue(string pText)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
        }

        public override string ToString() => $"{nameof(cHeaderTextValue)}({Text})";
    }

    public class cHeaderCommentValue : cHeaderCommentOrTextValue
    {
        public readonly ReadOnlyCollection<cHeaderCommentOrTextValue> Parts;

        public cHeaderCommentValue(IEnumerable<cHeaderCommentOrTextValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderCommentOrTextValue>();

            foreach (var lPart in pParts)
            {
                // can't have adjacent text parts
            }
                ;?;
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                else lParts.Add(lPart);

            Parts = lParts.AsReadOnly();
        }

        public cHeaderCommentValue(string pText)
        {
            var lParts = new List<cHeaderCommentOrTextValue>();
            lParts.Add(new cHeaderTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderCommentValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public class cHeaderPhraseValue : cHeaderCommentTextOrPhraseValue
    {
        public readonly ReadOnlyCollection<cHeaderCommentOrTextValue> Parts;

        public cHeaderPhraseValue(IEnumerable<cHeaderCommentOrTextValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderCommentOrTextValue>();
            bool lHasContent = false;

            foreach (var lPart in pParts)
            {
                ;?;                // can't have adjacent text parts

                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (lPart is cHeaderTextValue lText) lHasContent = true;
                lParts.Add(lPart);
            }

            if (!lHasContent) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.HasNoContent);

            Parts = lParts.AsReadOnly();
        }

        public cHeaderPhraseValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (string.IsNullOrWhiteSpace(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
            var lParts = new List<cHeaderCommentOrTextValue>();
            lParts.Add(new cHeaderTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderPhraseValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public class cHeaderStructuredValue
    {
        public readonly ReadOnlyCollection<cHeaderCommentTextOrPhraseValue> Parts;

        public cHeaderStructuredValue(IEnumerable<cHeaderCommentTextOrPhraseValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderCommentTextOrPhraseValue>();

            foreach (var lPart in pParts)
            {
                // cant have phrase next to phrase

                ;?;
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lParts.Add(lPart);
            }

            Parts = lParts.AsReadOnly();
        }

        public cHeaderStructuredValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (string.IsNullOrWhiteSpace(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
            var lParts = new List<cHeaderCommentTextOrPhraseValue>();
            lParts.Add(new cHeaderTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderStructuredValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}
