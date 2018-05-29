using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.mailclient.support
{
    public abstract class cEncodingStream : Stream
    {
        private readonly Stream mStream;
        private readonly cEncoder mEncoder;
        private readonly byte[] mUnencodedBuffer = new byte[cMailClient.BufferSize];
        private bool mStreamReadToEnd = false;
        private byte[] mEncodedBuffer = cMailClient.ZeroLengthBuffer;
        private int mEncodedBufferPosition = 0;

        private long mUnencodedBytesRead = 0;
        private long mEncodedBytesProduced = 0;

        internal cEncodingStream(Stream pStream, cEncoder pEncoder)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            mEncoder = pEncoder ?? throw new ArgumentNullException(nameof(pEncoder));
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
                int lBytesRead = ZReadFromEncodedBuffer(pBuffer, pOffset, pCount);
                if (lBytesRead != 0 || mStreamReadToEnd) return lBytesRead;
                lBytesRead = mStream.Read(mUnencodedBuffer, 0, cMailClient.BufferSize);
                mUnencodedBytesRead += lBytesRead;
                if (lBytesRead == 0) mStreamReadToEnd = true;
                mEncodedBuffer = mEncoder.Encode(mUnencodedBuffer, lBytesRead);
                mEncodedBufferPosition = 0;
                mEncodedBytesProduced += mEncodedBuffer.Length;
            }
        }

        public override async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            while (true)
            {
                int lBytesRead = ZReadFromEncodedBuffer(pBuffer, pOffset, pCount);
                if (lBytesRead != 0 || mStreamReadToEnd) return lBytesRead;
                lBytesRead = await mStream.ReadAsync(mUnencodedBuffer, 0, cMailClient.BufferSize).ConfigureAwait(false);
                mUnencodedBytesRead += lBytesRead;
                if (lBytesRead == 0) mStreamReadToEnd = true;
                mEncodedBuffer = mEncoder.Encode(mUnencodedBuffer, lBytesRead);
                mEncodedBufferPosition = 0;
                mEncodedBytesProduced += mEncodedBuffer.Length;
            }
        }

        public override int ReadByte()
        {
            ZPositionCheck();

            while (true)
            {
                if (mEncodedBufferPosition < mEncodedBuffer.Length) return mEncodedBuffer[mEncodedBufferPosition++];
                if (mStreamReadToEnd) return -1;
                int lBytesRead = mStream.Read(mUnencodedBuffer, 0, cMailClient.BufferSize);
                mUnencodedBytesRead += lBytesRead;
                if (lBytesRead == 0) mStreamReadToEnd = true;
                mEncodedBuffer = mEncoder.Encode(mUnencodedBuffer, lBytesRead);
                mEncodedBufferPosition = 0;
                mEncodedBytesProduced += mEncodedBuffer.Length;
            }
        }

        public long GetBufferedBytes()
        {
            // for reporting progress in terms of input bytes (take the input stream position, subtract off the buffered bytes, report that against the input stream length)
            if (mEncodedBytesProduced == 0) return 0;
            var lBufferedUnencodedBytes = mEncoder.GetBufferedInputBytes();
            var lBufferedEncodedBytes = mEncodedBuffer.Length - mEncodedBufferPosition;
            var lEstimatedUnencodedBytesInBufferedEncodedBytes = lBufferedEncodedBytes * (mUnencodedBytesRead - lBufferedUnencodedBytes) / mEncodedBytesProduced;
            return lBufferedUnencodedBytes + lEstimatedUnencodedBytesInBufferedEncodedBytes;
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

            if (mStream.Position == mUnencodedBytesRead) return;

            if (mUnencodedBytesRead == 0)
            {
                mStream.Position = 0;
                return;
            }

            throw new cStreamPositionException();
        }

        private int ZReadFromEncodedBuffer(byte[] pBuffer, int pOffset, int pCount)
        {
            int lBytesRead = 0;

            while (pCount > 0 && mEncodedBufferPosition < mEncodedBuffer.Length)
            {
                pBuffer[pOffset++] = mEncodedBuffer[mEncodedBufferPosition++];
                pCount--;
                lBytesRead++;
            }

            return lBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}