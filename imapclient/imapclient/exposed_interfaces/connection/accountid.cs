using System;

namespace work.bacome.imapclient
{
    public enum eAccountType { none, anonymous, userid }

    public class cAccountId
    {
        public readonly string Host;
        public readonly eAccountType Type;
        public readonly string UserId;

        public cAccountId(string pHost, eAccountType pType)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (pType == eAccountType.userid) throw new ArgumentOutOfRangeException(nameof(pType));

            Host = pHost;
            Type = pType;
            UserId = null;
        }

        public cAccountId(string pHost, string pUserId)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));

            Host = pHost;
            Type = eAccountType.userid;
            UserId = pUserId;
        }

        public cAccountId(string pHost, eAccountType pType, string pUserId)
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

        public override bool Equals(object pObject) => this == pObject as cAccountId;

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

        public override string ToString() => $"{nameof(cAccountId)}({Host},{Type},{UserId})";

        public static bool operator ==(cAccountId pA, cAccountId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Host == pB.Host && pA.Type == pB.Type && pA.UserId == pB.UserId);
        }

        public static bool operator !=(cAccountId pA, cAccountId pB) => !(pA == pB);
    }
}