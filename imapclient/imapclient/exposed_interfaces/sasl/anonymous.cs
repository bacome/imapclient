using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters for use with the IMAP AUTHENTICATE ANONYMOUS command.
    /// </summary>
    /// <remarks>
    /// RFC 4505 specifies that the trace information must be a valid email address or 1 to 255 characters of text not including '@'.
    /// </remarks>
    public class cSASLAnonymous : cSASL
    {
        // rfc4505

        private const string kName = "ANONYMOUS";

        private readonly string mTrace;
        private readonly eTLSRequirement mTLSRequirement;

        private cSASLAnonymous(string pTrace, eTLSRequirement pTLSRequirement, bool pPrechecked)
        {
            mTrace = pTrace;
            mTLSRequirement = pTLSRequirement;
        }

        /// <summary>
        /// Initialises a new instance with the specified trace information and TLS requirement. Will throw if the trace information isn't valid.
        /// </summary>
        /// <param name="pTrace"></param>
        /// <param name="pTLSRequirement"></param>
        /// <inheritdoc cref="cSASLAnonymous" select="remarks"/>
        public cSASLAnonymous(string pTrace, eTLSRequirement pTLSRequirement)
        {
            if (!ZIsValid(pTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));
            mTrace = pTrace;
            mTLSRequirement = pTLSRequirement;
        }

        internal static bool TryConstruct(string pTrace, eTLSRequirement pTLSRequirement, out cSASLAnonymous rAnonymous)
        {
            if (ZIsValid(pTrace))
            {
                rAnonymous = new cSASLAnonymous(pTrace, pTLSRequirement, true);
                return true;
            }

            rAnonymous = null;
            return false;
        }

        private static bool ZIsValid(string pTrace)
        {
            if (string.IsNullOrEmpty(pTrace)) return false;

            if (pTrace.IndexOf('@') == -1) return pTrace.Length < 256;

            try
            {
                var lAddress = new MailAddress(pTrace);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the value "ANONYMOUS"
        /// </summary>
        public override string MechanismName => kName;

        /// <inheritdoc/>
        public override eTLSRequirement TLSRequirement => mTLSRequirement;

        /// <inheritdoc cref="cSASL.GetAuthentication" select="summary"/>
        public override cSASLAuthentication GetAuthentication() => new cAuth(mTrace);

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