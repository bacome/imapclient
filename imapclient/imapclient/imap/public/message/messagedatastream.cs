using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cIMAPMessageDataStream : Stream
    {
        private bool mDisposed = false;

        private readonly object mGetReadStreamLock = new object();

        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cSinglePartBody Part;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;
        private readonly string mFileName;
        private readonly Stream mStream;

        private int mReadTimeout = Timeout.Infinite;
        private string mTempFileName = null;
        private Stream mFileStream = null;

        private Stream mReadStream = null; // will be either the mStream or the mFileStream
        private bool mReadStreamComplete;

        // build temp file task
        private CancellationTokenSource mCancellationTokenSource = null;
        private cReleaser mBuildTempFileReleaser = null;
        private Task mBuildTempFileTask = null;
        private cTrace.cContext mBuildTempFileFromFetchContext = null;

        public cIMAPMessageDataStream(cIMAPMessage pMessage)
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

            mFileName = null;
            mStream = null;
        }

        public cIMAPMessageDataStream(cIMAPAttachment pAttachment, bool pDecoded = true)
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

            mFileName = null;
            mStream = null;
        }

        public cIMAPMessageDataStream(cIMAPMessage pMessage, cSinglePartBody pPart, bool pDecoded = true)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);
            pMessage.CheckPart(pPart);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            Part = pPart;

            MailboxHandle = null;
            UID = null;

            Section = pPart.Section;

            if (pDecoded) Decoding = pPart.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            mFileName = null;
            mStream = null;
        }

        public cIMAPMessageDataStream(cIMAPMessage pMessage, cSection pSection, eDecodingRequired pDecoding)
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

            mFileName = null;
            mStream = null;
        }

        public cIMAPMessageDataStream(cMailbox pMailbox, cUID pUID, cSection pSection, eDecodingRequired pDecoding)
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

            mFileName = null;
            mStream = null;
        }

        public cIMAPMessageDataStream(cMailbox pMailbox, cUID pUID, cSection pSection, eDecodingRequired pDecoding, string pFileName)
        {
            // the file must contain the decoded data

            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MessageHandle = null;
            Part = null;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            mFileName = pFileName;
            mStream = null;
        }

        public cIMAPMessageDataStream(cMailbox pMailbox, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream)
        {
            // the stream must contain the decoded data

            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MessageHandle = null;
            Part = null;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            mFileName = null;
            mStream = pStream;
        }

        public override bool CanRead => !mDisposed;

        public override bool CanSeek => !mDisposed;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(Length));
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                var lTask = GetLengthAsync(lContext);
                Client.Wait(lTask, lContext);
                return lTask.Result;
            }
        }

        public override long Position
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (mReadStream == null) return 0;
                return mReadStream.Position;
            }

            set
            {
                var lContext = Client.RootContext.NewSetProp(nameof(cIMAPMessageDataStream), nameof(Position), value);

                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

                if (value < 0) throw new ArgumentOutOfRangeException();

                if (value == 0)
                {
                    if (mReadStream != null) mReadStream.Position = 0;
                    return;
                }

                ZSetReadStream(lContext);

                if (mReadStreamComplete || mReadStream.Length >= value)
                {
                    mReadStream.Position = value;
                    return;
                }

                var lTask = ZSetPositionAsync(value, lContext);
                Client.Wait(lTask, lContext);
            }
        }
    
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

        public override long Seek(long pOffset, SeekOrigin pOrigin)
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            long lPosition;

            switch (pOrigin)
            {
                case SeekOrigin.Begin:

                    lPosition = pOffset;
                    break;

                case SeekOrigin.Current:

                    lPosition = Position + pOffset;
                    break;

                case SeekOrigin.End:

                    lPosition = Length - pOffset;
                    break;

                default:

                    throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(Seek));
            }

            if (lPosition < 0) throw new ArgumentException();

            Position = lPosition;

            return lPosition;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] pBuffer, int pOffset, int pCount)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(Read), pCount);
            var lTask = ZReadAsync(pBuffer, pOffset, pCount, CancellationToken.None, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        public override Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ReadAsync), pCount);
            return ZReadAsync(pBuffer, pOffset, pCount, pCancellationToken, lContext);
        }

        private async Task<int> ZReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZReadAsync), pCount);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();

            ZSetReadStream(lContext);
            
            long lRequiredLength = mReadStream.Position + pCount;

            while (true)
            {
                Task lTask = ZGetAwaitReadTask(lContext); 
                if (mReadStreamComplete || mReadStream.Length >= lRequiredLength) return await mReadStream.ReadAsync(pBuffer, pOffset, pCount, pCancellationToken).ConfigureAwait(false);
                if (lTask == null) throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(ZReadAsync));
                await lTask.ConfigureAwait(false);
            }
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        internal async Task<long> GetLengthAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (mFileName == null && (mStream == null || !mStream.CanSeek) && (mReadStream == null || !mReadStreamComplete))
            {
                // if we can, use IMAP features to get the length

                if (MessageHandle != null)
                {
                    if (Part == null)
                    {
                        if (Section == cSection.All && Decoding == eDecodingRequired.none)
                        {
                            // special case, the whole message

                            if (!(await Client.FetchAsync(MessageHandle, cMessageCacheItems.Size).ConfigureAwait(false)))
                            {
                                if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                                throw new cRequestedIMAPDataNotReturnedException(MessageHandle);
                            }

                            return MessageHandle.Size.Value;
                        }
                    }
                    else
                    {
                        var lDecodedSizeInBytes = await Client.DecodedSizeInBytesAsync(MessageHandle, Part).ConfigureAwait(false);
                        if (lDecodedSizeInBytes != null) return lDecodedSizeInBytes.Value;
                    }
                }
            }

            // measure the length by looking at the data

            ZSetReadStream(lContext);
            
            while (true)
            {
                Task lTask = ZGetAwaitReadTask(lContext);
                if (mReadStreamComplete) return mReadStream.Length;
                if (lTask == null) throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));
                await lTask.ConfigureAwait(false);
            }
        }

        private void ZSetReadStream(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZSetReadStream));

            lock (mGetReadStreamLock)
            {
                if (mReadStream != null) return;

                if (mFileName != null)
                {
                    mFileStream = new FileStream(mFileName, FileMode.Open, FileAccess.Read);
                    mReadStream = mFileStream;
                    mReadStreamComplete = true;
                    return;
                }

                if (mStream != null)
                {
                    if (mStream.CanSeek)
                    {
                        mReadStream = mStream;
                        mReadStreamComplete = true;
                        return;
                    }

                    // copy the stream

                    mTempFileName = Path.GetTempFileName();
                    mFileStream = new FileStream(mTempFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    mReadStream = mFileStream;
                    mReadStreamComplete = false;

                    mCancellationTokenSource = new CancellationTokenSource();
                    mBuildTempFileReleaser = new cReleaser("cIMAPMessageDataStream", mCancellationTokenSource.Token);
                    mBuildTempFileTask = ZBuildTempFileFromStreamAsync(lContext);

                    return;
                }

                // fetch the data from the IMAP server

                mTempFileName = Path.GetTempFileName();
                mFileStream = new FileStream(mTempFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                mReadStream = mFileStream;
                mReadStreamComplete = false;

                mCancellationTokenSource = new CancellationTokenSource();
                mBuildTempFileReleaser = new cReleaser("cIMAPMessageDataStream", mCancellationTokenSource.Token);
                mBuildTempFileTask = ZBuildTempFileFromFetchAsync(lContext);
            }
        }

        private Task ZGetAwaitReadTask(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZGetAwaitReadTask));
            if (mReadStreamComplete) return null;
            if (mBuildTempFileReleaser == null) throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(ZGetAwaitReadTask));
            return mBuildTempFileReleaser.GetAwaitReleaseTask(lContext);
        }

        private async Task ZBuildTempFileFromStreamAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZBuildTempFileFromStreamAsync));

            using (var lWriteStream = new FileStream(mTempFileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                cBatchSizer lReadSizer = new cBatchSizer(Client.LocalStreamReadConfiguration);

                byte[] lBuffer = null;
                Stopwatch lStopwatch = new Stopwatch();

                while (true)
                {
                    // read some data

                    int lCurrent = lReadSizer.Current;
                    if (lBuffer == null || lCurrent > lBuffer.Length) lBuffer = new byte[lCurrent];

                    lStopwatch.Restart();

                    int lBytesReadIntoBuffer = await mStream.ReadAsync(lBuffer, 0, lCurrent, mCancellationTokenSource.Token).ConfigureAwait(false);

                    lStopwatch.Stop();

                    if (lBytesReadIntoBuffer == 0) break;

                    lReadSizer.AddSample(lBytesReadIntoBuffer, lStopwatch.ElapsedMilliseconds);

                    // write to the tempfile
                    await lWriteStream.WriteAsync(lBuffer, 0, lBytesReadIntoBuffer, mCancellationTokenSource.Token).ConfigureAwait(false);

                    // notify any observers
                    mBuildTempFileReleaser.ReleaseReset(lContext);
                }
            }

            // done
            mReadStreamComplete = true;
            mBuildTempFileReleaser.Release(lContext);
        }

        private async Task ZBuildTempFileFromFetchAsync(cTrace.cContext pParentContext)
        {
            mBuildTempFileFromFetchContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZBuildTempFileFromFetchAsync));

            using (var lWriteStream = new FileStream(mTempFileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                cFetchConfiguration lConfiguration = new cFetchConfiguration(mCancellationTokenSource.Token, ZBuildTempFileFromFetchIncrement);
                if (MessageHandle == null) await Client.UIDFetchAsync(MailboxHandle, UID, Section, Decoding, lWriteStream, lConfiguration).ConfigureAwait(false);
                else await Client.FetchAsync(MessageHandle, Section, Decoding, lWriteStream, lConfiguration).ConfigureAwait(false);
            }

            // done
            mReadStreamComplete = true;
            mBuildTempFileReleaser.Release(mBuildTempFileFromFetchContext);
        }

        private void ZBuildTempFileFromFetchIncrement(int pSize)
        {
            var lContext = mBuildTempFileFromFetchContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZBuildTempFileFromFetchIncrement));
            mBuildTempFileReleaser.ReleaseReset(lContext);
        }

        private async Task ZSetPositionAsync(long pPosition, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZSetPositionAsync), pPosition);
            
            while (true)
            {
                Task lTask = ZGetAwaitReadTask(lContext);

                if (mReadStreamComplete || mReadStream.Length >= pPosition)
                {
                    mReadStream.Position = pPosition;
                    return;
                }

                if (lTask == null) throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(ZSetPositionAsync));

                await lTask.ConfigureAwait(false);
            }
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

                if (mBuildTempFileTask != null)
                {
                    try { mBuildTempFileTask.Wait(); }
                    catch { }
                    mBuildTempFileTask.Dispose();
                }

                if (mBuildTempFileReleaser != null)
                {
                    try { mBuildTempFileReleaser.Dispose(); }
                    catch { }
                }

                if (mFileStream != null)
                {
                    try { mFileStream.Dispose(); }
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
            }

            mDisposed = true;

            base.Dispose(pDisposing);
        }

        public override string ToString() => $"{nameof(cIMAPMessageDataStream)}({MessageHandle},{MailboxHandle},{UID},{Section},{Decoding},{mFileName})";
    }
}