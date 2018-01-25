using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal class cBase64Encoder : Stream
    {
        private readonly Stream mStream;

        private readonly Queue<byte> mEncodedBytesBuffer = new Queue<byte>();
        private readonly Queue<byte> mUnencodedBytesBuffer = new Queue<byte>();

        private bool mReadStreamToEnd = false;
        private int mUnencodedBytesOnCurrentLine = 0; // will be a multiple of 3 < 57

        private readonly byte[] mReadBuffer = new byte[57];
        private int mBytesReadIntoReadBuffer = 0;

        private readonly List<byte> mBytesToEncode = new List<byte>(57);

        public cBase64Encoder(Stream pStream)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int ReadTimeout
        {
            get => mStream.ReadTimeout;
            set => mStream.ReadTimeout = value;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            int lBytesRead = 0;

            while (true)
            {
                lBytesRead += ZReadFromEncodedBytesBuffer(pBuffer, ref pOffset, ref pCount);
                if (pCount == 0) return lBytesRead;

                while (true)
                {
                    if (ZEncodeSomeBytes()) break;
                    if (mReadStreamToEnd) return lBytesRead;
                    mBytesReadIntoReadBuffer = mStream.Read(mReadBuffer, 0, mReadBuffer.Length);
                    ZCopyReadBufferToUnencodedBytesBuffer();
                }
            }
        }

        public override async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            int lBytesRead = 0;

            while (true)
            {
                lBytesRead += ZReadFromEncodedBytesBuffer(pBuffer, ref pOffset, ref pCount);
                if (pCount == 0) return lBytesRead;

                while (true)
                {
                    if (ZEncodeSomeBytes()) break;
                    if (mReadStreamToEnd) return lBytesRead;
                    mBytesReadIntoReadBuffer = await mStream.ReadAsync(mReadBuffer, 0, mReadBuffer.Length, pCancellationToken);
                    ZCopyReadBufferToUnencodedBytesBuffer();
                }
            }
        }

        private void ZReadInit(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
        }

        private int ZReadFromEncodedBytesBuffer(byte[] pBuffer, ref int pOffset, ref int pCount)
        {
            int lBytesRead = 0;

            while (pCount > 0 && mEncodedBytesBuffer.Count > 0)
            {
                pBuffer[pOffset++] = mEncodedBytesBuffer.Dequeue();
                pCount--;
                lBytesRead++;
            }

            return lBytesRead;
        }

        private void ZCopyReadBufferToUnencodedBytesBuffer()
        {
            if (mBytesReadIntoReadBuffer == 0)
            {
                mReadStreamToEnd = true;
                return;
            }

            for (int i = 0; i < mBytesReadIntoReadBuffer; i++) mUnencodedBytesBuffer.Enqueue(mReadBuffer[i]);
        }

        private bool ZEncodeSomeBytes()
        {
            bool lResult = false;

            while (true)
            {
                mBytesToEncode.Clear();

                while (true)
                {
                    if (mUnencodedBytesBuffer.Count == 0) break;
                    if (mUnencodedBytesBuffer.Count < 3 && !mReadStreamToEnd) break;
                    mBytesToEncode.Add(mUnencodedBytesBuffer.Dequeue());
                    mUnencodedBytesOnCurrentLine++;
                    if (mUnencodedBytesBuffer.Count == 0) break;
                    mBytesToEncode.Add(mUnencodedBytesBuffer.Dequeue());
                    mUnencodedBytesOnCurrentLine++;
                    if (mUnencodedBytesBuffer.Count == 0) break;
                    mBytesToEncode.Add(mUnencodedBytesBuffer.Dequeue());
                    mUnencodedBytesOnCurrentLine++;
                    if (mUnencodedBytesOnCurrentLine == 57) break;
                }

                if (mBytesToEncode.Count == 0) break;
                var lEncodedBytes = cBase64.Encode(mBytesToEncode);
                foreach (var lByte in lEncodedBytes) mEncodedBytesBuffer.Enqueue(lByte);

                if (mUnencodedBytesOnCurrentLine == 57 || (mReadStreamToEnd && mUnencodedBytesBuffer.Count == 0))
                {
                    mEncodedBytesBuffer.Enqueue(cASCII.CR);
                    mEncodedBytesBuffer.Enqueue(cASCII.LF);
                    mUnencodedBytesOnCurrentLine = 0;
                }
            }

            return lResult;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public static long EncodedLength(long pUnencodedLength)
        {
            if (pUnencodedLength < 0) throw new ArgumentOutOfRangeException(nameof(pUnencodedLength));

            long l3s = pUnencodedLength / 3;
            if (pUnencodedLength % 3 != 0) l3s++;

            long l57s = pUnencodedLength / 57;
            if (pUnencodedLength % 57 != 0) l57s++;

            return l3s * 4 + l57s * 2;
        }



        internal static void _Tests(cTrace.cContext pParentContext)
        {
            ZTest("1", "this is a test", pParentContext).Wait();
        }

        private static async Task ZTest(string pTestName, string pInputString, cTrace.cContext pParentContext)
        {
            cMethodControl lMC = new cMethodControl(-1, CancellationToken.None);
            cBatchSizer lWS = new cBatchSizer(new cBatchSizerConfiguration(10, 100, 10000, 10));
            string lIntermediateString;
            string lFinalString;

            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            {
                cBase64Encoder lEncoder = new cBase64Encoder(lInput);

                using (MemoryStream lIntermediate = new MemoryStream())
                {
                    lEncoder.CopyTo(lIntermediate);

                    if (lIntermediate.Length != EncodedLength(lInput.Length)) throw new cTestsException($"{nameof(cBase64Encoder)}({pTestName}.l)");

                    lIntermediateString = new string(Encoding.UTF8.GetChars(lIntermediate.ToArray()));

                    using (MemoryStream lFinal = new MemoryStream())
                    {
                        cBase64Decoder lDecoder = new cBase64Decoder(lFinal);
                        await lDecoder.WriteAsync(lMC, lFinal.ToArray(), 0, lWS, pParentContext);
                        lFinalString = new string(Encoding.UTF8.GetChars(lFinal.ToArray()));
                    }
                }
            }

            if (lFinalString != pInputString) throw new cTestsException($"{nameof(cBase64Encoder)}({pTestName}.f)");

            // check the lines are no longer than 76 chars
            // TODO;?;
        }
    }
}