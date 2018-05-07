using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal interface iSectionCacheItemReader
    {
        Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext);
        long ReadPosition { get; }
        Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext);
        Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
        Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext);
    }

    internal sealed class cSectionCacheItemReader : iSectionCacheItemReader, IDisposable
    {
        private bool mDisposed = false;
        private readonly cTrace.cContext mContext;
        private readonly Stream mStream;
        private readonly Action<cTrace.cContext> mDecrementOpenStreamCount;
        private int mCount = 1;

        public cSectionCacheItemReader(Stream pStream, Action<cTrace.cContext> pDecrementOpenStreamCount, cTrace.cContext pParentContext)
        {
            mContext = pParentContext.NewObject(nameof(cSectionCacheItemReader));
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead || !mStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));
            mDecrementOpenStreamCount = pDecrementOpenStreamCount ?? throw new ArgumentNullException(nameof(pDecrementOpenStreamCount));
        }

        public Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReader), nameof(GetLengthAsync), pMC);
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReader));
            return Task.FromResult(mStream.Length);
        }

        public long ReadPosition
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReader));
                if (mStream == null) return 0;
                return mStream.Position;
            }
        }

        public Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReader), nameof(SetReadPositionAsync), pReadPosition);
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReader));
            if (pReadPosition < 0) throw new ArgumentOutOfRangeException(nameof(pReadPosition));
            if (pReadPosition == 0 && mStream == null) return Task.WhenAll();
            mStream.Position = pReadPosition;
            return Task.WhenAll();
        }

        public async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReader), nameof(ReadAsync), pOffset, pCount, pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReader));

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
            if (pCount == 0) return 0;

            if (mStream.CanTimeout) mStream.ReadTimeout = pTimeout;
            return await mStream.ReadAsync(pBuffer, pOffset, pCount, pCancellationToken).ConfigureAwait(false);
        }

        public Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReader), nameof(ReadByteAsync), pTimeout);
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReader));
            if (mStream.CanTimeout) mStream.ReadTimeout = pTimeout;
            return Task.FromResult(mStream.ReadByte());
        }

        public void Dispose()
        {
            if (mDisposed) return;

            if (mStream != null)
            {
                try { mStream.Dispose(); }
                catch { }
            }

            if (Interlocked.Decrement(ref mCount) == 0) mDecrementOpenStreamCount(mContext);

            mDisposed = true;
        }
    }
}