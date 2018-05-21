using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal sealed class cSectionCacheItemReaderWriter : iSectionCacheItemReader, iFetchSectionTarget, IDisposable
    {
        private enum eWritingState { notstarted, inprogress, completedok, failed }

        private bool mDisposed = false;
        private int mCount = 1;

        private readonly Stream mStream;
        private readonly Action<cTrace.cContext> mDecrementOpenStreamCount;
        private readonly cTrace.cContext mContextToUseWhenDisposing;
        private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly cReleaser mReleaser;

        // read
        private long mReadPosition = 0;

        // write
        private eWritingState mWritingState = eWritingState.notstarted;
        private long mWritePosition = 0;
        private Exception mWriteException = null;
        private long mFetchedBytesWritten = 0;

        // read/write
        private long mFetchedBytesReadPosition = 0;

        internal cSectionCacheItemReaderWriter(Stream pStream, Action<cTrace.cContext> pDecrementOpenStreamCount, cTrace.cContext pContextToUseWhenDisposing)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead || !mStream.CanSeek || !mStream.CanWrite || mStream.Position != 0) throw new ArgumentOutOfRangeException(nameof(pStream));
            mDecrementOpenStreamCount = pDecrementOpenStreamCount ?? throw new ArgumentNullException(nameof(pDecrementOpenStreamCount));
            mContextToUseWhenDisposing = pContextToUseWhenDisposing;
            mReleaser = new cReleaser(nameof(cSectionCacheItemReaderWriter), mCancellationTokenSource.Token);
        }

        public async Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(GetLengthAsync), pMC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));

            if (mWritingState == eWritingState.completedok) return mStream.Length;

            using (var lAwaiter = new cAwaiter(pMC))
            {
                while (true)
                {
                    Task lTask = ZGetAwaitWriteTask(lContext);
                    if (mWritingState == eWritingState.completedok) return mStream.Length;
                    await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                }
            }
        }

        public long ReadPosition
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));
                return mReadPosition;
            }
        }

        public async Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(SetReadPositionAsync), pReadPosition);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));

            if (pReadPosition < 0) throw new ArgumentOutOfRangeException(nameof(pReadPosition));

            if (pReadPosition == 0)
            {
                mReadPosition = 0;
                ZSetFetchedBytesReadPosition();
                return;
            }

            while (true)
            {
                Task lTask = ZGetAwaitWriteTask(lContext);

                if (mStream.Length > pReadPosition)
                {
                    mReadPosition = pReadPosition;

                    await mSemaphore.WaitAsync().ConfigureAwait(false);
                    ZSetFetchedBytesReadPosition();
                    mSemaphore.Release();

                    return;
                }

                if (mWritingState == eWritingState.completedok)
                {
                    if (pReadPosition > mStream.Length) throw new ArgumentOutOfRangeException(nameof(pReadPosition));
                    mReadPosition = pReadPosition;
                    ZSetFetchedBytesReadPosition();
                    return;
                }

                await lTask.ConfigureAwait(false);
            }
        }

        public bool WritingHasCompletedOK => (mWritingState == eWritingState.completedok);

        public long FetchedBytesReadPosition => mFetchedBytesReadPosition;

        public async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(ReadAsync), pOffset, pCount, pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
            if (pCount == 0) return 0;

            var lMC = new cMethodControl(pTimeout, pCancellationToken);

            await ZWaitForDataToReadAsync(lMC, lContext).ConfigureAwait(false);
            await mSemaphore.WaitAsync(lMC.Timeout, lMC.CancellationToken).ConfigureAwait(false);

            try
            {
                lContext.TraceVerbose("reading bytes");

                if (mStream.CanTimeout) mStream.ReadTimeout = lMC.Timeout;
                else _ = lMC.Timeout; // check for timeout

                mStream.Position = mReadPosition;
                var lBytesRead = await mStream.ReadAsync(pBuffer, pOffset, pCount, lMC.CancellationToken).ConfigureAwait(false);
                mReadPosition = mStream.Position;
                ZSetFetchedBytesReadPosition();

                return lBytesRead;
            }
            finally { mSemaphore.Release(); }
        }

        public async Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(ReadByteAsync), pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));

            var lMC = new cMethodControl(pTimeout);

            await ZWaitForDataToReadAsync(lMC, lContext).ConfigureAwait(false);
            await mSemaphore.WaitAsync(lMC.Timeout).ConfigureAwait(false);

            try
            {
                lContext.TraceVerbose("reading byte");

                if (mStream.CanTimeout) mStream.ReadTimeout = lMC.Timeout;
                else _ = lMC.Timeout; // check for timeout

                mStream.Position = mReadPosition;
                var lResult = mStream.ReadByte();
                mReadPosition = mStream.Position;
                ZSetFetchedBytesReadPosition();

                return lResult;
            }
            finally { mSemaphore.Release(); }
        }

        private void ZSetFetchedBytesReadPosition()
        {
            if (mReadPosition == 0) mFetchedBytesReadPosition = 0;
            else mFetchedBytesReadPosition = mFetchedBytesWritten - mStream.Length + mReadPosition;
        }

        private async Task ZWaitForDataToReadAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(ZWaitForDataToReadAsync), pMC);

            if (mStream.Length == mReadPosition && mWritingState != eWritingState.completedok)
            {
                using (var lAwaiter = new cAwaiter(pMC))
                {
                    while (true)
                    {
                        Task lTask = ZGetAwaitWriteTask(lContext);
                        if (mStream.Length > mReadPosition || mWritingState == eWritingState.completedok) break;
                        await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                    }
                }
            }
        }

        private Task ZGetAwaitWriteTask(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(ZGetAwaitWriteTask));
            if (mWritingState == eWritingState.failed) throw new IOException(nameof(cSectionCacheItemReaderWriter), mWriteException);
            return mReleaser.GetAwaitReleaseTask(lContext);
        }

        public void WriteBegin(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(WriteBegin));
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));
            if (mWritingState != eWritingState.notstarted) throw new InvalidOperationException();
            if (mStream.Position != 0) throw new cUnexpectedSectionCacheActionException(lContext);
            mWritingState = eWritingState.inprogress;
        }

        public async Task WriteAsync(byte[] pBuffer, int pBytesInBuffer, int pFetchedBytesInBuffer, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(WriteAsync), pBytesInBuffer, pFetchedBytesInBuffer);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));
            if (mWritingState != eWritingState.inprogress) throw new InvalidOperationException();

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pBytesInBuffer < 0 || pBytesInBuffer > pBuffer.Length) throw new ArgumentOutOfRangeException(nameof(pBytesInBuffer));
            if (pFetchedBytesInBuffer < pBytesInBuffer) throw new ArgumentOutOfRangeException(nameof(pFetchedBytesInBuffer));

            await mSemaphore.WaitAsync(pCancellationToken).ConfigureAwait(false);

            try
            {
                lContext.TraceVerbose("writing");

                if (pBytesInBuffer > 0)
                {
                    mStream.Position = mWritePosition;
                    await mStream.WriteAsync(pBuffer, 0, pBytesInBuffer, pCancellationToken).ConfigureAwait(false);
                    mWritePosition = mStream.Position;
                }

                mFetchedBytesWritten += pFetchedBytesInBuffer;
            }
            finally { mSemaphore.Release(); }

            // let any pending read know that there is more data to consider
            mReleaser.ReleaseReset(lContext);
        }

        public void WritingFailed(Exception pException, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(WritingFailed));

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));

            mWriteException = pException;
            mWritingState = eWritingState.failed;

            // let any pending read know that there is no more data to consider
            mReleaser.Release(lContext);
        }

        public async Task WritingCompletedOKAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemReaderWriter), nameof(WritingCompletedOKAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemReaderWriter));
            if (mWritingState != eWritingState.inprogress) throw new InvalidOperationException();

            await mStream.FlushAsync(pCancellationToken).ConfigureAwait(false);

            mWritingState = eWritingState.completedok;

            // let any pending read know that there is no more data to consider
            mReleaser.Release(lContext);
        }

        public void Dispose()
        {
            if (mDisposed) return;

            if (mStream != null)
            {
                try { mStream.Dispose(); }
                catch { }
            }

            if (Interlocked.Decrement(ref mCount) == 0) mDecrementOpenStreamCount(mContextToUseWhenDisposing);

            if (mSemaphore != null)
            {
                try { mSemaphore.Dispose(); }
                catch { }
            }

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Cancel(); }
                catch { }
            }

            if (mReleaser != null)
            {
                try { mReleaser.Dispose(); }
                catch { }
            }

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }

            mDisposed = true;
        }
    }
}