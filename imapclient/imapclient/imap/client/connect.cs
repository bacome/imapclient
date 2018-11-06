using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Connects to the IMAP service identified by the <see cref="ServiceId"/> using the <see cref="Authentication"/>. 
        /// May only be called when the instance <see cref="IsUnconnected"/>.
        /// Will throw if an authenticated IMAP connection cannot be established.
        /// </summary>
        /// <remarks>
        /// <para>
        /// TLS is established if possible before authentication is attempted.
        /// TLS will be established immediately upon connect if the <see cref="ServiceId"/> indicates that the service requires this (<see cref="cServiceId.SSL"/>),
        /// otherwise the library will use the IMAP STARTTLS command if <see cref="cIMAPCapabilities.StartTLS"/> is in use.
        /// </para>
        /// <para>
        /// During the authentication part of connecting the <see cref="Capabilities"/> will be set (most likely more than once).
        /// The <see cref="IgnoreCapabilities"/> value is used to determine what capabilities offered by the server are actually used by the client.
        /// Any attempted SASL authentications that fail will result in entries in the collection returned by <see cref="FailedSASLAuthentications"/>.
        /// It is possible that the <see cref="HomeServerReferral"/> will be set during authentication: this indicates that the connected server suggests that we disconnect and try a different server.
        /// If authentication is successful then <see cref="ConnectedAccountId"/> will be set.
        /// </para>
        /// <para>
        /// After authentication, depending on what the <see cref="Capabilities"/> allow;
        /// <list type="bullet">
        /// <item><see cref="fEnableableExtensions.utf8"/> and <see cref="fEnableableExtensions.qresync"/> are enabled (see <see cref="cIMAPCapabilities.UTF8Accept"/>, <see cref="cIMAPCapabilities.UTF8Only"/> and <see cref="cIMAPCapabilities.QResync"/>); this sets <see cref="EnabledExtensions"/>, <see cref="UTF8Enabled"/> and <see cref="MessageSizesAreReliable"/>.</item>
        /// <item>ID (RFC 2971) information is exchanged with the server; this sends <see cref="ClientId"/> (or <see cref="ClientIdUTF8"/>) and sets <see cref="ServerId"/>.</item>
        /// <item>Namespace (RFC 2342) information is retrieved from the server; this sets <see cref="Namespaces"/>.</item>
        /// <item>A special syntax IMAP LIST command is used to discover the hierarchy delimiter and one personal namespace may be generated using it; this sets <see cref="Namespaces"/>.</item>
        /// </list>
        /// </para>
        /// <para>
        /// Normally only one of NAMESPACE and LIST are used during connect, but under some strange circumstances both may be required.
        /// (The specific case is when the personal namespaces retrieved from the server do not contain the INBOX.)
        /// Once <see cref="Namespaces"/> is known <see cref="Inbox"/> is set.
        /// </para>
        /// <para>
        /// At the end of a successful connect the <see cref="ConnectionState"/> will be <see cref="eIMAPConnectionState.notselected"/>,
        /// at the end of a failed connect <see cref="ConnectionState"/> will be <see cref="eIMAPConnectionState.disconnected"/> and this method will throw.
        /// </para>
        /// <para>Some of the exceptions that might be thrown and why;
        /// <list type="bullet">
        /// <item>
        ///   <term><see cref="cConnectByeException"/></term>
        ///   <description>
        ///   The server explicitly rejected the attempt to connect.
        ///   </description>
        /// </item>
        /// <item>
        ///   <term><see cref="cUnexpectedPreAuthenticatedConnectionException"/></term>
        ///   <description>
        ///   The server connected pre-authenticated when the <see cref="Authentication"/> did not anticipate this.
        ///   </description>
        /// </item>
        /// <item>
        ///   <term><see cref="cIMAPCredentialsException"/></term>
        ///   <description>
        ///   The client was able to try credentials from <see cref="Authentication"/>, but they didn't work.
        ///   If the server explicitly rejected the credentials using one of the 
        ///   <see cref="eIMAPResponseTextCode.authenticationfailed"/>, <see cref="eIMAPResponseTextCode.authorizationfailed"/> or <see cref="eIMAPResponseTextCode.expired"/> codes,
        ///   then <see cref="cIMAPCredentialsException.ResponseText"/> will contain the details (otherwise <see cref="cIMAPCredentialsException.ResponseText"/> will be <see langword="null"/>).
        ///   </description>
        /// </item>
        /// <item>
        ///   <term><see cref="cAuthenticationMechanismsException"/></term>
        ///   <description>
        ///   The client was not able to try any credentials from <see cref="Authentication"/>. 
        ///   If the TLS state of the connection was to blame for this then <see cref="cAuthenticationMechanismsException.TLSIssue"/> will be set to <see langword="true"/>.
        ///   </description>
        /// </item>
        /// <item>
        ///   <term><see cref="cIMAPHomeServerReferralException"/></term>
        ///   <description>
        ///   While connecting the server explicitly rejected the attempt to connect or authenticate and as part of the rejection suggested that we try connecting to a different server instead
        ///   (see <see cref="cIMAPHomeServerReferralException.ResponseText"/> and the contained <see cref="cIMAPResponseText.Arguments"/>).
        ///   </description>
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        public void Connect()
        {
            var lContext = RootContext.NewMethod(nameof(cIMAPClient), nameof(Connect));
            mSynchroniser.Wait(ZConnectAsync(lContext), lContext);
        }

        /// <summary>
        /// Asynchronously connects to the IMAP service identified by the <see cref="ServiceId"/> using the <see cref="Authentication"/>. 
        /// May only be called when the instance <see cref="IsUnconnected"/>.
        /// Will throw if an authenticated IMAP connection cannot be established.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Connect" select="remarks"/>
        public Task ConnectAsync()
        {
            var lContext = RootContext.NewMethod(nameof(cIMAPClient), nameof(ConnectAsync));
            return ZConnectAsync(lContext);
        }

        private async Task ZConnectAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConnectAsync));

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            cServiceId lServiceId = base.ServiceId;
            cIMAPAuthentication lAuthentication = mAuthentication;

            if (lServiceId == null) throw new InvalidOperationException("connect requires serviceid to be set");
            if (lAuthentication == null) throw new InvalidOperationException("connect requires authentication to be set");

            bool lSessionReplaced;

            if (mSession == null) lSessionReplaced = false;
            else
            {
                if (!mSession.IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mSession.Dispose();

                lSessionReplaced = true;

                mNamespaces = null;

                mInbox = null;
                mSynchroniser.InvokePropertyChanged(nameof(Inbox), lContext);
            }

            mSession = new cSession(PersistentCache, mIMAPSynchroniser, IncrementInvokeMillisecondsDelay, NetworkWriteConfiguration, mIgnoreCapabilities, mMailboxCacheDataItems, mFetchBodyConfiguration, mDefaultAppendFlags, mAppendBatchConfiguration, mIdleConfiguration, mEncoding, mMaxItemsInSequenceSet, lContext);
            var lSession = mSession;

            if (lSessionReplaced)
            {
                mSynchroniser.InvokePropertyChanged(nameof(Capabilities), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(ConnectionState), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(IsConnected), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(IsUnconnected), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(ConnectedAccountId), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(EnabledExtensions), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(HomeServerReferral), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(ServerId), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(Namespaces), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(SelectedMailbox), lContext);
                mSynchroniser.InvokePropertyChanged(nameof(SelectedMailboxDetails), lContext);
            }

            mSynchroniseCacheSizer = new cBatchSizer(mSynchroniseCacheConfiguration);
            mFetchCacheItemsSizer = new cBatchSizer(mFetchCacheItemsConfiguration);

            List<cSASLAuthentication> lFailedSASLAuthentications = new List<cSASLAuthentication>();
            FailedSASLAuthentications = lFailedSASLAuthentications.AsReadOnly();
            mSynchroniser.InvokePropertyChanged(nameof(FailedSASLAuthentications), lContext);

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);

                try
                {
                    await lSession.ConnectAsync(lMC, lServiceId, lAuthentication.PreAuthenticatedCredentialId, lContext).ConfigureAwait(false);

                    if (lSession.Capabilities == null) await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);

                    if (lSession.ConnectionState == eIMAPConnectionState.notauthenticated && !lSession.TLSInstalled && lSession.Capabilities.StartTLS)
                    {
                        await lSession.StartTLSAsync(lMC, lContext).ConfigureAwait(false);
                        await lSession.CapabilityAsync(lMC, lContext).ConfigureAwait(false);
                    }

                    object lOriginalCapabilities = lSession.Capabilities;
                    cIMAPCapabilities lCurrentCapabilities = lSession.Capabilities;

                    if (lSession.ConnectionState == eIMAPConnectionState.notauthenticated)
                    {
                        bool lTLSIssue = false;
                        bool lTriedCredentials = false;
                        Exception lAuthenticateException = null;

                        bool lTLSInstalled = lSession.TLSInstalled;

                        if (lAuthentication.SASLs != null)
                        {
                            foreach (var lSASL in lAuthentication.SASLs)
                            {
                                if (lAuthentication.TryAllSASLs || lCurrentCapabilities.AuthenticationMechanisms.Contains(lSASL.MechanismName)) // no case-invariance required because SASL (rfc 2222) says only uppercase is allowed
                                {
                                    if ((lSASL.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (lSASL.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                                    else
                                    {
                                        lTriedCredentials = true;

                                        var lAuthenticateResult = await lSession.AuthenticateAsync(lMC, lServiceId.Host, lSASL, lContext).ConfigureAwait(false);

                                        if (lAuthenticateResult != null)
                                        {
                                            lFailedSASLAuthentications.Add(lAuthenticateResult.Authentication);
                                            mSynchroniser.InvokePropertyChanged(nameof(FailedSASLAuthentications), lContext);
                                            lAuthenticateException = lAuthenticateResult.Exception;
                                        }

                                        if (lSession.ConnectionState != eIMAPConnectionState.notauthenticated || lAuthenticateException != null) break;
                                    }
                                }
                            }
                        }

                        if (lSession.ConnectionState == eIMAPConnectionState.notauthenticated && lAuthenticateException == null && !lCurrentCapabilities.LoginDisabled && lAuthentication.Login != null)
                        {
                            if ((lAuthentication.Login.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (lAuthentication.Login.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                            else
                            {
                                lTriedCredentials = true;
                                lAuthenticateException = await lSession.LoginAsync(lMC, lServiceId.Host, lAuthentication.Login, lContext).ConfigureAwait(false);
                            }
                        }

                        if (lSession.ConnectionState != eIMAPConnectionState.authenticated)
                        {
                            lContext.TraceError("could not authenticate");

                            // log out
                            await lSession.LogoutAsync(lMC, lContext).ConfigureAwait(false);

                            // throw an exception that indicates why we couldn't connect

                            if (lTriedCredentials)
                            {
                                if (lAuthenticateException != null) throw lAuthenticateException;
                                throw new cIMAPCredentialsException(lContext);
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
                        if (lCurrentCapabilities.QResync) lExtensions = lExtensions | fEnableableExtensions.qresync;
                        if (lExtensions != fEnableableExtensions.none) await lSession.EnableAsync(lMC, lExtensions, lContext).ConfigureAwait(false);
                    }

                    // enabled (lock in the capabilities and enabled extensions)
                    lSession.SetEnabled(lContext);

                    Task lIdTask;

                    if (lCurrentCapabilities.Id)
                    {
                        cIMAPId lClientId;

                        if (lSession.UTF8Enabled) lClientId = mClientIdUTF8 ?? mClientId;
                        else lClientId = mClientId;

                        lIdTask = lSession.IdAsync(lMC, lClientId, lContext);
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
                                // special case, where the personal namespace is "INBOX/" (where "/" is the delimiter)
                                if (lName.Delimiter != null && lName.Prefix.Equals(cMailboxName.InboxString + lName.Delimiter, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    mInbox = new cMailbox(this, lSession.GetMailboxHandle(new cMailboxName(cMailboxName.InboxString, lName.Delimiter)));
                                    mSynchroniser.InvokePropertyChanged(nameof(Inbox), lContext);
                                    break;
                                }

                                var lNotPrefixedWith = ZGetNotPrefixedWith(lSession, lName.Prefix);
                                cMailboxPathPattern lPattern = new cMailboxPathPattern(lName.Prefix, lNotPrefixedWith, "%", lName.Delimiter);

                                if (lPattern.Matches(cMailboxName.InboxString))
                                {
                                    mInbox = new cMailbox(this, lSession.GetMailboxHandle(new cMailboxName(cMailboxName.InboxString, lName.Delimiter)));
                                    mSynchroniser.InvokePropertyChanged(nameof(Inbox), lContext);
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
                            mSynchroniser.InvokePropertyChanged(nameof(Namespaces), lContext);
                        }

                        mInbox = new cMailbox(this, lSession.GetMailboxHandle(new cMailboxName(cMailboxName.InboxString, lDelimiter)));
                        mSynchroniser.InvokePropertyChanged(nameof(Inbox), lContext);
                    }

                    // wait for id to complete
                    if (lIdTask != null) await lIdTask.ConfigureAwait(false);

                    // initialised (namespaces set, inbox available, id available (if server supports it); user may now issue commands)
                    lSession.SetInitialised(lContext);
                }
                catch when (lSession.ConnectionState != eIMAPConnectionState.disconnected)
                {
                    lSession.Disconnect(lContext);
                    throw;
                }
            }
        }
    }
}