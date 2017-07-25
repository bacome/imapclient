using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cConnection
            {
                private sealed class cStreamReader
                {
                    const int kBufferSize = 1000;

                    private bool mClosed = false;
                    private readonly Stream mStream;

                    public cStreamReader(Stream pStream, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewObject(nameof(cStreamReader));
                        mStream = pStream ?? throw new ArgumentNullException(nameof(pStream)); ;
                        mStream.ReadTimeout = System.Threading.Timeout.Infinite;
                    }

                    public async Task<byte[]> GetBufferAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cStreamReader), nameof(GetBufferAsync));

                        if (mClosed) throw new cStreamClosedException(lContext);

                        byte[] lBuffer = new byte[kBufferSize];

                        int lByteCount;

                        try { lByteCount = await mStream.ReadAsync(lBuffer, 0, kBufferSize).ConfigureAwait(false); }
                        catch (Exception e)
                        {
                            lContext.TraceException(e);
                            mClosed = true;
                            throw;
                        }

                        if (lByteCount == 0)
                        {
                            lContext.TraceInformation("stream closed");
                            mClosed = true;
                            throw new cStreamClosedException(lContext);
                        }

                        Array.Resize(ref lBuffer, lByteCount);

                        return lBuffer;
                    }
                }
            }
        }
    }
}


