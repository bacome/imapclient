using System;
using System.Collections.Generic;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cBase64Encoder : cEncoder
    {
        private static readonly cBytes kCRLF = new cBytes("\r\n");

        private readonly List<byte> mPendingInput = new List<byte>(57);

        public cBase64Encoder() { }

        protected sealed override void YEncode(byte[] pInput, int pCount)
        {
            int lInputPosition = 0;

            while (true)
            {
                while (mPendingInput.Count < 57 && lInputPosition < pCount) mPendingInput.Add(pInput[lInputPosition++]);

                if (mPendingInput.Count < 57 && pCount != 0) return;
                if (mPendingInput.Count == 0) return;

                var lEncodedBytes = cBase64.Encode(mPendingInput);

                mOutput.AddRange(lEncodedBytes);
                mOutput.AddRange(kCRLF);

                mPendingInput.Clear();
            }
        }

        protected sealed override int YGetBufferedInputBytes() => mPendingInput.Count;

        public static long EncodedLength(long pUnencodedLength)
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