using System;
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
                private readonly cNonPersistentKey mNonPersistentKey;
                private readonly cTrace.cContext mContext;
                private int mCount = 1;

                // read
                private long mReadPosition = 0;

                // write
                private eWriteState mWriteState = eWriteState.notstarted;
                private long mWritePosition = 0;

                // stream management
                private readonly object mLock = new object();
                private Stream mStream = null;
                private SemaphoreSlim mSemaphore = null;
                private CancellationTokenSource mCancellationTokenSource = null;
                private cReleaser mReleaser = null;

                public cReaderWriter(cItem pItem, cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                    mPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
                    mNonPersistentKey = null;
                    mContext = pParentContext.NewObject(nameof(cReaderWriter), pItem, pKey);
                }

                public cReaderWriter(cItem pItem, cNonPersistentKey pKey, cTrace.cContext pParentContext)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                    mPersistentKey = null;
                    mNonPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
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
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ReadByteAsync), pMC);

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

                public async Task Copy

                public void WriteBegin()
                {
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.notstarted) throw new InvalidOperationException();
                    ZSetStream();
                    mWriteState = eWriteState.writing;
                }

                public async Task WriteAsync(cMethodControl pMC, byte[] pBuffer, int pCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteAsync), pMC, pCount);
                    
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.writing) throw new InvalidOperationException();
                    if (pCount == 0) return;

                    await mSemaphore.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false);

                    try
                    {
                        lContext.TraceVerbose("writing");

                        if (mStream.CanTimeout) mStream.WriteTimeout = pMC.Timeout;
                        else _ = pMC.Timeout; // check for timeout

                        mStream.Position = mWritePosition;
                        await mStream.WriteAsync(pBuffer, 0, pCount, pMC.CancellationToken).ConfigureAwait(false);
                        mWritePosition = mStream.Position;
                    }
                    finally { mSemaphore.Release(); }

                    // let any pending read know that there is more data to consider
                    mReleaser.Release(lContext);
                }

                public async Task WriteEndAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteEndAsync), pMC);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.writing) throw new InvalidOperationException();

                    if (mStream.CanTimeout) mStream.WriteTimeout = pMC.Timeout;
                    else _ = pMC.Timeout; // check for timeout

                    await mStream.FlushAsync(pMC.CancellationToken).ConfigureAwait(false);

                    mWriteState = eWriteState.complete;

                    // let any pending read know that there is no more data to consider
                    mReleaser.Release(lContext);

                    // submit to cache
                    if (mPersistentKey == null)
                    {
                        if (mNonPersistentKey.UID == null) mItem.mCache.ZAdd(mNonPersistentKey, mItem, lContext);
                        else mItem.mCache.ZAdd(new cSectionCachePersistentKey(mNonPersistentKey), mItem, lContext);
                    }
                    else mItem.mCache.ZAdd(mPersistentKey, mItem, lContext);
                }

                private void ZSetStream()
                {
                    if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mStream != null) return;

                    lock (mLock)
                    {
                        if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                        if (mStream != null) return;

                        var lStream = mItem.GetReadWriteStream(mContext);
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

                    if (mStream != null)
                    {
                        try { mStream.Dispose(); }
                        catch { }
                    }

                    if (Interlocked.Decrement(ref mCount) == 0) mItem.ZDecrementOpenStreamCount(mContext);

                    if (mSemaphore != null)
                    {
                        try { mSemaphore.Dispose(); }
                        catch { }
                    }

                    if (mCancellationTokenSource != null)
                    {
                        try { mCancellationTokenSource.Cancel(); }
                        catch { }
                    }

                    if (mReleaser != null)
                    {
                        try { mReleaser.Dispose(); }
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