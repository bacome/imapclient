﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents part of an RFC 5322 header field value. The part can be one of; an RFC 5322 comment, an RFC 5322 quoted string, an RFC 5322 phrase or text.
    /// </summary>
    public abstract class cHeaderFieldCommentTextQuotedStringPhraseValue: iCanComposeHeaderFieldValue
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        protected cHeaderFieldCommentTextQuotedStringPhraseValue() { }

        /// <inheritdoc cref="iCanComposeHeaderFieldValue.CanComposeHeaderFieldValue(bool)"/>
        public abstract bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed);
    }

    /// <summary>
    /// Represents part of an RFC 5322 header field value. The part can be one of; an RFC 5322 comment, an RFC 5322 quoted string or text.
    /// </summary>
    public abstract class cHeaderFieldCommentTextQuotedStringValue : cHeaderFieldCommentTextQuotedStringPhraseValue
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        protected cHeaderFieldCommentTextQuotedStringValue() { }
    }

    /// <summary>
    /// Represents part of an RFC 5322 header field value. The part can be either an RFC 5322 comment or text.
    /// </summary>
    public abstract class cHeaderFieldCommentTextValue : cHeaderFieldCommentTextQuotedStringValue
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        protected cHeaderFieldCommentTextValue() { }
    }

    /// <summary>
    /// Represents text in an RFC 5322 header field value.
    /// </summary>
    public class cHeaderFieldTextValue : cHeaderFieldCommentTextValue
    {
        /// <summary>
        /// The text.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// Initialises a new instance with the specified text.
        /// </summary>
        /// <param name="pText"></param>
        public cHeaderFieldTextValue(string pText)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
        }

        /// <inheritdoc />
        public override bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed) => !cCharset.WSPVChar.ContainsAll(Text); // only checks for invalid characters

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cHeaderFieldTextValue)}({Text})";
    }

    /// <summary>
    /// Represents a comment in an RFC 5322 header field value.
    /// </summary>
    public class cHeaderFieldCommentValue : cHeaderFieldCommentTextValue
    {
        /// <summary>
        /// The parts of the comment.
        /// </summary>
        public readonly ReadOnlyCollection<cHeaderFieldCommentTextValue> Parts;

        /// <summary>
        /// Initialises a new instance with the specified parts.
        /// </summary>
        /// <param name="pParts"></param>
        public cHeaderFieldCommentValue(IEnumerable<cHeaderFieldCommentTextValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentTextValue>();

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lParts.Add(lPart);
            }

            Parts = lParts.AsReadOnly();
        }

        /// <summary>
        /// Initialises a new instance with a single text part using the specified text.
        /// </summary>
        /// <param name="pText"></param>
        public cHeaderFieldCommentValue(string pText)
        {
            var lParts = new List<cHeaderFieldCommentTextValue>();
            lParts.Add(new cHeaderFieldTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        /// <inheritdoc />
        public override bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            foreach (var lPart in Parts) if (!lPart.CanComposeHeaderFieldValue(pUTF8HeadersAllowed)) return false; // only checks for invalid characters as encoded words can be used in comments
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldCommentValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Represents the text of a quoted string in an RFC 5322 header field value.
    /// </summary>
    public class cHeaderFieldQuotedStringValue : cHeaderFieldCommentTextQuotedStringValue
    {
        /// <summary>
        /// Represents the empty quoted string.
        /// </summary>
        public static readonly cHeaderFieldQuotedStringValue Empty = new cHeaderFieldQuotedStringValue(string.Empty);

        /// <summary>
        /// The text of the quoted string.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// Initialises a new instance with the specified text.
        /// </summary>
        /// <param name="pText"></param>
        public cHeaderFieldQuotedStringValue(string pText)
        {
            Text = pText ?? throw new ArgumentNullException(nameof(pText));
        }

        /// <inheritdoc />
        public override bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            if (!cCharset.WSPVChar.ContainsAll(Text)) return false; // check for invalid characters
            if (pUTF8HeadersAllowed) return true;
            if (cTools.ContainsNonASCII(Text)) return false; // encoded-words can't be used in quoted strings
            return true;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cHeaderFieldQuotedStringValue)}({Text})";
    }

    /// <summary>
    /// Represents a phrase in an RFC 5322 header field value.
    /// </summary>
    public class cHeaderFieldPhraseValue : cHeaderFieldCommentTextQuotedStringPhraseValue
    {
        /// <summary>
        /// The parts of the phrase.
        /// </summary>
        public readonly ReadOnlyCollection<cHeaderFieldCommentTextQuotedStringValue> Parts;

        /// <summary>
        /// Initialises a new instance with the specified parts.
        /// </summary>
        /// <remarks>
        /// The parts must contain some content; either a quoted string or non-WSP text.
        /// </remarks>
        /// <param name="pParts"></param>
        public cHeaderFieldPhraseValue(IEnumerable<cHeaderFieldCommentTextQuotedStringValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentTextQuotedStringValue>();

            bool lHasContent = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lPart is cHeaderFieldTextValue lText && !cCharset.WSP.ContainsAll(lText.Text)) lHasContent = true;
                if (lPart is cHeaderFieldQuotedStringValue) lHasContent = true;

                lParts.Add(lPart);
            }

            if (!lHasContent) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.HasNoContent);

            Parts = lParts.AsReadOnly();
        }

        /// <summary>
        /// Initialises a new instance with a single text part using the specified text.
        /// </summary>
        /// <remarks>
        /// The text must have some non-WSP characters.
        /// </remarks>
        /// <param name="pText"></param>
        public cHeaderFieldPhraseValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (cCharset.WSP.ContainsAll(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
            var lParts = new List<cHeaderFieldCommentTextQuotedStringValue>();
            lParts.Add(new cHeaderFieldTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        /// <inheritdoc />
        public override bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            // encoded words can be used in phrase
            foreach (var lPart in Parts) if (!lPart.CanComposeHeaderFieldValue(pUTF8HeadersAllowed)) return false;
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldPhraseValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Represents a structured RFC 5322 header field value.
    /// </summary>
    public class cHeaderFieldStructuredValue : iCanComposeHeaderFieldValue
    {
        /// <summary>
        /// The parts of the value.
        /// </summary>
        public readonly ReadOnlyCollection<cHeaderFieldCommentTextQuotedStringPhraseValue> Parts;

        /// <summary>
        /// Initialises a new instance with the specified parts.
        /// </summary>
        /// <param name="pParts"></param>
        public cHeaderFieldStructuredValue(IEnumerable<cHeaderFieldCommentTextQuotedStringPhraseValue> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cHeaderFieldCommentTextQuotedStringPhraseValue>();

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lParts.Add(lPart);
            }

            Parts = lParts.AsReadOnly();
        }

        /// <summary>
        /// Initialises a new instance with a single text part using the specified text.
        /// </summary>
        /// <param name="pText"></param>
        public cHeaderFieldStructuredValue(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            var lParts = new List<cHeaderFieldCommentTextQuotedStringPhraseValue>();
            lParts.Add(new cHeaderFieldTextValue(pText));
            Parts = lParts.AsReadOnly();
        }

        /// <inheritdoc cref="iCanComposeHeaderFieldValue.CanComposeHeaderFieldValue(bool)"/>
        public bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            // encoded words are only allowed in phrase and comment
            foreach (var lPart in Parts)
            {
                if (lPart is cHeaderFieldTextValue lText && !pUTF8HeadersAllowed && cTools.ContainsNonASCII(lText.Text)) return false;
                if (!lPart.CanComposeHeaderFieldValue(pUTF8HeadersAllowed)) return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldStructuredValue));
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}