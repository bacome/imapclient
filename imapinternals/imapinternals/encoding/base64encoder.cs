using System;
using System.Collections.Generic;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    public class cBase64Encoder : cEncoder
    {
        private static readonly cBytes kCRLF = new cBytes("\r\n");

        private readonly List<byte> mPendingInputBytes = new List<byte>(57);

        public cBase64Encoder() { }

        public sealed override int BufferedInputByteCount => mPendingInputBytes.Count;

        protected sealed override void YEncode(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            int lInputByte = 0;

            while (true)
            {
                while (mPendingInputBytes.Count < 57 && lInputByte < pCount) mPendingInputBytes.Add(pInputBytes[pOffset + lInputByte++]);

                if (mPendingInputBytes.Count < 57 && pCount != 0) return;
                if (mPendingInputBytes.Count == 0) return;

                var lEncodedBytes = cBase64.Encode(mPendingInputBytes);

                mEncodedBytes.AddRange(lEncodedBytes);
                mEncodedBytes.AddRange(kCRLF);

                mPendingInputBytes.Clear();
            }
        }

        public static long GetEncodedLength(long pUnencodedLength)
        {
            if (pUnencodedLength < 0) throw new ArgumentOutOfRangeException(nameof(pUnencodedLength));

            long l3s = pUnencodedLength / 3;
            if (pUnencodedLength % 3 != 0) l3s++;

            long l57s = pUnencodedLength / 57;
            if (pUnencodedLength % 57 != 0) l57s++;

            return l3s * 4 + l57s * 2;
        }
    }
}