using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cSASLXOAuth2 : cSASL
    {
        // https://developers.google.com/gmail/imap/xoauth2-protocol
        //  (untested) : TODO: test it

        private const string kName = "XOAUTH2";

        private readonly string mUserId;
        private readonly string mAccessToken;

        public cSASLXOAuth2(string pUserId, string pAccessToken) : base(kName, eTLSRequirement.required)
        {
            if (string.IsNullOrEmpty(pUserId) || pUserId.IndexOf(cChar.CtrlA) != -1) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pAccessToken)) throw new ArgumentOutOfRangeException(nameof(pAccessToken));
            foreach (var lChar in pAccessToken) if (!cCharset.VSChar.Contains(lChar)) throw new ArgumentOutOfRangeException(nameof(pAccessToken));
            mUserId = pUserId;
            mAccessToken = pAccessToken;
        }

        public override cSASLAuthentication GetAuthentication() => new cAuth(mUserId, mAccessToken);

        public override object CredentialId => mUserId;

        private class cAuth : cSASLAuthentication
        {
            private enum eState { unchallenged, challenged, errorreceived }

            private eState mState = eState.unchallenged;
            private readonly string mUserId;
            private readonly string mAccessToken;
            private string mErrorMessage = null;

            public cAuth(string pUserId, string pAccessToken)
            {
                mUserId = pUserId;
                mAccessToken = pAccessToken;
            }

            public override IList<byte> GetResponse(IList<byte> pChallenge)
            {
                switch (mState)
                {
                    case eState.unchallenged:

                        if (pChallenge != null && pChallenge.Count != 0) throw new ArgumentOutOfRangeException("non zero length challenge");
                        mState = eState.challenged;
                        string lResponse = "user=" + mUserId + cChar.CtrlA + "auth=Bearer " + mAccessToken + cChar.CtrlA + cChar.CtrlA;
                        return new List<byte>(Encoding.UTF8.GetBytes(lResponse));

                    case eState.challenged:

                        if (pChallenge == null) throw new ArgumentOutOfRangeException("null error message challenge");
                        mState = eState.errorreceived;
                        mErrorMessage = cTools.UTF8BytesToString(pChallenge);
                        return new List<byte>();

                    default:

                        throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyChallenged);
                }
            }

            public string ErrorMessage => mErrorMessage;

            public override cSASLSecurity GetSecurity() => null;
        }
    }
}