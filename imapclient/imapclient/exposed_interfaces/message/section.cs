using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a text part of an IMAP message section (see <see cref="cSection"/>).
    /// </summary>
    public enum eSectionTextPart
    {
        /** <sumary>The entire section.</sumary> */
        all,

        /** <sumary>The entire header part of the section.</sumary> */
        header,

        /** <sumary>Specified headers from the header part of the section.</sumary> */
        headerfields,

        /** <sumary>All headers other than the specified headers from the header part of the section.</sumary> */
        headerfieldsnot,

        /** <sumary>The entire text part of the section.</sumary> */
        text,

        /** <sumary>The mime headers of the section.</sumary> */
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
        /// A section that represents the entire header fields part of a message.
        /// </summary>
        public static readonly cSection Header = new cSection(null, eSectionTextPart.header);

        /// <summary>
        /// A section that represents the entire text part of a message.
        /// </summary>
        public static readonly cSection Text = new cSection(null, eSectionTextPart.text);

        /// <summary>
        /// The IMAP section-part (a dot separated set of integers) that this instance represents. May be null if the instance represents the top level part.
        /// </summary>
        public readonly string Part;

        /// <summary>
        /// The text part that this instance represents.
        /// </summary>
        public readonly eSectionTextPart TextPart;

        /// <summary>
        /// The header fields included (<see cref="TextPart"/> = <see cref="eSectionTextPart.headerfields"/>) or excluded (<see cref="TextPart"/> = <see cref="eSectionTextPart.headerfieldsnot"/>) by this instance.
        /// </summary>
        public readonly cHeaderFieldNames Names;

        /// <summary>
        /// Initialises a new instance so it represents an entire part.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or null for the top level part.</param>
        public cSection(string pPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            TextPart = eSectionTextPart.all;
            Names = null;
        }

        /// <summary>
        /// Initialises a new instance so it represents a whole text part.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or null for the top level part.</param>
        /// <param name="pTextPart">Must be <see cref="eSectionTextPart.all"/>, <see cref="eSectionTextPart.header"/>, <see cref="eSectionTextPart.text"/> or if <paramref name="pPart"/> is not null, <see cref="eSectionTextPart.mime"/>.</param>
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
        /// Initialises a new instance so it represents a sub-part of the <see cref="eSectionTextPart.header"/>.
        /// </summary>
        /// <param name="pPart">Must be a valid IMAP section-part (a dot separated set of integers) or null for the message headers.</param>
        /// <param name="pNames"></param>
        /// <param name="pNot">True to represent all headers except <paramref name="pNames"/>, false to represent only the headers in <paramref name="pNames"/>.</param>
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

        /// <summary>
        /// Determines whether this instance and the specified object are the same.
        /// </summary>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public override bool Equals(object pObject) => this == pObject as cSection;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
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

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString() => $"{nameof(cSection)}({Part},{TextPart},{Names})";

        /// <summary>
        /// Determines whether two instances are the same.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator ==(cSection pA, cSection pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Part == pB.Part && pA.TextPart == pB.TextPart && pA.Names == pB.Names);
        }

        /// <summary>
        /// Determines whether two instances are different.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator !=(cSection pA, cSection pB) => !(pA == pB);
    }
}