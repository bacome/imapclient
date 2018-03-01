using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains arguments for use with the IMAP AUTHENTICATE ANONYMOUS command.
    /// </summary>
    /// <remarks>
    /// RFC 4505 specifies that the trace information must be a valid email address or 1 to 255 characters of text not including '@'.
    /// </remarks>
    public class cSASLAnonymous : cSASL
    {
        // rfc4505

        private const string kName = "ANONYMOUS";

        private readonly cBytes mTrace;

        private cSASLAnonymous(cBytes pTrace, eTLSRequirement pTLSRequirement) : base(kName, pTLSRequirement)
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
            if (!ZIsValid(pTrace, out var lTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));
            mTrace = lTrace;
        }

        internal static bool TryConstruct(string pTrace, eTLSRequirement pTLSRequirement, out cSASLAnonymous rAnonymous)
        {
            if (ZIsValid(pTrace, out var lTrace))
            {
                rAnonymous = new cSASLAnonymous(lTrace, pTLSRequirement);
                return true;
            }

            rAnonymous = null;
            return false;
        }

        private static bool ZIsValid(string pTrace, out cBytes rTrace)
        {
            if (string.IsNullOrEmpty(pTrace)) { rTrace = null; return false; }

            // note that the stringprep required by the RFC is not done: TODO
            //  the character count restriction in the RFC is not really enforced here as surrogate pairs should count as 1 character: TODO

            if (pTrace.IndexOf('@') == -1)
            {
                if (pTrace.Length < 256) { rTrace = null; return false; }
                rTrace = new cBytes(Encoding.UTF8.GetBytes(pTrace));
                return true;
            }

            try
            {
                var lAddress = new MailAddress(pTrace);

                if (!cHeaderFieldValuePart.TryAsAddrSpec(lAddress.User, lAddress.Host, out var lPart)) { rTrace = null; return false; }

                cHeaderFieldBytes lBytes = new cHeaderFieldBytes();
                lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);

                rTrace = new cBytes(lBytes.Bytes);
                return true;
            }
            catch
            {
                rTrace = null;
                return false;
            }
        }

        /// <inheritdoc cref="cSASL.GetAuthentication" select="summary"/>
        public override cSASLAuthentication GetAuthentication() => new cAuth(mTrace);

        private class cAuth : cSASLAuthentication
        {
            private bool mDone = false;
            private readonly cBytes mTrace;

            public cAuth(cBytes pTrace) { mTrace = pTrace; }

            public override IList<byte> GetResponse(IList<byte> pChallenge)
            {
                if (mDone) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyChallenged);
                mDone = true;

                if (pChallenge != null && pChallenge.Count != 0) throw new ArgumentOutOfRangeException("non zero length challenge");
                return mTrace;
            }

            public override cSASLSecurity GetSecurity() => null;
        }
    }
}