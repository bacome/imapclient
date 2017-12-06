using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public sealed class cMessageStream : Stream
    {
        private bool mDisposed = false;

        private readonly cMessage mMessage;
        private readonly cMailbox mMailbox;
        private readonly cUID mUID;

        private readonly cSection mSection;
        private readonly eDecodingRequired mDecoding;
        private readonly int mMaxBufferSize;

        private int mReadTimeout = Timeout.Infinite;

        // background fetch task
        private CancellationTokenSource mCancellationTokenSource = null;
        private Task mFetchTask = null;
        private cBuffer mBuffer = null;

        public cMessageStream(cMessage pMessage, cSection pSection, eDecodingRequired pDecoding, int pMaxBufferSize = 1000000)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));

            if (!ReferenceEquals(pMessage.MessageHandle.MessageCache.MailboxHandle, pMessage.Client.SelectedMailboxDetails.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);
            if (pMaxBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pMaxBufferSize));

            mMessage = pMessage;
            mMailbox = null;
            mUID = null;

            mSection = pSection;
            mDecoding = pDecoding;
            mMaxBufferSize = pMaxBufferSize;
        }

        public cMessageStream(cMailbox pMailbox, cUID pUID, cSection pSection, eDecodingRequired pDecoding, int pMaxBufferSize = 1000000)
        {
            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));

            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);
            if (pMaxBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pMaxBufferSize));

            mMessage = null;
            mMailbox = pMailbox;
            mUID = pUID;

            mSection = pSection;
            mDecoding = pDecoding;
            mMaxBufferSize = pMaxBufferSize;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    
        public override int ReadTimeout
        {
            get => mReadTimeout;

            set
            {
                if (value < Timeout.Infinite) throw new ArgumentOutOfRangeException();
                mReadTimeout = value;
            }
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount <= 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
            if (mDisposed) throw new ObjectDisposedException(nameof(cMessageStream));

            if (mFetchTask == null) mFetchTask = ZFetch();

            return mBuffer.Read(pBuffer, pOffset, pCount, mReadTimeout);
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private async Task ZFetch()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mBuffer = new cBuffer(mMaxBufferSize);
            cBodyFetchConfiguration lConfiguration = new cBodyFetchConfiguration(mCancellationTokenSource.Token, null);

            Exception lException = null;

            try
            {
                if (mMessage == null) await mMailbox.UIDFetchAsync(mUID, mSection, mDecoding, mBuffer, lConfiguration).ConfigureAwait(false);
                else await mMessage.FetchAsync(mSection, mDecoding, mBuffer, lConfiguration).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                lException = e;
            }

            mBuffer.Complete(lException);
        }

        public new void Dispose()
        {
            if (mDisposed) return;

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Cancel(); }
                catch { }
            }

            if (mFetchTask != null)
            {
                try { mFetchTask.Wait(); }
                catch { }
                mFetchTask.Dispose();
            }

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }

            if (mBuffer != null) mBuffer.Dispose();

            base.Dispose();

            mDisposed = true;
        }

        private sealed class cBuffer : Stream
        {
            private static readonly byte[] kEndOfStream = new byte[0];

            private bool mDisposed = false;
            private readonly int mMaxSize;
            private readonly SemaphoreSlim mReadSemaphore = new SemaphoreSlim(0);
            private readonly SemaphoreSlim mWriteSemaphore = new SemaphoreSlim(0);
            private readonly object mLock = new object();
            private readonly Queue<byte[]> mBuffers = new Queue<byte[]>();
            private byte[] mCurrentBuffer = null;
            private int mCurrentBufferPosition = 0;
            private int mTotalBufferSize = 0;
            private bool mComplete = false;
            private Exception mCompleteException = null;

            public cBuffer(int pMaxSize)
            {
                if (pMaxSize < 1) throw new ArgumentOutOfRangeException(nameof(pMaxSize));
                mMaxSize = pMaxSize;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanTimeout => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override void Flush() => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override int Read(byte[] pBuffer, int pOffset, int pCount) => throw new NotSupportedException();

            public int Read(byte[] pBuffer, int pOffset, int pCount, int pTimeout)
            {
                if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
                if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                if (pCount <= 0) throw new ArgumentOutOfRangeException(nameof(pCount));
                if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
                if (mDisposed) throw new ObjectDisposedException(nameof(cMessageStream));

                while (true)
                {
                    if (mCurrentBuffer == null)
                    {
                        lock (mLock)
                        {
                            if (mBuffers.Count > 0) mCurrentBuffer = mBuffers.Dequeue();
                            mCurrentBufferPosition = 0;
                        }
                    }

                    if (ReferenceEquals(mCurrentBuffer, kEndOfStream))
                    {
                        if (mCompleteException != null) throw new IOException("fetch failed", mCompleteException);
                        else return 0;
                    }

                    if (mCurrentBuffer != null)
                    {
                        int lPosition = pOffset;

                        while (lPosition < pBuffer.Length && mCurrentBufferPosition < mCurrentBuffer.Length)
                        {
                            pBuffer[lPosition++] = mCurrentBuffer[mCurrentBufferPosition++];
                        }

                        int lBytesRead = lPosition - pOffset;

                        lock (mLock)
                        {
                            mTotalBufferSize -= lBytesRead;
                        }

                        if (mCurrentBufferPosition == mCurrentBuffer.Length) mCurrentBuffer = null;

                        if (mWriteSemaphore.CurrentCount == 0) mWriteSemaphore.Release();

                        return lBytesRead;
                    }

                    if (!mReadSemaphore.Wait(pTimeout)) throw new TimeoutException();
                }
            }

            public override void Write(byte[] pBuffer, int pOffset, int pCount)
            {
                if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
                if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                if (pCount <= 0) throw new ArgumentOutOfRangeException(nameof(pCount));
                if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
                if (mDisposed) throw new ObjectDisposedException(nameof(cMessageStream));
                if (mComplete) throw new InvalidOperationException();

                byte[] lBuffer = new byte[pCount];
                for (int i = 0, j = pOffset; i < pCount; i++, j++) lBuffer[i] = pBuffer[j];

                lock (mLock)
                {
                    mBuffers.Enqueue(lBuffer);
                    mTotalBufferSize += pCount;
                }

                if (mReadSemaphore.CurrentCount == 0) mReadSemaphore.Release();

                while (mTotalBufferSize > mMaxSize) mWriteSemaphore.Wait();
            }

            public void Complete(Exception pException)
            {
                if (mComplete) throw new InvalidOperationException();
                mComplete = true;
                mCompleteException = pException;
                mBuffers.Enqueue(kEndOfStream);
                if (mReadSemaphore.CurrentCount == 0) mReadSemaphore.Release();
            }

            public new void Dispose()
            {
                if (mDisposed) return;

                if (mReadSemaphore != null)
                {
                    try { mReadSemaphore.Dispose(); }
                    catch { }
                }

                if (mWriteSemaphore != null)
                {
                    try { mWriteSemaphore.Dispose(); }
                    catch { }
                }

                base.Dispose();

                mDisposed = true;
            }
        }
    }
}