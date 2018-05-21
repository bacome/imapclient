using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Values that can be used in the IMAP LOGIN command.
    /// </summary>
    /// <remarks>
    /// The IMAP LOGIN command limits userids and passwords to ASCII characters excluding the NUL character.
    /// </remarks>
    public class cIMAPLogin
    {
        public const string Anonymous = "anonymous";

        /**<summary>The userid to use.</summary>*/
        public readonly string UserId;
        /**<summary>The password to use.</summary>*/
        public readonly string Password;
        /**<summary>The TLS requirement for the userid and password to be used.</summary>*/
        public readonly eTLSRequirement TLSRequirement;

        private cIMAPLogin(string pUserId, string pPassword, eTLSRequirement pTLSRequirement, bool pValidated)
        {
            UserId = pUserId;
            Password = pPassword;
            TLSRequirement = pTLSRequirement;
        }

        /// <summary>
        /// Initialises a new instance with the specified userid, password and TLS requirement. Will throw if the userid and password specified can't be used with IMAP LOGIN.
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for the userid and password to be used.</param>
        /// <inheritdoc cref="cIMAPLogin" select="remarks"/>
        public cIMAPLogin(string pUserId, string pPassword, eTLSRequirement pTLSRequirement)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pPassword)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            if (!cCommandPartFactory.TryAsASCIILiteral(pUserId, true, out _)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (!cCommandPartFactory.TryAsASCIILiteral(pPassword, true, out _)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            UserId = pUserId;
            Password = pPassword;
            TLSRequirement = pTLSRequirement;
        }

        internal static bool TryConstruct(string pUserId, string pPassword, eTLSRequirement pTLSRequirement, out cIMAPLogin rLogin)
        {
            if (string.IsNullOrEmpty(pUserId) || string.IsNullOrEmpty(pPassword)) { rLogin = null; return false; }

            if (!cCommandPartFactory.TryAsASCIILiteral(pUserId, true, out _)) { rLogin = null; return false; }
            if (!cCommandPartFactory.TryAsASCIILiteral(pPassword, true, out _)) { rLogin = null; return false; }

            rLogin = new cIMAPLogin(pUserId, pPassword, pTLSRequirement, true);
            return true;
        }
    }
}
