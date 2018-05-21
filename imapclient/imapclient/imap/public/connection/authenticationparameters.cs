using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Parameters that can be used to authenticate during <see cref="cIMAPClient.Connect"/>.
    /// </summary>
    public class cIMAPAuthentication
    {
        /// <summary>
        /// A set of authentication parameters that will never result in a successful connection.
        /// </summary>
        /// <remarks>
        /// Useful to retrieve the property values set during <see cref="cIMAPClient.Connect"/> without actually connecting.
        /// </remarks>
        public static readonly cIMAPAuthentication None = new cIMAPAuthentication();

        /// <summary>
        /// The credential id to use if the connection is pre-authenticated. May be <see langword="null"/> in which case a pre-authenticated connection will be disconnected.
        /// </summary>
        public readonly object PreAuthenticatedCredentialId;

        /// <summary>
        /// The set of SASL objects that can used used during <see cref="cIMAPClient.Connect"/>. May be <see langword="null"/> or empty.
        /// </summary>
        public readonly ReadOnlyCollection<cSASL> SASLs;

        /// <summary>
        /// Indicates whether all <see cref="SASLs"/> should be tried even if the server doesn't advertise the associated authentication mechanism.
        /// </summary>
        public readonly bool TryAllSASLs;

        /// <summary>
        /// The arguments to use with the IMAP LOGIN command during <see cref="cIMAPClient.Connect"/>. May be <see langword="null"/>.
        /// </summary>
        public readonly cIMAPLogin Login;

        /// <summary>
        /// Initialises a new instance with the specified pre-authenticated credential id, <see cref="cSASL"/> objects and behaviour, and <see cref="cIMAPLogin"/> arguments.
        /// </summary>
        /// <param name="pPreAuthenticatedCredentialId"></param>
        /// <param name="pSASLs"></param>
        /// <param name="pTryAllSASLs"></param>
        /// <param name="pLogin"></param>
        public cIMAPAuthentication(object pPreAuthenticatedCredentialId = null, IEnumerable<cSASL> pSASLs = null, bool pTryAllSASLs = false, cIMAPLogin pLogin = null)
        {
            PreAuthenticatedCredentialId = pPreAuthenticatedCredentialId;

            if (pSASLs == null) SASLs = null;
            else
            {
                var lSASLs = new List<cSASL>(from s in pSASLs where s != null select s);
                if (lSASLs.Count == 0) SASLs = null;
                else SASLs = lSASLs.AsReadOnly();
            }

            TryAllSASLs = pTryAllSASLs;

            Login = pLogin;
        }
    
        /// <summary>
        /// Returns a new set of authentication parameters for connecting anonymously.
        /// </summary>
        /// <param name="pTrace">The trace information to be sent to the server.</param>
        /// <param name="pTLSRequirement">The TLS requirement for the parameters to be used.</param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Indicates whether the SASL ANONYMOUS mechanism should be tried even if it isn't advertised.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method will throw if <paramref name="pTrace"/> can be used with neither <see cref="cIMAPLogin.Password"/> nor <see cref="cSASLAnonymous"/>.
        /// </remarks>
        public static cIMAPAuthentication Anonymous(string pTrace, eTLSRequirement pTLSRequirement = eTLSRequirement.indifferent, bool pTryAuthenticateEvenIfAnonymousIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));

            cIMAPLogin.TryConstruct(cIMAPLogin.Anonymous, pTrace, pTLSRequirement, out var lLogin);
            cSASLAnonymous.TryConstruct(pTrace, pTLSRequirement, out var lSASL);
            if (lLogin == null && lSASL == null) throw new ArgumentOutOfRangeException(nameof(pTrace));
            return new cIMAPAuthentication(null, new cSASL[] { lSASL }, pTryAuthenticateEvenIfAnonymousIsntAdvertised, lLogin);
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
        /// This method will throw if the userid and password can be used with neither <see cref="cIMAPLogin"/> nor <see cref="cSASLPlain"/>.
        /// </remarks>
        public static cIMAPAuthentication Plain(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfPlainIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pPassword)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            cIMAPLogin.TryConstruct(pUserId, pPassword, pTLSRequirement, out var lLogin);
            cSASLPlain.TryConstruct(pUserId, pPassword, pTLSRequirement, out var lSASL);
            if (lLogin == null && lSASL == null) throw new ArgumentOutOfRangeException(); // argument_s_outofrange

            return new cIMAPAuthentication(null, new cSASL[] { lSASL }, pTryAuthenticateEvenIfPlainIsntAdvertised, lLogin);
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
            var lContext = pParentContext.NewMethod(nameof(cIMAPAuthentication), nameof(_Tests));

            bool lFailed;
            cIMAPAuthentication lAP;

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

            lAP = Anonymous("\"fr€d blogs\" @ fred.com");
            if (lAP.Login != null || lAP.SASLs.Count != 1) throw new cTestsException("unexpected anon result");
            string lResult = cTools.UTF8BytesToString(lAP.SASLs[0].GetAuthentication().GetResponse(new byte[0]));
            if (lResult != "\"fr€d blogs\"@fred.com") throw new cTestsException("unexpected anon result");

            lAP = Anonymous("fred@fr€d.com");
            if (lAP.Login != null || lAP.SASLs.Count != 1) throw new cTestsException("unexpected anon result");
            if (cTools.UTF8BytesToString(lAP.SASLs[0].GetAuthentication().GetResponse(new byte[0])) != "fred@fr€d.com") throw new cTestsException("unexpected anon result");

            lAP = Anonymous("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890");
            if (lAP.Login == null || lAP.SASLs != null) throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lAP = Anonymous("fred€@fred@fred.com"); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");
        }
    }
}
