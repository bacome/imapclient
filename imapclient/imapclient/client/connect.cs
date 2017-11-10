using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Connect to the <see cref="Server"/> using the <see cref="Credentials"/>. Can only be called while unconnected: see <see cref="IsUnconnected"/>. Will throw if an authenticated IMAP connection cannot be established.
        /// </summary>
        /// <remarks>
        /// <para>
        /// TLS is established if possible before authentication is attempted.
        /// TLS can be established immediately upon connect if the <see cref="Server"/> specifies <see cref="cServer.SSL"/> 
        /// or
        /// after connecting using the IMAP STARTTLS command if both the client and server support it - see <see cref="cCapabilities.StartTLS"/>, <see cref="Capabilities"/> and <see cref="IgnoreCapabilities"/>.
        /// </para>
        /// <para>
        /// During authentication the <see cref="Capabilities"/> will be set.
        /// (Note that the <see cref="IgnoreCapabilities"/> value is used to determine what actual capabilities are used by the client - see <see cref="cCapabilities.EffectiveCapabilities"/>.)
        /// </para>
        /// <para>
        /// After authentication depending on what the <see cref="Capabilities"/> allow;
        /// <list type="bullet">
        /// <item>RFC 6855 (UTF8) is enabled (see <see cref="cCapabilities.UTF8Accept"/>, <see cref="cCapabilities.UTF8Only"/>).</item>
        /// <item>ID (RFC 2971) information is exchanged (see <see cref="ClientId"/> and <see cref="ServerId"/>).</item>
        /// <item>Namespace (RFC 2342) information is retrieved from the server (see <see cref="Namespaces"/>).</item>
        /// <item>IMAP LIST is used to discover the hierarchy delimiter and one personal namespace is generated using it (see <see cref="Namespaces"/>).</item>
        /// </list>
        /// </para>
        /// <para>(Note normally only one of Namespace and LIST are used: but under some strange circumstances both may be used [specifically if the Namespaces returned do not contain the inbox].)</para>
        /// <para>At the end of a successful connect process the <see cref="ConnectionState"/> will be <see cref="eConnectionState.notselected"/>.</para>
        /// <para>Exceptions relating to authentication and their meanings;
        /// <list type="bullet">
        /// <item><term><see cref="cCredentialsException"/>:</term><description>Was able to try credentials from <see cref="Credentials"/> but they didn't work. (If the <see cref="cCredentialsException.ResponseText"/> is set the server explicitly rejected the credentials.)</description></item>
        /// <item><term><see cref="cAuthenticationMechanismsException"/>:</term><description>Was not able to try any credentials from <see cref="Credentials"/>.</description></item>
        /// <item><term><see cref="cHomeServerReferralException"/>:</term><description>The server suggests that we try a different server (see <see cref="cHomeServerReferralException.ResponseText"/> and <see cref="cResponseText.Strings"/>).</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public void Connect()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Connect));
            mSynchroniser.Wait(ZConnectAsync(lContext), lContext);
        }

        /**<summary>The async version of <see cref="Connect"/></summary>*/
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
                mSynchroniser.InvokePropertyChanged(nameof(Inbox), lContext);
            }

            // initialise the SASLs
            foreach (var lSASL in lCredentials.SASLs) lSASL.LastAuthentication = null;

            mSession = new cSession(mSynchroniser, mIgnoreCapabilities, mMailboxCacheData, mNetworkWriteConfiguration, mIdleConfiguration, mFetchCacheItemsConfiguration, mFetchBodyReadConfiguration, mEncoding, lContext);
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

                        if (lCredentials.TryAllSASLs)
                        {
                            foreach (var lSASL in lCredentials.SASLs)
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
                            foreach (var lSASL in lCredentials.SASLs)
                            {
                                if (lCurrentCapabilities.AuthenticationMechanisms.Contains(lSASL.MechanismName)) // no case-invariance required because SASL (rfc 2222) says only uppercase is allowed
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

                        if (lSession.ConnectionState == eConnectionState.notauthenticated && lAuthenticateException == null && !lCurrentCapabilities.LoginDisabled && lCredentials.Login != null)
                        {
                            if ((lCredentials.Login.TLSRequirement == eTLSRequirement.required && !lTLSInstalled) || (lCredentials.Login.TLSRequirement == eTLSRequirement.disallowed && lTLSInstalled)) lTLSIssue = true;
                            else
                            {
                                lTriedCredentials = true;
                                lAuthenticateException = await lSession.LoginAsync(lMC, lAccountId, lCredentials.Login, lContext).ConfigureAwait(false);
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
                        cId lClientId;

                        if ((lSession.EnabledExtensions & fEnableableExtensions.utf8) == 0) lClientId = mClientId;
                        else lClientId = mClientIdUTF8 ?? mClientId;

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

                                cMailboxPathPattern lPattern = new cMailboxPathPattern(lName.Prefix, "%", lName.Delimiter);

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
                catch when (lSession.ConnectionState != eConnectionState.disconnected)
                {
                    lSession.Disconnect(lContext);
                    throw;
                }
            }
        }
    }
}