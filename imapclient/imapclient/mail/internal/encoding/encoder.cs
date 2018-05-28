using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal abstract class cEncoder
    {
        private bool mComplete = false;
        protected readonly List<byte> mOutput = new List<byte>();

        public cEncoder() { }

        public byte[] Encode(byte[] pInput, int pCount)
        {
            ZEncode(pInput, pCount);
            if (mOutput.Count == 0) return cMailClient.ZeroLengthBuffer;
            var lOutput = mOutput.ToArray();
            mOutput.Clear();
            return lOutput;
        }

        public long GetEncodedLength(byte[] pInput, int pCount)
        {
            ZEncode(pInput, pCount);
            var lCount = mOutput.Count;
            mOutput.Clear();
            return lCount;
        }

        public int GetBufferedInputBytes()
        {
            if (mComplete) return 0;
            return YGetBufferedInputBytes();
        }

        private void ZEncode(byte[] pInput, int pCount)
        {
            if (pInput == null) throw new ArgumentNullException(nameof(pInput));
            if (pCount < 0 || pCount > pInput.Length) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (mComplete) throw new InvalidOperationException();
            if (pCount == 0) mComplete = true;
            YEncode(pInput, pCount);
        }

        protected abstract void YEncode(byte[] pInput, int pCount);
        protected abstract int YGetBufferedInputBytes();
    }
}
