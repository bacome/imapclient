using System;
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

        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly bool DecodedIfRequired;

        private bool? mDecoded;

        private readonly cSectionHandle mSectionHandle;
        private readonly bool mUIDNotSticky;

        private cSinglePartBody mPart;
        private bool mToTrySetPart;
        private cSectionId mSectionId;

        private int mReadTimeout;

        private long mLength = -1;

        private readonly object mMessageDataReaderLock = new object();
        private iSectionReader mMessageDataReader = null; // this will be one of the two following
        private cSectionReader mSectionReader = null;
        private cSectionReaderWriter mSectionReaderWriter = null;

        // background fetch task
        private CancellationTokenSource mBackgroundCancellationTokenSource = null;
        private Task mBackgroundTask = null;   
        
        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart, bool pDecodedIfRequired)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));

            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            if (pMessageHandle.MessageCache.IsInvalid) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            mPart = pPart ?? throw new ArgumentNullException(nameof(pPart));

            MailboxHandle = null;
            UID = null;
            Section = pPart.Section;
            DecodedIfRequired = pDecodedIfRequired;

            mDecoded = pDecodedIfRequired && pPart.DecodingRequired != eDecodingRequired.none;

            mSectionHandle = new cSectionHandle(pMessageHandle, pPart.Section, mDecoded.Value);
            mUIDNotSticky = pMessageHandle.MessageCache.UIDNotSticky;

            mToTrySetPart = false;
            mSectionId = null;

            mReadTimeout = Client.Timeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));

            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            if (pMessageHandle.MessageCache.IsInvalid) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            MailboxHandle = null;
            UID = null;
            DecodedIfRequired = false;

            mDecoded = false;

            mSectionHandle = new cSectionHandle(pMessageHandle, pSection, false);
            mUIDNotSticky = pMessageHandle.MessageCache.UIDNotSticky;

            mPart = null;
            mToTrySetPart = pSection.CouldDescribeASinglePartBody;
            mSectionId = null;

            mReadTimeout = Client.Timeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, bool pDecodedIfRequired)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));

            MailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
            if (!ReferenceEquals(pClient.MailboxCache, pMailboxHandle.MailboxCache)) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            MessageHandle = null;
            DecodedIfRequired = pDecodedIfRequired;

            if (pDecodedIfRequired && pSection.CouldDescribeASinglePartBody)
            {
                mDecoded = null;
                mSectionId = null;
            }
            else
            {
                mDecoded = false;
                mSectionId = new cSectionId(new cMessageUID(pMailboxHandle.MailboxId, pUID, mUIDNotSticky, Client.UTF8Enabled), pSection, false);
            }

            mSectionHandle = null;
            mUIDNotSticky = pMailboxHandle.SelectedProperties.UIDNotSticky.Value;

            mPart = null;
            mToTrySetPart = pSection.CouldDescribeASinglePartBody;

            mReadTimeout = Client.Timeout;
        }

        public override bool CanRead => !mDisposed;

        public override bool CanSeek => !mDisposed;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (mLength >= 0) return mLength;
                var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(Length));
                var lTask = ZGetLengthAsync(cMethodControl.None, lContext);
                Client.Wait(lTask, lContext);
                mLength = lTask.Result;
                return mLength;
            }
        }

        public async Task<long> GetLengthAsync()
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
            if (mLength >= 0) return mLength;
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));
            var lMC = new cMethodControl(mReadTimeout);
            mLength = await ZGetLengthAsync(lMC, lContext).ConfigureAwait(false);
            return mLength;
        }

        public async Task<long> GetLengthAsync(CancellationToken pCancellationToken)
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
            if (mLength >= 0) return mLength;
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));
            var lMC = new cMethodControl(mReadTimeout, pCancellationToken);
            mLength = await ZGetLengthAsync(lMC, lContext).ConfigureAwait(false);
            return mLength;
        }

        private async Task<long> ZGetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync), pMC);

            if (mMessageDataReader != null) return await mMessageDataReader.GetLengthAsync(pMC, lContext).ConfigureAwait(false);

            if (ZTryGetIdealSectionReader(lContext)) return mSectionReader.Length;

            if (Section == cSection.All) 
            {
                if (Client.MessageSizesAreReliable)
                {
                    if (mSectionHandle == null) return await Client.GetMessageSizeInBytesAsync(pMC, MailboxHandle, mSectionId.MessageUID, lContext).ConfigureAwait(false);
                    else return await Client.GetMessageSizeInBytesAsync(pMC, MessageHandle, lContext).ConfigureAwait(false);
                }
                else return await LGetLengthFromSectionReaderWriterAsync();
            }

            if (mToTrySetPart) await ZTrySetPart(pMC, lContext).ConfigureAwait(false);

            if (mPart == null) return await LGetLengthFromSectionReaderWriterAsync();

            if (mDecoded == true)
            {
                uint? lDecodedSizeInBytes;

                if (mSectionHandle == null) lDecodedSizeInBytes = await Client.GetDecodedSizeInBytesAsync(pMC, MailboxHandle, mSectionId.MessageUID, mPart, lContext).ConfigureAwait(false);
                else lDecodedSizeInBytes = await Client.GetDecodedSizeInBytesAsync(pMC, MessageHandle, mPart, lContext).ConfigureAwait(false);

                if (lDecodedSizeInBytes == null) return await LGetLengthFromSectionReaderWriterAsync();

                return lDecodedSizeInBytes.Value;                
            }

            if (mPart is cMessageBodyPart && !Client.MessageSizesAreReliable) return await LGetLengthFromSectionReaderWriterAsync();

            return mPart.SizeInBytes;

            async Task<long> LGetLengthFromSectionReaderWriterAsync()
            {
                ZGetSectionReaderWriter(lContext);
                return await mMessageDataReader.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
            }
        }

        public override long Position
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (mMessageDataReader == null) return 0;
                return mMessageDataReader.ReadPosition;
            }

            set
            {
                var lContext = Client.RootContext.NewSetProp(nameof(cIMAPMessageDataStream), nameof(Position), value);

                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (value < 0) throw new ArgumentOutOfRangeException();

                ZSetPosition(value, lContext);
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
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(Seek), pOffset, pOrigin);

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

            ZSetPosition(lPosition, lContext);

            return lPosition;
        }

        private void ZSetPosition(long pPosition, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZSetPosition), pPosition);
            if (pPosition == 0 && mMessageDataReader == null) return;
            if (mMessageDataReader == null) ZGetMessageDataReader(lContext);
            Client.Wait(mMessageDataReader.SetReadPositionAsync(pPosition, lContext), lContext);
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

        private Task<int> ZReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZReadAsync), pCount);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();

            if (mMessageDataReader == null) ZGetMessageDataReader(lContext);

            return mMessageDataReader.ReadAsync(pBuffer, pOffset, pCount, mReadTimeout, pCancellationToken, lContext);
        }

        public override int ReadByte()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ReadByte));
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
            if (mMessageDataReader == null) ZGetMessageDataReader(lContext);
            var lTask = mMessageDataReader.ReadByteAsync(mReadTimeout, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public cScale GetScale()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetScale));
            var lTask = ZGetScaleAsync(cMethodControl.None, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cScale> GetScaleAsync()
        {
            var lContext = Client.RootContext.NewMethodV(nameof(cIMAPMessageDataStream), nameof(GetScaleAsync), 1);
            var lMC = new cMethodControl(mReadTimeout);
            return ZGetScaleAsync(lMC, lContext);
        }

        public Task<cScale> GetScaleAsync(CancellationToken pCancellationToken)
        {
            var lContext = Client.RootContext.NewMethodV(nameof(cIMAPMessageDataStream), nameof(GetScaleAsync), 2);
            var lMC = new cMethodControl(mReadTimeout, pCancellationToken);
            return ZGetScaleAsync(lMC, lContext);
        }

        internal Task<cScale> GetScaleAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetScaleAsync), pMC);
            return ZGetScaleAsync(pMC, lContext);
        }

        private async Task<cScale> ZGetScaleAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZGetScaleAsync), pMC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (mMessageDataReader == null) ZTryGetIdealSectionReader(lContext);

            if (mMessageDataReader != null && mMessageDataReader.LengthIsKnown) return new cScale(mMessageDataReader.Length, false);

            if (Section == cSection.All)
            {
                uint lMessageSizeInBytes;

                if (mSectionHandle == null) lMessageSizeInBytes = await Client.GetMessageSizeInBytesAsync(pMC, MailboxHandle, mSectionId.MessageUID, lContext).ConfigureAwait(false);
                else lMessageSizeInBytes = await Client.GetMessageSizeInBytesAsync(pMC, MessageHandle, lContext).ConfigureAwait(false);

                return new cScale(lMessageSizeInBytes, false);
            }

            if (mToTrySetPart) await ZTrySetPart(pMC, lContext).ConfigureAwait(false);

            if (mPart == null) return null;

            if (mDecoded == true)
            {
                uint? lDecodedSizeInBytes;

                if (mSectionHandle == null) lDecodedSizeInBytes = await Client.GetDecodedSizeInBytesAsync(pMC, MailboxHandle, mSectionId.MessageUID, mPart, lContext).ConfigureAwait(false);
                else lDecodedSizeInBytes = await Client.GetDecodedSizeInBytesAsync(pMC, MessageHandle, mPart, lContext).ConfigureAwait(false);

                if (lDecodedSizeInBytes != null) return new cScale(lDecodedSizeInBytes.Value, false);

                return new cScale(mPart.SizeInBytes, true);
            }

            return new cScale(mPart.SizeInBytes, false);
        }

        public long GetPositionOnScale(cScale pScale)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetPositionOnScale), pScale);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (pScale == null) throw new ArgumentNullException(nameof(pScale));

            if (mMessageDataReader == null) return 0;

            var lReadPosition = mMessageDataReader.ReadPosition;

            if (lReadPosition == 0) return 0;

            if (mMessageDataReader.LengthIsKnown)
            {
                if (lReadPosition == mMessageDataReader.Length) return pScale.Value;
                return lReadPosition * pScale.Value / mMessageDataReader.Length;
            }

            if (mSectionReaderWriter.IsDecoding && pScale.ValueIsInEncodedBytes) return Math.Min(mSectionReaderWriter.ReadPositionInInputBytes, pScale.Value);

            return Math.Min(lReadPosition, pScale.Value);
        }

        private async Task ZTrySetPart(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZTrySetPart), pMC);

            if (mPart != null || !mToTrySetPart) throw new InvalidOperationException();

            if (mSectionHandle == null)
            {
                mPart = await Client.GetSinglePartBodyAsync(pMC, MailboxHandle, UID, Section, lContext).ConfigureAwait(false);

                if (DecodedIfRequired)
                {
                    if (mPart == null) mDecoded = false;
                    else mDecoded = mPart.DecodingRequired != eDecodingRequired.none;
                    mSectionId = new cSectionId(new cMessageUID(MailboxHandle.MailboxId, UID, mUIDNotSticky, Client.UTF8Enabled), Section, mDecoded.Value);
                }
            }
            else mPart = await Client.GetSinglePartBodyAsync(pMC, MessageHandle, Section, lContext).ConfigureAwait(false);

            mToTrySetPart = false;
        }

        private void ZGetMessageDataReader(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZGetMessageDataReader));
            if (mMessageDataReader != null) throw new InvalidOperationException();
            if (ZTryGetIdealSectionReader(lContext)) return;
            ZGetSectionReaderWriter(lContext);
        }

        private bool ZTryGetIdealSectionReader(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZTryGetIdealSectionReader));

            lock (mMessageDataReaderLock)
            {
                if (mMessageDataReader != null) throw new InvalidOperationException();

                if (mSectionHandle == null)
                {
                    if (Client.PersistentCache.TryGetSectionReader(mSectionId, out mSectionReader, lContext))
                    {
                        mMessageDataReader = mSectionReader;
                        return true;
                    }
                }
                else
                {
                    if (Client.PersistentCache.TryGetSectionReader(mSectionHandle, out mSectionReader, lContext))
                    {
                        mMessageDataReader = mSectionReader;
                        return true;
                    }
                }
            }

            return false;
        }

        private void ZGetSectionReaderWriter(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZGetSectionReaderWriter));

            lock (mMessageDataReader)
            {
                if (mMessageDataReader != null) throw new InvalidOperationException();

                eDecodingRequired lDecoding;
                if (mDecoded == true) lDecoding = mPart.DecodingRequired;
                else lDecoding = eDecodingRequired.none;

                mBackgroundCancellationTokenSource = new CancellationTokenSource();
                var lCancellationToken = mBackgroundCancellationTokenSource.Token;

                if (mSectionHandle == null)
                {
                    mSectionReaderWriter = Client.PersistentCache.GetSectionReaderWriter(mSectionId, lContext);
                    if (mSectionId.Decoded && Client.PersistentCache.TryGetSectionReader(new cSectionId(mSectionId.MessageUID, Section, false), out mSectionReader, lContext)) mBackgroundTask = ZBackgroundDecodeAsync(lDecoding, lCancellationToken, lContext);
                    else mBackgroundTask = Client.UIDFetchSectionAsync(MailboxHandle, UID, Section, lDecoding, mSectionReaderWriter, lCancellationToken, lContext);
                }
                else
                {
                    mSectionReaderWriter = Client.PersistentCache.GetSectionReaderWriter(mSectionHandle, lContext);
                    if (mSectionHandle.Decoded && Client.PersistentCache.TryGetSectionReader(new cSectionHandle(MessageHandle, Section, false), out mSectionReader, lContext)) mBackgroundTask = ZBackgroundDecodeAsync(lDecoding, lCancellationToken, lContext);
                    else mBackgroundTask = Client.FetchSectionAsync(MessageHandle, Section, lDecoding, mSectionReaderWriter, lCancellationToken, lContext);
                }

                mMessageDataReader = mSectionReaderWriter;
            }
        }

        private async Task ZBackgroundDecodeAsync(eDecodingRequired pDecoding, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZBackgroundDecodeAsync), pDecoding);


            var lBuffer = new byte[cMailClient.BufferSize];

            try
            {
                mSectionReaderWriter.WriteBegin(false, pDecoding, lContext);

                while (true)
                {
                    var lBytesRead = await mSectionReader.ReadAsync(lBuffer, 0, lBuffer.Length, Timeout.Infinite, pCancellationToken, lContext).ConfigureAwait(false);
                    if (lBytesRead == 0) break;
                    await mSectionReaderWriter.WriteAsync(lBuffer, 0, lBytesRead, pCancellationToken, lContext).ConfigureAwait(false);
                }

                await mSectionReaderWriter.WritingCompletedOKAsync(pCancellationToken, lContext).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                mSectionReaderWriter.WritingFailed(e, lContext);
                return;
            }
        }

        protected override void Dispose(bool pDisposing)
        {
            if (mDisposed) return;

            if (pDisposing)
            {
                if (mBackgroundCancellationTokenSource != null)
                {
                    try { mBackgroundCancellationTokenSource.Cancel(); }
                    catch { }
                }

                if (mBackgroundTask != null)
                {
                    try { mBackgroundTask.Wait(); }
                    catch { }
                    mBackgroundTask.Dispose();
                }

                if (mBackgroundCancellationTokenSource != null)
                {
                    try { mBackgroundCancellationTokenSource.Dispose(); }
                    catch { }
                }

                if (mSectionReader != null)
                {
                    try { mSectionReader.Dispose(); }
                    catch { }
                }

                if (mSectionReaderWriter != null)
                {
                    try { mSectionReaderWriter.Dispose(); }
                    catch { }
                }
            }

            mDisposed = true;

            base.Dispose(pDisposing);
        }

        public override string ToString() => $"{nameof(cIMAPMessageDataStream)}({MessageHandle},{MailboxHandle},{UID},{Section},{DecodedIfRequired})";

        public class cScale
        {
            public readonly long Value;
            internal readonly bool ValueIsInEncodedBytes;

            internal cScale(long pValue, bool pValueIsInEncodedBytes)
            {
                Value = pValue;
                ValueIsInEncodedBytes = pValueIsInEncodedBytes;
            }

            public override string ToString() => $"{nameof(cScale)}({Value},{ValueIsInEncodedBytes})";
        }
    }
}