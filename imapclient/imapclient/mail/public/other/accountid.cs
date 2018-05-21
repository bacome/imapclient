using System;
using work.bacome.imapclient;


namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents a server account.
    /// </summary>
    public class cAccountId : IEquatable<cAccountId>
    {
        /// <summary>
        /// The punycode decoded host that contains the account.
        /// </summary>
        ///
        public readonly string Host;

        /// <summary>
        /// The identifier of the credentials used to access the account.
        /// </summary>
        /// <remarks>
        /// If a UserId was used to access the account then this will be a <see cref="String>"/> containing the UserId. 
        /// (For an IMAP client the UserId could be <see cref="cIMAPLogin.Anonymous"/>).
        /// If SASL anonymous authentication was used to access the account then this will be <see cref="cSASLAnonymous.AnonymousCredentialId"/>.
        /// If access to an IMAP account was pre-authorised then this will be <see cref="cIMAPAuthentication.PreAuthenticatedCredentialId"/>.
        /// </remarks>
        public readonly object CredentialId;

        public cAccountId(string pHost, object pCredentialId)
        {
            if (pHost == null) throw new ArgumentNullException(nameof(pHost));
            Host = cTools.GetDisplayHost(pHost);
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
                lHash = lHash * 23 + Host.GetHashCode();
                lHash = lHash * 23 + CredentialId.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cAccountId)}({Host},{CredentialId})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cAccountId pA, cAccountId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Host == pB.Host && pA.CredentialId.Equals(pB.CredentialId);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cAccountId pA, cAccountId pB) => !(pA == pB);



        internal static void _Tests()
        {
            cAccountId lFredFred1 = new cAccountId("xn--frd-l50a.com", "fred");
            cAccountId lFredFred2 = new cAccountId("fr€d.com", "fred");
            cAccountId lAngusFred = new cAccountId("angus.com", "fred");
            cAccountId lFredAngus = new cAccountId("xn--frd-l50a.com", "angus");
            cAccountId lFredAnon1 = new cAccountId("xn--frd-l50a.com", cSASLAnonymous.AnonymousCredentialId);
            cAccountId lFredAnon2 = new cAccountId("fr€d.com", cSASLAnonymous.AnonymousCredentialId);
            object lobj = new object();
            cAccountId lFredPreAuth1 = new cAccountId("xn--frd-l50a.com", lobj);
            cAccountId lFredPreAuth2 = new cAccountId("fr€d.com", lobj);
            cAccountId lFredPreAuth3 = new cAccountId("fr€d.com", new object());

            if (lFredFred1 != lFredFred2 || lFredFred1 == lAngusFred || lFredFred1 == lFredAngus || lFredFred1 == lFredAnon1 || lFredFred1 == lFredPreAuth1) throw new cTestsException($"{nameof(cAccountId)}.1");
            if (lFredAnon1 != lFredAnon2 || lFredAnon1 == lFredPreAuth1) throw new cTestsException($"{nameof(cAccountId)}.2");
            if (lFredPreAuth1 != lFredPreAuth2 || lFredPreAuth1 == lFredPreAuth3) throw new cTestsException($"{nameof(cAccountId)}.3");
        }
    }
}