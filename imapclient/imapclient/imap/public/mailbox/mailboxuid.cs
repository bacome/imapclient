using System;

namespace work.bacome.imapclient
{
    public class cMailboxUID : IEquatable<cMailboxUID>
    {
        public readonly cMailboxId MailboxId;
        public readonly uint UIDValidity;
        public readonly bool UIDNotSticky;

        public cMailboxUID(cMailboxId pMailboxId, uint pUIDValidity, bool pUIDNotSticky)
        {
            MailboxId = pMailboxId ?? throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            UIDValidity = pUIDValidity;
            UIDNotSticky = pUIDNotSticky;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMailboxUID pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cMailboxUID;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MailboxId.GetHashCode();
                lHash = lHash * 23 + UIDValidity.GetHashCode();
                lHash = lHash * 23 + UIDNotSticky.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxUID)}({MailboxId},{UIDValidity},{UIDNotSticky})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMailboxUID pA, cMailboxUID pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.MailboxId == pB.MailboxId && pA.UIDValidity == pB.UIDValidity && pA.UIDNotSticky == pB.UIDNotSticky;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMailboxUID pA, cMailboxUID pB) => !(pA == pB);
    }
}