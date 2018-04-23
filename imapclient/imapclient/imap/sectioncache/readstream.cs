using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCacheItem
    {
        internal sealed class cReader : IDisposable
        {
            private bool mDisposed = false;
            private readonly cSectionCacheItem mItem;
            private readonly cTrace.cContext mContext;
            private int mCount = 1;
            private Stream mStream = null;
            private int mReadTimeout = -1;

            public cReader(cSectionCacheItem pItem, cTrace.cContext pParentContext)
            {
                mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                mContext = pParentContext.NewObject(nameof(cReader), pItem);
            }

            public long Length
            {
                get
                {
                    ZSetStream();
                    return mStream.Length;
                }
            }

            public long Position
            {
                get
                {
                    if (mStream == null) return 0;
                    return mStream.Position;
                }

                set
                {
                    if (value == 0 && mStream == null) return;
                    ZSetStream();
                    mStream.Position = value;
                }
            }

            public int ReadTimeout
            {
                get => mReadTimeout;

                set
                {
                    if (value < -1) throw new ArgumentOutOfRangeException();
                    mReadTimeout = value;
                }
            }

            public async Task FlushAsync(CancellationToken pCancellationToken)
            {
                if (mStream == null) return;
                await mStream.FlushAsync(pCancellationToken).ConfigureAwait(false);
            }

            public Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
            {
                ZSetStream();
                if (mStream.CanTimeout) mStream.ReadTimeout = mReadTimeout;
                return mStream.ReadAsync(pBuffer, pOffset, pCount, pCancellationToken);
            }

            public int ReadByte()
            {
                ZSetStream();
                if (mStream.CanTimeout) mStream.ReadTimeout = mReadTimeout;
                return mStream.ReadByte();
            }

            private void ZSetStream()
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cReader));

                if (mStream == null)
                {
                    mStream = mItem.ReadStream;
                    if (!mStream.CanRead || !mStream.CanSeek) throw new cUnexpectedSectionCacheActionException(mContext);
                }
            }

            public void Dispose()
            {
                if (mDisposed) return;

                if (mStream != null)
                {
                    try { mStream.Dispose(); }
                    catch { }
                }

                if (Interlocked.Decrement(ref mCount) == 0) mItem.DecrementOpenStreamCount(mContext);

                mDisposed = true;
            }
        }
    }
}