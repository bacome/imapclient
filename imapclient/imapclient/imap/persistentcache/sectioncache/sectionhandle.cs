using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    internal class cSectionHandle : IEquatable<cSectionHandle>
    {
        public readonly iMessageHandle MessageHandle;
        public readonly cSection Section;
        public readonly bool Decoded;

        private cSectionId mSectionId = null;

        public cSectionHandle(iMessageHandle pMessageHandle, cSection pSection, bool pDecoded)
        {
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoded = pDecoded;
        }

        public cSectionId SectionId
        {
            get
            {
                if (mSectionId != null) return mSectionId;
                if (MessageHandle.MessageUID == null) return null;
                mSectionId = new cSectionId(MessageHandle.MessageUID, Section, Decoded);
                return mSectionId;
            }
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionHandle pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionHandle;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MessageHandle.GetHashCode();
                lHash = lHash * 23 + Section.GetHashCode();
                lHash = lHash * 23 + Decoded.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionHandle)}({MessageHandle},{Section},{Decoded})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionHandle pA, cSectionHandle pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.MessageHandle == pB.MessageHandle && pA.Section == pB.Section && pA.Decoded == pB.Decoded;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionHandle pA, cSectionHandle pB) => !(pA == pB);
    }
}
