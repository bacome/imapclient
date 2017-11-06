using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Describes a set of credentials
    /// </summary>
    public class cCredentials
    {
        /// <summary>
        /// The account type that the credentials give access to
        /// </summary>
        public readonly eAccountType Type;

        /// <summary>
        /// The userid for the credentials
        /// </summary>
        /// <remarks>
        /// may be null for anonymous and NONE; must not be null otherwise
        /// </remarks>
        public readonly string UserId;

        /// <summary>
        /// The parameters to use with the IMAP LOGIN command for these credentials
        /// </summary>
        public readonly cLogin Login;

        /// <summary>
        /// Whether all authentication mechanisms should be tried regardless of whether they are advertised by the server or not
        /// </summary>
        public readonly bool TryAllSASLs;

        /// <summary>
        /// The set of SASL objects to try when authenticating
        /// </summary>
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

        public ReadOnlyCollection<cSASL> SASLs => mSASLs.AsReadOnly();

        /// <summary>
        /// An empty set of credentials
        /// </summary>
        /// <remarks>
        /// Useful for testing, checking what capabilities the server offers without connecting and for pre-authorised connections
        /// </remarks>
        public static readonly cCredentials None = new cCredentials(eAccountType.none, null);

        /// <summary>
        /// Generates an anonymous set of credentials
        /// </summary>
        /// <param name="pTrace">The trace information to be sent to the server when connecting</param>
        /// <param name="pTLSRequirement">The TLS requirement for these credentials to be used</param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Try AUTHENTICATE ANONYMOUS even if it isn't advertised</param>
        /// <returns>Anonymous credentials</returns>
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
        /// Generates a plain set of credentials
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for these credentials to be used</param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Try AUTHENTICATE PLAIN even if it isn't advertised</param>
        /// <returns>Plain credentials</returns>
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
        public static void _Tests(cTrace.cContext pParentContext)
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
