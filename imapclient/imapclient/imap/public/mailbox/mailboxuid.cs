using System;

namespace work.bacome.imapclient
{
    public class cMailboxUID : IEquatable<cMailboxUID>
    {
        public readonly cMailboxId MailboxId;
        public readonly uint UIDValidity;

        public cMailboxUID(cMailboxId pMailboxId, uint pUIDValidity)
        {
            MailboxId = pMailboxId ?? throw new ArgumentOutOfRangeException(nameof(pMailboxId));
            if (UIDValidity < 1) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            UIDValidity = pUIDValidity;
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

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxUID)}({MailboxId},{UIDValidity})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMailboxUID pA, cMailboxUID pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MailboxId == pB.MailboxId && pA.UIDValidity == pB.UIDValidity;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMailboxUID pA, cMailboxUID pB) => !(pA == pB);
    }
}