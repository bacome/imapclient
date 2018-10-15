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
        public readonly iMailboxHandle MailboxHandle;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        private readonly cSectionHandle mSectionHandle;
        private readonly cSectionId mSectionId;

        private cSinglePartBody mPart;

        private int mReadTimeout;

        private iSectionReader mMessageDataReader = null; // this will be one of the two following
        private cSectionReader mSectionReader = null;
        private cSectionReaderWriter mSectionReaderWriter = null;

        // background fetch task
        private CancellationTokenSource mBackgroundCancellationTokenSource = null;
        private Task mBackgroundTask = null;   
        
        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart, bool pDecoded)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pMessageHandle.MessageCache.IsInvalid) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            mPart = pPart ?? throw new ArgumentNullException(nameof(pPart));

            MailboxHandle = null;
            Section = pPart.Section;

            if (pDecoded) Decoding = pPart.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            mSectionHandle = new cSectionHandle(pMessageHandle, pPart.Section, pDecoded);
            mSectionId = null;

            mReadTimeout = Client.Timeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pMessageHandle.MessageCache.IsInvalid) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
            if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);

            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            MailboxHandle = null;
            Decoding = eDecodingRequired.none;

            mSectionHandle = new cSectionHandle(pMessageHandle, pSection, false);
            mSectionId = null;

            mPart = null;

            mReadTimeout = Client.Timeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));

            MailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
            if (!ReferenceEquals(pClient.MailboxCache, pMailboxHandle.MailboxCache)) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));

            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            if (pSection.Part == null && pDecoding != eDecodingRequired.none) throw new ArgumentOutOfRangeException(nameof(pDecoding));
            if (pDecoding != eDecodingRequired.none && !pSection.CouldDescribeABodyPart) throw new ArgumentOutOfRangeException(nameof(pDecoding));

            Decoding = pDecoding;

            mSectionHandle = null;
            mSectionId = new cSectionId(new cMessageUID(pMailboxHandle.MailboxId, pUID, Client.UTF8Enabled), pSection, pDecoding != eDecodingRequired.none);

            mPart = null;

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

        private async Task ZSetPartIfPossible(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZSetPartIfPossible), pMC);

            if (mPart != null) return;
            if (Section.Part == null || !Section.CouldDescribeABodyPart) return;

            cBodyPart lPart;

            if (mSectionHandle == null) lPart = await Client.GetBodyPartAsync(pMC, MailboxHandle, mSectionId.MessageUID, Section, lContext).ConfigureAwait(false);
            else lPart = await Client.GetBodyPartAsync(pMC, MailboxHandle, mSectionId.MessageUID, Section, lContext).ConfigureAwait(false);

            var lSinglePartBody = lPart as cSinglePartBody;

            if (lSinglePartBody == null)
            {
                if (Decoding != eDecodingRequired.none) throw new cMessageDataStreamDecodingInconsistencyException(this);
            }
            else
            {
                if (Decoding != eDecodingRequired.none && Decoding != lSinglePartBody.DecodingRequired) throw new cMessageDataStreamDecodingInconsistencyException(this); ;
                mPart = lSinglePartBody;
            }
        }

        private async Task<long> ZGetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync), pMC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (mMessageDataReader != null) return await mMessageDataReader.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
            if (ZTryGetIdealSectionReader(lContext)) return await mMessageDataReader.GetLengthAsync(pMC, lContext).ConfigureAwait(false);

            if (Section == cSection.All && Client.SizesAreReliable) 
            {
                if (mSectionHandle == null) return await Client.GetSizeInBytesAsync(pMC, MailboxHandle, mSectionId.MessageUID, lContext).ConfigureAwait(false);
                else return await Client.GetSizeInBytesAsync(pMC, mSectionHandle.MessageHandle, lContext).ConfigureAwait(false);
            }

            if (mPart == null) await ZSetPartIfPossible(pMC, lContext).ConfigureAwait(false);

            if (mPart == null)
            {
                ZGetSectionReaderWriter(lContext);
                return await mMessageDataReader.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
            }

            if (mPart is cMessageBodyPart && !Client.SizesAreReliable)
            {
                ZGetSectionReaderWriter(lContext);
                return await mMessageDataReader.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
            }

            uint? lDecodedSizeInBytes;

            if (mSectionHandle == null) lDecodedSizeInBytes = await Client.GetDecodedSizeInBytesAsync(pMC, MailboxHandle, mSectionId.MessageUID, mPart, lContext).ConfigureAwait(false);
            else lDecodedSizeInBytes = await Client.GetDecodedSizeInBytesAsync(pMC, mSectionHandle.MessageHandle, mPart, lContext).ConfigureAwait(false);

            if (lDecodedSizeInBytes != null) return lDecodedSizeInBytes.Value;

            ZGetSectionReaderWriter(lContext);
            return await mMessageDataReader.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
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
                if (value == 0 && mMessageDataReader == null) return;

                ZSetMessageDataReader(lContext);

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
            return mMessageDataReader.ReadAsync(pBuffer, pOffset, pCount, mReadTimeout, pCancellationToken, lContext);
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

            ;?; // check both
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
            ;?; // messagedatareader
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













        private bool ZTryGetIdealSectionReader(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZTryGetIdealSectionReader));

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

            return false;
        }

        private void ZGetSectionReaderWriter(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZGetSectionReaderWriter));

            if (mMessageDataReader != null) throw new InvalidOperationException();

            mBackgroundCancellationTokenSource = new CancellationTokenSource();

            if (mSectionHandle == null)
            {
                mSectionReaderWriter = Client.PersistentCache.GetSectionReaderWriter(mSectionId, lContext);
                if (mSectionId.Decoded && Client.PersistentCache.TryGetSectionReader(new cSectionId(mSectionId.MessageUID, Section, false), out mSectionReader, lContext)) mBackgroundTask = ZBackgroundDecodeAsync(lContext);
                else mBackgroundTask = ZBackgroundFetchAsync(mSectionId, lContext);
            }
            else
            {
                mSectionReaderWriter = Client.PersistentCache.GetSectionReaderWriter(mSectionHandle, lContext);
                if (mSectionHandle.Decoded && Client.PersistentCache.TryGetSectionReader(new cSectionHandle(mSectionHandle.MessageHandle, Section, false), out mSectionReader, lContext)) mBackgroundTask = ZBackgroundDecodeAsync(lContext);
                else mBackgroundTask = ZBackgroundFetchAsync(mSectionHandle, lContext);
            }

            mMessageDataReader = mSectionReaderWriter;
        }






























        private void ZGetSectionCacheKey(out cSectionHandle rNonPersistentKey, out iMailboxHandle rMailboxHandle, out cUID rUID, out cSectionId rPersistentKey)
        {
            ;?; // return both
            if (UID == null && MessageHandle.UID == null)
            {
                rNonPersistentKey = new cSectionHandle(Client, MessageHandle, Section, Decoding);
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

            rPersistentKey = new cSectionId(rMailboxHandle, rUID, Section, Decoding);
        }

        private void ZSetSectionCacheItemReader(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ZSetSectionCacheItemReader));

            if (mSectionReader != null) return;

            ;?; // VERIFY that the item exists BEFORE returning hte reader: NOTE: get the reader first, then check for existance
                //  ONLY check if accessing with a UID: otherwise the handles expunged is sufficient
                //   use Client.GetMessageHandleAsync() and then check the expunged (the API will return null if the item doesn't exist)


            var lSectionCache = Client.SectionCache;
            ZGetSectionCacheKey(out var lNonPersistentKey, out var lMailboxHandle, out var lUID, out var lPersistentKey);

            ;?; // check both
            ;?; //  NOTE that getting the readerwriter by the nonpersistent key is preferred as that key contains the handle

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

        private async Task ZBackgroundFetchAsync(cSectionHandle pKey, cTrace.cContext pParentContext)
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

        private async Task ZBackgroundFetchAsync(iMailboxHandle pMailboxHandle, cUID pUID, cSectionId pKey, cTrace.cContext pParentContext)
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

        public override string ToString() => $"{nameof(cIMAPMessageDataStream)}({MessageHandle},{MailboxHandle},{UID},{Section},{Decoding})";
    }
}