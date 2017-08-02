using System;
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
            private static readonly cBytes kConnectAsteriskSpaceOKSpace = new cBytes("* OK ");
            private static readonly cBytes kConnectAsteriskSpacePreAuthSpace = new cBytes("* PREAUTH ");
            private static readonly cBytes kConnectAsteriskSpaceBYESpace = new cBytes("* BYE ");

            public async Task ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ConnectAsync), pMC, pServer);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notconnected) throw new InvalidOperationException();

                try
                {
                    ZSetState(eState.connecting, lContext);

                    await mConnection.ConnectAsync(pMC, pServer, lContext).ConfigureAwait(false);

                    var lCommandHook = new cCommandHookInitial(true);

                    using (cTerminator lTerminator = new cTerminator(pMC))
                    {
                        while (true)
                        {
                            lContext.TraceVerbose("waiting");
                            await lTerminator.WhenAny(mConnection.GetAwaitResponseTask(lContext)).ConfigureAwait(false);

                            var lLines = mConnection.GetResponse(lContext);
                            mEventSynchroniser.FireIncre;
                            var lCursor = new cBytesCursor(lLines);

                            if (lCursor.SkipBytes(kConnectAsteriskSpaceOKSpace))
                            {
                                cResponseText lResponseText = mResponseTextProcessor.Process(lCursor, eResponseTextType.greeting, lCommandHook, lContext);

                                lContext.TraceVerbose("got ok: {0}", lResponseText);

                                if (lCommandHook.Capabilities != null) ZSetCapabilities(lCommandHook.Capabilities, lCommandHook.AuthenticationMechanisms, lContext);
                                if (lCommandHook.HomeServerReferral != null) lContext.TraceError("received a referral on an ok greeting");
                                ZSetState(eState.notauthenticated, lContext);

                                return;
                            }
                            
                            if (lCursor.SkipBytes(kConnectAsteriskSpacePreAuthSpace))
                            {
                                cResponseText lResponseText = mResponseTextProcessor.Process(lCursor, eResponseTextType.greeting, lCommandHook, lContext);

                                lContext.TraceVerbose("got preauth: {0}", lResponseText);

                                if (lCommandHook.Capabilities != null) ZSetCapabilities(lCommandHook.Capabilities, lCommandHook.AuthenticationMechanisms, lContext);
                                if (lCommandHook.HomeServerReferral != null) ZSetHomeServerReferral(new cURL(lCommandHook.HomeServerReferral), lContext);
                                ZSetConnectedAccountId(new cAccountId(pServer.Host, eAccountType.none), lContext);

                                return;
                            }

                            if (lCursor.SkipBytes(kConnectAsteriskSpaceBYESpace))
                            {
                                cResponseText lResponseText = mResponseTextProcessor.Process(lCursor, eResponseTextType.greeting, lCommandHook, lContext);

                                lContext.TraceError("got bye: {0}", lResponseText);

                                if (lCommandHook.Capabilities != null) lContext.TraceError("received capability on a bye greeting");

                                Disconnect(lContext);

                                if (lCommandHook.HomeServerReferral != null)
                                {
                                    cURL lURL = new cURL(lCommandHook.HomeServerReferral);
                                    ZSetHomeServerReferral(lURL, lContext);
                                    throw new cHomeServerReferralException(lURL, lResponseText, lContext);
                                }

                                throw new cByeException(lResponseText, lContext);
                            }

                            lContext.TraceError("unrecognised response: {0}", lLines);
                        }
                    }
                }
                catch when (_State != eState.disconnected)
                {
                    Disconnect(lContext);
                    throw;
                }
            }
        }
    }
}
