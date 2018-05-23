using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cBase64Encoder
    {
        public delegate void dOutput(cMethodControl pMC, byte[] pOutput, cTrace.cContext pParentContext);
        public delegate Task dOutputAsync(cMethodControl pMC, byte[] pOutput, cTrace.cContext pParentContext);

        private static readonly cBytes kCRLF = new cBytes("\r\n");

        private readonly dOutput mOutput;
        private readonly dOutputAsync mOutputAsync;
        private readonly List<byte> mBytesToEncode = new List<byte>(57);

        public cBase64Encoder(dOutput pOutput)
        {
            mOutput = pOutput ?? throw new ArgumentNullException(nameof(pOutput));
            mOutputAsync = null;
        }

        public cBase64Encoder(dOutputAsync pOutputAsync)
        {
            mOutput = null;
            mOutputAsync = pOutputAsync ?? throw new ArgumentNullException(nameof(pOutputAsync));
        }

        public cBase64Encoder(dOutput pOutput, dOutputAsync pOutputAsync)
        {
            mOutput = pOutput ?? throw new ArgumentNullException(nameof(pOutput));
            mOutputAsync = pOutputAsync ?? throw new ArgumentNullException(nameof(pOutputAsync));
        }

        public void Encode(cMethodControl pMC, byte[] pInput, int pCount, cTrace.cContext pParentContext)
        {
            if (pInput == null) throw new ArgumentNullException(nameof(pInput));
            if (pCount < 0 || pCount > pInput.Length) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (mOutput == null) throw new InvalidOperationException();

            int lInputPosition = 0;

            while (true)
            {
                while (mBytesToEncode.Count < 57 && lInputPosition < pCount) mBytesToEncode.Add(pInput[lInputPosition++]);

                if (mBytesToEncode.Count < 57 && pCount != 0) return;
                if (mBytesToEncode.Count == 0) return;

                var lEncodedBytes = cBase64.Encode(mBytesToEncode);
                lEncodedBytes.AddRange(kCRLF);

                mOutput(pMC, lEncodedBytes.ToArray(), pParentContext);

                mBytesToEncode.Clear();
            }
        }

        public async Task EncodeAsync(cMethodControl pMC, byte[] pInput, int pCount, cTrace.cContext pParentContext)
        {
            if (pInput == null) throw new ArgumentNullException(nameof(pInput));
            if (pCount < 0 || pCount > pInput.Length) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (mOutputAsync == null) throw new InvalidOperationException();

            int lInputPosition = 0;

            while (true)
            {
                while (mBytesToEncode.Count < 57 && lInputPosition < pCount) mBytesToEncode.Add(pInput[lInputPosition++]);

                if (mBytesToEncode.Count < 57 && pCount != 0) return;
                if (mBytesToEncode.Count == 0) return;

                var lEncodedBytes = cBase64.Encode(mBytesToEncode);
                lEncodedBytes.AddRange(kCRLF);

                await mOutputAsync(pMC, lEncodedBytes.ToArray(), pParentContext).ConfigureAwait(false);

                mBytesToEncode.Clear();
            }
        }

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