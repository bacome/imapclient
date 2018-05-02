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
        Task SetReadPositionAsync(cMethodControl pMC, long pReadPosition, cTrace.cContext pParentContext);
        Task<int> ReadAsync(cMethodControl pMC, byte[] pBuffer, int pOffset, int pCount, cTrace.cContext pParentContext);
        Task<int> ReadByteAsync(cMethodControl pMC, cTrace.cContext pParentContext);
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

                public async Task<long> GetLengthAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(GetLengthAsync), pMC);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));
                    ZSetStream(lContext);
                    return mStream.Length;
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

                public async Task SetReadPositionAsync(cMethodControl pMC, long pReadPosition, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(SetReadPositionAsync), pMC, pReadPosition);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));
                    if (pReadPosition < 0) throw new ArgumentOutOfRangeException(nameof(pReadPosition));
                    if (pReadPosition == 0 && mStream == null) return;
                    ZSetStream(lContext);
                    mStream.Position = pReadPosition;
                }

                public async Task<int> ReadAsync(cMethodControl pMC, byte[] pBuffer, int pOffset, int pCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(ReadAsync), pMC, pOffset, pCount);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReaderWriter));

                    if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
                    if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
                    if (pCount == 0) return 0;

                    ZSetStream(lContext);

                    if (mStream.CanTimeout) mStream.ReadTimeout = pMC.Timeout;
                    else _ = pMC.Timeout; // check for timeout

                    return await mStream.ReadAsync(pBuffer, pOffset, pCount, pMC.CancellationToken).ConfigureAwait(false);
                }

                public async Task<int> ReadByteAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(ReadByteAsync));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));

                    ZSetStream(lContext);

                    if (mStream.CanTimeout) mStream.ReadTimeout = pMC.Timeout;
                    else _ = pMC.Timeout; // check for timeout

                    return mStream.ReadByte();
                }

                private void ZSetStream(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cReader), nameof(ZSetStream));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cReader));
                    if (mStream != null) return;

                    var lStream = mItem.GetReadStream(lContext);
                    if (!lStream.CanRead || !lStream.CanSeek) throw new cUnexpectedSectionCacheActionException(mContext);
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