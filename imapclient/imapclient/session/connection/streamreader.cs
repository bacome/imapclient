using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
                private sealed class cStreamReader : IDisposable
                {
                    private bool mStopped = false;
                    private bool mDisposed = false;

                    private readonly Stream mStream;
                    private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(0, 1);
                    private readonly Task mBackgroundTask;
                    private Exception mBackgroundTaskException = null;
                    private readonly ConcurrentQueue<byte[]> mBuffers = new ConcurrentQueue<byte[]>();

                    public cStreamReader(Stream pStream, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewObject(nameof(cStreamReader));
                        mStream = pStream;
                        mBackgroundTask = ZBackgroundTaskAsync(lContext);
                    }

                    public async Task<byte[]> GetBufferAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cStreamReader), nameof(GetBufferAsync));

                        if (mDisposed) throw new ObjectDisposedException(nameof(cStreamReader));

                        if (mStopped)
                        {
                            if (mBackgroundTaskException == null) throw new cStreamReaderStoppedException(lContext);
                            else throw new cStreamReaderStoppedException("streamreader is stopped", mBackgroundTaskException, lContext);
                        }

                        while (true)
                        {
                            if (mBuffers.TryDequeue(out byte[] lBuffer))
                            {
                                if (lBuffer == null)
                                {
                                    mStopped = true;
                                    if (mBackgroundTaskException == null) throw new cStreamReaderStoppedException("stream was closed", lContext);
                                    else new cStreamReaderStoppedException("streamreader is stopped", mBackgroundTaskException, lContext);
                                }

                                if (!lContext.ContextTraceDelay) lContext.TraceVerbose("read {0} bytes", lBuffer.Length);
                                return lBuffer;
                            }

                            if (!lContext.ContextTraceDelay) lContext.TraceVerbose("waiting");
                            await mSemaphore.WaitAsync(pCancellationToken).ConfigureAwait(false);
                        }
                    }

                    private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
                    {
                        // SUPERVERBOSE
                        var lContext = pParentContext.NewRootMethod(true, nameof(cStreamReader), nameof(ZBackgroundTaskAsync));

                        const int kBufferSize = 1000;
                        byte[] lBuffer = new byte[kBufferSize];
                        int lByteCount = 0;

                        mStream.ReadTimeout = System.Threading.Timeout.Infinite;

                        try
                        {
                            while (true)
                            {
                                lByteCount = await mStream.ReadAsync(lBuffer, 0, kBufferSize).ConfigureAwait(false);

                                if (lByteCount == 0)
                                {
                                    lContext.TraceInformation("stream closed");
                                    mBuffers.Enqueue(null);
                                    ZRelease(lContext);
                                    return;
                                }

                                if (!lContext.ContextTraceDelay) lContext.TraceVerbose("read {0} bytes", lByteCount);

                                byte[] lBytes = new byte[lByteCount];
                                Array.Copy(lBuffer, lBytes, lByteCount);
                                mBuffers.Enqueue(lBytes);

                                ZRelease(lContext);
                            }
                        }
                        catch (Exception e)
                        {
                            lContext.TraceException(TraceEventType.Information, "reader exiting", e);
                            mBackgroundTaskException = e;
                            mBuffers.Enqueue(null);
                            ZRelease(lContext);
                        }
                    }

                    private void ZRelease(cTrace.cContext pParentContext)
                    {                        
                        var lContext = pParentContext.NewMethod(nameof(cStreamReader), nameof(ZRelease));

                        if (mSemaphore.CurrentCount == 0)
                        {
                            if (!lContext.ContextTraceDelay) lContext.TraceVerbose("releasing semaphore");                           
                            mSemaphore.Release();
                        }
                    }

                    public void Dispose()
                    {
                        if (mDisposed) return;

                        if (mBackgroundTask != null)
                        {
                            try { mBackgroundTask.Wait(); }
                            catch { }

                            mBackgroundTask.Dispose();
                        }

                        if (mSemaphore != null)
                        {
                            try { mSemaphore.Dispose(); }
                            catch { }
                        }

                        mDisposed = true;
                    }
                }
            }
        }
    }
}


