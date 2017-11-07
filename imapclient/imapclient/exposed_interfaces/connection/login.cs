using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Parameters to use with the IMAP LOGIN command.
    /// </summary>
    public class cLogin
    {
        public readonly string UserId;
        public readonly string Password;

        /// <summary>
        /// The TLS requirement for the IMAP LOGIN command to be used with this userid and password.
        /// </summary>
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

            if (!cCommandPartFactory.TryAsASCIILiteral(pUserId, true, out _)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (!cCommandPartFactory.TryAsASCIILiteral(pPassword, true, out _)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            UserId = pUserId;
            Password = pPassword;
            TLSRequirement = pTLSRequirement;
        }

        /// <summary>
        /// <para>IMAP LOGIN only allows ASCII userids and passwords, so this may fail.</para>
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement"></param>
        /// <param name="rLogin"></param>
        /// <returns></returns>
        public static bool TryConstruct(string pUserId, string pPassword, eTLSRequirement pTLSRequirement, out cLogin rLogin)
        {
            if (string.IsNullOrEmpty(pUserId) || string.IsNullOrEmpty(pPassword)) { rLogin = null; return false; }

            if (!cCommandPartFactory.TryAsASCIILiteral(pUserId, true, out _)) { rLogin = null; return false; }
            if (!cCommandPartFactory.TryAsASCIILiteral(pPassword, true, out _)) { rLogin = null; return false; }

            rLogin = new cLogin(pUserId, pPassword, pTLSRequirement, true);
            return true;
        }
    }
}
