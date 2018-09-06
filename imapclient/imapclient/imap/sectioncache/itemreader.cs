using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal sealed class cSectionCacheItemReader : Stream, iSectionReader
    {
        private bool mDisposed = false;
        private readonly Stream mStream;
        private readonly Action<cTrace.cContext> mDecrementOpenStreamCount;
        private readonly cTrace.cContext mContextToUseWhenDisposing;
        private int mCount = 1;

        public cSectionCacheItemReader(Stream pStream, Action<cTrace.cContext> pDecrementOpenStreamCount, cTrace.cContext pContextToUseWhenDisposing)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead || !mStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));
            mDecrementOpenStreamCount = pDecrementOpenStreamCount ?? throw new ArgumentNullException(nameof(pDecrementOpenStreamCount));
            mContextToUseWhenDisposing = pContextToUseWhenDisposing;
        }

        // stream

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanTimeout => mStream.CanTimeout;

        public override bool CanWrite => false;

        public override long Length => mStream.Length;

        public override long Position
        {
            get => mStream.Position;
            set => mStream.Position = value;
        }

        public override int ReadTimeout
        {
            get => mStream.ReadTimeout;
            set => mStream.ReadTimeout = value;
        }

        public override void Flush() => mStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => mStream.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] pBuffer, int pOffset, int pCount) => mStream.Read(pBuffer, pOffset, pCount);

        public override Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken) => mStream.ReadAsync(pBuffer, pOffset, pCount, pCancellationToken);

        public override int ReadByte() => mStream.ReadByte();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        // isectionreader

        public long ReadPosition
        {
            get => mStream.Position;
            set => mStream.Position = value;
        }

        public Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReader), nameof(ReadAsync), pOffset, pCount, pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReader));

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
            if (pCount == 0) return Task.FromResult(0);

            if (mStream.CanTimeout) mStream.ReadTimeout = pTimeout;
            return mStream.ReadAsync(pBuffer, pOffset, pCount, pCancellationToken);
        }

        // dispose (Stream)

        protected override void Dispose(bool pDisposing)
        {
            if (mDisposed) return;

            if (pDisposing)
            {
                if (mStream != null)
                {
                    try { mStream.Dispose(); }
                    catch { }
                }

                if (Interlocked.Decrement(ref mCount) == 0) mDecrementOpenStreamCount(mContextToUseWhenDisposing);
            }

            mDisposed = true;

            base.Dispose(pDisposing);
        }
    }
}