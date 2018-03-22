using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract class cHeaderFieldCommentTextQuotedStringPhraseValue
    {
        internal cHeaderFieldCommentTextQuotedStringPhraseValue() { }
    }

    public abstract class cHeaderFieldCommentTextQuotedStringValue : cHeaderFieldCommentTextQuotedStringPhraseValue
    {
        internal cHeaderFieldCommentTextQuotedStringValue() { }
    }

    public abstract class cHeaderFieldCommentTextValue : cHeaderFieldCommentTextQuotedStringValue
    {
        internal cHeaderFieldCommentTextValue() { }
    }

    public class cHeaderFieldTextValue : cHeaderFieldCommentTextValue
    {
        public readonly string Text;

        internal cHeaderFieldTextValue(string pText, bool pInternal)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
        }

        public cHeaderFieldTextValue(string pText)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
        }

        public override string ToString() => $"{nameof(cHeaderFieldTextValue)}({Text})";
    }

    public class cHeaderFieldCommentValue : cHeaderFieldCommentTextValue
    {
        public readonly ReadOnlyCollection<cHeaderFieldCommentTextValue> Parts;

        public cHeaderFieldCommentValue(IEnumerable<cHeaderFieldCommentTextValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentTextValue>();
            bool lLastWasText = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lPart is cHeaderFieldTextValue)
                {
                    if (lLastWasText) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.AdjacencyProblem);
                    lLastWasText = true;
                }
                else lLastWasText = false;

                lParts.Add(lPart);
            }

            Parts = lParts.AsReadOnly();
        }

        public cHeaderFieldCommentValue(string pText)
        {
            var lParts = new List<cHeaderFieldCommentTextValue>();
            lParts.Add(new cHeaderFieldTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldCommentValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public class cHeaderFieldQuotedStringValue : cHeaderFieldCommentTextQuotedStringValue
    {
        public static readonly cHeaderFieldQuotedStringValue Empty = new cHeaderFieldQuotedStringValue(string.Empty);

        public readonly string Text;

        public cHeaderFieldQuotedStringValue(string pText)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
        }

        public override string ToString() => $"{nameof(cHeaderFieldQuotedStringValue)}({Text})";
    }

    public class cHeaderFieldPhraseValue : cHeaderFieldCommentTextQuotedStringPhraseValue
    {
        public readonly ReadOnlyCollection<cHeaderFieldCommentTextQuotedStringValue> Parts;

        public cHeaderFieldPhraseValue(IEnumerable<cHeaderFieldCommentTextQuotedStringValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentTextQuotedStringValue>();
            bool lLastWasText = false;
            bool lHasText = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lPart is cHeaderFieldTextValue)
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

        public cHeaderFieldPhraseValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            var lParts = new List<cHeaderFieldCommentTextQuotedStringValue>();
            lParts.Add(new cHeaderFieldTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldPhraseValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public class cHeaderFieldStructuredValue
    {
        public readonly ReadOnlyCollection<cHeaderFieldCommentTextQuotedStringPhraseValue> Parts;

        public cHeaderFieldStructuredValue(IEnumerable<cHeaderFieldCommentTextQuotedStringPhraseValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentTextQuotedStringPhraseValue>();
            bool lLastWasPhrase = false;

            // text next to text is ok in unstructured;
            //  but you have to be careful that there is a special at the end/ beginning
            //  the same on the text to phrase boundary also

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lPart is cHeaderFieldPhraseValue)
                {
                    if (lLastWasPhrase) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.AdjacencyProblem);
                    lLastWasPhrase = true;
                }
                else lLastWasPhrase = false;

                lParts.Add(lPart);
            }

            Parts = lParts.AsReadOnly();
        }

        public cHeaderFieldStructuredValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            var lParts = new List<cHeaderFieldCommentTextQuotedStringPhraseValue>();
            lParts.Add(new cHeaderFieldTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldStructuredValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}
