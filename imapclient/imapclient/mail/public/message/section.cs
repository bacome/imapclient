using System;
using System.Runtime.Serialization;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents a text part of a message body-part.
    /// </summary>
    public enum eSectionTextPart
    {
        /** <summary>All the text.</summary> */
        all,

        /** <summary>All message headers.</summary> */
        header,

        /** <summary>Specified message headers.</summary> */
        headerfields,

        /** <summary>All message headers except for specified ones.</summary> */
        headerfieldsnot,

        /** <summary>The message text.</summary> */
        text,

        /** <summary>The mime headers.</summary> */
        mime
    }

    /// <summary>
    /// Represents a message section specification.
    /// </summary>
    [Serializable]
    public class cSection : IEquatable<cSection>, IComparable<cSection>
    {
        /// <summary>
        /// The section specification for an entire message.
        /// </summary>
        public static readonly cSection All = new cSection(null);

        /// <summary>
        /// The section specification for the message headers of a message.
        /// </summary>
        public static readonly cSection Header = new cSection(null, eSectionTextPart.header);

        /// <summary>
        /// The section specification for the message text of a message.
        /// </summary>
        public static readonly cSection Text = new cSection(null, eSectionTextPart.text);

        /// <summary>
        /// The body-part of the section specification. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// A dot separated list of integers specifying the body-part, or <see langword="null"/> for the whole message.
        /// </remarks>
        public readonly string Part;

        /// <summary>
        /// The text part of the section specification.
        /// </summary>
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
        /// <param name="pPart">A dot separated list of integers specifying the body-part, or <see langword="null"/> for the whole message.</param>
        /// <remarks>
        /// <see cref="TextPart"/> is set to <see cref="eSectionTextPart.all"/>, <see cref="Names"/> to <see langword="null"/>.
        /// </remarks>
        public cSection(string pPart)
        {
            if (pPart != null && !cMailValidation.IsSectionPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            TextPart = eSectionTextPart.all;
            Names = null;
        }

        /// <summary>
        /// Initialises a new instance so that it represents a whole text part.
        /// </summary>
        /// <param name="pPart">A dot separated list of integers specifying the body-part, or <see langword="null"/> for the whole message.</param>
        /// <param name="pTextPart">May be <see cref="eSectionTextPart.all"/>, <see cref="eSectionTextPart.header"/>, <see cref="eSectionTextPart.text"/> or <see cref="eSectionTextPart.mime"/> (<see cref="eSectionTextPart.mime"/> only if <paramref name="pPart"/> is not <see langword="null"/>).</param>
        public cSection(string pPart, eSectionTextPart pTextPart)
        {
            if (pPart != null && !cMailValidation.IsSectionPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            if (pTextPart == eSectionTextPart.headerfields || pTextPart == eSectionTextPart.headerfieldsnot) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            if (pPart == null && pTextPart == eSectionTextPart.mime) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            TextPart = pTextPart;
            Names = null;
        }

        /// <summary>
        /// Initialises a new instance so that it represents a subset of the <see cref="eSectionTextPart.header"/>.
        /// </summary>
        /// <param name="pPart">A dot separated list of integers specifying the body-part, or <see langword="null"/> for the whole message.</param>
        /// <param name="pNames">Must not be <see langword="null"/> nor empty.</param>
        /// <param name="pNot"><see langword="true"/> to represent all header fields except those specified, <see langword="false"/> to represent only the header fields specified.</param>
        public cSection(string pPart, cHeaderFieldNames pNames, bool pNot = false)
        {
            if (pPart != null && !cMailValidation.IsSectionPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;

            if (pNot) TextPart = eSectionTextPart.headerfieldsnot;
            else TextPart = eSectionTextPart.headerfields;

            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            if (pNames.Count == 0) throw new ArgumentOutOfRangeException(nameof(pNames));
            Names = pNames;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Part != null && !cMailValidation.IsSectionPart(Part)) throw new cDeserialiseException(nameof(cSection), nameof(Part), kDeserialiseExceptionMessage.IsInvalid);

            if (Names == null)
            {
                if (TextPart == eSectionTextPart.headerfields || TextPart == eSectionTextPart.headerfieldsnot) throw new cDeserialiseException(nameof(cSection), nameof(TextPart), kDeserialiseExceptionMessage.IsInconsistent);
                if (Part == null && TextPart == eSectionTextPart.mime) throw new cDeserialiseException(nameof(cSection), nameof(TextPart), "mime");
            }
            else
            {
                if (TextPart != eSectionTextPart.headerfields && TextPart != eSectionTextPart.headerfieldsnot) throw new cDeserialiseException(nameof(cSection), nameof(TextPart), kDeserialiseExceptionMessage.IsInconsistent);
                if (Names.Count == 0) throw new cDeserialiseException(nameof(cSection), nameof(Names), kDeserialiseExceptionMessage.IsEmpty);
            }
        }

        internal string GetSubPartPrefix()
        {
            if (Part == null) return string.Empty;
            return Part + ".";
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSection pObject) => this == pObject;

        public int CompareTo(cSection pOther)
        {
            if (pOther == null) return 1;

            int lCompareTo;

            if ((lCompareTo = string.Compare(Part, pOther.Part)) != 0) return lCompareTo;
            if ((lCompareTo = TextPart.CompareTo(pOther.TextPart)) != 0) return lCompareTo;

            return cHeaderFieldNames.Compare(Names, pOther.Names);
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

            if (pA.Part != pB.Part || pA.TextPart != pB.TextPart) return false;

            if (ReferenceEquals(pA.Names, pB.Names)) return true;
            if (pA.Names == null) return false;
            return pA.Names.Equals(pB.Names);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSection pA, cSection pB) => !(pA == pB);
    }
}