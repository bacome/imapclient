using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal interface iSectionReader
    {
        bool LengthIsKnown { get; }
        long Length { get; }
        Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext);
        long ReadPosition { get; }
        Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext);
        Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
        Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext);
    }

    internal sealed class cSectionReader : iSectionReader, IDisposable
    {
        private bool mDisposed = false;
        private readonly Stream mStream;

        internal cSectionReader(Stream pStream)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead || !mStream.CanSeek || mStream.CanWrite || mStream.Position != 0) throw new ArgumentOutOfRangeException(nameof(pStream));
        }

        public bool LengthIsKnown => true;

        public long Length => mStream.Length;

        public Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReader));
            return Task.FromResult(mStream.Length);
        }

        public long ReadPosition
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReader));
                return mStream.Position;
            }
        }

        public Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReader), nameof(SetReadPositionAsync), pReadPosition);
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReader));
            if (pReadPosition < 0 || pReadPosition > mStream.Length) throw new ArgumentOutOfRangeException(nameof(pReadPosition));
            mStream.Position = pReadPosition;
            return Task.WhenAll(); // TODO => Task.CompletedTask;
        }

        public Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReader), nameof(ReadAsync), pOffset, pCount, pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReader));

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
            if (pCount == 0) return Task.FromResult(0);

            if (mStream.CanTimeout) mStream.ReadTimeout = pTimeout;
            return mStream.ReadAsync(pBuffer, pOffset, pCount, pCancellationToken);
        }

        public Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReader), nameof(ReadByteAsync), pTimeout);
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReader));
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

            mDisposed = true;
        }
    }
}