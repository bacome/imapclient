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

                public cReaderWriter(cItem pItem, cTrace.cContext pParentContext)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                    mContext = pParentContext.NewObject(nameof(cReaderWriter), pItem);
                }

                public async Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(GetLengthAsync), pMC);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    ZSetStream(lContext);

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

                public async Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(SetReadPositionAsync), pReadPosition);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    if (pReadPosition < 0) throw new ArgumentOutOfRangeException(nameof(pReadPosition));

                    if (pReadPosition == 0)
                    {
                        mReadPosition = pReadPosition;
                        return;
                    }

                    ZSetStream(lContext);

                    if (ZTrySetReadPosition(pReadPosition)) return;

                    while (true)
                    {
                        Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                        if (ZTrySetReadPosition(pReadPosition)) return;
                        await lTask.ConfigureAwait(false);
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

                public async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ReadAsync), pOffset, pCount, pTimeout);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
                    if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
                    if (pCount == 0) return 0;

                    var lMC = new cMethodControl(pTimeout, pCancellationToken);

                    ZSetStream(lContext);

                    if (mStream.Length == mReadPosition || mWriteState != eWriteState.complete)
                    {
                        using (var lAwaiter = new cAwaiter(lMC))
                        {
                            while (true)
                            {
                                Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                                if (mStream.Length > mReadPosition || mWriteState == eWriteState.complete) break;
                                await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                            }
                        }
                    }

                    await mSemaphore.WaitAsync(lMC.Timeout, lMC.CancellationToken).ConfigureAwait(false);

                    try
                    {
                        lContext.TraceVerbose("reading bytes");

                        if (mStream.CanTimeout) mStream.ReadTimeout = lMC.Timeout;
                        else _ = lMC.Timeout; // check for timeout

                        mStream.Position = mReadPosition;
                        var lBytesRead = await mStream.ReadAsync(pBuffer, pOffset, pCount, lMC.CancellationToken).ConfigureAwait(false);
                        mReadPosition = mStream.Position;

                        return lBytesRead;
                    }
                    finally { mSemaphore.Release(); }
                }

                public async Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ReadByteAsync), pTimeout);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    var lMC = new cMethodControl(pTimeout);

                    ZSetStream(lContext);

                    if (mStream.Length == mReadPosition || mWriteState != eWriteState.complete)
                    {
                        using (var lAwaiter = new cAwaiter(lMC))
                        {
                            while (true)
                            {
                                Task lTask = mReleaser.GetAwaitReleaseTask(lContext);
                                if (mStream.Length > mReadPosition || mWriteState == eWriteState.complete) break;
                                await lAwaiter.AwaitAny(lTask).ConfigureAwait(false);
                            }
                        }
                    }

                    await mSemaphore.WaitAsync(lMC.Timeout).ConfigureAwait(false);

                    try
                    {
                        lContext.TraceVerbose("reading byte");

                        if (mStream.CanTimeout) mStream.ReadTimeout = lMC.Timeout;
                        else _ = lMC.Timeout; // check for timeout

                        mStream.Position = mReadPosition;
                        var lResult = mStream.ReadByte();
                        mReadPosition = mStream.Position;

                        return lResult;
                    }
                    finally { mSemaphore.Release(); }
                }

                public void WriteBegin(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteBegin));
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.notstarted) throw new InvalidOperationException();
                    ZSetStream(lContext);
                    mWriteState = eWriteState.writing;
                }

                public async Task WriteAsync(byte[] pBuffer, int pCount, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteAsync), pCount);
                    
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.writing) throw new InvalidOperationException();
                    if (pCount == 0) return;

                    await mSemaphore.WaitAsync(pCancellationToken).ConfigureAwait(false);

                    try
                    {
                        lContext.TraceVerbose("writing");

                        mStream.Position = mWritePosition;
                        await mStream.WriteAsync(pBuffer, 0, pCount, pCancellationToken).ConfigureAwait(false);
                        mWritePosition = mStream.Position;
                    }
                    finally { mSemaphore.Release(); }

                    // let any pending read know that there is more data to consider
                    mReleaser.Release(lContext);
                }

                public async Task WriteEndAsync(cSectionCachePersistentKey pKey, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteEndAsync), pKey);
                    await ZWriteEndAsync(pCancellationToken, lContext).ConfigureAwait(false);
                    mItem.mCache.ZAddItem(pKey, mItem, lContext);
                }

                public async Task WriteEndAsync(cNonPersistentKey pKey, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(WriteEndAsync), pKey);
                    await ZWriteEndAsync(pCancellationToken, lContext).ConfigureAwait(false);
                    if (pKey.UID == null) mItem.mCache.ZAddItem(pKey, mItem, lContext);
                    else mItem.mCache.ZAddItem(new cSectionCachePersistentKey(pKey), mItem, lContext);
                }

                private async Task ZWriteEndAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ZWriteEndAsync));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mWriteState != eWriteState.writing) throw new InvalidOperationException();

                    await mStream.FlushAsync(pCancellationToken).ConfigureAwait(false);

                    mWriteState = eWriteState.complete;

                    // let any pending read know that there is no more data to consider
                    mReleaser.Release(lContext);
                }

                private void ZSetStream(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReaderWriter), nameof(ZSetStream));

                    if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (mStream != null) return;

                    lock (mLock)
                    {
                        if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                        if (mStream != null) return;

                        var lStream = mItem.GetReadWriteStream(lContext);
                        if (!lStream.CanRead || !lStream.CanSeek || !lStream.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);

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