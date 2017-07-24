using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Connect()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Connect));
            mEventSynchroniser.Wait(ZConnectAsync(lContext), lContext);
        }

        public Task ConnectAsync()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConnectAsync));
            return ZConnectAsync(lContext);
        }

        private async Task ZConnectAsync(cTrace.cContext pParentContext)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ZConnectAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            cServer lServer = Server;
            cCredentials lCredentials = Credentials;

            if (lServer == null) throw new InvalidOperationException("connect requires server to be set");
            if (lCredentials == null) throw new InvalidOperationException("connect requires credentials to be set");

            if (mSession != null)
            {
                if (!mSession.IsUnconnected) throw new InvalidOperationException("must be unconnected");
                mSession.Dispose();
            }

            mSession = new cSession(mEventSynchroniser, mIgnoreCapabilities, mIdleConfiguration, mFetchAttributesConfiguration, mFetchBodyReadConfiguration, mEncoding, lContext);
            var lSession = mSession;

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                await lSession.ConnectAsync(lMC, lServer, lContext).ConfigureAwait(false);

                if (lSession.Capability == null) await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);

                if (!lServer.SSL && lSession.Capability.StartTLS)
                {
                    await lSession.StartTLSAsync(lMC, lContext).ConfigureAwait(false);
                    await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);
                }

                object lOriginalCapability = lSession.Capability;
                cCapability lCurrentCapability = lSession.Capability;

                if (lSession.State == eState.notauthenticated)
                {
                    bool lTriedCredentials = false;
                    Exception lAuthenticateException = null;

                    cAccountId lAccountId = new cAccountId(lServer.Host, lCredentials.Type, lCredentials.UserId);

                    if (Credentials.TryAllSASLs)
                    {
                        foreach (var lSASL in Credentials.SASLs)
                        {
                            ;?; // check the require tls


                            lTriedCredentials = true;
                            lAuthenticateException = await lSession.AuthenticateAsync(lMC, lAccountId, lSASL, lContext).ConfigureAwait(false);
                            if (lSession.State != eState.notauthenticated || lAuthenticateException != null) break;
                        }
                    }
                    else
                    {
                        foreach (var lSASL in Credentials.SASLs)
                        {
                            if (lCurrentCapability.SupportsAuthenticationMechanism(lSASL.MechanismName))
                            {
                                ;?; // check the require tls

                                lTriedCredentials = true;
                                lAuthenticateException = await lSession.AuthenticateAsync(lMC, lAccountId, lSASL, lContext).ConfigureAwait(false);
                                if (lSession.State != eState.notauthenticated || lAuthenticateException != null) break;
                            }
                        }
                    }

                    if (lSession.State == eState.notauthenticated && lAuthenticateException == null && !lCurrentCapability.LoginDisabled && Credentials.Login != null)
                    {
                        ;?; // check require tls
                        lTriedCredentials = true;
                        lAuthenticateException = await lSession.LoginAsync(lMC, lAccountId, Credentials.Login, lContext).ConfigureAwait(false);
                    }

                    if (lSession.State != eState.authenticated)
                    {
                        lContext.TraceError("could not authenticate");

                        // log out
                        await lSession.LogoutAsync(lMC, lContext).ConfigureAwait(false);

                        // throw an exception that indicates why we couldn't connect
                        if (lTriedCredentials)
                        {
                            if (lAuthenticateException != null) throw lAuthenticateException;
                            throw new cCredentialsException(lContext);
                        }

                        throw new cAuthenticationException(lContext); // the server has no mechanisms that we can try
                    }

                    // re-get the capabilities if we didn't get new ones as part of the authentication/ login OR if a security layer was installed (SASL requires this)
                    if (ReferenceEquals(lOriginalCapability, lSession.Capability) || lSession.SecurityInstalled) await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);
                    lCurrentCapability = lSession.Capability;
                }

                if (lCurrentCapability.Enable)
                {
                    fEnableableExtensions lExtensions = fEnableableExtensions.none;
                    if (lCurrentCapability.UTF8Accept || lCurrentCapability.UTF8Only) lExtensions = lExtensions | fEnableableExtensions.utf8;
                    if (lExtensions != fEnableableExtensions.none) await lSession.EnableAsync(lMC, lExtensions, lContext).ConfigureAwait(false);
                }

                ;?; // call post enable initialisation



                ;?; // id goes here

                // start an id using the ASCII dictionary (we don't know if the server supports UTF8 yet and even if we did we can't turn UTF8 on until we are authenticated)
                if (lCurrentCapability.Id) lIdTask = lSession.IdAsync(lMC, mClientId?.ASCIIDictionary, lContext);




                if (lIdTask == null && 

                        ;?; // do this after enable done

                        // if we just enabled UTF8 redo the id incase 1) we have UTF8 in our id OR 2) the server has UTF8 in its id (that it couldn't send before)
                        if ((lSession.EnabledExtensions & fEnableableExtensions.utf8) != 0 && lCurrentCapability.Id)
                        {
                            if (lIdTask != null) await cTerminator.AwaitAll(lMC, lIdTask).ConfigureAwait(false);
                            lIdTask = lSession.IdAsync(lMC, mClientId?.Dictionary, lContext);
                        }
                    }
                }
                else
                {
                    // if we haven't done an id yet, now do one
                    if (lIdTask == null && lCurrentCapability.Id) lIdTask = lSession.IdAsync(lMC, mClientId?.ASCIIDictionary, lContext);
                }





                if (lCurrentCapability.Enable)
                {
                    fEnableableExtensions lExtensions = fEnableableExtensions.none;

                    if (lCurrentCapability.UTF8Accept || lCurrentCapability.UTF8Only) lExtensions = lExtensions | fEnableableExtensions.utf8;

                    if (lExtensions != fEnableableExtensions.none)
                    {
                        await lSession.EnableAsync(lMC, lExtensions, lContext).ConfigureAwait(false);

                        ;?; // do this after enable done

                        // if we just enabled UTF8 redo the id incase 1) we have UTF8 in our id OR 2) the server has UTF8 in its id (that it couldn't send before)
                        if ((lSession.EnabledExtensions & fEnableableExtensions.utf8) != 0 && lCurrentCapability.Id) 
                        {
                            if (lIdTask != null) await cTerminator.AwaitAll(lMC, lIdTask).ConfigureAwait(false);
                            lIdTask = lSession.IdAsync(lMC, mClientId?.Dictionary, lContext);
                        }
                    }
                }
                else
                {
                    // if we haven't done an id yet, now do one
                    if (lIdTask == null && lCurrentCapability.Id) lIdTask = lSession.IdAsync(lMC, mClientId?.ASCIIDictionary, lContext);
                }





                // further initialise the session
                lSession.EnableDone(lContext);




                if (




                // do a namespace (or list) now ... AFTER possibly enabling UTF8 (namespace processing depends on UTF8)
                Task lNamespaceTask;
                Task<cMailboxList> lListTask;

                if (lCurrentCapability.Namespace)
                {
                    lNamespaceTask = lSession.NamespaceAsync(lMC, lContext);
                    lListTask = null;
                }
                else
                {
                    lNamespaceTask = null;
                    lListTask = lSession.ListAsync(lMC, new cListPattern(string.Empty, null, new cMailboxNamePattern(string.Empty, string.Empty, null)), lContext);
                }

                // wait for everything to complete
                await cTerminator.AwaitAll(lMC, lIdTask, lNamespaceTask, lListTask).ConfigureAwait(false);

                // set the namespace property
                //
                if (!lCurrentCapability.Namespace)
                {
                    var lMailboxes = lListTask.Result;
                    if (lMailboxes.Count != 1) throw new cUnexpectedServerActionException(0, "list special request failed", lContext);
                    lSession.SetNamespaces(new cNamespaceList(lMailboxes.FirstItem().MailboxName.Delimiter), null, null, lContext);
                }

                // set the inbox property
                //
                if (lSession.PersonalNamespaces != null)
                {
                    foreach (var lNamespace in lSession.PersonalNamespaces)
                    {
                        cMailboxNamePattern lPattern = new cMailboxNamePattern(lNamespace.Prefix, "%", lNamespace.Delimiter);

                        if (lPattern.Matches(cMailboxName.InboxString))
                        {
                            lSession.Inbox = new cMailbox(this, new cMailboxId(lSession.ConnectedAccountId, new cMailboxName(cMailboxName.InboxString, lNamespace.Delimiter)));
                            break;
                        }
                    }
                }
            }
            catch when (lSession.State != eState.disconnected)
            {
                lSession.Disconnect(lContext);
                throw;
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}