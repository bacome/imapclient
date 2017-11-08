using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>Describes a text part of an IMAP message section (see <see cref="cSection"/>).</para>
    /// </summary>
    public enum eSectionTextPart
    {
        /** <sumary>The entire part.</sumary> */
        all,

        /** <sumary>The entire header part.</sumary> */
        header,

        /** <sumary>Specified headers from the header part.</sumary> */
        headerfields,

        /** <sumary>All headers other than the specified headers from the header part.</sumary> */
        headerfieldsnot,

        /** <sumary>The entire text part.</sumary> */
        text,

        /** <sumary>The mime headers of the part.</sumary> */
        mime
    }

    /// <summary>
    /// <para>Describes a section of an IMAP message.</para>
    /// </summary>
    public class cSection
    {
        /// <summary>
        /// Describes the section of a message that includes the entire message.
        /// </summary>
        public static readonly cSection All = new cSection(null);

        /// <summary>
        /// Describes the entire header fields section of a message.
        /// </summary>
        public static readonly cSection Header = new cSection(null, eSectionTextPart.header);

        /// <summary>
        /// Describes the entire text section of a message.
        /// </summary>
        public static readonly cSection Text = new cSection(null, eSectionTextPart.text);

        /// <summary>
        /// <para>The part of the message that this section describes.</para>
        /// <para>A dot separated set of integers e.g. 1, 2, 1.3, 1.1.4.5</para>
        /// <para>May be null if the section refers to the whole message.</para>
        /// </summary>
        public readonly string Part;

        /// <summary>
        /// The text part of the <see cref="Part"/> that this section describes.
        /// </summary>
        public readonly eSectionTextPart TextPart;

        /// <summary>
        /// The header fields included (<see cref="eSectionTextPart.headerfields"/>) or excluded (<see cref="eSectionTextPart.headerfieldsnot"/>) from this section.
        /// </summary>
        public readonly cHeaderFieldNames Names;

        public cSection(string pPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            TextPart = eSectionTextPart.all;
            Names = null;
        }

        public cSection(string pPart, eSectionTextPart pTextPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            if (pTextPart == eSectionTextPart.headerfields || pTextPart == eSectionTextPart.headerfieldsnot) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            if (pPart == null && pTextPart == eSectionTextPart.mime) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            TextPart = pTextPart;
            Names = null;
        }

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

        public override bool Equals(object pObject) => this == pObject as cSection;

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

        public override string ToString() => $"{nameof(cSection)}({Part},{TextPart},{Names})";

        public static bool operator ==(cSection pA, cSection pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Part == pB.Part && pA.TextPart == pB.TextPart && pA.Names == pB.Names);
        }

        public static bool operator !=(cSection pA, cSection pB) => !(pA == pB);
    }
}