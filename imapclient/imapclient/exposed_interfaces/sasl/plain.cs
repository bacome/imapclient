﻿using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cSASLPlain : cSASL
    {
        // rfc4616

        private static string kName = "PLAIN";

        private string mAuthenticationId;
        private string mPassword;

        public cSASLPlain(string pAuthenticationId, string pPassword)
        {
            if (string.IsNullOrEmpty(pAuthenticationId) || pAuthenticationId.IndexOf(cChar.NUL) != -1) throw new ArgumentOutOfRangeException(nameof(pAuthenticationId));
            if (string.IsNullOrEmpty(pPassword) || pPassword.IndexOf(cChar.NUL) != -1) throw new ArgumentOutOfRangeException(nameof(pPassword));
            mAuthenticationId = pAuthenticationId;
            mPassword = pPassword;
        }

        private cSASLPlain(string pAuthenticationId, string pPassword, bool pPrechecked)
        {
            mAuthenticationId = pAuthenticationId;
            mPassword = pPassword;
        }

        public static bool TryConstruct(string pAuthenticationId, string pPassword, out cSASLPlain rPlain)
        {
            if (!string.IsNullOrEmpty(pAuthenticationId) &&
                pAuthenticationId.IndexOf(cChar.NUL) == -1 &&
                !string.IsNullOrEmpty(pPassword) &&
                pPassword.IndexOf(cChar.NUL) == -1)
            {
                rPlain = new cSASLPlain(pAuthenticationId, pPassword, true);
                return true;
            }

            rPlain = null;
            return false;
        }

        public override string MechanismName => kName;
        public override cSASLAuthentication GetAuthentication() => new cAuth(mAuthenticationId, mPassword);

        private class cAuth : cSASLAuthentication
        {
            private bool mDone = false;
            private string mAuthenticationId;
            private string mPassword;

            public cAuth(string pAuthenticationId, string pPassword)
            {
                mAuthenticationId = pAuthenticationId;
                mPassword = pPassword;
            }

            public override IList<byte> GetResponse(IList<byte> pChallenge)
            {
                if (mDone) throw new InvalidOperationException("already challenged");
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