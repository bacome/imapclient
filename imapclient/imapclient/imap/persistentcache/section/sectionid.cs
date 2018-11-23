using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cSectionId : IEquatable<cSectionId>
    {
        public readonly cMessageUID MessageUID;
        public readonly cSection Section;
        public readonly bool Decoded;

        internal cSectionId(cMessageUID pMessageUID, cSection pSection, bool pDecoded)
        {
            MessageUID = pMessageUID ?? throw new ArgumentNullException(nameof(pMessageUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoded = pDecoded;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionId pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionId;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MessageUID.GetHashCode();
                lHash = lHash * 23 + Section.GetHashCode();
                lHash = lHash * 23 + Decoded.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionId)}({MessageUID},{Section},{Decoded})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionId pA, cSectionId pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.MessageUID == pB.MessageUID && pA.Section == pB.Section && pA.Decoded == pB.Decoded;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionId pA, cSectionId pB) => !(pA == pB);
    }
}