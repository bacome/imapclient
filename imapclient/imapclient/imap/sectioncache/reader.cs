using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal interface iSectionCacheItemReader
    {
        Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext);
        long ReadPosition { get; }
        Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext);
        Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
        Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext);
    }

    public partial class cSectionCache
    {
        public abstract partial class cItem
        {
            internal sealed class cReader : iSectionCacheItemReader, IDisposable
            {
                private bool mDisposed = false;
                private readonly cItem mItem;
                private readonly cTrace.cContext mContext;
                private int mCount = 1;
                private Stream mStream = null;

                public cReader(cItem pItem, cTrace.cContext pParentContext)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                    mContext = pParentContext.NewObject(nameof(cReader), pItem);
                }

                public Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(GetLengthAsync), pMC);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));
                    ZSetStream(lContext);
                    return Task.FromResult(mStream.Length);
                }

                public long ReadPosition
                {
                    get
                    {
                        if (mDisposed) throw new ObjectDisposedException(nameof(cReader));
                        if (mStream == null) return 0;
                        return mStream.Position;
                    }
                }

                public Task SetReadPositionAsync(long pReadPosition, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(SetReadPositionAsync), pReadPosition);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));
                    if (pReadPosition < 0) throw new ArgumentOutOfRangeException(nameof(pReadPosition));
                    if (pReadPosition == 0 && mStream == null) return Task.WhenAll();
                    ZSetStream(lContext);
                    mStream.Position = pReadPosition;
                    return Task.WhenAll();
                }

                public async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(ReadAsync), pOffset, pCount, pTimeout);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));

                    if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
                    if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
                    if (pCount == 0) return 0;

                    ZSetStream(lContext);
                    if (mStream.CanTimeout) mStream.ReadTimeout = pTimeout;
                    return await mStream.ReadAsync(pBuffer, pOffset, pCount, pCancellationToken).ConfigureAwait(false);
                }

                public Task<int> ReadByteAsync(int pTimeout, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(ReadByteAsync), pTimeout);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));
                    ZSetStream(lContext);
                    if (mStream.CanTimeout) mStream.ReadTimeout = pTimeout;
                    return Task.FromResult(mStream.ReadByte());
                }

                private void ZSetStream(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(ZSetStream));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));
                    if (mStream != null) return;

                    var lStream = mItem.GetReadStream(lContext);
                    if (!lStream.CanRead || !lStream.CanSeek) throw new cUnexpectedSectionCacheActionException(lContext);
                    mStream = lStream;
                }

                public void Dispose()
                {
                    if (mDisposed) return;

                    if (mStream != null)
                    {
                        try { mStream.Dispose(); }
                        catch { }
                    }

                    if (Interlocked.Decrement(ref mCount) == 0) mItem.ZDecrementOpenStreamCount(mContext);

                    mDisposed = true;
                }
            }
        }
    }
}