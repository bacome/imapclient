using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ConnectAsync), pMC, pServer);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (_ConnectionState != eConnectionState.notconnected) throw new InvalidOperationException();
                ZSetState(eConnectionState.connecting, lContext);

                sGreeting lGreeting;

                try { lGreeting = await mPipeline.ConnectAsync(pMC, pServer, lContext).ConfigureAwait(false); }
                catch (Exception)
                {
                    ZSetState(eConnectionState.disconnected, lContext);
                    throw;
                }

                if (lGreeting.Type == eGreetingType.bye)
                {
                    ZSetState(eConnectionState.disconnected, lContext);
                    if (ZSetHomeServerReferral(lGreeting.ResponseText, lContext)) throw new cHomeServerReferralException(lGreeting.ResponseText, lContext);
                    throw new cConnectByeException(lGreeting.ResponseText, lContext);
                }

                if (lGreeting.Capabilities != null)
                {
                    mCapabilities = new cCapabilities(lGreeting.Capabilities, lGreeting.AuthenticationMechanisms, mIgnoreCapabilities);
                    mPipeline.SetCapabilities(mCapabilities, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
                }

                if (lGreeting.Type == eGreetingType.ok)
                {
                    ZSetState(eConnectionState.notauthenticated, lContext);
                    return;
                }

                // preauth

                ZSetHomeServerReferral(lGreeting.ResponseText, lContext);
                ZSetConnectedAccountId(new cAccountId(pServer.Host, eAccountType.none), lContext);
            }

            /*
            public async Task ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ConnectAsync), pMC, pServer);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notconnected) throw new InvalidOperationException();

                try
                {
                    ZSetState(eConnectionState.connecting, lContext);

                    await mConnection.ConnectAsync(pMC, pServer, lContext).ConfigureAwait(false);

                    var lHook = new cCommandHookInitial();

                    using (var lAwaiter = new cAwaiter(pMC))
                    {
                        while (true)
                        {
                            lContext.TraceVerbose("waiting");
                            await lAwaiter.AwaitAny(mConnection.GetBuildResponseTask(lContext)).ConfigureAwait(false);

                            var lLines = mConnection.GetResponse(lContext);
                            mSynchroniser.InvokeNetworkActivity(lLines, lContext);
                            var lCursor = new cBytesCursor(lLines);

                            if (lCursor.SkipBytes(kConnectAsteriskSpaceOKSpace))
                            {
                                cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.greeting, lCursor, lHook, lContext);

                                lContext.TraceVerbose("got ok: {0}", lResponseText);

                                if (lHook.Capabilities != null)
                                {
                                    mCapabilities = new cCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, mIgnoreCapabilities);
                                    mPipeline.SetCapability(mCapabilities, lContext);
                                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
                                }

                                ZSetState(eConnectionState.notauthenticated, lContext);

                                return;
                            }
                            
                            if (lCursor.SkipBytes(kConnectAsteriskSpacePreAuthSpace))
                            {
                                cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.greeting, lCursor, lHook, lContext);

                                lContext.TraceVerbose("got preauth: {0}", lResponseText);

                                if (lHook.Capabilities != null)
                                {
                                    mCapabilities = new cCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, mIgnoreCapabilities);
                                    mPipeline.SetCapability(mCapabilities, lContext);
                                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
                                }

                                ZSetHomeServerReferral(lResponseText, lContext);
                                ZSetConnectedAccountId(new cAccountId(pServer.Host, eAccountType.none), lContext);

                                return;
                            }

                            if (lCursor.SkipBytes(kConnectAsteriskSpaceBYESpace))
                            {
                                cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.greeting, lCursor, lHook, lContext);

                                lContext.TraceError("got bye: {0}", lResponseText);

                                if (lHook.Capabilities != null) lContext.TraceError("received capability on a bye greeting");

                                Disconnect(lContext);

                                if (ZSetHomeServerReferral(lResponseText, lContext)) throw new cHomeServerReferralException(lResponseText, lContext);
                                throw new cConnectByeException(lResponseText, lContext);
                            }

                            lContext.TraceError("unrecognised response: {0}", lLines);
                        }
                    }
                }
                catch when (_ConnectionState != eConnectionState.disconnected)
                {
                    Disconnect(lContext);
                    throw;
                }
            } */
        } 
    }
}
