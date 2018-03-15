using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private sealed partial class cConnection : IDisposable
                {
                    // state
                    private enum eState { notconnected, connecting, connected, disconnecting, disconnected }
                    private eState mState = eState.notconnected;

                    // sizing
                    private readonly cBatchSizer mWriteSizer;
                    private readonly Stopwatch mStopwatch = new Stopwatch();

                    // dns host
                    private string mDNSHost = null;

                    // network connections
                    private TcpClient mTCPClient = null;
                    private SslStream mSSLStream = null;
                    private Stream mStream = null;

                    // reader
                    private bool mClosed = false;

                    // security
                    private cSASLSecurity mSASLSecurity = null;

                    // builder
                    private readonly cResponseBuilder mBuilder = new cResponseBuilder();

                    // current buffer
                    private byte[] mBuffer = null;
                    private int mBufferPosition;

                    // task that is getting the next response
                    private Task<cResponse> mBuildResponseTask = null;

                    public cConnection(cBatchSizerConfiguration pWriteConfiguration)
                    {
                        mWriteSizer = new cBatchSizer(pWriteConfiguration);
                    }

                    public async Task ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(ConnectAsync), pMC, pServer);

                        if (mState != eState.notconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyConnected);

                        if (pServer == null) throw new ArgumentNullException(nameof(pServer));

                        mState = eState.connecting;
                        mDNSHost = cTools.GetDNSHost(pServer.Host);

                        try
                        {
                            lContext.TraceVerbose("creating tcpclient");

                            mTCPClient = new TcpClient();

                            mTCPClient.ReceiveTimeout = pMC.Timeout;
                            mTCPClient.SendTimeout = pMC.Timeout;

                            lContext.TraceVerbose("connecting tcpclient");

                            await mTCPClient.ConnectAsync(mDNSHost, pServer.Port).ConfigureAwait(false);

                            NetworkStream lStream = mTCPClient.GetStream();

                            if (pServer.SSL)
                            {
                                lContext.TraceVerbose("creating sslstream");

                                mSSLStream = new SslStream(lStream);

                                mSSLStream.ReadTimeout = pMC.Timeout;
                                mSSLStream.WriteTimeout = pMC.Timeout;

                                lContext.TraceVerbose("authenticating as client");

                                await mSSLStream.AuthenticateAsClientAsync(mDNSHost).ConfigureAwait(false);

                                mStream = mSSLStream;
                            }
                            else mStream = lStream;

                            mStream.ReadTimeout = -1;
                            mStream.WriteTimeout = -1;

                            mState = eState.connected;
                        }
                        catch
                        {
                            Disconnect(lContext);
                            throw;
                        }
                    }

                    public void InstallTLS(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(InstallTLS));

                        if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                        if (mSSLStream != null) throw new InvalidOperationException();
                        if (mBuildResponseTask != null) throw new InvalidOperationException();

                        lContext.TraceInformation("installing tls");

                        mSSLStream = new SslStream(mStream);
                        mSSLStream.AuthenticateAsClientAsync(mDNSHost);
                        mStream = mSSLStream;
                    }

                    public bool TLSInstalled => mSSLStream != null;

                    public void InstallSASLSecurity(cSASLSecurity pSASLSecurity, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(InstallSASLSecurity));

                        if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                        if (mSASLSecurity != null) throw new InvalidOperationException();

                        lContext.TraceInformation("installing sasl security");

                        mSASLSecurity = pSASLSecurity ?? throw new ArgumentNullException(nameof(pSASLSecurity));

                        if (mBuffer != null && mBufferPosition < mBuffer.Length)
                        {
                            lContext.TraceVerbose("decoding the remainder of the current buffer");
                            byte[] lBuffer = new byte[mBuffer.Length - mBufferPosition];
                            for (int i = mBufferPosition, j = 0; i < mBuffer.Length; i++, j++) lBuffer[j] = mBuffer[i];
                            ZNewBuffer(lBuffer, lContext);
                        }
                    }

                    public bool SASLSecurityInstalled => mSASLSecurity != null;

                    public Task GetBuildResponseTask(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(GetBuildResponseTask));

                        if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                        if (mBuildResponseTask == null)
                        {
                            lContext.TraceVerbose("starting a new buildresponse task");
                            mBuildResponseTask = ZBuildResponseAsync(lContext);
                        }

                        return mBuildResponseTask;
                    }

                    public cResponse GetResponse(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(GetResponse));
                        if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                        if (mBuildResponseTask == null || !mBuildResponseTask.IsCompleted) throw new InvalidOperationException();
                        cResponse lResult = mBuildResponseTask.Result;
                        mBuildResponseTask.Dispose();
                        mBuildResponseTask = null;
                        if (lContext.EmitsVerbose) ZLogResponse(lResult, lContext);
                        return lResult;
                    }

                    public async Task WriteAsync(byte[] pBuffer, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(WriteAsync));

                        if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                        byte[] lBuffer;

                        if (mSASLSecurity == null) lBuffer = pBuffer;
                        else
                        {
                            try { lBuffer = mSASLSecurity.Encode(pBuffer); }
                            catch (Exception e)
                            {
                                Disconnect(lContext);
                                throw new cSASLSecurityException("encoder failed", e, lContext);
                            }

                            if (lBuffer == null)
                            {
                                Disconnect(lContext);
                                throw new cSASLSecurityException("encoder failed", lContext);
                            }
                        }

                        mStopwatch.Restart();
                        await mStream.WriteAsync(lBuffer, 0, lBuffer.Length, pCancellationToken).ConfigureAwait(false);
                        mStopwatch.Stop();

                        // store the time taken so the next write is a better size
                        mWriteSizer.AddSample(lBuffer.Length, mStopwatch.ElapsedMilliseconds);
                    }

                    public int CurrentWriteSize => mWriteSizer.Current;

                    private void ZLogResponse(cResponse pResponse, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(ZLogResponse));

                        cByteList lLogBytes = new cByteList();

                        foreach (var lLine in pResponse)
                        {
                            if (lLine.Literal) lLogBytes.Add(cASCII.LBRACE);
                            else lLogBytes.Add(cASCII.LBRACKET);

                            foreach (var lByte in lLine)
                            {
                                if (lLogBytes.Count == 60)
                                {
                                    lContext.TraceVerbose($"received: {cTools.BytesToLoggableString(lLogBytes)}...");
                                    return;
                                }

                                lLogBytes.Add(lByte);
                            }

                            if (lLine.Literal) lLogBytes.Add(cASCII.RBRACE);
                            else lLogBytes.Add(cASCII.RBRACKET);
                        }

                        lContext.TraceVerbose($"received: {cTools.BytesToLoggableString(lLogBytes)}");
                    }

                    public async Task<cResponse> ZBuildResponseAsync(cTrace.cContext pParentContext)
                    {
                        // SUPERVERBOSE
                        var lContext = pParentContext.NewMethod(true, nameof(cConnection), nameof(ZBuildResponseAsync));

                        while (true)
                        {
                            if (mBuffer != null && mBufferPosition < mBuffer.Length)
                            {
                                cResponse lResponse = mBuilder.BuildFromBuffer(mBuffer, ref mBufferPosition, lContext);
                                if (lResponse != null) return lResponse;
                            }

                            var lBuffer = await ZReadAsync(lContext).ConfigureAwait(false);

                            ZNewBuffer(lBuffer, lContext);
                        }
                    }

                    public async Task<byte[]> ZReadAsync(cTrace.cContext pParentContext)
                    {
                        const int kBufferSize = 1000;

                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(ZReadAsync));

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
                            throw new cStreamClosedException();
                        }

                        Array.Resize(ref lBuffer, lByteCount);

                        return lBuffer;
                    }

                    private void ZNewBuffer(byte[] pBuffer, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(ZNewBuffer));

                        if (mSASLSecurity == null) mBuffer = pBuffer;
                        else
                        {
                            try
                            {
                                var lDecodedBuffer = mSASLSecurity.Decode(pBuffer);
                                if (lDecodedBuffer == null) mBuffer = null;
                                else mBuffer = lDecodedBuffer;
                            }
                            catch (Exception e)
                            {
                                Disconnect(lContext);
                                throw new cSASLSecurityException("decoder failed", e, lContext);
                            }
                        }

                        mBufferPosition = 0;
                    }

                    public void Disconnect(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(Disconnect));
                        Dispose();
                    }

                    public void Dispose()
                    {
                        if (mState == eState.disconnected) return;

                        mState = eState.disconnecting;

                        if (mSASLSecurity != null)
                        {
                            try { mSASLSecurity.Dispose(); }
                            catch { }
                        }

                        if (mStream != null)
                        {
                            try { mStream.Close(); }
                            catch { }
                        }

                        if (mSSLStream != null)
                        {
                            try { mSSLStream.Close(); }
                            catch { }
                        }

                        if (mTCPClient != null)
                        {
                            try { mTCPClient.Close(); }
                            catch { }
                        }

                        // this has to be done after closing the stream, as the only way the read can be cancelled is for the stream to be disposed
                        if (mBuildResponseTask != null)
                        {
                            try { mBuildResponseTask.Wait(); }
                            catch { }

                            try { mBuildResponseTask.Dispose(); }
                            catch { }
                        }

                        mState = eState.disconnected;
                    }

                    [Conditional("DEBUG")]
                    public static void _Tests(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(_Tests));
                        cResponseBuilder._Tests(lContext);
                    }
                }
            }
        }
    }
}
