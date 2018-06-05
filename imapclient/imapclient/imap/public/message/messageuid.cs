using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMessageUID : IEquatable<cMessageUID>
    {
        public readonly cMailboxId MailboxId;
        public readonly cUID UID;

        internal cMessageUID(cMailboxId pMailboxId, cUID pUID)
        {
            MailboxId = pMailboxId ?? throw new ArgumentOutOfRangeException(nameof(pMailboxId));
            UID = pUID ?? throw new ArgumentOutOfRangeException(nameof(pUID));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMessageUID pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        internal bool Equals(cSectionHandle pObject)
        {
            if (pObject == null) return false;
            return MailboxId == pObject.MailboxId && UID == pObject.MessageHandle.UID;
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cMessageUID;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MailboxId.GetHashCode();
                lHash = lHash * 23 + UID.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMessageUID)}({MailboxId},{UID})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMessageUID pA, cMessageUID pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MailboxId == pB.MailboxId && pA.UID == pB.UID;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMessageUID pA, cMessageUID pB) => !(pA == pB);
    }
}