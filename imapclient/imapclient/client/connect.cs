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

            bool lSessionReplaced;

            if (mSession == null) lSessionReplaced = false;
            else
            {
                if (!mSession.IsUnconnected) throw new InvalidOperationException("must be unconnected");
                mSession.Dispose();

                lSessionReplaced = true;

                mNamespaces = null;

                mInbox = null;
                mEventSynchroniser.FirePropertyChanged(nameof(Inbox), lContext);
            }

            mSession = new cSession(mEventSynchroniser, mIgnoreCapabilities, mMailboxCacheData, mIdleConfiguration, mFetchAttributesConfiguration, mFetchBodyReadConfiguration, mEncoding, lContext);
            var lSession = mSession;

            if (lSessionReplaced)
            {
                mEventSynchroniser.FirePropertyChanged(nameof(Capabilities), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(ConnectionState), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(IsConnected), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(IsUnconnected), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(ConnectedAccountId), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(EnabledExtensions), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(HomeServerReferral), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(ServerId), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(Namespaces), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(SelectedMailbox), lContext);
                mEventSynchroniser.FirePropertyChanged(nameof(SelectedMailboxDetails), lContext);
            }

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);

                try
                {
                    await lSession.ConnectAsync(lMC, lServer, lContext).ConfigureAwait(false);

                    if (lSession.Capabilities == null) await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);

                    if (lSession.ConnectionState == eConnectionState.notauthenticated && !lSession.TLSInstalled && lSession.Capabilities.StartTLS)
                    {
                        await lSession.StartTLSAsync(lMC, lContext).ConfigureAwait(false);
                        await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);
                    }

                    object lOriginalCapabilities = lSession.Capabilities;
                    cCapabilities lCurrentCapabilities = lSession.Capabilities;

                    if (lSession.ConnectionState == eConnectionState.notauthenticated)
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
                                    if (lSession.ConnectionState != eConnectionState.notauthenticated || lAuthenticateException != null) break;
                                }
                            }
                        }
                        else
                        {
                            foreach (var lSASL in Credentials.SASLs)
                            {
                                if (lCurrentCapabilities.AuthenticationMechanisms.Contains(lSASL.MechanismName))
                                {
                                    if ((lSASL.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (lSASL.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                                    else
                                    {
                                        lTriedCredentials = true;
                                        lAuthenticateException = await lSession.AuthenticateAsync(lMC, lAccountId, lSASL, lContext).ConfigureAwait(false);
                                        if (lSession.ConnectionState != eConnectionState.notauthenticated || lAuthenticateException != null) break;
                                    }
                                }
                            }
                        }

                        if (lSession.ConnectionState == eConnectionState.notauthenticated && lAuthenticateException == null && !lCurrentCapabilities.LoginDisabled && Credentials.Login != null)
                        {
                            if ((Credentials.Login.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (Credentials.Login.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                            else
                            {
                                lTriedCredentials = true;
                                lAuthenticateException = await lSession.LoginAsync(lMC, lAccountId, Credentials.Login, lContext).ConfigureAwait(false);
                            }
                        }

                        if (lSession.ConnectionState != eConnectionState.authenticated)
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

                            throw new cAuthenticationMechanismsException(lTLSIssue, lContext); // the server has no mechanisms that we can try
                        }

                        // re-get the capabilities if we didn't get new ones as part of the authentication/ login OR if a security layer was installed (SASL requires this)
                        if (ReferenceEquals(lOriginalCapabilities, lSession.Capabilities) || lSession.SASLSecurityInstalled) await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);
                        lCurrentCapabilities = lSession.Capabilities;
                    }

                    if (lCurrentCapabilities.Enable)
                    {
                        fEnableableExtensions lExtensions = fEnableableExtensions.none;
                        if (lCurrentCapabilities.UTF8Accept || lCurrentCapabilities.UTF8Only) lExtensions = lExtensions | fEnableableExtensions.utf8;
                        if (lExtensions != fEnableableExtensions.none) await lSession.EnableAsync(lMC, lExtensions, lContext).ConfigureAwait(false);
                    }

                    // enabled (lock in the capabilities and enabled extensions)
                    lSession.SetEnabled(lContext);

                    Task lIdTask;

                    if (lCurrentCapabilities.Id)
                    {
                        cIdDictionary lDictionary;

                        if ((lSession.EnabledExtensions & fEnableableExtensions.utf8) == 0) lDictionary = mClientId;
                        else lDictionary = mClientIdUTF8 ?? mClientId;

                        lIdTask = lSession.IdAsync(lMC, lDictionary, lContext);
                    }
                    else lIdTask = null;

                    if (lCurrentCapabilities.Namespace)
                    {
                        await lSession.NamespaceAsync(lMC, lContext).ConfigureAwait(false);

                        var lPersonalNamespaceNames = lSession.NamespaceNames?.Personal;

                        if (lPersonalNamespaceNames != null)
                        {
                            foreach (var lName in lPersonalNamespaceNames)
                            {
                                cMailboxPathPattern lPattern = new cMailboxPathPattern(lName.Prefix, "%", lName.Delimiter);

                                if (lPattern.Matches(cMailboxName.InboxString))
                                {
                                    mInbox = new cMailbox(this, lSession.GetMailboxHandle(new cMailboxName(cMailboxName.InboxString, lName.Delimiter)));
                                    mEventSynchroniser.FirePropertyChanged(nameof(Inbox), lContext);
                                    break;
                                }
                            }
                        }
                    }

                    if (mInbox == null)
                    {
                        var lDelimiter = await lSession.ListDelimiterAsync(lMC, lContext).ConfigureAwait(false);

                        if (!lCurrentCapabilities.Namespace)
                        {
                            mNamespaces = new cNamespaces(this, new cNamespaceName[] { new cNamespaceName("", lDelimiter) }, null, null);
                            mEventSynchroniser.FirePropertyChanged(nameof(Namespaces), lContext);
                        }

                        mInbox = new cMailbox(this, lSession.GetMailboxHandle(new cMailboxName(cMailboxName.InboxString, lDelimiter)));
                        mEventSynchroniser.FirePropertyChanged(nameof(Inbox), lContext);
                    }

                    // wait for id to complete
                    if (lIdTask != null) await lIdTask.ConfigureAwait(false);

                    // initialised (namespaces set, inbox available, id available (if server supports it); user may now issue commands)
                    lSession.SetInitialised(lContext);
                }
                catch when (lSession.ConnectionState != eConnectionState.disconnected)
                {
                    lSession.Disconnect(lContext);
                    throw;
                }
            }
        }
    }
}