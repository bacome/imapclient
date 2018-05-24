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
            if (pInput == null) throw new ArgumentNullException(nameof(pInput));
            if (pCount < 0 || pCount > pInput.Length) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (mComplete) throw new InvalidOperationException();
            if (pCount == 0) mComplete = true;
            YEncode(pInput, pCount);
            if (mOutput.Count == 0) return null;
            var lOutput = mOutput.ToArray();
            mOutput.Clear();
            return lOutput;
        }

        protected abstract void YEncode(byte[] pInput, int pCount);
    }
}
