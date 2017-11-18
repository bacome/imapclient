using System;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The type of an IMAP account.
    /// </summary>
    /// <seealso cref="cAccountId"/>
    /// <seealso cref="cCredentials"/>
    public enum eAccountType
    {
        /** <summary>The library has no idea about the type of the account.</summary>"*/
        unknown,
        /** <summary>The account is an anonymous one.</summary>"*/
        anonymous,
        /** <summary>The account has a userid.</summary>"*/
        userid
    }

    /// <summary>
    /// Represents an IMAP account.
    /// </summary>
    /// <seealso cref="cIMAPClient.ConnectedAccountId"/>
    public class cAccountId
    {
        /// <summary>
        /// The host that contains the account.
        /// </summary>
        public readonly string Host;

        /// <summary> 
        /// The account type.
        /// </summary>
        /// <remarks>
        /// If the connection was IMAP PREAUTHed then this will be <see cref="eAccountType.unknown"/>.
        /// </remarks>
        public readonly eAccountType Type;

        /// <summary>
        /// The account's userid. May be <see langword="null"/>.
        /// </summary>
        public readonly string UserId;

        internal cAccountId(string pHost, eAccountType pType)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (pType == eAccountType.userid) throw new ArgumentOutOfRangeException(nameof(pType));

            Host = pHost;
            Type = pType;
            UserId = null;
        }

        internal cAccountId(string pHost, string pUserId)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));

            Host = pHost;
            Type = eAccountType.userid;
            UserId = pUserId;
        }

        internal cAccountId(string pHost, eAccountType pType, string pUserId)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));

            if (pType == eAccountType.userid)
            {
                if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            }
            else
            {
                if (pUserId != null) throw new ArgumentOutOfRangeException(nameof(pUserId));
            }

            Host = pHost;
            Type = pType;
            UserId = pUserId;
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cAccountId;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Host.GetHashCode();
                if (UserId != null) lHash = lHash * 23 + UserId.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cAccountId)}({Host},{Type},{UserId})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cAccountId pA, cAccountId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Host == pB.Host && pA.Type == pB.Type && pA.UserId == pB.UserId);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cAccountId pA, cAccountId pB) => !(pA == pB);
    }
}