using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cLogin
    {
        public readonly string UserId;
        public readonly string Password;

        private cLogin(string pUserId, string pPassword, bool pValidated)
        {
            UserId = pUserId;
            Password = pPassword;
        }

        public cLogin(string pUserId, string pPassword)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pPassword)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            cCommandPart.cFactory lFactory = new cCommandPart.cFactory();
            if (!lFactory.TryAsLiteral(pUserId, true, out _)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (!lFactory.TryAsLiteral(pPassword, true, out _)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            UserId = pUserId;
            Password = pPassword;
        }

        public static bool TryConstruct(string pUserId, string pPassword, out cLogin rLogin)
        {
            if (string.IsNullOrEmpty(pUserId) || string.IsNullOrEmpty(pPassword)) { rLogin = null; return false; }

            cCommandPart.cFactory lFactory = new cCommandPart.cFactory();
            if (!lFactory.TryAsLiteral(pUserId, true, out _)) { rLogin = null; return false; }
            if (!lFactory.TryAsLiteral(pPassword, true, out _)) { rLogin = null; return false; }

            rLogin = new cLogin(pUserId, pPassword, true);
            return true;
        }
    }
}
