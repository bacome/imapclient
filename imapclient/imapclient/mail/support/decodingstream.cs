using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.mailclient.support
{
    public abstract class cDecodingStream : Stream
    {
        private readonly Stream mStream;
        private readonly cDecoder mDecoder;
        private readonly byte[] mUndecodedBuffer = new byte[cMailClient.BufferSize];
        private bool mStreamReadToEnd = false;
        private byte[] mDecodedBuffer = cMailClient.ZeroLengthBuffer;
        private int mDecodedBufferPosition = 0;

        private long mUndecodedBytesRead = 0;
        private long mDecodedBytesProduced = 0;

        internal cDecodingStream(Stream pStream, cDecoder pDecoder)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            mDecoder = pDecoder ?? throw new ArgumentNullException(nameof(pDecoder));
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
                int lBytesRead = ZReadFromDecodedBuffer(pBuffer, pOffset, pCount);
                if (lBytesRead != 0 || mStreamReadToEnd) return lBytesRead;
                lBytesRead = mStream.Read(mUndecodedBuffer, 0, cMailClient.BufferSize);
                mUndecodedBytesRead += lBytesRead;
                if (lBytesRead == 0) mStreamReadToEnd = true;
                mDecodedBuffer = mDecoder.Decode(mUndecodedBuffer, 0, lBytesRead);
                mDecodedBufferPosition = 0;
                mDecodedBytesProduced += mDecodedBuffer.Length;
            }
        }

        public override async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            while (true)
            {
                int lBytesRead = ZReadFromDecodedBuffer(pBuffer, pOffset, pCount);
                if (lBytesRead != 0 || mStreamReadToEnd) return lBytesRead;
                lBytesRead = await mStream.ReadAsync(mUndecodedBuffer, 0, cMailClient.BufferSize).ConfigureAwait(false);
                mUndecodedBytesRead += lBytesRead;
                if (lBytesRead == 0) mStreamReadToEnd = true;
                mDecodedBuffer = mDecoder.Decode(mUndecodedBuffer, 0, lBytesRead);
                mDecodedBufferPosition = 0;
                mDecodedBytesProduced += mDecodedBuffer.Length;
            }
        }

        public override int ReadByte()
        {
            ZPositionCheck();

            while (true)
            {
                if (mDecodedBufferPosition < mDecodedBuffer.Length) return mDecodedBuffer[mDecodedBufferPosition++];
                if (mStreamReadToEnd) return -1;
                int lBytesRead = mStream.Read(mUndecodedBuffer, 0, cMailClient.BufferSize);
                mUndecodedBytesRead += lBytesRead;
                if (lBytesRead == 0) mStreamReadToEnd = true;
                mDecodedBuffer = mDecoder.Decode(mUndecodedBuffer, 0, lBytesRead);
                mDecodedBufferPosition = 0;
                mDecodedBytesProduced += mDecodedBuffer.Length;
            }
        }

        public long GetBufferedBytes()
        {
            // for reporting progress in terms of input bytes (take the input stream position, subtract off the buffered bytes, report that against the input stream length)
            if (mDecodedBytesProduced == 0) return 0;
            var lBufferedUndecodedBytes = mDecoder.GetBufferedInputBytes();
            var lBufferedDecodedBytes = mDecodedBuffer.Length - mDecodedBufferPosition;
            var lEstimatedUndecodedBytesInBufferedDecodedBytes = lBufferedDecodedBytes * (mUndecodedBytesRead - lBufferedUndecodedBytes) / mDecodedBytesProduced;
            return lBufferedUndecodedBytes + lEstimatedUndecodedBytesInBufferedDecodedBytes;
        }

        internal static long GetDecodedLength(Stream pStream, cDecoder pDecoder)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pDecoder == null) throw new ArgumentNullException(nameof(pDecoder));
            if (pStream.CanSeek && pStream.Position != 0) pStream.Position = 0;

            byte[] lUndecodedBuffer = new byte[cMailClient.BufferSize];
            long lDecodedLength = 0;

            while (true)
            {
                int lBytesRead = pStream.Read(lUndecodedBuffer, 0, cMailClient.BufferSize);
                lDecodedLength += pDecoder.GetDecodedLength(lUndecodedBuffer, 0, lBytesRead);
                if (lBytesRead == 0) return lDecodedLength;
            }
        }

        internal static async Task<long> GetDecodedLengthAsync(Stream pStream, cDecoder pDecoder)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pDecoder == null) throw new ArgumentNullException(nameof(pDecoder));
            if (pStream.CanSeek && pStream.Position != 0) pStream.Position = 0;

            byte[] lUndecodedBuffer = new byte[cMailClient.BufferSize];
            long lDecodedLength = 0;

            while (true)
            {
                int lBytesRead = await pStream.ReadAsync(lUndecodedBuffer, 0, cMailClient.BufferSize).ConfigureAwait(false);
                lDecodedLength += pDecoder.GetDecodedLength(lUndecodedBuffer, 0, lBytesRead);
                if (lBytesRead == 0) return lDecodedLength;
            }
        }

        private void ZReadInit(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
            ZPositionCheck();
        }

        private void ZPositionCheck()
        {
            if (!mStream.CanSeek) return;

            if (mStream.Position == mUndecodedBytesRead) return;

            if (mUndecodedBytesRead == 0)
            {
                mStream.Position = 0;
                return;
            }

            throw new cStreamPositionException();
        }

        private int ZReadFromDecodedBuffer(byte[] pBuffer, int pOffset, int pCount)
        {
            int lBytesRead = 0;

            while (pCount > 0 && mDecodedBufferPosition < mDecodedBuffer.Length)
            {
                pBuffer[pOffset++] = mDecodedBuffer[mDecodedBufferPosition++];
                pCount--;
                lBytesRead++;
            }

            return lBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}