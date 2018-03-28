using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cIMAPMessageDataStream : Stream
    {
        internal const int DefaultTargetBufferSize = 100000;

        private bool mDisposed = false;

        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cSinglePartBody Part;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;
        public readonly int TargetBufferSize;

        private readonly bool mHasKnownFormatAndLength;
        private fMessageDataFormat? mFormat;
        private uint? mLength;

        private int mReadTimeout = Timeout.Infinite;

        // background fetch task
        private CancellationTokenSource mCancellationTokenSource = null;
        private Task mFetchTask = null;
        private cBuffer mBuffer = null;

        public cIMAPMessageDataStream(cIMAPMessage pMessage, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            Part = null;

            MailboxHandle = null;
            UID = null;

            Section = cSection.All;
            Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            mHasKnownFormatAndLength = true;

            if (MessageHandle.BodyStructure != null) mFormat = (Client.SupportedFormats & fMessageDataFormat.utf8headers) | MessageHandle.BodyStructure.Format;
            else mFormat = null;

            mLength = MessageHandle.Size;
        }

        public cIMAPMessageDataStream(cIMAPAttachment pAttachment, bool pDecoded = true, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pAttachment == null) throw new ArgumentNullException(nameof(pAttachment));
            if (!pAttachment.IsValid()) throw new ArgumentOutOfRangeException(nameof(pAttachment), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pAttachment.Client;
            MessageHandle = pAttachment.MessageHandle;
            Part = pAttachment.Part;

            MailboxHandle = null;
            UID = null;

            Section = pAttachment.Part.Section;

            if (pDecoded) Decoding = pAttachment.Part.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            if (Decoding == eDecodingRequired.none)
            {
                mHasKnownFormatAndLength = true;
                mFormat = pAttachment.Part.Format;
                mLength = pAttachment.Part.SizeInBytes;
            }
            else
            {
                mHasKnownFormatAndLength = false;
                mFormat = null;
                mLength = null;
            }
        }

        public cIMAPMessageDataStream(cIMAPMessage pMessage, cSinglePartBody pPart, bool pDecoded = true, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            MailboxHandle = null;
            UID = null;

            Section = pPart.Section;

            if (pDecoded) Decoding = pPart.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            if (Decoding == eDecodingRequired.none)
            {
                mHasKnownFormatAndLength = true;
                mFormat = pPart.Format;
                mLength = pPart.SizeInBytes;
            }
            else
            {
                mHasKnownFormatAndLength = false;
                mFormat = null;
                mLength = null;
            }
        }

        public cIMAPMessageDataStream(cIMAPMessage pMessage, cSection pSection, eDecodingRequired pDecoding, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            Part = null;

            MailboxHandle = null;
            UID = null;

            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            if (pSection == cSection.All && pDecoding == eDecodingRequired.none)
            {
                mHasKnownFormatAndLength = true;

                if (MessageHandle.BodyStructure != null) mFormat = (Client.SupportedFormats & fMessageDataFormat.utf8headers) | MessageHandle.BodyStructure.Format;
                else mFormat = null;

                mLength = MessageHandle.Size;
            }
            else
            {
                mHasKnownFormatAndLength = false;
                mFormat = null;
                mLength = null;
            }
        }

        public cIMAPMessageDataStream(cMailbox pMailbox, cUID pUID, cSection pSection, fMessageDataFormat pFormat, uint pLength, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            // note that this API if the format and/or length is wrong could lead to bad things

            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MessageHandle = null;
            Part = null;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = eDecodingRequired.none;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            mHasKnownFormatAndLength = true;

            mFormat = pFormat;

            if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            mLength = pLength;
        }

        public cIMAPMessageDataStream(cMailbox pMailbox, cUID pUID, cSection pSection, eDecodingRequired pDecoding, int pTargetBufferSize = DefaultTargetBufferSize)
        {
            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MessageHandle = null;
            Part = null;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;

            mHasKnownFormatAndLength = false;
            mFormat = null;
            mLength = null;
        }

        internal cIMAPMessageDataStream(cIMAPMessageDataStream pStream)
        {
            Client = pStream.Client;
            MessageHandle = pStream.MessageHandle;
            Part = pStream.Part;
            MailboxHandle = pStream.MailboxHandle;
            UID = pStream.UID;
            Section = pStream.Section;
            Decoding = pStream.Decoding;
            TargetBufferSize = pStream.TargetBufferSize;
            mHasKnownFormatAndLength = pStream.mHasKnownFormatAndLength;
            mFormat = pStream.mFormat;
            mLength = pStream.mLength;
            mReadTimeout = pStream.mReadTimeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, int pTargetBufferSize)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            Part = null;
            MailboxHandle = null;
            UID = null;
            Section = pSection;
            Decoding = eDecodingRequired.none;
            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;
            mHasKnownFormatAndLength = false;
            mFormat = null;
            mLength = null;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, int pTargetBufferSize)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = null;
            Part = null;
            MailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection;
            Decoding = eDecodingRequired.none;
            if (pTargetBufferSize < 1) throw new ArgumentOutOfRangeException(nameof(pTargetBufferSize));
            TargetBufferSize = pTargetBufferSize;
            mHasKnownFormatAndLength = false;
            mFormat = null;
            mLength = null;
        }

        internal void GetKnownFormatAndLength()
        {
            if (!mHasKnownFormatAndLength) throw new InvalidOperationException();

            if (mFormat != null && mLength != null ) return;

            if (MessageHandle != null && Section == cSection.All && Decoding == eDecodingRequired.none)
            {
                if (!Client.Fetch(MessageHandle, fMessageCacheAttributes.size | fMessageCacheAttributes.bodystructure))
                {
                    if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                    throw new cRequestedIMAPDataNotReturnedException(MessageHandle);
                }

                mFormat = (Client.SupportedFormats & fMessageDataFormat.utf8headers) | MessageHandle.BodyStructure.Format;
                mLength = MessageHandle.Size;
            }

            throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(GetKnownFormatAndLength));
        }

        internal async Task GetKnownFormatAndLengthAsync()
        {
            if (!mHasKnownFormatAndLength) throw new InvalidOperationException();

            if (mFormat != null && mLength != null) return;

            if (MessageHandle != null && Section == cSection.All && Decoding == eDecodingRequired.none)
            {
                if (!await Client.FetchAsync(MessageHandle, fMessageCacheAttributes.size | fMessageCacheAttributes.bodystructure).ConfigureAwait(false))
                {
                    if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                    throw new cRequestedIMAPDataNotReturnedException(MessageHandle);
                }

                mFormat = (Client.SupportedFormats & fMessageDataFormat.utf8headers) | MessageHandle.BodyStructure.Format;
                mLength = MessageHandle.Size;
            }

            throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(GetKnownFormatAndLengthAsync));
        }

        public bool HasKnownFormatAndLength => mHasKnownFormatAndLength;

        public fMessageDataFormat KnownFormat
        {
            get
            {
                GetKnownFormatAndLength();
                return mFormat.Value;
            }
        }

        public uint KnownLength
        {
            get
            {
                GetKnownFormatAndLength();
                return mLength.Value;
            }
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
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (mFetchTask == null) mFetchTask = ZFetch();
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private async Task ZFetch()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mBuffer = new cBuffer(TargetBufferSize, mCancellationTokenSource.Token);
            cFetchConfiguration lConfiguration = new cFetchConfiguration(mCancellationTokenSource.Token, null);

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

        public override string ToString() => $"{nameof(cIMAPMessageDataStream)}({MessageHandle},{MailboxHandle},{UID},{Section},{Decoding},{mLength})";

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
                    catch (OperationCanceledException) { throw new IOException($"{nameof(cIMAPMessageDataStream)} closed"); }
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
                        catch (OperationCanceledException) { throw new IOException($"{nameof(cIMAPMessageDataStream)} closed"); }
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
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
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
                    catch (OperationCanceledException) { throw new IOException($"{nameof(cIMAPMessageDataStream)} closed"); }
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