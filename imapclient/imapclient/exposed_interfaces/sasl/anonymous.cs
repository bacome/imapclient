using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace work.bacome.imapclient
{
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

        public cSASLAnonymous(string pTrace, eTLSRequirement pTLSRequirement)
        {
            if (!ZIsValid(pTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));
            mTrace = pTrace;
            mTLSRequirement = pTLSRequirement;
        }

        public static bool TryConstruct(string pTrace, eTLSRequirement pTLSRequirement, out cSASLAnonymous rAnonymous)
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
            if (string.IsNullOrWhiteSpace(pTrace)) return false;

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

        public override string MechanismName => kName;
        public override eTLSRequirement TLSRequirement => mTLSRequirement;
        public override cSASLAuthentication GetAuthentication() => new cAuth(mTrace);

        private class cAuth : cSASLAuthentication
        {
            private bool mDone = false;
            private string mTrace;

            public cAuth(string pTrace) { mTrace = pTrace; }

            public override IList<byte> GetResponse(IList<byte> pChallenge)
            {
                if (mDone) throw new InvalidOperationException("already challenged");
                mDone = true;

                if (pChallenge != null && pChallenge.Count != 0) throw new ArgumentOutOfRangeException("non zero length challenge");
                return Encoding.UTF8.GetBytes(mTrace);
            }


            public override cSASLSecurity GetSecurity() => null;
        }
    }
}