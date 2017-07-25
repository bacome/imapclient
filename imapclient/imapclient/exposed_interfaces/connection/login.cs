using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cLogin
    {
        public readonly string UserId;
        public readonly string Password;
        public readonly eTLSRequirement TLSRequirement;

        private cLogin(string pUserId, string pPassword, eTLSRequirement pTLSRequirement, bool pValidated)
        {
            UserId = pUserId;
            Password = pPassword;
            TLSRequirement = pTLSRequirement;
        }

        public cLogin(string pUserId, string pPassword, eTLSRequirement pTLSRequirement)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pPassword)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            if (!cCommandPartFactory.Validation.TryAsLiteral(pUserId, true, out _)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (!cCommandPartFactory.Validation.TryAsLiteral(pPassword, true, out _)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            UserId = pUserId;
            Password = pPassword;
            TLSRequirement = pTLSRequirement;
        }

        public static bool TryConstruct(string pUserId, string pPassword, eTLSRequirement pTLSRequirement, out cLogin rLogin)
        {
            if (string.IsNullOrEmpty(pUserId) || string.IsNullOrEmpty(pPassword)) { rLogin = null; return false; }

            if (!cCommandPartFactory.Validation.TryAsLiteral(pUserId, true, out _)) { rLogin = null; return false; }
            if (!cCommandPartFactory.Validation.TryAsLiteral(pPassword, true, out _)) { rLogin = null; return false; }

            rLogin = new cLogin(pUserId, pPassword, pTLSRequirement, true);
            return true;
        }
    }
}
