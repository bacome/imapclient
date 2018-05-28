using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal abstract class cDecoder
    {
        private bool mComplete = false;
        private bool mPendingCR = false;
        protected readonly List<byte> mPendingInput = new List<byte>(76);
        protected readonly List<byte> mOutput = new List<byte>();

        public cDecoder() { }

        public byte[] Decode(IList<byte> pInput, int pOffset, int pCount)
        {
            ZDecode(pInput, pOffset, pCount);
            if (mOutput.Count == 0) return cMailClient.ZeroLengthBuffer;
            var lOutput = mOutput.ToArray();
            mOutput.Clear();
            return lOutput;
        }

        public long GetDecodedLength(IList<byte> pInput, int pOffset, int pCount)
        {
            ZDecode(pInput, pOffset, pCount);
            var lCount = mOutput.Count;
            mOutput.Clear();
            return lCount;
        }

        public int GetBufferedInputBytes()
        {
            if (mComplete) return 0;
            if (mPendingCR) return mPendingInput.Count + 1;
            else return mPendingInput.Count;
        }

        private void ZDecode(IList<byte> pInput, int pOffset, int pCount)
        {
            if (pInput == null) throw new ArgumentNullException(nameof(pInput));
            if (pOffset < 0 || pOffset > pInput.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0 || pOffset + pCount > pInput.Count) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (mComplete) throw new InvalidOperationException();
            if (pCount == 0) mComplete = true;

            if (pCount == 0)
            {
                if (mPendingInput.Count != 0) YDecode(false);
                return;
            }

            while (pCount-- != 0)
            {
                byte lByte = pInput[pOffset++];

                if (mPendingCR)
                {
                    mPendingCR = false;

                    if (lByte == cASCII.LF)
                    {
                        YDecode(true);
                        mPendingInput.Clear();
                        continue;
                    }

                    mPendingInput.Add(cASCII.CR);
                }

                if (lByte == cASCII.CR) mPendingCR = true;
                else mPendingInput.Add(lByte);
            }
        }

        protected abstract void YDecode(bool pCRLF);
    }
}
