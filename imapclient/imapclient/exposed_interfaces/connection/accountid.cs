using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The type of account. See <see cref="cAccountId"/>.
    /// </summary>
    public enum eAccountType
    {
        /** <summary>The library has no idea about the account.</summary>"*/
        none,

        /** <summary>The account is an anonymous one.</summary>"*/
        anonymous,

        /** <summary>The account has a userid.</summary>"*/
        userid
    }

    /// <summary>
    /// Describes an IMAP account. See <see cref="cIMAPClient.ConnectedAccountId"/>.
    /// </summary>
    public class cAccountId
    {
        /// <summary>
        /// The host that contains the account.
        /// </summary>
        public readonly string Host;

        /// <summary> 
        /// The account type. If the connection was IMAP PREAUTHed then this will be <see cref="eAccountType.none"/>.
        /// </summary>
        public readonly eAccountType Type;

        /// <summary>
        /// The account's userid, if any. May be null.
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

        /// <summary>
        /// Determines whether this instance and the specified object have the same values.
        /// </summary>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public override bool Equals(object pObject) => this == pObject as cAccountId;

        /// <summary>
        /// Returns the hash code for this <see cref="cAccountId"/>.
        /// </summary>
        /// <returns></returns>
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

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString() => $"{nameof(cAccountId)}({Host},{Type},{UserId})";

        /// <summary>
        /// Determines whether two instances have the same values.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator ==(cAccountId pA, cAccountId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Host == pB.Host && pA.Type == pB.Type && pA.UserId == pB.UserId);
        }

        /// <summary>
        /// Determines whether two instances have different values.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator !=(cAccountId pA, cAccountId pB) => !(pA == pB);
    }
}