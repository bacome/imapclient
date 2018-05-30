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
        public readonly iMessageHandle MessageHandle; ?????????????; // before getting 
        public readonly cSinglePartBody Part;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        private int mReadTimeout;

        private bool mCommittedToNotEncodingACachedCopy = false; // I become committed if I make a statement about the length
        private iSectionReader mSectionReader = null;
        private cSectionCacheItemReader mCacheItemReader = null;
        private cSectionCacheItem mSectionCacheItem = null;
        private cSectionCacheItemReaderWriter mCacheItemReaderWriter = null;

        // support for progress
        private long? mProgressLength = null;
        private bool mProgressLengthIsFetchSizeInBytes = false;

        // background fetch task
        private CancellationTokenSource mBackgroundCancellationTokenSource = null;
        private Task mBackgroundTask = null;   
        
        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart, bool pDecoded)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            if (!ReferenceEquals(pClient.SelectedMailboxDetails?.MessageCache, pMessageHandle.MessageCache)) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            MailboxHandle = null;
            UID = null;

            Section = pPart.Section;

            if (pDecoded) Decoding = pPart.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            mReadTimeout = Client.Timeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            if (!ReferenceEquals(pClient.SelectedMailboxDetails?.MessageCache, pMessageHandle.MessageCache)) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            Part = null;

            MailboxHandle = null;
            UID = null;

            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            mReadTimeout = Client.Timeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = null;
            Part = null;
            MailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
            if (!ReferenceEquals(pClient.MailboxCache, pMailboxHandle.MailboxCache)) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

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
                var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(Length));
                var lTask = ZGetLengthAsync(cMethodControl.None, lContext);
                Client.Wait(lTask, lContext);
                return lTask.Result;
            }
        }

        public Task<long> GetLengthAsync()
        {
            var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));
            var lMC = new cMethodControl(mReadTimeout);
            return ZGetLengthAsync(lMC, lContext);
        }

        public Task<long> GetLengthAsync(CancellationToken pCancellationToken)
        {
            var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));
            var lMC = new cMethodControl(mReadTimeout, pCancellationToken);
            return ZGetLengthAsync(lMC, lContext);
        }

        private async Task<long> ZGetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (mSectionReader == null)
            {
                // see if the cache knows

                ZGetSectionCacheKey(out var lNonPersistentKey, out _, out _, out var lPersistentKey);

                if (lPersistentKey == null)
                {
                    if (Client.SectionCache.TryGetItemLength(lNonPersistentKey, out var lLength, lContext)) return lLength;
                    ;?; // if part and none and not committed, see if I have it decoded; set the qp or b64 reader, start background conversion and 
                    // if b64 calculate length, set reader, start background converter, return, if qp convert (waiting), set reader, return length
                }
                else
                {
                    if (Client.SectionCache.TryGetItemLength(lPersistentKey, out var lLength, lContext)) return lLength;
                    ;?; // ditto
                }

                // see if IMAP knows

                if (MessageHandle != null)
                {
                    if (Part == null)
                    {
                        if (Section == cSection.All && Decoding == eDecodingRequired.none)
                        {
                            // special case, the whole message
                            await Client.FetchCacheItemsAsync(cMessageHandleList.FromMessageHandle(MessageHandle), cMessageCacheItems.Size, null, lContext).ConfigureAwait(false);

                            if (MessageHandle.Size == null)
                            {
                                if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                                throw new cRequestedIMAPDataNotReturnedException(MessageHandle);
                            }

                            return MessageHandle.Size.Value;
                        }
                    }
                    else
                    {
                        if (Decoding == eDecodingRequired.none)
                        {
                            ;?; // commit to not encoding a local copy
                            return Part.SizeInBytes;
                        }

                        var lDecodedSizeInBytes = await Client.GetDecodedSizeInBytesAsync(MessageHandle, Part, lContext).ConfigureAwait(false);
                        if (lDecodedSizeInBytes != null) return lDecodedSizeInBytes.Value;
                    }
                }

                // have to read it to find out

                ZSetSectionCacheItemReader(lContext);
            }

            ;?; // this would need enhancing - if encodingreader, getlengthasync
            if (mCacheItemReader != null) return mCacheItemReader.Length;
            if (mCacheItemReaderWriter == null) throw new cInternalErrorException(lContext);
            return await mCacheItemReaderWriter.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
        }

        public override long Position
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (mSectionReader == null) return 0;
                return mSectionReader.ReadPosition;
            }

            set
            {
                var lContext = Client.RootContext.NewSetProp(nameof(cIMAPMessageDataStream), nameof(Position), value);

                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (value < 0) throw new ArgumentOutOfRangeException();
                if (value == 0 && mSectionReader == null) return;

                ZSetSectionCacheItemReader(lContext);

                ;?; // this would need enhancing - if b64 setreadpositionasync, if qp just set?
                if (mCacheItemReader != null) mCacheItemReader.ReadPosition = value;
                else if (mCacheItemReaderWriter == null) throw new cInternalErrorException(lContext);
                else Client.Wait(mCacheItemReaderWriter.SetReadPositionAsync(value, lContext), lContext);
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

        public override int ReadByte()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ReadByte));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            ;?; // VERIFY THAT TH
            ZSetSectionCacheItemReader(lContext);

            ;?;
            if (mCacheItemReader != null) return mCacheItemReader.ReadByte();

            if (mCacheItemReaderWriter == null) throw new cInternalErrorException(lContext);

            ;?;
            var lTask = mCacheItemReaderWriter.ReadByteAsync(mReadTimeout, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        private Task<int> ZReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZReadAsync), pCount);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();

            ZSetSectionCacheItemReader(lContext);
            return mSectionReader.ReadAsync(pBuffer, pOffset, pCount, mReadTimeout, pCancellationToken, lContext);
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public long? GetProgressLength()
        {
            var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(GetProgressLength));
            var lTask = GetProgressLengthAsync(cMethodControl.None, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<long?> GetProgressLengthAsync()
        {
            var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(GetProgressLengthAsync));
            var lMC = new cMethodControl(mReadTimeout);
            return GetProgressLengthAsync(lMC, lContext);
        }

        public Task<long?> GetProgressLengthAsync(CancellationToken pCancellationToken)
        {
            var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(GetProgressLengthAsync));
            var lMC = new cMethodControl(mReadTimeout, pCancellationToken);
            return GetProgressLengthAsync(lMC, lContext);
        }

        internal async Task<long?> GetProgressLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetProgressLengthAsync), pMC);
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
            if (mProgressLength != null) return mProgressLength;
            await ZSetProgressLengthAsync(pMC, lContext).ConfigureAwait(false);
            return mProgressLength;
        }

        private async Task ZSetProgressLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZSetProgressLengthAsync), pMC);

            if (mProgressLength != null) return;

            // see if we know the length from the open stream

            if (mCacheItemReader != null)
            {
                mProgressLength = mCacheItemReader.Length;
                return;
            }

            if (mCacheItemReaderWriter != null)
            {
                ;?; // this will wait for the read to be finished
                mProgressLength = await mCacheItemReaderWriter.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
                return;
            }

            ;?; // if b64 reader, get length, if qpreader, getlengthasync

            // see if the cache has the data

            ZGetSectionCacheKey(out var lNonPersistentKey, out _, out _, out var lPersistentKey);

            if (lPersistentKey == null)
            {
                if (Client.SectionCache.TryGetItemLength(lNonPersistentKey, out var lProgressLength, lContext))
                {
                    mProgressLength = lProgressLength;
                    return;
                }

                ;?; // if part and none and not committed, see if I have it decoded; set the qp or b64reader, st ...
            }
            else
            {
                if (Client.SectionCache.TryGetItemLength(lPersistentKey, out var lProgressLength, lContext))
                {
                    mProgressLength = lProgressLength;
                    return;
                }

                ;?; // ditto
            }

            // see if IMAP can be used to find the length

            if (MessageHandle != null)
            {
                if (Part == null)
                {
                    if (Section == cSection.All && Decoding == eDecodingRequired.none)
                    {
                        // special case, the whole message
                        await Client.FetchCacheItemsAsync(cMessageHandleList.FromMessageHandle(MessageHandle), cMessageCacheItems.Size, null, lContext).ConfigureAwait(false);

                        if (MessageHandle.Size == null)
                        {
                            if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                            throw new cRequestedIMAPDataNotReturnedException(MessageHandle);
                        }

                        mProgressLength = MessageHandle.Size.Value;
                        return;
                    }
                }
                else
                {
                    if (Decoding == eDecodingRequired.none)
                    {
                        m
                        ;?; // committed

                        ;?; // and set progress length
                    }
                    else
                    {
                        mProgressLength = await Client.GetDecodedSizeInBytesAsync(MessageHandle, Part, lContext).ConfigureAwait(false);
                        if (mProgressLength != null) return;

                        mProgressLength = await Client.GetFetchSizeInBytesAsync(MessageHandle, Part, lContext).ConfigureAwait(false);
                        mProgressLengthIsFetchSizeInBytes = true;
                    }
                }
            }
        }

        public long GetProgressPosition()
        {
            var lContext = Client.RootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(GetProgressPosition));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (mSectionReader == null || mSectionReader.ReadPosition == 0) return 0;

            if (mProgressLengthIsFetchSizeInBytes)
            {
                if (mCacheItemReaderWriter == null)
                {
                    ;?; // put the others here
                    if (mCacheItemReader == null) throw new cInternalErrorException(lContext);
                    return mProgressLength.Value * mCacheItemReader.ReadPosition / mCacheItemReader.Length;
                }
                else return mCacheItemReaderWriter.FetchedBytesReadPosition;
            }
            else return mSectionReader.ReadPosition;
        }

        private void ZGetSectionCacheKey(out cSectionCacheNonPersistentKey rNonPersistentKey, out iMailboxHandle rMailboxHandle, out cUID rUID, out cSectionCachePersistentKey rPersistentKey)
        {
            if (UID == null && MessageHandle.UID == null)
            {
                rNonPersistentKey = new cSectionCacheNonPersistentKey(Client, MessageHandle, Section, Decoding);
                rMailboxHandle = null;
                rUID = null;
                rPersistentKey = null;
                return;
            }

            rNonPersistentKey = null;

            if (UID == null)
            {
                rMailboxHandle = MessageHandle.MessageCache.MailboxHandle;
                rUID = MessageHandle.UID;
            }
            else
            {
                rMailboxHandle = MailboxHandle;
                rUID = UID;
            }

            rPersistentKey = new cSectionCachePersistentKey(rMailboxHandle, rUID, Section, Decoding);
        }

        private void ZSetSectionCacheItemReader(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZSetSectionCacheItemReader));

            if (mSectionReader != null) return;

            ;?; // VERIFY that the item exists first

            var lSectionCache = Client.SectionCache;
            ZGetSectionCacheKey(out var lNonPersistentKey, out var lMailboxHandle, out var lUID, out var lPersistentKey);

            if (lPersistentKey == null)
            {
                if (lSectionCache.TryGetItemReader(lNonPersistentKey, out mCacheItemReader, lContext))
                {
                    mSectionReader = mCacheItemReader;
                }
                else
                {
                    ;?; // if part and none and not committed, see if I have it decoded; set and start background conversion


                    mSectionCacheItem = lSectionCache.GetNewItem(lContext);
                    mCacheItemReaderWriter = mSectionCacheItem.GetReaderWriter(lContext);
                    mSectionReader = mCacheItemReaderWriter;

                    mBackgroundCancellationTokenSource = new CancellationTokenSource();
                    mBackgroundTask = ZBackgroundFetchAsync(lNonPersistentKey, lContext);
                }
            }
            else
            {
                if (lSectionCache.TryGetItemReader(lPersistentKey, out mCacheItemReader, lContext))
                {
                    mSectionReader = mCacheItemReader;
                }
                else
                {
                    ;?; // if part and none and not committed, see if I have it decoded; set and start background conversion

                    mSectionCacheItem = lSectionCache.GetNewItem(lContext);
                    mCacheItemReaderWriter = mSectionCacheItem.GetReaderWriter(lContext);
                    mSectionReader = mCacheItemReaderWriter;

                    mBackgroundCancellationTokenSource = new CancellationTokenSource();
                    mBackgroundTask = ZBackgroundFetchAsync(lMailboxHandle, lUID, lPersistentKey, lContext);
                }
            }
        }

        private async Task ZBackgroundFetchAsync(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cIMAPMessageDataStream), nameof(ZBackgroundFetchAsync), pKey);

            CancellationToken lCancellationToken = mBackgroundCancellationTokenSource.Token;

            try
            {
                mCacheItemReaderWriter.WriteBegin(lContext);
                await Client.FetchSectionAsync(MessageHandle, Section, Decoding, mCacheItemReaderWriter, lCancellationToken, lContext).ConfigureAwait(false);
                await mCacheItemReaderWriter.WritingCompletedOKAsync(lCancellationToken, lContext).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                mCacheItemReaderWriter.WritingFailed(e, lContext);
                return;
            }

            mSectionCacheItem.Cache.AddItem(pKey, mSectionCacheItem, lContext);
        }

        private async Task ZBackgroundFetchAsync(iMailboxHandle pMailboxHandle, cUID pUID, cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cIMAPMessageDataStream), nameof(ZBackgroundFetchAsync), pMailboxHandle, pUID, pKey);

            CancellationToken lCancellationToken = mBackgroundCancellationTokenSource.Token;

            try
            {
                mCacheItemReaderWriter.WriteBegin(lContext);
                await Client.UIDFetchSectionAsync(pMailboxHandle, pUID, Section, Decoding, mCacheItemReaderWriter, lCancellationToken, lContext).ConfigureAwait(false);
                await mCacheItemReaderWriter.WritingCompletedOKAsync(lCancellationToken, lContext).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                mCacheItemReaderWriter.WritingFailed(e, lContext);
                return;
            }

            mSectionCacheItem.Cache.AddItem(pKey, mSectionCacheItem, lContext);
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

                if (mCacheItemReader != null)
                {
                    try { mCacheItemReader.Dispose(); }
                    catch { }
                }

                if (mCacheItemReaderWriter != null)
                {
                    try { mCacheItemReaderWriter.Dispose(); }
                    catch { }
                }
            }

            mDisposed = true;

            base.Dispose(pDisposing);
        }

        public override string ToString() => $"{nameof(cIMAPMessageDataStream)}({MessageHandle},{MailboxHandle},{UID},{Section},{Decoding})";
    }
}