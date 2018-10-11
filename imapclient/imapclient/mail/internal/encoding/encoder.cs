using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal abstract class cEncoder : iTransformer
    {
        private bool mComplete = false;
        protected readonly List<byte> mEncodedBytes = new List<byte>();

        public cEncoder() { }

        public byte[] Transform(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            ZEncode(pInputBytes, pOffset, pCount);
            if (mEncodedBytes.Count == 0) return cMailClient.ZeroLengthBuffer;
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

        public int BufferedInputByteCount
        {
            get
            {
                if (mComplete) return 0;
                return YGetBufferedInputByteCount();
            }
        }

        private void ZEncode(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            if (pInputBytes == null) throw new ArgumentNullException(nameof(pInputBytes));
            if (pOffset < 0 || pOffset > pInputBytes.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0 || pOffset + pCount > pInputBytes.Count) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (mComplete) throw new InvalidOperationException();
            if (pCount == 0) mComplete = true;
            YEncode(pInputBytes, pOffset, pCount);
        }

        protected abstract void YEncode(IList<byte> pInputBytes, int pOffset, int pCount);
        protected abstract int YGetBufferedInputByteCount();
    }
}
