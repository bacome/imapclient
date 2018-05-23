using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal sealed class cSectionCacheItemReader : iSectionReader, IDisposable
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

        public long Length => mStream.Length;

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

        public int ReadByte() => mStream.ReadByte();

        public void Dispose()
        {
            if (mDisposed) return;

            if (mStream != null)
            {
                try { mStream.Dispose(); }
                catch { }
            }

            if (Interlocked.Decrement(ref mCount) == 0) mDecrementOpenStreamCount(mContextToUseWhenDisposing);

            mDisposed = true;
        }
    }
}