using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cSectionCache
    {
        public abstract partial class cItem
        {
            internal sealed class cReaderWriter : iSectionCacheItemReader, IDisposable
            {
                private enum eWriteState { notstarted, writing, complete }

                private bool mDisposing = false;
                private bool mDisposed = false;
                private readonly cItem mItem;
                private readonly cSectionCachePersistentKey mPersistentKey;
                private readonly cSectionCacheNonPersistentKey mNonPersistentKey;
                private readonly cBatchSizer mWriteSizer;
                private readonly cTrace.cContext mContext;
                private int mCount = 1;

                // read
                private long mReadPosition = 0;

                // write
                private eWriteState mWriteState = eWriteState.notstarted;
                private cMethodControl mWriteMC;
                private CancellationTokenSource mWriteCancellationTokenSource;
                private Stopwatch mStopwatch;
                private int mBytesInBuffer;
                private long mWritePosition;
                private int mBufferSize;
                private byte[] mBuffer;

                // stream management
                private readonly object mLock = new object();
                private Stream mStream = null;
                private SemaphoreSlim mSemaphore = null;
                private CancellationTokenSource mCancellationTokenSource = null;
                private cReleaser mReleaser = null;

                public cReaderWriter(cItem pItem, cSectionCachePersistentKey pKey, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                    mPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
                    mNonPersistentKey = null;
                    mWriteSizer = pWriteSizer ?? throw new ArgumentNullException(nameof(pWriteSizer));
                    mContext = pParentContext.NewObject(nameof(cReaderWriter), pItem, pKey);
                }

                public cReaderWriter(cItem pItem, cSectionCacheNonPersistentKey pKey, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                    mPersistentKey = null;
                    mNonPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
                    mWriteSizer = pWriteSizer ?? throw new ArgumentNullException(nameof(pWriteSizer));
                    mContext = pParentContext.NewObject(nameof(cReaderWriter), pItem, pKey);
                }

                public async Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(GetLengthAsync), pMC);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    ZSetStream();

                    if (mWriteState == eWriteState.complete) return mStream.Length;

                    using (var lAwaiter = new cAwaiter(pMC))
                    {
                        while (true)
                        {
                            Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                            if (mWriteState == eWriteState.complete) return mStream.Length;
                            await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                        }
                    }
                }

                public long ReadPosition
                {
                    get
                    {
                        if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                        return mReadPosition;
                    }
                }

                public async Task SetReadPositionAsync(cMethodControl pMC, long pReadPosition, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(SetReadPositionAsync), pMC, pReadPosition);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    if (pReadPosition < 0) throw new ArgumentOutOfRangeException(nameof(pReadPosition));

                    if (pReadPosition == 0)
                    {
                        mReadPosition = pReadPosition;
                        return;
                    }

                    ZSetStream();

                    if (ZTrySetReadPosition(pReadPosition)) return;

                    using (var lAwaiter = new cAwaiter(pMC))
                    {
                        while (true)
                        {
                            Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                            if (ZTrySetReadPosition(pReadPosition)) return;
                            await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                        }
                    }
                }

                private bool ZTrySetReadPosition(long pReadPosition)
                {
                    if (mStream.Length > pReadPosition)
                    {
                        mReadPosition = pReadPosition;
                        return true;
                    }

                    if (mWriteState == eWriteState.complete)
                    {
                        if (pReadPosition > mStream.Length) throw new ArgumentOutOfRangeException(nameof(pReadPosition));
                        mReadPosition = pReadPosition;
                        return true;
                    }

                    return false;
                }

                public async Task<int> ReadAsync(cMethodControl pMC, byte[] pBuffer, int pOffset, int pCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ReadAsync), pMC, pOffset, pCount);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
                    if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
                    if (pCount == 0) return 0;

                    ZSetStream();

                    if (mStream.Length == mReadPosition || mWriteState != eWriteState.complete)
                    {
                        using (var lAwaiter = new cAwaiter(pMC))
                        {
                            while (true)
                            {
                                Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                                if (mStream.Length > mReadPosition || mWriteState == eWriteState.complete) break;
                                await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                            }
                        }
                    }

                    await mSemaphore.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false);

                    try
                    {
                        lContext.TraceVerbose("reading bytes");

                        if (mStream.CanTimeout) mStream.ReadTimeout = pMC.Timeout;
                        else _ = pMC.Timeout; // check for timeout

                        mStream.Position = mReadPosition;
                        var lBytesRead = await mStream.ReadAsync(pBuffer, pOffset, pCount, pMC.CancellationToken).ConfigureAwait(false);
                        mReadPosition = mStream.Position;

                        return lBytesRead;
                    }
                    finally { mSemaphore.Release(); }
                }

                public async Task<int> ReadByteAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ReadByteAsync));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    ZSetStream();

                    if (mStream.Length == mReadPosition || mWriteState != eWriteState.complete)
                    {
                        using (var lAwaiter = new cAwaiter(pMC))
                        {
                            while (true)
                            {
                                Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                                if (mStream.Length > mReadPosition || mWriteState == eWriteState.complete) break;
                                await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                            }
                        }
                    }

                    await mSemaphore.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false);

                    try
                    {
                        lContext.TraceVerbose("reading byte");

                        if (mStream.CanTimeout) mStream.ReadTimeout = pMC.Timeout;
                        else _ = pMC.Timeout; // check for timeout

                        mStream.Position = mReadPosition;
                        var lResult = mStream.ReadByte();
                        mReadPosition = mStream.Position;

                        return lResult;
                    }
                    finally { mSemaphore.Release(); }
                }

                public void WriteBegin(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteBegin), pMC);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.notstarted) throw new InvalidOperationException();
                    mWriteState = eWriteState.writing;

                    ZSetStream();

                    mWriteMC = pMC;
                    mWriteCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(mCancellationTokenSource.Token, pMC.CancellationToken);
                    mStopwatch = new Stopwatch();
                    mBytesInBuffer = 0;
                    mWritePosition = 0;
                }

                public async Task WriteByteAsync(byte pByte, cTrace.cContext pParentContext)
                {
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.writing) throw new InvalidOperationException();

                    if (mBytesInBuffer == 0)
                    {
                        mBufferSize = mWriteSizer.Current;
                        if (mBuffer == null || mBufferSize > mBuffer.Length) mBuffer = new byte[mBufferSize];
                    }

                    mBuffer[mBytesInBuffer++] = pByte;

                    if (mBytesInBuffer == mBufferSize) await ZWriteAsync(pParentContext);
                }

                public async Task WriteEndAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteEndAsync));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.writing) throw new InvalidOperationException();

                    if (mBytesInBuffer > 0) await ZWriteAsync(pParentContext).ConfigureAwait(false);

                    mWriteState = eWriteState.complete;

                    // let any pending read know that there is no more data to consider
                    mReleaser.Release(lContext);

                    // submit to cache
                    if (ReferenceEquals(mPersistentKey, null))
                    {
                        if (mNonPersistentKey.UID == null) mItem.mCache.ZAdd(mNonPersistentKey, mItem, lContext);
                        else mItem.mCache.ZAdd(new cSectionCachePersistentKey(mNonPersistentKey), mItem, lContext);
                    }
                    else mItem.mCache.ZAdd(mPersistentKey, mItem, lContext);
                }

                private async Task ZWriteAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ZWriteAsync));

                    if (mBytesInBuffer == 0) throw new InvalidOperationException();

                    await mSemaphore.WaitAsync(mWriteMC.Timeout, mWriteCancellationTokenSource.Token).ConfigureAwait(false);

                    try
                    {
                        lContext.TraceVerbose("writing {0} bytes", mBytesInBuffer);

                        if (mStream.CanTimeout) mStream.WriteTimeout = mWriteMC.Timeout;
                        else _ = mWriteMC.Timeout; // check for timeout

                        mStopwatch.Restart();
                        mStream.Position = mWritePosition;
                        await mStream.WriteAsync(mBuffer, 0, mBytesInBuffer, mWriteMC.CancellationToken).ConfigureAwait(false);
                        mWritePosition = mStream.Position;
                        mStopwatch.Stop();

                    }
                    finally { mSemaphore.Release(); }

                    // let any pending read know that there is more data to consider
                    mReleaser.Release(lContext);

                    // store the time taken so the next write is a better size
                    mWriteSizer.AddSample(mBytesInBuffer, mStopwatch.ElapsedMilliseconds);

                    mBytesInBuffer = 0;
                }

                private void ZSetStream()
                {
                    if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mStream != null) return;

                    lock (mLock)
                    {
                        if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                        if (mStream != null) return;

                        var lStream = mItem.ReadWriteStream;
                        if (!lStream.CanRead || !lStream.CanSeek || !lStream.CanWrite) throw new cUnexpectedSectionCacheActionException(mContext);

                        mStream = lStream;
                        mSemaphore = new SemaphoreSlim(1, 1);
                        mCancellationTokenSource = new CancellationTokenSource();
                        mReleaser = new cReleaser(nameof(cReaderWriter), mCancellationTokenSource.Token);
                    }
                }

                public void Dispose()
                {
                    if (mDisposed) return;

                    lock (mLock)
                    {
                        mDisposing = true;
                    }

                    if (mCancellationTokenSource != null)
                    {
                        try { mCancellationTokenSource.Cancel(); }
                        catch { }
                    }

                    if (mWriteCancellationTokenSource != null)
                    {
                        try { mWriteCancellationTokenSource.Cancel(); }
                        catch { }
                    }

                    if (mStream != null)
                    {
                        try { mStream.Dispose(); }
                        catch { }
                    }

                    if (Interlocked.Decrement(ref mCount) == 0) mItem.ZDecrementOpenStreamCount(mContext);

                    if (mReleaser != null)
                    {
                        try { mReleaser.Dispose(); }
                        catch { }
                    }

                    if (mWriteCancellationTokenSource != null)
                    {
                        try { mWriteCancellationTokenSource.Dispose(); }
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
    }
}