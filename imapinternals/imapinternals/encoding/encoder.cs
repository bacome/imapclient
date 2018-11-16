using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    public abstract class cEncoder : iTransformer
    {
        protected readonly List<byte> mEncodedBytes = new List<byte>();

        public cEncoder() { }

        public byte[] Transform(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            ZEncode(pInputBytes, pOffset, pCount);
            if (mEncodedBytes.Count == 0) return kConst.ZeroLengthBuffer;
            var lOutput = mEncodedBytes.ToArray();
            mEncodedBytes.Clear();
            return lOutput;
        }

        public int GetTransformedLength(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            ZEncode(pInputBytes, pOffset, pCount);
            var lCount = mEncodedBytes.Count;
            mEncodedBytes.Clear();
            return lCount;
        }

        private void ZEncode(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            if (pInputBytes == null) throw new ArgumentNullException(nameof(pInputBytes));
            if (pOffset < 0 || pOffset > pInputBytes.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0 || pOffset + pCount > pInputBytes.Count) throw new ArgumentOutOfRangeException(nameof(pCount));
            YEncode(pInputBytes, pOffset, pCount);
        }

        public abstract int BufferedInputByteCount { get; }

        protected abstract void YEncode(IList<byte> pInputBytes, int pOffset, int pCount);
    }
}
