using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;

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
        /// The arguments to use with the IMAP LOGIN command for these credentials. May be <see langword="null"/>.
        /// </summary>
        public readonly cLogin Login;

        /// <summary>
        /// Gets the set of SASL objects that can used used during <see cref="cIMAPClient.Connect"/>. May be <see langword="null"/> or empty.
        /// </summary>
        public readonly ReadOnlyCollection<cSASL> SASLs;

        /// <summary>
        /// Indicates whether all <see cref="SASLs"/> should be tried even if the server doesn't advertise the associated authentication mechanism.
        /// </summary>
        /// <seealso cref="cCapabilities.AuthenticationMechanisms"/>
        public readonly bool TryAllSASLs;

        private cCredentials()
        {
            Type = eAccountType.unknown;
            UserId = null;
            Login = null;
            SASLs = null;
            TryAllSASLs = false;
        }

        /// <summary>
        /// Initialises a new instance with the anonymous account type and the specified <see cref="cLogin"/> and <see cref="cSASL"/> objects. 
        /// </summary>
        /// <param name="pLogin"></param>
        /// <param name="pSASLs"></param>
        /// <param name="pTryAllSASLs"></param>
        public cCredentials(cLogin pLogin, IEnumerable<cSASL> pSASLs, bool pTryAllSASLs = false)
        {
            Type = eAccountType.anonymous;
            UserId = null;
            Login = pLogin;

            var lSASLs = new List<cSASL>(from s in pSASLs where s != null select s);

            if (lSASLs.Count == 0) SASLs = null;
            else SASLs = lSASLs.AsReadOnly();

            TryAllSASLs = pTryAllSASLs;
        }

        /// <summary>
        /// Initialises a new instance with the specified userid, <see cref="cLogin"/> and <see cref="cSASL"/> objects.
        /// </summary>
        /// <param name="pUserId">The server account name that the <paramref name="pLogin"/> and <paramref name="pSASLs"/> give access to.</param>
        /// <param name="pLogin"></param>
        /// <param name="pSASLs"></param>
        /// <param name="pTryAllSASLs">Indicates whether all <paramref name="pSASLs"/> should be tried even if the server doesn't advertise the associated authentication mechanism.</param>
        public cCredentials(string pUserId, cLogin pLogin, IEnumerable<cSASL> pSASLs, bool pTryAllSASLs = false)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));

            Type = eAccountType.userid;
            UserId = pUserId;
            Login = pLogin;

            var lSASLs = new List<cSASL>(from s in pSASLs where s != null select s);

            if (lSASLs.Count == 0) SASLs = null;
            else SASLs = lSASLs.AsReadOnly();

            TryAllSASLs = pTryAllSASLs;
        }

        /// <summary>
        /// An empty set of credentials. 
        /// </summary>
        /// <remarks>
        /// Useful to retrieve the property values set during <see cref="cIMAPClient.Connect"/> without actually connecting.
        /// Also useful when there is external authentication.
        /// </remarks>
        public static readonly cCredentials None = new cCredentials();
    
        /// <summary>
        /// Returns a new set of anonymous credentials.
        /// </summary>
        /// <param name="pTrace">The trace information to be sent to the server.</param>
        /// <param name="pTLSRequirement">The TLS requirement for the credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Indicates whether the SASL ANONYMOUS mechanism should be tried even if it isn't advertised.</param>
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
            return new cCredentials(lLogin, new cSASL[] { lSASL },  pTryAuthenticateEvenIfAnonymousIsntAdvertised);
        }

        /// <summary>
        /// Returns a new set of plain credentials.
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for the credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Indicates whether the SASL PLAIN mechanism should be tried even if it isn't advertised.</param>
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
            cSASLPlain.TryConstruct(pUserId, pPassword, pTLSRequirement, out var lSASL);
            if (lLogin == null && lSASL == null) throw new ArgumentOutOfRangeException(); // argument_s_outofrange

            return new cCredentials(pUserId, lLogin, new cSASL[] { lSASL }, pTryAuthenticateEvenIfPlainIsntAdvertised);
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
            if (lCredentials.Login == null || lCredentials.SASLs != null) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred€fred.com");
            if (lCredentials.Login != null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred€@fred.com");
            if (lCredentials.Login != null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890");
            if (lCredentials.Login == null || lCredentials.SASLs != null) throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lCredentials = Anonymous("fred€@fred@fred.com"); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");
        }
    }
}
