using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private sealed partial class cConnection : IDisposable
            {
                // network connections
                private TcpClient mTCPClient = null;
                private SslStream mSSLStream = null;
                private Stream mStream = null;

                // reader
                private cStreamReader mStreamReader = null;

                // security
                private cSASLSecurity mSecurity = null;

                // builder
                private readonly cResponseBuilder mBuilder = new cResponseBuilder();

                // current buffer
                private byte[] mBuffer = null;
                private int mBufferPosition;

                // state
                private enum eState { notconnected, connecting, connected, disconnecting, disconnected }
                private eState mState = eState.notconnected;

                // task that is getting the next response
                private Task<cBytesLines> mBuildResponseTask = null;

                // for killing the above task when disposing
                private CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();

                public cConnection() { }

                public async Task ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(ConnectAsync), pMC, pServer);

                    if (mState != eState.notconnected) throw new InvalidOperationException("must be notconnected");

                    try
                    {
                        mState = eState.connecting;

                        lContext.TraceVerbose("creating tcpclient");

                        mTCPClient = new TcpClient();

                        mTCPClient.ReceiveTimeout = pMC.Timeout;
                        mTCPClient.SendTimeout = pMC.Timeout;

                        lContext.TraceVerbose("connecting tcpclient");

                        await mTCPClient.ConnectAsync(pServer.Host, pServer.Port).ConfigureAwait(false);

                        NetworkStream lStream = mTCPClient.GetStream();

                        if (pServer.SSL)
                        {
                            lContext.TraceVerbose("creating sslstream");

                            mSSLStream = new SslStream(lStream);

                            mSSLStream.ReadTimeout = pMC.Timeout;
                            mSSLStream.WriteTimeout = pMC.Timeout;

                            lContext.TraceVerbose("authenticating as client");

                            await mSSLStream.AuthenticateAsClientAsync(pServer.Host).ConfigureAwait(false);

                            mStream = mSSLStream;
                        }
                        else mStream = lStream;

                        mStreamReader = new cStreamReader(mStream, lContext);

                        mState = eState.connected;
                    }
                    catch
                    {
                        Disconnect(lContext);
                        throw;
                    }
                }

                public void SetCapability(cCapability pCapability, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(SetCapability), pCapability.Binary);
                    if (mState != eState.connected) throw new InvalidOperationException("must be connected");
                    mBuilder.Binary = pCapability.Binary;
                }

                public void InstallTLS(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(InstallSecurity));

                    if (mState != eState.connected) throw new InvalidOperationException("must be connected");
                    if (mSSLStream != null) throw new InvalidOperationException("tls already installed");

                    lContext.TraceInformation("installing TLS");








                    lContext.TraceInformation("installing security");







                    ;?;
                }

                public bool TLSInstalled => mSSLStream != null;

                public void InstallSecurity(cSASLSecurity pSecurity, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(InstallSecurity));

                    if (mState != eState.connected) throw new InvalidOperationException("must be connected");
                    if (mSecurity != null) throw new InvalidOperationException("security already installed");

                    lContext.TraceInformation("installing security");

                    mSecurity = pSecurity ?? throw new ArgumentNullException(nameof(pSecurity));

                    if (mBuffer != null && mBufferPosition < mBuffer.Length)
                    {
                        lContext.TraceVerbose("decoding the remainder of the current buffer");
                        byte[] lBuffer = new byte[mBuffer.Length - mBufferPosition];
                        for (int i = mBufferPosition, j = 0; i < mBuffer.Length; i++, j++) lBuffer[j] = mBuffer[i];
                        ZNewBuffer(lBuffer, lContext);
                    }
                }

                public bool SecurityInstalled => mSecurity != null;

                public Task GetAwaitResponseTask(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(GetAwaitResponseTask));

                    if (mState != eState.connected) throw new InvalidOperationException("must be connected");

                    if (mBuildResponseTask == null)
                    {
                        lContext.TraceVerbose("starting a new buildresponse task");
                        mBuildResponseTask = ZBuildResponseAsync(lContext);
                    }

                    return mBuildResponseTask;
                }

                public cBytesLines GetResponse(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(GetResponse));
                    if (mState != eState.connected) throw new InvalidOperationException("must be connected");
                    if (mBuildResponseTask == null || !mBuildResponseTask.IsCompleted) throw new InvalidOperationException("await response task must be complete");
                    cBytesLines lResult = mBuildResponseTask.Result;
                    mBuildResponseTask.Dispose();
                    mBuildResponseTask = null;
                    if (lContext.EmitsVerbose) ZLogResponse(lResult, lContext);
                    return lResult;
                }

                private void ZLogResponse(cBytesLines pResponse, cTrace.cContext pParentContext)
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

                public Task WriteAsync(cMethodControl pMC, byte[] pBuffer, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(WriteAsync), pMC);

                    if (mState != eState.connected) throw new InvalidOperationException("must be connected");

                    byte[] lBuffer;

                    if (mSecurity == null) lBuffer = pBuffer;
                    else
                    {
                        try { lBuffer = mSecurity.Encode(pBuffer); }
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

                    mStream.WriteTimeout = pMC.Timeout;

                    return mStream.WriteAsync(lBuffer, 0, lBuffer.Length, pMC.CancellationToken);
                }

                public async Task<cBytesLines> ZBuildResponseAsync(cTrace.cContext pParentContext)
                {
                    // SUPERVERBOSE
                    var lContext = pParentContext.NewMethod(true, nameof(cConnection), nameof(ZBuildResponseAsync));

                    while (true)
                    {
                        if (mBuffer != null && mBufferPosition < mBuffer.Length)
                        {
                            cBytesLines lLines = mBuilder.BuildFromBuffer(mBuffer, ref mBufferPosition, lContext);
                            if (lLines != null) return lLines;
                        }

                        var lBuffer = await mStreamReader.GetBufferAsync(mCancellationTokenSource.Token, lContext).ConfigureAwait(false);
                        ZNewBuffer(lBuffer, lContext);
                    }
                }

                private void ZNewBuffer(byte[] pBuffer, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(ZNewBuffer));

                    if (mSecurity == null) mBuffer = pBuffer;
                    else
                    {
                        try
                        {
                            var lDecodedBuffer = mSecurity.Decode(pBuffer);
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

                    if (mBuildResponseTask != null)
                    {
                        if (mCancellationTokenSource != null) mCancellationTokenSource.Cancel();

                        try { mBuildResponseTask.Wait(); }
                        catch { }

                        try { mBuildResponseTask.Dispose(); }
                        catch { }
                    }

                    if (mCancellationTokenSource != null)
                    {
                        try { mCancellationTokenSource.Dispose(); }
                        catch { }
                    }

                    if (mSecurity != null)
                    {
                        try { mSecurity.Dispose(); }
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

                    // has to be disposed after the connection is closed
                    //  because the closing of the connection is what kills the background reader task
                    //
                    if (mStreamReader != null)
                    {
                        try { mStreamReader.Dispose(); }
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
