using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMessageDataStream : Stream
    {
        internal const int DefaultTargetBufferSize = 100000;

        private bool mDisposed = false;

        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;
        public readonly int TargetBufferSize;

        // for streams that may be appended via a mailmessage: the length is required 
        public readonly bool HasKnownLength;

        private uint mLength;
        private int mReadTimeout = Timeout.Infinite;

        // background fetch task
        private CancellationTokenSource mCancellationTokenSource = null;
        private Task mFetchTask = null;
        private cBuffer mBuffer = null;

        public cMessageDataStream(cMessage pMessage, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);

            MailboxHandle = null;
            UID = null;

            Section = cSection.All;
            Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            HasKnownLength = true;
            mLength = MessageHandle.Size ?? 0;
        }

        public cMessageDataStream(cAttachment pAttachment, bool pDecoded = true, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pAttachment == null) throw new ArgumentNullException(nameof(pAttachment));

            Client = pAttachment.Client;
            MessageHandle = pAttachment.MessageHandle;

            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pAttachment), kArgumentOutOfRangeExceptionMessage.AttachmentMustBeInTheSelectedMailbox);

            MailboxHandle = null;
            UID = null;

            Section = pAttachment.Part.Section;

            if (pDecoded) Decoding = pAttachment.Part.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            if (Decoding == eDecodingRequired.none)
            {
                HasKnownLength = true;
                mLength = pAttachment.Part.SizeInBytes;
            }
            else
            {
                HasKnownLength = false;
                mLength = 0;
            }
        }

        public cMessageDataStream(cMessage pMessage, cSinglePartBody pPart, bool pDecoded = true, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);

            MailboxHandle = null;
            UID = null;

            if (pPart == null) throw new ArgumentNullException(nameof(pPart));

            Section = pPart.Section;

            if (pDecoded) Decoding = pPart.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            if (Decoding == eDecodingRequired.none)
            {
                HasKnownLength = true;
                mLength = pPart.SizeInBytes;
            }
            else
            {
                HasKnownLength = false;
                mLength = 0;
            }
        }

        public cMessageDataStream(cMessage pMessage, cSection pSection, eDecodingRequired pDecoding, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            if (!ReferenceEquals(MessageHandle.MessageCache.MailboxHandle, Client.SelectedMailboxDetails?.MailboxHandle)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.MessageMustBeInTheSelectedMailbox);

            MailboxHandle = null;
            UID = null;

            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            HasKnownLength = false;
            mLength = 0;
        }

        public cMessageDataStream(cMailbox pMailbox, cUID pUID, cSection pSection, uint pLength, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MessageHandle = null;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            HasKnownLength = true;

            if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            mLength = pLength;
        }

        public cMessageDataStream(cMailbox pMailbox, cUID pUID, cSection pSection, eDecodingRequired pDecoding, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MessageHandle = null;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            HasKnownLength = false;
            mLength = 0;
        }

        internal cMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, int pTargetBufferSize)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            MailboxHandle = null;
            UID = null;
            Section = pSection;
            Decoding = eDecodingRequired.none;
            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;
            HasKnownLength = false;
            mLength = 0;
        }

        internal cMessageDataStream(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, int pTargetBufferSize)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = null;
            MailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection;
            Decoding = eDecodingRequired.none;
            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;
            HasKnownLength = false;
            mLength = 0;
        }

        public uint GetKnownLength()
        {
            if (!HasKnownLength) throw new InvalidOperationException();

            if (mLength > 0) return mLength;

            if (MessageHandle != null && Section == cSection.All && Decoding == eDecodingRequired.none)
            {
                if (!Client.Fetch(MessageHandle, fMessageCacheAttributes.size))
                {
                    if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                    throw new cRequestedDataNotReturnedException(MessageHandle);
                }

                return MessageHandle.Size.Value;
            }

            throw new cInternalErrorException();
        }

        public int CurrentBufferSize => mBuffer?.CurrentSize ?? 0;

        public override bool CanRead => !mDisposed;

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
            if (pCount == 0) return 0;
            ZReadInit(pBuffer, pOffset, pCount);
            return mBuffer.Read(pBuffer, pOffset, pCount, mReadTimeout);
        }

        public override async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            if (pCount == 0) return 0;
            ZReadInit(pBuffer, pOffset, pCount);
            return await mBuffer.ReadAsync(pBuffer, pOffset, pCount, mReadTimeout, pCancellationToken);
        }

        private void ZReadInit(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
            if (mDisposed) throw new ObjectDisposedException(nameof(cMessageDataStream));

            if (mFetchTask == null) mFetchTask = ZFetch();
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private async Task ZFetch()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mBuffer = new cBuffer(TargetBufferSize, mCancellationTokenSource.Token);
            cBodyFetchConfiguration lConfiguration = new cBodyFetchConfiguration(mCancellationTokenSource.Token, null);

            Exception lException = null;

            try
            {
                if (MessageHandle == null) await Client.UIDFetchAsync(MailboxHandle, UID, Section, Decoding, mBuffer, lConfiguration).ConfigureAwait(false);
                else await Client.FetchAsync(MessageHandle, Section, Decoding, mBuffer, lConfiguration).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                lException = e;
            }

            mBuffer.Complete(lException);
        }

        protected override void Dispose(bool pDisposing)
        {
            if (mDisposed) return;

            if (pDisposing)
            {
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
            }

            mDisposed = true;

            base.Dispose(pDisposing);
        }

        public override string ToString() => $"{nameof(cMessageDataStream)}({MessageHandle},{MailboxHandle},{UID},{Section},{Decoding},{mLength})";

        private sealed class cBuffer : Stream
        {
            private static readonly byte[] kEndOfStream = new byte[0];

            private bool mDisposed = false;
            private readonly int mTargetSize;
            private readonly CancellationToken mCancellationToken;
            private readonly SemaphoreSlim mReadSemaphore = new SemaphoreSlim(0);
            private readonly SemaphoreSlim mWriteSemaphore = new SemaphoreSlim(0);
            private readonly object mLock = new object();
            private readonly Queue<byte[]> mBuffers = new Queue<byte[]>();
            private byte[] mCurrentBuffer = null;
            private int mCurrentBufferPosition = 0;
            private int mCurrentSize = 0;
            private bool mComplete = false;
            private Exception mCompleteException = null;

            public cBuffer(int pTargetSize, CancellationToken pCancellationToken)
            {
                if (pTargetSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetSize));
                mTargetSize = pTargetSize;
                mCancellationToken = pCancellationToken;
            }

            public int CurrentSize => mCurrentSize;

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
                while (true)
                {
                    if (ZTryRead(pBuffer, pOffset, pCount, out var lBytesRead)) return lBytesRead;
                    try { if (!mReadSemaphore.Wait(pTimeout, mCancellationToken)) throw new TimeoutException(); }
                    catch (OperationCanceledException) { throw new IOException($"{nameof(cMessageDataStream)} closed"); }
                }
            }

            public async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken)
            {
                using (var lCTS = CancellationTokenSource.CreateLinkedTokenSource(pCancellationToken, mCancellationToken))
                {
                    while (true)
                    {
                        if (ZTryRead(pBuffer, pOffset, pCount, out var lBytesRead)) return lBytesRead;
                        try { if (!await mReadSemaphore.WaitAsync(pTimeout, lCTS.Token)) throw new TimeoutException(); }
                        catch (OperationCanceledException) { throw new IOException($"{nameof(cMessageDataStream)} closed"); }
                    }
                }
            }

            private bool ZTryRead(byte[] pBuffer, int pOffset, int pCount, out int rBytesRead)
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
                    else
                    {
                        rBytesRead = 0;
                        return true;
                    }
                }

                if (mCurrentBuffer != null)
                {
                    int lPosition = pOffset;

                    while (lPosition < pOffset + pCount && mCurrentBufferPosition < mCurrentBuffer.Length)
                    {
                        pBuffer[lPosition++] = mCurrentBuffer[mCurrentBufferPosition++];
                    }

                    rBytesRead = lPosition - pOffset;

                    lock (mLock)
                    {
                        mCurrentSize -= rBytesRead;
                    }

                    if (mCurrentBufferPosition == mCurrentBuffer.Length) mCurrentBuffer = null;

                    if (mWriteSemaphore.CurrentCount == 0) mWriteSemaphore.Release();

                    return true;
                }

                rBytesRead = 0;
                return false;
            }

            public override void Write(byte[] pBuffer, int pOffset, int pCount)
            {
                if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
                if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
                if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
                if (mDisposed) throw new ObjectDisposedException(nameof(cMessageDataStream));
                if (mComplete) throw new InvalidOperationException();

                if (pCount == 0) return;

                byte[] lBuffer = new byte[pCount];
                for (int i = 0, j = pOffset; i < pCount; i++, j++) lBuffer[i] = pBuffer[j];

                lock (mLock)
                {
                    mBuffers.Enqueue(lBuffer);
                    mCurrentSize += pCount;
                }

                if (mReadSemaphore.CurrentCount == 0) mReadSemaphore.Release();

                while (mCurrentSize > mTargetSize)
                {
                    try { mWriteSemaphore.Wait(mCancellationToken); }
                    catch (OperationCanceledException) { throw new IOException($"{nameof(cMessageDataStream)} closed"); }
                }
            }

            public void Complete(Exception pException)
            {
                if (mComplete) throw new InvalidOperationException();
                mComplete = true;
                mCompleteException = pException;
                mBuffers.Enqueue(kEndOfStream);
                if (mReadSemaphore.CurrentCount == 0) mReadSemaphore.Release();
            }

            protected override void Dispose(bool pDisposing)
            {
                if (mDisposed) return;

                if (pDisposing)
                {
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
                }

                mDisposed = true;

                base.Dispose(pDisposing);
            }
        }
    }
}