using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal sealed class cSectionReaderWriter : iSectionReader, iSectionWriter, IDisposable
    {
        private enum eWritingState { notstarted, inprogress, completedok, failed }

        private bool mDisposed = false;

        private readonly Stream mStream;
        private readonly iSectionAdder mAdder;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(1, 1);
        private readonly cReleaser mReleaser;

        // read
        private long mReadPosition = 0;

        // write
        private eWritingState mWritingState = eWritingState.notstarted;
        private cDecoder mDecoder = null;
        private int mBufferedEncodedByteCount = 0;
        private long mWritePosition = 0;
        private Exception mWriteException = null;
        private long mEncodedBytesWrittenToStream = 0;

        // read/write
        private long mReadPositionInEncodedBytes = 0;

        internal cSectionReaderWriter(Stream pStream, iSectionAdder pAdder)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            mAdder = pAdder ?? throw new ArgumentNullException(nameof(pAdder));
            if (!pStream.CanRead || !pStream.CanSeek || !pStream.CanWrite || pStream.Position != 0) throw new ArgumentOutOfRangeException(nameof(pStream));
            mReleaser = new cReleaser(nameof(cSectionReaderWriter), mCancellationTokenSource.Token);
        }

        public bool LengthIsKnown => mWritingState == eWritingState.completedok;

        public async Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(GetLengthAsync), pMC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));

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
                if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));
                return mReadPosition;
            }
        }

        public async Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(SetReadPositionAsync), pReadPosition);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));

            if (pReadPosition < 0) throw new ArgumentOutOfRangeException(nameof(pReadPosition));

            if (pReadPosition == 0)
            {
                mReadPosition = 0;
                ZSetReadPositionInEncodedBytes();
                return;
            }

            while (true)
            {
                Task lTask = ZGetAwaitWriteTask(lContext);

                if (mStream.Length > pReadPosition)
                {
                    mReadPosition = pReadPosition;

                    await mSemaphore.WaitAsync().ConfigureAwait(false);
                    ZSetReadPositionInEncodedBytes();
                    mSemaphore.Release();

                    return;
                }

                if (mWritingState == eWritingState.completedok)
                {
                    if (pReadPosition > mStream.Length) throw new ArgumentOutOfRangeException(nameof(pReadPosition));
                    mReadPosition = pReadPosition;
                    ZSetReadPositionInEncodedBytes();
                    return;
                }

                await lTask.ConfigureAwait(false);
            }
        }

        public bool WritingHasCompletedOK => (mWritingState == eWritingState.completedok);

        public long ReadPositionInEncodedBytes => mReadPositionInEncodedBytes;

        public async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(ReadAsync), pOffset, pCount, pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));

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

                mStream.Position = mReadPosition;
                var lBytesRead = await mStream.ReadAsync(pBuffer, pOffset, pCount, lMC.CancellationToken).ConfigureAwait(false);
                mReadPosition = mStream.Position;
                ZSetReadPositionInEncodedBytes();

                return lBytesRead;
            }
            finally { mSemaphore.Release(); }
        }

        public async Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(ReadByteAsync), pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));

            var lMC = new cMethodControl(pTimeout);

            await ZWaitForDataToReadAsync(lMC, lContext).ConfigureAwait(false);
            await mSemaphore.WaitAsync(lMC.Timeout).ConfigureAwait(false);

            try
            {
                lContext.TraceVerbose("reading byte");

                if (mStream.CanTimeout) mStream.ReadTimeout = lMC.Timeout;

                mStream.Position = mReadPosition;
                var lResult = mStream.ReadByte();
                mReadPosition = mStream.Position;
                ZSetReadPositionInEncodedBytes();

                return lResult;
            }
            finally { mSemaphore.Release(); }
        }

        private void ZSetReadPositionInEncodedBytes()
        {
            if (mReadPosition == 0) mReadPositionInEncodedBytes = 0;
            else mReadPositionInEncodedBytes = mEncodedBytesWrittenToStream - mStream.Length + mReadPosition;
        }

        private async Task ZWaitForDataToReadAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(ZWaitForDataToReadAsync), pMC);

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
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(ZGetAwaitWriteTask));
            if (mWritingState == eWritingState.failed) throw new IOException(nameof(cSectionReaderWriter), mWriteException);
            return mReleaser.GetAwaitReleaseTask(lContext);
        }

        public void InstallDecoder(bool pBinary, eDecodingRequired pDecoding, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(InstallDecoder), pBinary, pDecoding);
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));
            if (mWritingState != eWritingState.notstarted) throw new InvalidOperationException();

            if (pBinary || pDecoding == eDecodingRequired.none) mDecoder = null;
            else if (pDecoding == eDecodingRequired.base64) mDecoder = new cBase64Decoder();
            else if (pDecoding == eDecodingRequired.quotedprintable) mDecoder = new cQuotedPrintableDecoder();
            else throw new cContentTransferDecodingNotSupportedException(pDecoding);
        }

        public void WriteBegin(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(WriteBegin));
            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));
            if (mWritingState != eWritingState.notstarted) throw new InvalidOperationException();
            if (mStream.Position != 0) throw new cUnexpectedPersistentCacheActionException(lContext);
            mWritingState = eWritingState.inprogress;
        }

        public async Task WriteAsync(IList<byte> pEncodedBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(WriteAsync), pOffset);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));
            if (mWritingState != eWritingState.inprogress) throw new InvalidOperationException();

            if (pEncodedBytes == null) throw new ArgumentNullException(nameof(pEncodedBytes));
            if (pOffset > pEncodedBytes.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pOffset == pEncodedBytes.Count) return;

            byte[] lDecodedBytes;
            int lEncodedBytesInDecodedBytes;

            if (mDecoder == null)
            {
                lDecodedBytes = new byte[pEncodedBytes.Count - pOffset];
                for (int i = 0; i < lDecodedBytes.Length; i++) lDecodedBytes[i] = pEncodedBytes[pOffset + i];
                lEncodedBytesInDecodedBytes = lDecodedBytes.Length;
            }
            else
            {
                var lCount = pEncodedBytes.Count - pOffset;

                lDecodedBytes = mDecoder.Decode(pEncodedBytes, pOffset, lCount);

                int lBufferedEncodedByteCount = mDecoder.GetBufferedInputByteCount();
                lEncodedBytesInDecodedBytes = lCount - lBufferedEncodedByteCount + mBufferedEncodedByteCount;
                mBufferedEncodedByteCount = lBufferedEncodedByteCount;
            }

            await mSemaphore.WaitAsync(pCancellationToken).ConfigureAwait(false);

            try
            {
                lContext.TraceVerbose("writing");

                if (lDecodedBytes.Length > 0)
                {
                    mStream.Position = mWritePosition;
                    await mStream.WriteAsync(lDecodedBytes, 0, lDecodedBytes.Length, pCancellationToken).ConfigureAwait(false);
                    mWritePosition = mStream.Position;
                }

                mEncodedBytesWrittenToStream += lEncodedBytesInDecodedBytes;
            }
            finally { mSemaphore.Release(); }

            // let any pending read know that there is more data to consider
            mReleaser.ReleaseReset(lContext);
        }

        public void WritingFailed(Exception pException, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(WritingFailed));

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));

            mWriteException = pException;
            mWritingState = eWritingState.failed;

            // let any pending read know that there is no more data to consider
            mReleaser.Release(lContext);
        }

        public async Task WritingCompletedOKAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionReaderWriter), nameof(WritingCompletedOKAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionReaderWriter));
            if (mWritingState != eWritingState.inprogress) throw new InvalidOperationException();

            await mStream.FlushAsync(pCancellationToken).ConfigureAwait(false);

            mWritingState = eWritingState.completedok;

            mAdder.Add(lContext);

            // let any pending read know that there is no more data to consider
            mReleaser.Release(lContext);
        }

        public void Dispose()
        {
            if (mDisposed) return;

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

            if (mSemaphore != null)
            {
                try { mSemaphore.Dispose(); }
                catch { }
            }

            if (mStream != null)
            {
                try { mStream.Dispose(); }
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