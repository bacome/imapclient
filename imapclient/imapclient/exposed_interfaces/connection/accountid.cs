using System;
using work.bacome.imapclient.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP account.
    /// </summary>
    /// <seealso cref="cIMAPClient.ConnectedAccountId"/>
    public class cAccountId : IEquatable<cAccountId>
    {
        /// <summary>
        /// The host that contains the account.
        /// </summary>
        public readonly string UnicodeHost;


        /* <summary>
        /// The identifier of the credentials used to access the account.
        /// </summary>
        /// <remarks>
        /// For plain 
        /// </remarks> */

        public readonly object CredentialId;

        internal cAccountId(string pHost, object pCredentialId)
        {
            if (pHost == null) throw new ArgumentNullException(nameof(pHost));
            string lASCIIHost = cIMAPClient.IDNMapping.GetAscii(pHost);
            UnicodeHost = cIMAPClient.IDNMapping.GetUnicode(lASCIIHost);

            CredentialId = pCredentialId ?? throw new ArgumentNullException(nameof(pCredentialId));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cAccountId pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cAccountId;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + UnicodeHost.GetHashCode();
                lHash = lHash * 23 + CredentialId.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cAccountId)}({UnicodeHost},{CredentialId})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cAccountId pA, cAccountId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.UnicodeHost == pB.UnicodeHost && pA.CredentialId.Equals(pB.CredentialId);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cAccountId pA, cAccountId pB) => !(pA == pB);
    }
}