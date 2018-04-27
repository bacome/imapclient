using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains arguments for use with ANONYMOUS authentication.
    /// </summary>
    /// <remarks>
    /// RFC 4505 specifies that the trace information must be a valid email address or 1 to 255 characters of text not including '@'.
    /// </remarks>
    public class cSASLAnonymous : cSASL
    {
        // rfc4505

            ;?;
        public static readonly object AnonymousCredentialId = new object(); // should have a tostring -> anonymous

        private const string kName = "ANONYMOUS";

        private readonly string mTrace;

        private cSASLAnonymous(string pTrace, eTLSRequirement pTLSRequirement, bool pValidated) : base(kName, pTLSRequirement)
        {
            mTrace = pTrace;
        }

        /// <summary>
        /// Initialises a new instance with the specified trace information and TLS requirement. Will throw if the trace information isn't valid.
        /// </summary>
        /// <param name="pTrace"></param>
        /// <param name="pTLSRequirement"></param>
        /// <inheritdoc cref="cSASLAnonymous" select="remarks"/>
        public cSASLAnonymous(string pTrace, eTLSRequirement pTLSRequirement) : base(kName, pTLSRequirement)
        {
            if (!ZIsValid(pTrace, out mTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));
        }

        internal static bool TryConstruct(string pTrace, eTLSRequirement pTLSRequirement, out cSASLAnonymous rAnonymous)
        {
            if (ZIsValid(pTrace, out var lTrace))
            {
                rAnonymous = new cSASLAnonymous(lTrace, pTLSRequirement, true);
                return true;
            }

            rAnonymous = null;
            return false;
        }

        private static bool ZIsValid(string pTrace, out string rTrace)
        {
            if (string.IsNullOrEmpty(pTrace)) { rTrace = null; return false; }

            // note that the stringprep required by the RFC is not done: TODO
            //  the character count restriction in the RFC is not really enforced here as surrogate pairs should count as 1 character: TODO

            if (pTrace.IndexOf('@') == -1)
            {
                if (pTrace.Length < 256)
                {
                    rTrace = pTrace;
                    return true;
                }

                rTrace = null;
                return false;
            }

            try
            {
                var lAddress = new MailAddress(pTrace);
                rTrace = lAddress.User + "@" + lAddress.Host;
                return true;
            }
            catch
            {
                rTrace = null;
                return false;
            }
        }

        /// <inheritdoc cref="cSASL.GetAuthentication"/>
        public override cSASLAuthentication GetAuthentication() => new cAuth(mTrace);

        /// <summary>
        /// Gets <see cref="AnonymousCredentialId"/>.
        /// </summary>
        public override object CredentialId => AnonymousCredentialId;

        private class cAuth : cSASLAuthentication
        {
            private bool mDone = false;
            private readonly string mTrace;

            public cAuth(string pTrace) { mTrace = pTrace; }

            public override IList<byte> GetResponse(IList<byte> pChallenge)
            {
                if (mDone) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyChallenged);
                mDone = true;

                if (pChallenge != null && pChallenge.Count != 0) throw new ArgumentOutOfRangeException("non zero length challenge");
                return Encoding.UTF8.GetBytes(mTrace);
            }

            public override cSASLSecurity GetSecurity() => null;
        }
    }
}