using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace work.bacome.imapclient
{
    public class cSASLAnonymous : cSASL
    {
        // rfc4505

        private static string kName = "ANONYMOUS";

        private string mTrace;

        public cSASLAnonymous(string pTrace)
        {
            if (!ZIsValid(pTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));
            mTrace = pTrace;
        }

        private cSASLAnonymous(string pTrace, bool pPrechecked)
        {
            mTrace = pTrace;
        }

        public static bool TryConstruct(string pTrace, out cSASLAnonymous rAnonymous)
        {
            if (ZIsValid(pTrace))
            {
                rAnonymous = new cSASLAnonymous(pTrace, true);
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