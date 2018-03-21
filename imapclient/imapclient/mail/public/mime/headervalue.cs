using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract class cHeaderCommentTextQuotedStringPhraseValue
    {
        internal cHeaderCommentTextQuotedStringPhraseValue() { }
    }

    public abstract class cHeaderCommentTextQuotedStringValue : cHeaderCommentTextQuotedStringPhraseValue
    {
        internal cHeaderCommentTextQuotedStringValue() { }
    }

    public abstract class cHeaderCommentTextValue : cHeaderCommentTextQuotedStringValue
    {
        internal cHeaderCommentTextValue() { }
    }

    public class cHeaderTextValue : cHeaderCommentTextValue
    {
        public readonly string Text;

        public cHeaderTextValue(string pText)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
        }

        public override string ToString() => $"{nameof(cHeaderTextValue)}({Text})";
    }

    public class cHeaderCommentValue : cHeaderCommentTextValue
    {
        public readonly ReadOnlyCollection<cHeaderCommentTextValue> Parts;

        public cHeaderCommentValue(IEnumerable<cHeaderCommentTextValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderCommentTextValue>();
            bool lLastWasText = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lPart is cHeaderTextValue)
                {
                    if (lLastWasText) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.AdjacencyProblem);
                    lLastWasText = true;
                }
                else lLastWasText = false;

                lParts.Add(lPart);
            }

            Parts = lParts.AsReadOnly();
        }

        public cHeaderCommentValue(string pText)
        {
            var lParts = new List<cHeaderCommentTextValue>();
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

    public class cHeaderQuotedStringValue : cHeaderCommentTextQuotedStringValue
    {
        public static readonly cHeaderQuotedStringValue Empty = new cHeaderQuotedStringValue(string.Empty);

        public readonly string Text;

        public cHeaderQuotedStringValue(string pText)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
        }

        public override string ToString() => $"{nameof(cHeaderQuotedStringValue)}({Text})";
    }

    public class cHeaderPhraseValue : cHeaderCommentTextQuotedStringPhraseValue
    {
        public readonly ReadOnlyCollection<cHeaderCommentTextQuotedStringValue> Parts;

        public cHeaderPhraseValue(IEnumerable<cHeaderCommentTextQuotedStringValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderCommentTextQuotedStringValue>();
            bool lLastWasText = false;
            bool lHasText = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lPart is cHeaderTextValue)
                {
                    if (lLastWasText) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.AdjacencyProblem);
                    lLastWasText = true;
                    lHasText = true;
                }
                else lLastWasText = false;

                lParts.Add(lPart);
            }

            if (!lHasText) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.HasNoContent);

            Parts = lParts.AsReadOnly();
        }

        public cHeaderPhraseValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            var lParts = new List<cHeaderCommentTextQuotedStringValue>();
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
        public readonly ReadOnlyCollection<cHeaderCommentTextQuotedStringPhraseValue> Parts;

        public cHeaderStructuredValue(IEnumerable<cHeaderCommentTextQuotedStringPhraseValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderCommentTextQuotedStringPhraseValue>();
            bool lLastWasPhrase = false;

            // text next to text is ok in unstructured;
            //  but you have to be careful that there is a special at the end/ beginning
            //  the same on the text to phrase boundary also

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lPart is cHeaderPhraseValue)
                {
                    if (lLastWasPhrase) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.AdjacencyProblem);
                    lLastWasPhrase = true;
                }
                else lLastWasPhrase = false;

                lParts.Add(lPart);
            }

            Parts = lParts.AsReadOnly();
        }

        public cHeaderStructuredValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            var lParts = new List<cHeaderCommentTextQuotedStringPhraseValue>();
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
