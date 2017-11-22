using System;
using System.Collections.Generic;
using System.Text;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters for use with the IMAP AUTHENTICATE PLAIN command.
    /// </summary>
    /// <remarks>
    /// RFC 4616 specifies that the authentication-id and password must be at least 1 character long and that they may not include the NUL character.
    /// </remarks>
    public class cSASLPlain : cSASL
    {
        // rfc4616

        private const string kName = "PLAIN";

        private readonly string mAuthenticationId;
        private readonly string mPassword;
        private readonly eTLSRequirement mTLSRequirement;

        private cSASLPlain(string pAuthenticationId, string pPassword, eTLSRequirement pTLSRequirement, bool pPrechecked)
        {
            mAuthenticationId = pAuthenticationId;
            mPassword = pPassword;
            mTLSRequirement = pTLSRequirement;
        }

        /// <summary>
        /// Initialises a new instance with the specified authentication-id, password and TLS requirement. Will throw if the authentication-id or password are not valid.
        /// </summary>
        /// <param name="pAuthenticationId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement"></param>
        public cSASLPlain(string pAuthenticationId, string pPassword, eTLSRequirement pTLSRequirement)
        {
            if (string.IsNullOrEmpty(pAuthenticationId) || pAuthenticationId.IndexOf(cChar.NUL) != -1) throw new ArgumentOutOfRangeException(nameof(pAuthenticationId));
            if (string.IsNullOrEmpty(pPassword) || pPassword.IndexOf(cChar.NUL) != -1) throw new ArgumentOutOfRangeException(nameof(pPassword));
            mAuthenticationId = pAuthenticationId;
            mPassword = pPassword;
            mTLSRequirement = pTLSRequirement;
        }

        internal static bool TryConstruct(string pAuthenticationId, string pPassword, eTLSRequirement pTLSRequirement, out cSASLPlain rPlain)
        {
            if (!string.IsNullOrEmpty(pAuthenticationId) &&
                pAuthenticationId.IndexOf(cChar.NUL) == -1 &&
                !string.IsNullOrEmpty(pPassword) &&
                pPassword.IndexOf(cChar.NUL) == -1)
            {
                rPlain = new cSASLPlain(pAuthenticationId, pPassword, pTLSRequirement, true);
                return true;
            }

            rPlain = null;
            return false;
        }

        /// <summary>
        /// Gets the value "PLAIN"
        /// </summary>
        public override string MechanismName => kName;

        /// <inheritdoc/>
        public override eTLSRequirement TLSRequirement => mTLSRequirement;

        /// <inheritdoc/>
        public override cSASLAuthentication GetAuthentication() => new cAuth(mAuthenticationId, mPassword);

        private class cAuth : cSASLAuthentication
        {
            private bool mDone = false;
            private readonly string mAuthenticationId;
            private readonly string mPassword;

            public cAuth(string pAuthenticationId, string pPassword)
            {
                mAuthenticationId = pAuthenticationId;
                mPassword = pPassword;
            }

            public override IList<byte> GetResponse(IList<byte> pChallenge)
            {
                if (mDone) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyChallenged);
                mDone = true;

                if (pChallenge != null && pChallenge.Count != 0) throw new ArgumentOutOfRangeException("non zero length challenge");

                var lBytes = new cByteList();

                lBytes.Add(0);
                lBytes.AddRange(Encoding.UTF8.GetBytes(mAuthenticationId));
                lBytes.Add(0);
                lBytes.AddRange(Encoding.UTF8.GetBytes(mPassword));

                return lBytes;
            }

            public override cSASLSecurity GetSecurity() => null;
        }
    }
}