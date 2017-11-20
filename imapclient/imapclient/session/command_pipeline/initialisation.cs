using System;
using System.Collections.Generic;
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
            private partial class cCommandPipeline
            {
                private static readonly cBytes kGreetingAsteriskSpaceOKSpace = new cBytes("* OK ");
                private static readonly cBytes kGreetingAsteriskSpacePreAuthSpace = new cBytes("* PREAUTH ");
                private static readonly cBytes kGreetingAsteriskSpaceBYESpace = new cBytes("* BYE ");

                public async Task<sGreeting> ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ConnectAsync), pMC, pServer);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));

                    if (mState != eState.notconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                    mState = eState.connecting;

                    try
                    {
                        await mConnection.ConnectAsync(pMC, pServer, lContext).ConfigureAwait(false);

                        var lHook = new cCommandHookInitial();

                        using (var lAwaiter = new cAwaiter(pMC))
                        {
                            while (true)
                            {
                                lContext.TraceVerbose("waiting");
                                await lAwaiter.AwaitAny(mConnection.GetBuildResponseTask(lContext)).ConfigureAwait(false);

                                var lLines = mConnection.GetResponse(lContext);
                                mSynchroniser.InvokeNetworkReceive(lLines, lContext);
                                var lCursor = new cBytesCursor(lLines);

                                if (lCursor.SkipBytes(kGreetingAsteriskSpaceOKSpace))
                                {
                                    cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextContext.greeting, lCursor, lHook, lContext);
                                    lContext.TraceVerbose("got ok: {0}", lResponseText);

                                    mState = eState.connected;
                                    mBackgroundTask = ZBackgroundTaskAsync(lContext);
                                    return new sGreeting(eGreetingType.ok, null, lHook.Capabilities, lHook.AuthenticationMechanisms);
                                }

                                if (lCursor.SkipBytes(kGreetingAsteriskSpacePreAuthSpace))
                                {
                                    cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextContext.greeting, lCursor, lHook, lContext);
                                    lContext.TraceVerbose("got preauth: {0}", lResponseText);

                                    mState = eState.connected;
                                    mBackgroundTask = ZBackgroundTaskAsync(lContext);
                                    return new sGreeting(eGreetingType.preauth, lResponseText, lHook.Capabilities, lHook.AuthenticationMechanisms);
                                }

                                if (lCursor.SkipBytes(kGreetingAsteriskSpaceBYESpace))
                                {
                                    cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextContext.greeting, lCursor, lHook, lContext);
                                    lContext.TraceError("got bye: {0}", lResponseText);

                                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a bye greeting");

                                    mConnection.Disconnect(lContext);

                                    mState = eState.stopped;
                                    return new sGreeting(eGreetingType.bye, lResponseText, null, null);
                                }

                                lContext.TraceError("unrecognised response: {0}", lLines);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        mConnection.Disconnect(lContext);
                        mState = eState.stopped;
                        throw;
                    }
                }

                public void SetCapabilities(cCapabilities pCapabilities, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetCapabilities));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                    if (pCapabilities == null) throw new ArgumentNullException(nameof(pCapabilities));

                    mLiteralPlus = pCapabilities.LiteralPlus;
                    mLiteralMinus = pCapabilities.LiteralMinus;
                }

                public bool TLSInstalled => mConnection.TLSInstalled;

                public void InstallTLS(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(InstallTLS));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                    mConnection.InstallTLS(lContext);
                }

                private static readonly cBytes kSASLAsterisk = new cBytes("*");
                private static readonly cBytes kSASLAuthenticationResponse = new cBytes("<SASL authentication response>");

                private async Task ZProcessChallengeAsync(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessChallengeAsync));

                    IList<byte> lResponse;

                    if (cBase64.TryDecode(pCursor.GetRestAsBytes(), out var lChallenge, out var lError))
                    {
                        try { lResponse = mCurrentCommand.GetAuthenticationResponse(lChallenge); }
                        catch (Exception e)
                        {
                            lContext.TraceException("SASL authentication object threw", e);
                            lResponse = null;
                        }
                    }
                    else
                    {
                        lContext.TraceError("Could not decode challenge: {0}", lError);
                        lResponse = null;
                    }

                    byte[] lBuffer;

                    if (lResponse == null)
                    {
                        lContext.TraceVerbose("sending cancellation");
                        lBuffer = new byte[] { cASCII.ASTERISK, cASCII.CR, cASCII.LF };
                        mSynchroniser.InvokeNetworkSend(kSASLAsterisk, lContext);
                        mCurrentCommand.WaitingForContinuationRequest = false;
                    }
                    else
                    {
                        lContext.TraceVerbose("sending response");
                        cByteList lBytes = cBase64.Encode(lResponse);
                        lBytes.Add(cASCII.CR);
                        lBytes.Add(cASCII.LF);
                        lBuffer = lBytes.ToArray();
                        mSynchroniser.InvokeNetworkSend(kSASLAuthenticationResponse, lContext);
                    }

                    await mConnection.WriteAsync(lBuffer, mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);
                }

                public bool SASLSecurityInstalled => mConnection.SASLSecurityInstalled;

                public void InstallSASLSecurity(cSASLSecurity pSASLSecurity, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(InstallSASLSecurity));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                    mConnection.InstallSASLSecurity(pSASLSecurity, lContext);
                }

                public void Enable(cMailboxCache pMailboxCache, cCapabilities pCapabilities, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(Enable));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                    if (pMailboxCache == null) throw new ArgumentNullException(nameof(pMailboxCache));
                    if (pCapabilities == null) throw new ArgumentNullException(nameof(pCapabilities));

                    mResponseTextProcessor.Enable(pMailboxCache, lContext);

                    mMailboxCache = pMailboxCache;

                    mLiteralPlus = pCapabilities.LiteralPlus;
                    mLiteralMinus = pCapabilities.LiteralMinus;
                    mIdleCommandSupported = pCapabilities.Idle;

                    lock (mPipelineLock)
                    {
                        if (mState == eState.connected) mState = eState.enabled;
                    }

                    mBackgroundReleaser.Release(lContext); // to allow idle to start
                }

                public void Install(iResponseTextCodeParser pResponseTextCodeParser) => mResponseTextProcessor.Install(pResponseTextCodeParser);
                public void Install(iResponseDataParser pResponseDataParser) => mResponseDataParsers.Add(pResponseDataParser);
                public void Install(cUnsolicitedDataProcessor pUnsolicitedDataProcessor) => mUnsolicitedDataProcessors.Add(pUnsolicitedDataProcessor);
            }
        }
    }
}