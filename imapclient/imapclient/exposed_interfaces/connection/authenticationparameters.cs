using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Parameters that can be used to authenticate during <see cref="cIMAPClient.Connect"/>.
    /// </summary>
    /// <seealso cref="cIMAPClient.AuthenticationParameters"/>
    public class cAuthenticationParameters
    {
        /// <summary>
        /// A set of authentication parameters that will never result in a successful connection.
        /// </summary>
        /// <remarks>
        /// Useful to retrieve the property values set during <see cref="cIMAPClient.Connect"/> without actually connecting.
        /// </remarks>
        public static readonly cAuthenticationParameters None = new cAuthenticationParameters();

        public static readonly object AnonymousCredentialsId = new object();

        public readonly object PreAuthenticatedCredentialId;
        public readonly object AuthenticatedCredentialId;

        /// <summary>
        /// The arguments to use with the IMAP LOGIN command during <see cref="cIMAPClient.Connect"/>. May be <see langword="null"/>.
        /// </summary>
        public readonly cLogin Login;

        /// <summary>
        /// The set of SASL objects that can used used during <see cref="cIMAPClient.Connect"/>. May be <see langword="null"/> or empty.
        /// </summary>
        public readonly ReadOnlyCollection<cSASL> SASLs;

        /// <summary>
        /// Indicates whether all <see cref="SASLs"/> should be tried even if the server doesn't advertise the associated authentication mechanism.
        /// </summary>
        /// <seealso cref="cCapabilities.AuthenticationMechanisms"/>
        public readonly bool TryAllSASLs;

        private cAuthenticationParameters()
        {
            PreAuthenticatedCredentialId = null;
            AuthenticatedCredentialId = null;
            Login = null;
            SASLs = null;
            TryAllSASLs = false;
        }

        public cAuthenticationParameters(object pPreAuthenticatedCredentialId)
        {
            PreAuthenticatedCredentialId = pPreAuthenticatedCredentialId ?? throw new ArgumentNullException(nameof(pPreAuthenticatedCredentialId));
            AuthenticatedCredentialId = null;
            Login = null;
            SASLs = null;
            TryAllSASLs = false;
        }

        /*
        /// <summary>
        /// Initialises a new instance with the specified credential identifiers, <see cref="cLogin"/> and <see cref="cSASL"/> objects.
        /// </summary>
        /// <param name="pUserId">The server account name that the <paramref name="pLogin"/> and <paramref name="pSASLs"/> give access to.</param>
        /// <param name="pLogin"></param>
        /// <param name="pSASLs"></param>
        /// <param name="pTryAllSASLs">Indicates whether all <paramref name="pSASLs"/> should be tried even if the server doesn't advertise the associated authentication mechanism.</param> */

        public cAuthenticationParameters(object pAuthenticatedCredentialId, cLogin pLogin, IEnumerable<cSASL> pSASLs, bool pTryAllSASLs = false, object pPreAuthenticatedCredentialId = null)
        {
            AuthenticatedCredentialId = pAuthenticatedCredentialId ?? throw new ArgumentNullException(nameof(pAuthenticatedCredentialId));

            Login = pLogin;

            var lSASLs = new List<cSASL>(from s in pSASLs where s != null select s);

            if (lSASLs.Count == 0) SASLs = null;
            else SASLs = lSASLs.AsReadOnly();

            TryAllSASLs = pTryAllSASLs;
            PreAuthenticatedCredentialId = pPreAuthenticatedCredentialId;
        }
    
        /// <summary>
        /// Returns a new set of authentication parameters for connecting anonymously.
        /// </summary>
        /// <param name="pTrace">The trace information to be sent to the server.</param>
        /// <param name="pTLSRequirement">The TLS requirement for the parameters to be used.</param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Indicates whether the SASL ANONYMOUS mechanism should be tried even if it isn't advertised.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method will throw if <paramref name="pTrace"/> can be used with neither <see cref="cLogin.Password"/> nor <see cref="cSASLAnonymous"/>.
        /// </remarks>
        public static cAuthenticationParameters Anonymous(string pTrace, eTLSRequirement pTLSRequirement = eTLSRequirement.indifferent, bool pTryAuthenticateEvenIfAnonymousIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));

            cLogin.TryConstruct("anonymous", pTrace, pTLSRequirement, out var lLogin);
            cSASLAnonymous.TryConstruct(pTrace, pTLSRequirement, out var lSASL);
            if (lLogin == null && lSASL == null) throw new ArgumentOutOfRangeException(nameof(pTrace));
            return new cAuthenticationParameters(AnonymousCredentialsId, lLogin, new cSASL[] { lSASL }, pTryAuthenticateEvenIfAnonymousIsntAdvertised);
        }

        /// <summary>
        /// Returns a new set of authentication parameters for connecting using plain authentication.
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for the parameters to be used.</param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Indicates whether the SASL PLAIN mechanism should be tried even if it isn't advertised.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method will throw if the userid and password can be used with neither <see cref="cLogin"/> nor <see cref="cSASLPlain"/>.
        /// </remarks>
        public static cAuthenticationParameters Plain(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfPlainIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pPassword)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            cLogin.TryConstruct(pUserId, pPassword, pTLSRequirement, out var lLogin);
            cSASLPlain.TryConstruct(pUserId, pPassword, pTLSRequirement, out var lSASL);
            if (lLogin == null && lSASL == null) throw new ArgumentOutOfRangeException(); // argument_s_outofrange

            return new cAuthenticationParameters(pUserId, lLogin, new cSASL[] { lSASL }, pTryAuthenticateEvenIfPlainIsntAdvertised);
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
            var lContext = pParentContext.NewMethod(nameof(cAuthenticationParameters), nameof(_Tests));

            bool lFailed;
            cAuthenticationParameters lAP;

            lAP = Anonymous("fred");
            if (lAP.Login == null || lAP.SASLs.Count != 1) throw new cTestsException("unexpected anon result");
            if (cTools.ASCIIBytesToString(lAP.SASLs[0].GetAuthentication().GetResponse(new byte[0])) != "fred") throw new cTestsException("unexpected anon result");

            lAP = Anonymous("fr€d");
            if (lAP.Login != null || lAP.SASLs.Count != 1) throw new cTestsException("unexpected anon result");
            if (cTools.UTF8BytesToString(lAP.SASLs[0].GetAuthentication().GetResponse(new byte[0])) != "fr€d") throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lAP = Anonymous(""); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");

            lAP = Anonymous("fred@fred.com");
            if (lAP.Login == null || lAP.SASLs.Count != 1) throw new cTestsException("unexpected anon result");
            if (cTools.ASCIIBytesToString(lAP.SASLs[0].GetAuthentication().GetResponse(new byte[0])) != "fred@fred.com") throw new cTestsException("unexpected anon result");

            lAP = Anonymous("fred@fred@fred.com");
            if (lAP.Login == null || lAP.SASLs != null) throw new cTestsException("unexpected anon result");

            lAP = Anonymous("fred€fred.com");
            if (lAP.Login != null || lAP.SASLs.Count != 1) throw new cTestsException("unexpected anon result");
            if (cTools.UTF8BytesToString(lAP.SASLs[0].GetAuthentication().GetResponse(new byte[0])) != "\"fred€\"@fred.com") throw new cTestsException("unexpected anon result");

            lAP = Anonymous("fred@fr€d.com");
            if (lAP.Login != null || lAP.SASLs.Count != 1) throw new cTestsException("unexpected anon result");
            if (cTools.ASCIIBytesToString(lAP.SASLs[0].GetAuthentication().GetResponse(new byte[0])) != "fred@xn--frd-l50a.com") throw new cTestsException("unexpected anon result");

            lAP = Anonymous("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890");
            if (lAP.Login == null || lAP.SASLs != null) throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lAP = Anonymous("fred€@fred@fred.com"); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");
        }
    }
}
