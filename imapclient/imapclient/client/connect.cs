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

            mSession = new cSession(mEventSynchroniser, mIgnoreCapabilities, mMailboxFlagSets, mIdleConfiguration, mFetchAttributesConfiguration, mFetchBodyReadConfiguration, mEncoding, lContext);
            var lSession = mSession;

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                await lSession.ConnectAsync(lMC, lServer, lContext).ConfigureAwait(false);

                if (lSession.Capability == null) await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);

                if (lSession.State == eState.notauthenticated && !lSession.TLSInstalled && lSession.Capability.StartTLS)
                {
                    await lSession.StartTLSAsync(lMC, lContext).ConfigureAwait(false);
                    await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);
                }

                object lOriginalCapability = lSession.Capability;
                cCapability lCurrentCapability = lSession.Capability;

                if (lSession.State == eState.notauthenticated)
                {
                    bool lTLSIssue = false;
                    bool lTriedCredentials = false;
                    Exception lAuthenticateException = null;

                    cAccountId lAccountId = new cAccountId(lServer.Host, lCredentials.Type, lCredentials.UserId);

                    bool lTLSInstalled = lSession.TLSInstalled;

                    if (Credentials.TryAllSASLs)
                    {
                        foreach (var lSASL in Credentials.SASLs)
                        {
                            if ((lSASL.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (lSASL.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                            else
                            {
                                lTriedCredentials = true;
                                lAuthenticateException = await lSession.AuthenticateAsync(lMC, lAccountId, lSASL, lContext).ConfigureAwait(false);
                                if (lSession.State != eState.notauthenticated || lAuthenticateException != null) break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var lSASL in Credentials.SASLs)
                        {
                            if (lCurrentCapability.SupportsAuthenticationMechanism(lSASL.MechanismName))
                            {
                                if ((lSASL.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (lSASL.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                                else
                                {
                                    lTriedCredentials = true;
                                    lAuthenticateException = await lSession.AuthenticateAsync(lMC, lAccountId, lSASL, lContext).ConfigureAwait(false);
                                    if (lSession.State != eState.notauthenticated || lAuthenticateException != null) break;
                                }
                            }
                        }
                    }

                    if (lSession.State == eState.notauthenticated && lAuthenticateException == null && !lCurrentCapability.LoginDisabled && Credentials.Login != null)
                    {
                        if ((Credentials.Login.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (Credentials.Login.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                        else
                        {
                            lTriedCredentials = true;
                            lAuthenticateException = await lSession.LoginAsync(lMC, lAccountId, Credentials.Login, lContext).ConfigureAwait(false);
                        }
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
                        
                        if (lTLSIssue) throw new cAuthenticationTLSException(lContext); // we didn't try some mechanisms because of the TLS state
                        throw new cAuthenticationException(lContext); // the server has no mechanisms that we can try
                    }

                    // re-get the capabilities if we didn't get new ones as part of the authentication/ login OR if a security layer was installed (SASL requires this)
                    if (ReferenceEquals(lOriginalCapability, lSession.Capability) || lSession.SASLSecurityInstalled) await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);
                    lCurrentCapability = lSession.Capability;
                }

                if (lCurrentCapability.Enable)
                {
                    fEnableableExtensions lExtensions = fEnableableExtensions.none;
                    if (lCurrentCapability.UTF8Accept || lCurrentCapability.UTF8Only) lExtensions = lExtensions | fEnableableExtensions.utf8;
                    if (lExtensions != fEnableableExtensions.none) await lSession.EnableAsync(lMC, lExtensions, lContext).ConfigureAwait(false);
                }

                // enabled (lock the capabilities and enabled extensions)
                lSession.Enabled(lContext);

                Task lIdTask;

                if (lCurrentCapability.Id)
                {
                    cIdReadOnlyDictionary lDictionary;

                    if ((lSession.EnabledExtensions & fEnableableExtensions.utf8) == 0) lDictionary = mClientId?.ASCIIDictionary;
                    else lDictionary = mClientId?.Dictionary;

                    lIdTask = lSession.IdAsync(lMC, lDictionary, lContext);
                }
                else lIdTask = null;

                Task lNamespaceTask;

                ;?; // do the namespace if it is allowed

                ;?; // check the personal namespace, looking for the one with the inbox in it : this is so we can find the delimiter.
                ;?; // if there isn't one , do the special list to find the delimiter
                ;?; // store the delimiter as a property so that when the inbox is requested we can make it

















                Task<List<iMailboxHandle>> lListTask;

                if (lCurrentCapability.Namespace)
                {
                    lNamespaceTask = lSession.NamespaceAsync(lMC, lContext);
                    lListTask = null;
                }
                else
                {
                    lNamespaceTask = null;
                    lListTask = lSession.ListAsync(lMC, string.Empty, null, new cMailboxNamePattern(string.Empty, string.Empty, null), lContext);
                }

                // wait for everything to complete
                await cTerminator.AwaitAll(lMC, lIdTask, lNamespaceTask, lListTask).ConfigureAwait(false);

                // set the namespace property
                //
                if (!lCurrentCapability.Namespace)
                {
                    var lHandles = lListTask.Result;
                    if (lHandles.Count != 1) throw new cUnexpectedServerActionException(0, "list special request failed", lContext);
                    lSession.SetNamespaces(new cNamespaceList(lHandles[0].MailboxName.Delimiter), null, null, lContext);
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
                            lSession.Inbox = new cMailbox(this, lSession.GetMailboxHandle(new cMailboxName(cMailboxName.InboxString, lNamespace.Delimiter)));
                            break;
                        }
                    }
                }

                // ready for action
                lSession.Initialised(lContext);
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