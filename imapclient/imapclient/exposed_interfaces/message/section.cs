using System;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The text-part of an IMAP body-part (see <see cref="cSection"/>).
    /// </summary>
    public enum eSectionTextPart
    {
        /** <sumary>The entire body-part.</sumary> */
        all,

        /** <sumary>The entire header text-part of the body-part.</sumary> */
        header,

        /** <sumary>Specified headers from the header text-part of the body-part.</sumary> */
        headerfields,

        /** <sumary>All headers other than the specified headers from the header text-part of the body-part.</sumary> */
        headerfieldsnot,

        /** <sumary>The entire text text-part of the body-part.</sumary> */
        text,

        /** <sumary>The mime headers of the body-part.</sumary> */
        mime
    }

    /// <summary>
    /// Represents a section of an IMAP message.
    /// </summary>
    public class cSection
    {
        /// <summary>
        /// A section that represents the entire message.
        /// </summary>
        public static readonly cSection All = new cSection(null);

        /// <summary>
        /// A section that represents the entire header-part of a message.
        /// </summary>
        public static readonly cSection Header = new cSection(null, eSectionTextPart.header);

        /// <summary>
        /// A section that represents the entire text-part of a message.
        /// </summary>
        public static readonly cSection Text = new cSection(null, eSectionTextPart.text);

        /// <summary>
        /// The body-part of the section. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// A dot separated list of integers denoting a message body-part or <see langword="null"/> which indicates the 'top-level' body-part.
        /// </remarks>
        public readonly string Part;

        /// <summary>
        /// The text-part of the section.
        /// </summary>
        /// <remarks>
        /// Will not be <see cref="eSectionTextPart.mime"/> if <see cref="Part"/> is <see langword="null"/>.
        /// </remarks>
        public readonly eSectionTextPart TextPart;

        /// <summary>
        /// The header field subset of the section. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will not be <see langword="null"/> or empty if <see cref="TextPart"/> is <see cref="eSectionTextPart.headerfields"/> or <see cref="eSectionTextPart.headerfieldsnot"/>.
        /// Will be null otherwise.
        /// </remarks>
        public readonly cHeaderFieldNames Names;

        /// <summary>
        /// Initialises a new instance so it represents an entire body-part.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or <see langword="null"/> for the 'top-level' body-part.</param>
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
        /// Initialises a new instance so it represents a whole text-part.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or <see langword="null"/> for the 'top-level' body-part.</param>
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
        /// Initialises a new instance so it represents a subset of <see cref="eSectionTextPart.header"/>.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or <see langword="null"/> for the 'top-level' body-part.</param>
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