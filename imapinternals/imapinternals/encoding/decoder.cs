using System;
using System.Collections.Generic;

namespace work.bacome.imapinternals
{
    public abstract class cDecoder : iTransformer
    {
        private bool mPendingCR = false;
        protected readonly List<byte> mBufferedInputBytes = new List<byte>(76);
        protected readonly List<byte> mDecodedBytes = new List<byte>();

        public cDecoder() { }

        public byte[] Transform(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            ZDecode(pInputBytes, pOffset, pCount);
            if (mDecodedBytes.Count == 0) return cConst.ZeroLengthBuffer;
            var lDecodedBytes = mDecodedBytes.ToArray();
            mDecodedBytes.Clear();
            return lDecodedBytes;
        }

        public int GetTransformedLength(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            ZDecode(pInputBytes, pOffset, pCount);
            var lCount = mDecodedBytes.Count;
            mDecodedBytes.Clear();
            return lCount;
        }

        public int BufferedInputByteCount
        {
            get
            {
                if (mPendingCR) return mBufferedInputBytes.Count + 1;
                else return mBufferedInputBytes.Count;
            }
        }

        private void ZDecode(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            if (pInputBytes == null) throw new ArgumentNullException(nameof(pInputBytes));
            if (pOffset < 0 || pOffset > pInputBytes.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0 || pOffset + pCount > pInputBytes.Count) throw new ArgumentOutOfRangeException(nameof(pCount));

            if (pCount == 0)
            {
                if (mPendingCR)
                {
                    mPendingCR = false;
                    mBufferedInputBytes.Add(cASCII.CR);
                }

                if (mBufferedInputBytes.Count != 0)
                {
                    YDecode(false);
                    mBufferedInputBytes.Clear();
                }

                return;
            }

            while (pCount-- != 0)
            {
                byte lByte = pInputBytes[pOffset++];

                if (mPendingCR)
                {
                    mPendingCR = false;

                    if (lByte == cASCII.LF)
                    {
                        YDecode(true);
                        mBufferedInputBytes.Clear();
                        continue;
                    }

                    mBufferedInputBytes.Add(cASCII.CR);
                }

                if (lByte == cASCII.CR) mPendingCR = true;
                else mBufferedInputBytes.Add(lByte);
            }
        }

        protected abstract void YDecode(bool pCRLF);
    }
}
