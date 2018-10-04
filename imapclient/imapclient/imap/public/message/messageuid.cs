using System;

namespace work.bacome.imapclient
{
    public class cMessageUID : IEquatable<cMessageUID>
    {
        public readonly cMailboxId MailboxId;
        public readonly cUID UID;
        public readonly bool UIDNotSticky;
        public readonly bool UTF8Enabled;

        public cMessageUID(cMailboxId pMailboxId, cUID pUID, bool pUIDNotSticky, bool pUTF8Enabled)
        {
            MailboxId = pMailboxId ?? throw new ArgumentOutOfRangeException(nameof(pMailboxId));
            UID = pUID ?? throw new ArgumentOutOfRangeException(nameof(pUID));
            UIDNotSticky = 
            UTF8Enabled = pUTF8Enabled;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMessageUID pObject) => this == pObject;

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
                lHash = lHash * 23 + UTF8Enabled.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMessageUID)}({MailboxId},{UID},{UTF8Enabled})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMessageUID pA, cMessageUID pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MailboxId == pB.MailboxId && pA.UID == pB.UID && pA.UTF8Enabled == pB.UTF8Enabled;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMessageUID pA, cMessageUID pB) => !(pA == pB);
    }
}