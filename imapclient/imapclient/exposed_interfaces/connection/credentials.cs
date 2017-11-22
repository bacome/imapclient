using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents credentials that can be used during <see cref="cIMAPClient.Connect"/>.
    /// </summary>
    /// <seealso cref="cIMAPClient.Credentials"/>
    public class cCredentials
    {
        /// <summary>
        /// The type of account that the credentials give access to.
        /// </summary>
        public readonly eAccountType Type;

        /// <summary>
        /// The userid of the account that the credentials give access to. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> when <see cref="Type"/> is <see cref="eAccountType.anonymous"/> or <see cref="eAccountType.unknown"/>, will not be <see langword="null"/> otherwise.
        /// </remarks>
        public readonly string UserId;

        /// <summary>
        /// The parameters to use with the IMAP LOGIN command for these credentials.
        /// </summary>
        public readonly cLogin Login;

        /// <summary>
        /// Indicates whether all <see cref="SASLs"/> should be tried even if the server doesn't advertise the associated authentication mechanism.
        /// </summary>
        /// <seealso cref="cCapabilities.AuthenticationMechanisms"/>
        public readonly bool TryAllSASLs;

        protected readonly List<cSASL> mSASLs = new List<cSASL>();

        private cCredentials(eAccountType pType, cLogin pLogin, bool pTryAllSASLs = false)
        {
            Type = pType;
            UserId = null;
            Login = pLogin;
            TryAllSASLs = pTryAllSASLs;
        }

        protected cCredentials(string pUserId, cLogin pLogin, bool pTryAllSASLs = false)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));

            Type = eAccountType.userid;
            UserId = pUserId;
            Login = pLogin;
            TryAllSASLs = pTryAllSASLs;
        }

        /// <summary>
        /// Gets the set of SASL objects that can used used during <see cref="cIMAPClient.Connect"/>.
        /// </summary>
        public ReadOnlyCollection<cSASL> SASLs => mSASLs.AsReadOnly();

        /// <summary>
        /// An empty set of credentials. 
        /// </summary>
        /// <remarks>
        /// Useful for testing.
        /// Useful for pre-authorised connections.
        /// </remarks>
        public static readonly cCredentials None = new cCredentials(eAccountType.unknown, null);
    
        /// <summary>
        /// Returns a new set of anonymous credentials.
        /// </summary>
        /// <param name="pTrace">The trace information to be sent to the server.</param>
        /// <param name="pTLSRequirement">The TLS requirement for the credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Indicates whether the SASL ANONYMOUS mechanism should be tried even if not advertised.</param>
        /// <returns></returns>
        /// <remarks>
        /// The credentials returned may fall back to IMAP LOGIN if SASL ANONYMOUS isn't available.
        /// This method will throw if <paramref name="pTrace"/> can be used in neither <see cref="cLogin.Password"/> nor <see cref="cSASLAnonymous"/>.
        /// </remarks>
        public static cCredentials Anonymous(string pTrace, eTLSRequirement pTLSRequirement = eTLSRequirement.indifferent, bool pTryAuthenticateEvenIfAnonymousIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));

            cLogin.TryConstruct("anonymous", pTrace, pTLSRequirement, out var lLogin);
            cSASLAnonymous.TryConstruct(pTrace, pTLSRequirement, out var lSASL);
            if (lLogin == null && lSASL == null) throw new ArgumentOutOfRangeException(nameof(pTrace));

            var lCredentials = new cCredentials(eAccountType.anonymous, lLogin, pTryAuthenticateEvenIfAnonymousIsntAdvertised);
            if (lSASL != null) lCredentials.mSASLs.Add(lSASL);
            return lCredentials;
        }

        /// <summary>
        /// Returns a new set of plain credentials.
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for the credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Indicates whether the SASL PLAIN mechanism should be tried even if not advertised.</param>
        /// <returns></returns>
        /// <remarks>
        /// The credentials returned may fall back to IMAP LOGIN if SASL PLAIN isn't available.
        /// This method will throw if the userid and password can be used in neither <see cref="cLogin"/> nor <see cref="cSASLPlain"/>.
        /// </remarks>
        public static cCredentials Plain(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfPlainIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pPassword)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            cLogin.TryConstruct(pUserId, pPassword, pTLSRequirement, out var lLogin);
            cSASLPlain.TryConstruct(pUserId, pPassword, pTLSRequirement, out var lPlain);
            if (lLogin == null && lPlain == null) throw new ArgumentOutOfRangeException(); // argument_s_outofrange

            var lCredentials = new cCredentials(pUserId, lLogin, pTryAuthenticateEvenIfPlainIsntAdvertised);
            if (lPlain != null) lCredentials.mSASLs.Add(lPlain);
            return lCredentials;
        }

        /* not tested yet
        public static cCredentials XOAuth2(string pUserId, string pAccessToken, bool pTryAuthenticateEvenIfXOAuth2IsntAdvertised = false)
        {
            var lXOAuth2 = new cSASLXOAuth2(pUserId, pAccessToken);
            var lCredentials = new cCredentials(pUserId, null, pTryAuthenticateEvenIfXOAuth2IsntAdvertised);
            lCredentials.mSASLs.Add(lXOAuth2);
            return lCredentials;
        } */

        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCredentials), nameof(_Tests));

            bool lFailed;
            cCredentials lCredentials;

            lCredentials = Anonymous("fred");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lCredentials = Anonymous(""); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred@fred.com");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred@fred@fred.com");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 0) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred€fred.com");
            if (lCredentials.Login != null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred€@fred.com");
            if (lCredentials.Login != null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 0) throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lCredentials = Anonymous("fred€@fred@fred.com"); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");
        }
    }
}
