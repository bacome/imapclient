﻿using System;
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
        public readonly cSinglePartBody Part;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        private int mReadTimeout;

        private iSectionCacheItemReader mSectionCacheItemReader = null;
        private cSectionCacheItemReader mReader = null;
        private cSectionCacheItem mSectionCacheItem = null;
        private cSectionCacheItemReaderWriter mReaderWriter = null;

        // background fetch task
        private CancellationTokenSource mBackgroundCancellationTokenSource = null;
        private Task mBackgroundTask = null;        

        public cIMAPMessageDataStream(cIMAPMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (!pMessage.IsValid) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            Part = null;

            MailboxHandle = null;
            UID = null;

            Section = cSection.All;
            Decoding = eDecodingRequired.none;

            mReadTimeout = Client.Timeout;
        }

        public cIMAPMessageDataStream(cIMAPAttachment pAttachment, bool pDecoded = true)
        {
            if (pAttachment == null) throw new ArgumentNullException(nameof(pAttachment));
            if (!pAttachment.IsValid) throw new ArgumentOutOfRangeException(nameof(pAttachment), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pAttachment.Client;
            MessageHandle = pAttachment.MessageHandle;
            Part = pAttachment.Part;

            MailboxHandle = null;
            UID = null;

            Section = pAttachment.Part.Section;

            if (pDecoded) Decoding = pAttachment.Part.DecodingRequired;
            else Decoding = eDecodingRequired.none;

            mReadTimeout = Client.Timeout;
        }

        // note that the decoding may be ignored
        public cIMAPMessageDataStream(cIMAPMessage pMessage, cSection pSection, eDecodingRequired pDecoding)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (!pMessage.IsValid) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            Part = null;

            MailboxHandle = null;
            UID = null;

            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;

            mReadTimeout = Client.Timeout;
        }

        internal cIMAPMessageDataStream(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart, bool pDecoded)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            if (!ReferenceEquals(pClient.SelectedMailboxDetails?.MessageCache, pMessageHandle.MessageCache)) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));

            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            MailboxHandle = null;
            UID = null;

            Section = pPart.Section;

            if (pDecoded) Decoding = pPart.DecodingRequired;
            else Decoding = eDecodingRequired.none;

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
                var lContext = Client.mRootContext.NewGetProp(nameof(cIMAPMessageDataStream), nameof(Length));
                var lTask = GetLengthAsync(cMethodControl.None, lContext);
                Client.Wait(lTask, lContext);
                return lTask.Result;
            }
        }

        internal async Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(GetLengthAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            if (mSectionCacheItemReader == null)
            {
                // see if the cache knows

                ZGetSectionCacheKey(out var lNonPersistentKey, out _, out _, out var lPersistentKey);

                if (lPersistentKey == null)
                {
                    if (Client.SectionCache.TryGetItemLength(lNonPersistentKey, out var lLength, lContext)) return lLength;
                }
                else
                {
                    if (Client.SectionCache.TryGetItemLength(lPersistentKey, out var lLength, lContext)) return lLength;
                }

                // see if IMAP knows

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
                        if (Decoding == eDecodingRequired.none) return Part.SizeInBytes;
                        var lDecodedSizeInBytes = await Client.DecodedSizeInBytesAsync(MessageHandle, Part).ConfigureAwait(false);
                        if (lDecodedSizeInBytes != null) return lDecodedSizeInBytes.Value;
                    }
                }

                // have to read it to find out

                ZSetSectionCacheItemReader(lContext);
            }

            if (mReader != null) return mReader.Length;
            if (mReaderWriter == null) throw new cInternalErrorException(lContext);
            return await mReaderWriter.GetLengthAsync(pMC, lContext).ConfigureAwait(false);
        }

        public override long Position
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (mSectionCacheItemReader == null) return 0;
                return mSectionCacheItemReader.ReadPosition;
            }

            set
            {
                var lContext = Client.mRootContext.NewSetProp(nameof(cIMAPMessageDataStream), nameof(Position), value);

                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));
                if (value < 0) throw new ArgumentOutOfRangeException();
                if (value == 0 && mSectionCacheItemReader == null) return;

                ZSetSectionCacheItemReader(lContext);

                if (mReader != null) mReader.ReadPosition = value;
                else if (mReaderWriter == null) throw new cInternalErrorException(lContext);
                else Client.Wait(mReaderWriter.SetReadPositionAsync(value, lContext), lContext);
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
            var lContext = Client.mRootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(Read), pCount);
            var lTask = ZReadAsync(pBuffer, pOffset, pCount, CancellationToken.None, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        public override Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            var lContext = Client.mRootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ReadAsync), pCount);
            return ZReadAsync(pBuffer, pOffset, pCount, pCancellationToken, lContext);
        }

        public override int ReadByte()
        {
            var lContext = Client.mRootContext.NewMethod(nameof(cIMAPMessageDataStream), nameof(ReadByte));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPMessageDataStream));

            ZSetSectionCacheItemReader(lContext);

            if (mReader != null) return mReader.ReadByte();

            if (mReaderWriter == null) throw new cInternalErrorException(lContext);

            var lTask = mReaderWriter.ReadByteAsync(mReadTimeout, lContext);
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
            return mSectionCacheItemReader.ReadAsync(pBuffer, pOffset, pCount, mReadTimeout, pCancellationToken, lContext);
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public bool? DataIsCached
        {
            get
            {
                if (mSectionCacheItemReader == null) return null;
                if (mReader != null) return true;
                if (mReaderWriter == null) throw new cInternalErrorException(nameof(cIMAPMessageDataStream), nameof(DataIsCached));
                return mReaderWriter.WritingHasCompletedOK;
            }
        }

        public uint FetchedBytesPosition
        {
            get
            {
                if (mReaderWriter == null) throw new InvalidOperationException();
                return mReaderWriter.FetchedBytesReadPosition;
            }
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

            if (mSectionCacheItemReader != null) return;

            var lSectionCache = Client.SectionCache;
            ZGetSectionCacheKey(out var lNonPersistentKey, out var lMailboxHandle, out var lUID, out var lPersistentKey);

            if (lPersistentKey == null)
            {
                if (lSectionCache.TryGetItemReader(lNonPersistentKey, out mReader, lContext))
                {
                    mSectionCacheItemReader = mReader;
                }
                else
                {
                    mSectionCacheItem = lSectionCache.GetNewItem(lContext);
                    mReaderWriter = mSectionCacheItem.GetReaderWriter(lContext);
                    mSectionCacheItemReader = mReaderWriter;

                    mBackgroundCancellationTokenSource = new CancellationTokenSource();
                    mBackgroundTask = ZBackgroundFetchAsync(lNonPersistentKey, lContext);
                }
            }
            else
            {
                if (lSectionCache.TryGetItemReader(lPersistentKey, out mReader, lContext))
                {
                    mSectionCacheItemReader = mReader;
                }
                else
                {
                    mSectionCacheItem = lSectionCache.GetNewItem(lContext);
                    mReaderWriter = mSectionCacheItem.GetReaderWriter(lContext);
                    mSectionCacheItemReader = mReaderWriter;

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
                mReaderWriter.WriteBegin(lContext);
                await Client.FetchAsync(MessageHandle, Section, Decoding, mReaderWriter, lCancellationToken, lContext);
                await mReaderWriter.WritingCompletedOKAsync(lCancellationToken, lContext);
            }
            catch (Exception e)
            {
                mReaderWriter.WritingFailed(e, lContext);
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
                mReaderWriter.WriteBegin(lContext);
                await Client.UIDFetchAsync(pMailboxHandle, pUID, Section, Decoding, mReaderWriter, lCancellationToken, lContext);
                await mReaderWriter.WritingCompletedOKAsync(lCancellationToken, lContext);
            }
            catch (Exception e)
            {
                mReaderWriter.WritingFailed(e, lContext);
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

                if (mReader != null)
                {
                    try { mReader.Dispose(); }
                    catch { }
                }

                if (mReaderWriter != null)
                {
                    try { mReaderWriter.Dispose(); }
                    catch { }
                }
            }

            mDisposed = true;

            base.Dispose(pDisposing);
        }

        public override string ToString() => $"{nameof(cIMAPMessageDataStream)}({MessageHandle},{MailboxHandle},{UID},{Section},{Decoding})";
    }
}