using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cTransformingStream : Stream
    {
        private readonly Stream mStream;
        private readonly iTransformer mTransformer;
        private readonly byte[] mInputBuffer = new byte[kConst.BufferSize];
        private bool mStreamReadToEnd = false;
        private byte[] mPendingTransformedBytes = kConst.ZeroLengthBuffer;
        private int mPendingTransformedBytesPosition = 0;
        private long mInputBytesRead = 0;
        private readonly byte[] mReadByteBuffer = new byte[1];

        public cTransformingStream(Stream pStream, iTransformer pTransformer)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            mTransformer = pTransformer ?? throw new ArgumentNullException(nameof(pTransformer));
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => mStream.CanTimeout;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int ReadTimeout
        {
            get => mStream.ReadTimeout;
            set => mStream.ReadTimeout = value;
        }

        public override void Flush() => mStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            while (true)
            {
                if (ZTryReadingFromPendingTransformedBytes(pBuffer, pOffset, pCount, out var lBytesRead) || mStreamReadToEnd) return lBytesRead;
                ZAfterReadIntoInputBuffer(mStream.Read(mInputBuffer, 0, kConst.BufferSize));
            }
        }

        public override async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            while (true)
            {
                if (ZTryReadingFromPendingTransformedBytes(pBuffer, pOffset, pCount, out var lBytesRead) || mStreamReadToEnd) return lBytesRead;
                ZAfterReadIntoInputBuffer(await mStream.ReadAsync(mInputBuffer, 0, kConst.BufferSize).ConfigureAwait(false));
            }
        }

        public override int ReadByte()
        {
            while (true)
            {
                if (ZTryReadingFromPendingTransformedBytes(mReadByteBuffer, 0, 1, out var lBytesRead)) return mReadByteBuffer[0];
                if (mStreamReadToEnd) return -1;
                ZAfterReadIntoInputBuffer(mStream.Read(mInputBuffer, 0, kConst.BufferSize));
            }
        }

        public static long GetTransformedLength(Stream pStream, iTransformer pTransformer)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pTransformer == null) throw new ArgumentNullException(nameof(pTransformer));
            if (pStream.CanSeek && pStream.Position != 0) pStream.Position = 0;

            byte[] lInputBuffer = new byte[kConst.BufferSize];
            long lDecodedLength = 0;

            while (true)
            {
                int lBytesRead = pStream.Read(lInputBuffer, 0, kConst.BufferSize);
                lDecodedLength += pTransformer.GetTransformedLength(lInputBuffer, 0, lBytesRead);
                if (lBytesRead == 0) return lDecodedLength;
            }
        }

        public static async Task<long> GetTransformedLengthAsync(Stream pStream, iTransformer pTransformer)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pTransformer == null) throw new ArgumentNullException(nameof(pTransformer));
            if (pStream.CanSeek && pStream.Position != 0) pStream.Position = 0;

            byte[] lInputBuffer = new byte[kConst.BufferSize];
            long lDecodedLength = 0;

            while (true)
            {
                int lBytesRead = await pStream.ReadAsync(lInputBuffer, 0, kConst.BufferSize).ConfigureAwait(false);
                lDecodedLength += pTransformer.GetTransformedLength(lInputBuffer, 0, lBytesRead);
                if (lBytesRead == 0) return lDecodedLength;
            }
        }

        private void ZReadInit(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
        }

        private bool ZTryReadingFromPendingTransformedBytes(byte[] pBuffer, int pOffset, int pCount, out int rBytesRead)
        {
            rBytesRead = 0;

            while (pCount > 0 && mPendingTransformedBytesPosition < mPendingTransformedBytes.Length)
            {
                pBuffer[pOffset++] = mPendingTransformedBytes[mPendingTransformedBytesPosition++];
                pCount--;
                rBytesRead++;
            }

            return rBytesRead != 0;
        }

        private void ZAfterReadIntoInputBuffer(int pBytesRead)
        {
            mInputBytesRead += pBytesRead;
            if (pBytesRead == 0) mStreamReadToEnd = true;
            mPendingTransformedBytes = mTransformer.Transform(mInputBuffer, 0, pBytesRead);
            mPendingTransformedBytesPosition = 0;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}