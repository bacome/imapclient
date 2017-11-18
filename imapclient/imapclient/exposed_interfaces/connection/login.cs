using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters to use with the IMAP LOGIN command.
    /// </summary>
    /// <remarks>
    /// IMAP userids and passwords are limited to ASCII and may not include the null character.
    /// </remarks>
    /// <seealso cref="cCredentials.Login"/>
    public class cLogin
    {
        /**<summary>The userid to use.</summary>*/
        public readonly string UserId;
        /**<summary>The password to use.</summary>*/
        public readonly string Password;
        /**<summary>The TLS requirement for the IMAP LOGIN command to be used with this userid and password.</summary>*/
        public readonly eTLSRequirement TLSRequirement;

        private cLogin(string pUserId, string pPassword, eTLSRequirement pTLSRequirement, bool pValidated)
        {
            UserId = pUserId;
            Password = pPassword;
            TLSRequirement = pTLSRequirement;
        }

        /// <summary>
        /// Initialises a new instance. Will throw if the parameters provided are not valid.
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for the IMAP LOGIN command to be used with the specified userid and password.</param>
        /// <inheritdoc cref="cLogin" select="remarks"/>
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

        internal static bool TryConstruct(string pUserId, string pPassword, eTLSRequirement pTLSRequirement, out cLogin rLogin)
        {
            if (string.IsNullOrEmpty(pUserId) || string.IsNullOrEmpty(pPassword)) { rLogin = null; return false; }

            if (!cCommandPartFactory.TryAsASCIILiteral(pUserId, true, out _)) { rLogin = null; return false; }
            if (!cCommandPartFactory.TryAsASCIILiteral(pPassword, true, out _)) { rLogin = null; return false; }

            rLogin = new cLogin(pUserId, pPassword, pTLSRequirement, true);
            return true;
        }
    }
}
