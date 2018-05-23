using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal sealed class cSectionCacheItemBase64Encoder : iSectionReader, IDisposable
    {
        private enum eEncodingState { notcomplete, completedok, failed }

        private bool mDisposed = false;

        private readonly cSectionCacheItemReader mCacheItemReader;
        private readonly long mLength;
        private readonly string mTempFileName;
        private readonly FileStream mTempFileStream;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(1, 1);
        private readonly cReleaser mReleaser;

        private long mReadPosition = 0;

        private eEncodingState mEncodingState = eEncodingState.notcomplete;
        private long mWritePosition = 0;
        private Exception mEncodingException = null;

        private readonly Task mBackgroundTask;

        public cSectionCacheItemBase64Encoder(cSectionCacheItemReader pCacheItemReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewObject(nameof(cSectionCacheItemBase64Encoder), pCacheItemReader);

            mCacheItemReader = pCacheItemReader ?? throw new ArgumentNullException(nameof(pCacheItemReader));

            var lUnencodedLength = mCacheItemReader.Length;

            long l3s = lUnencodedLength / 3;
            if (lUnencodedLength % 3 != 0) l3s++;

            long l57s = lUnencodedLength / 57;
            if (lUnencodedLength % 57 != 0) l57s++;

            mLength = l3s * 4 + l57s * 2;

            mTempFileName = Path.GetTempFileName();
            mTempFileStream = new FileStream(mTempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            mReleaser = new cReleaser(nameof(cSectionCacheItemBase64Encoder), mCancellationTokenSource.Token);

            mBackgroundTask = ZEncodeAsync(lContext);
        }

        public long Length => mLength;

        public long ReadPosition
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemBase64Encoder));
                return mReadPosition;
            }

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemBase64Encoder));
                if (value < 0 || value > mLength) throw new ArgumentOutOfRangeException();
                mReadPosition = value;
            }
        }

        public async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemBase64Encoder), nameof(ReadAsync), pOffset, pCount, pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemBase64Encoder));

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

                mTempFileStream.Position = mReadPosition;
                var lBytesRead = await mTempFileStream.ReadAsync(pBuffer, pOffset, pCount, lMC.CancellationToken).ConfigureAwait(false);
                mReadPosition = mTempFileStream.Position;

                return lBytesRead;
            }
            finally { mSemaphore.Release(); }
        }

        public async Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemBase64Encoder), nameof(ReadByteAsync), pTimeout);

            if (mDisposed) throw new ObjectDisposedException(nameof(cSectionCacheItemBase64Encoder));

            var lMC = new cMethodControl(pTimeout);

            await ZWaitForDataToReadAsync(lMC, lContext).ConfigureAwait(false);
            await mSemaphore.WaitAsync(lMC.Timeout).ConfigureAwait(false);

            try
            {
                lContext.TraceVerbose("reading byte");

                mTempFileStream.Position = mReadPosition;
                var lResult = mTempFileStream.ReadByte();
                mReadPosition = mTempFileStream.Position;

                return lResult;
            }
            finally { mSemaphore.Release(); }
        }

        private async Task ZWaitForDataToReadAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemBase64Encoder), nameof(ZWaitForDataToReadAsync), pMC);

            if (mTempFileStream.Length <= mReadPosition && mEncodingState != eEncodingState.completedok)
            {
                using (var lAwaiter = new cAwaiter(pMC))
                {
                    while (true)
                    {
                        Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                        if (mEncodingState == eEncodingState.failed) throw new IOException(nameof(cSectionCacheItemBase64Encoder), mEncodingException);
                        if (mTempFileStream.Length > mReadPosition || mEncodingState == eEncodingState.completedok) break;
                        await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task ZEncodeAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCacheItemBase64Encoder), nameof(ZEncodeAsync));

            var lMC = new cMethodControl(mCancellationTokenSource.Token);
            var lBuffer = new byte[cMailClient.BufferSize];
            var lEncoder = new cBase64Encoder(ZBase64EncoderOutputAsync);

            try
            {
                while (true)
                {
                    var lBytesRead = await mCacheItemReader.ReadAsync(lBuffer, 0, cMailClient.BufferSize, lMC.Timeout, lMC.CancellationToken, lContext).ConfigureAwait(false);
                    await lEncoder.EncodeAsync(lMC, lBuffer, lBytesRead, lContext).ConfigureAwait(false);
                    if (lBytesRead == 0) break;
                }

                mCacheItemReader.Dispose();
                mEncodingState = eEncodingState.completedok;
                mReleaser.Release(lContext);
            }
            catch (Exception e)
            {
                lContext.TraceException(e);
                mEncodingException = e;
                mEncodingState = eEncodingState.failed;
                mReleaser.Release(lContext);
            }
        }

        private async Task ZBase64EncoderOutputAsync(cMethodControl pMC, byte[] pBytes, cTrace.cContext pParentContext)
        {
            await mSemaphore.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false);

            try
            {
                mTempFileStream.Position = mWritePosition;
                await mTempFileStream.WriteAsync(pBytes, 0, pBytes.Length, pMC.CancellationToken).ConfigureAwait(false);
                mWritePosition = mTempFileStream.Position;
            }
            finally { mSemaphore.Release(); }

            mReleaser.ReleaseReset(pParentContext);
        }

        public void Dispose()
        {
            if (mDisposed) return;

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Cancel(); }
                catch { }
            }

            if (mBackgroundTask != null)
            {
                try { mBackgroundTask.Wait(); }
                catch { }
                mBackgroundTask.Dispose();
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

            if (mTempFileStream != null)
            {
                try { mTempFileStream.Dispose(); }
                catch { }
            }

            if (mTempFileName != null)
            {
                try { File.Delete(mTempFileName); }
                catch { }
            }

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }

            if (mCacheItemReader != null)
            {
                try { mCacheItemReader.Dispose(); }
                catch { }
            }

            mDisposed = true;
        }

        public override string ToString() => $"{nameof(cSectionCacheItemBase64Encoder)}({mCacheItemReader},{mTempFileName})";


        ;?;
        // TODO : tests
    }
}
