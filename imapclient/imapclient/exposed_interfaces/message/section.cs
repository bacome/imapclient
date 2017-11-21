using System;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a text part of a message body-part.
    /// </summary>
    /// <seealso cref="cSection"/>
    public enum eSectionTextPart
    {
        /** <sumary>All the text.</sumary> */
        all,

        /** <sumary>The message-headers text.</sumary> */
        header,

        /** <sumary>Specified message-headers text.</sumary> */
        headerfields,

        /** <sumary>All message-headers text except that for specified message-headers.</sumary> */
        headerfieldsnot,

        /** <sumary>The message-text text.</sumary> */
        text,

        /** <sumary>The mime headers text.</sumary> */
        mime
    }

    /// <summary>
    /// Represents an IMAP message section specification.
    /// </summary>
    /// <seealso cref="cBodyPart.Section"/>
    /// <seealso cref="cMessage.Fetch(cSection)"/>
    /// <seealso cref="cMessage.Fetch(cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMailbox.UIDFetch(cUID, cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    public class cSection
    {
        /// <summary>
        /// A section specification for the entire message.
        /// </summary>
        public static readonly cSection All = new cSection(null);

        /// <summary>
        /// A section specification for the message-headers of a message.
        /// </summary>
        public static readonly cSection Header = new cSection(null, eSectionTextPart.header);

        /// <summary>
        /// A section specification for the text of a message.
        /// </summary>
        public static readonly cSection Text = new cSection(null, eSectionTextPart.text);

        /// <summary>
        /// The body-part of the section specification. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// A dot separated list of integers denoting the message body-part or <see langword="null"/> which indicates the whole message.
        /// </remarks>
        public readonly string Part;

        /// <summary>
        /// The text part of the section specification.
        /// </summary>
        /// <remarks>
        /// Will not be <see cref="eSectionTextPart.mime"/> if <see cref="Part"/> is <see langword="null"/>.
        /// </remarks>
        public readonly eSectionTextPart TextPart;

        /// <summary>
        /// The header field subset of the section specification. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will not be <see langword="null"/> nor empty if <see cref="TextPart"/> is <see cref="eSectionTextPart.headerfields"/> or <see cref="eSectionTextPart.headerfieldsnot"/>.
        /// Will be <see langword="null"/> otherwise.
        /// </remarks>
        public readonly cHeaderFieldNames Names;

        /// <summary>
        /// Initialises a new instance so that it represents an entire body-part.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or <see langword="null"/> for the whole message.</param>
        /// <remarks>
        /// <see cref="TextPart"/> is set to <see cref="eSectionTextPart.all"/>, <see cref="Names"/> to <see langword="null"/>.
        /// </remarks>
        public cSection(string pPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            TextPart = eSectionTextPart.all;
            Names = null;
        }

        /// <summary>
        /// Initialises a new instance so that it represents a whole text part.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or <see langword="null"/> for the whole message.</param>
        /// <param name="pTextPart">May be <see cref="eSectionTextPart.all"/>, <see cref="eSectionTextPart.header"/>, <see cref="eSectionTextPart.text"/> or <see cref="eSectionTextPart.mime"/> (<see cref="eSectionTextPart.mime"/> only if <paramref name="pPart"/> is not <see langword="null"/>).</param>
        public cSection(string pPart, eSectionTextPart pTextPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            if (pTextPart == eSectionTextPart.headerfields || pTextPart == eSectionTextPart.headerfieldsnot) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            if (pPart == null && pTextPart == eSectionTextPart.mime) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            TextPart = pTextPart;
            Names = null;
        }

        /// <summary>
        /// Initialises a new instance so it represents a subset of a <see cref="eSectionTextPart.header"/>.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or <see langword="null"/> for the whole message.</param>
        /// <param name="pNames">Must not be <see langword="null"/> and must not be empty.</param>
        /// <param name="pNot"><see langword="true"/> to represent all header fields except <paramref name="pNames"/>, <see langword="false"/> to represent only the header fields in <paramref name="pNames"/>.</param>
        public cSection(string pPart, cHeaderFieldNames pNames, bool pNot = false)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;

            if (pNot) TextPart = eSectionTextPart.headerfieldsnot;
            else TextPart = eSectionTextPart.headerfields;

            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            if (pNames.Count == 0) throw new ArgumentOutOfRangeException(nameof(pNames));
            Names = pNames;
        }

        private bool ZValidPart(string pPart)
        {
            var lCursor = new cBytesCursor(pPart);

            while (true)
            {
                if (!lCursor.GetNZNumber(out _, out _)) return false;
                if (!lCursor.SkipByte(cASCII.DOT)) break;
            }

            return lCursor.Position.AtEnd;
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSection;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                if (Part != null) lHash = lHash * 23 + Part.GetHashCode();
                lHash = lHash * 23 + TextPart.GetHashCode();
                if (Names != null) lHash = lHash * 23 + Names.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSection)}({Part},{TextPart},{Names})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSection pA, cSection pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Part == pB.Part && pA.TextPart == pB.TextPart && pA.Names == pB.Names);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSection pA, cSection pB) => !(pA == pB);
    }
}