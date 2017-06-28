using System;
using System.Collections.ObjectModel;
using System.Text;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private sealed partial class cSession : IDisposable
        {
            // this class implements the methods in rfc3501 and extensions pretty much directly

            private bool mDisposed = false;
            private eState _State = eState.notconnected;
            private readonly cConnection mConnection = new cConnection();
            private readonly cEventSynchroniser mEventSynchroniser;
            private fCapabilities _IgnoreCapabilities;
            private readonly cResponseTextProcessor mResponseTextProcessor;
            private readonly cCapabilityDataProcessor mCapabilityDataProcessor;
            private readonly cCommandPipeline mPipeline;
            private cFetchSizer mFetchPropertiesSizer;
            private cFetchSizer mFetchBodyReadSizer;
            private Encoding mEncoding; // can be null

            // properties
            private cCapabilities _Capabilities = null;
            private cCapabilities _AuthenticationMechanisms = null;
            private cCapability _Capability = null;
            private cURL _HomeServerReferral = null;
            private cAccountId _ConnectedAccountId = null;

            // selected mailbox
            private cSelectedMailbox _SelectedMailbox = null;

            // locks
            private readonly cExclusiveAccess mSelectExclusiveAccess = new cExclusiveAccess("select", 1);
            private readonly cExclusiveAccess mSetUnseenExclusiveAccess = new cExclusiveAccess("setunseen", 40);
            private readonly cExclusiveAccess mSearchExclusiveAccess = new cExclusiveAccess("search", 50);
            private readonly cExclusiveAccess mSortExclusiveAccess = new cExclusiveAccess("sort", 50);
            // note that 100 is the idle block in the command pipeline
            private readonly cExclusiveAccess mMSNUnsafeBlock = new cExclusiveAccess("msnunsafeblock", 200);
            // (note for when adding more: they need to be disposed)

            public cSession(cEventSynchroniser pEventSynchroniser, fCapabilities pIgnoreCapabilities, cIdleConfiguration pIdleConfiguration, cFetchSizeConfiguration pFetchPropertiesConfiguration, cFetchSizeConfiguration pFetchBodyReadConfiguration, Encoding pEncoding, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cSession), pIgnoreCapabilities, pIdleConfiguration, pFetchPropertiesConfiguration, pFetchBodyReadConfiguration);

                mEventSynchroniser = pEventSynchroniser;

                _IgnoreCapabilities = pIgnoreCapabilities;

                mResponseTextProcessor = new cResponseTextProcessor(mEventSynchroniser.ResponseText);

                mPipeline = new cCommandPipeline(mConnection, mResponseTextProcessor, pIdleConfiguration, Disconnect, lContext);

                mCapabilityDataProcessor = new cCapabilityDataProcessor(ZSetCapabilities);
                mPipeline.Install(mCapabilityDataProcessor);

                mFetchPropertiesSizer = new cFetchSizer(pFetchPropertiesConfiguration);
                mFetchBodyReadSizer = new cFetchSizer(pFetchBodyReadConfiguration);
                mEncoding = pEncoding;
            }

            public void SetIgnoreCapabilities(fCapabilities pIgnoreCapabilities, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetIgnoreCapabilities), pIgnoreCapabilities);
                _IgnoreCapabilities = pIgnoreCapabilities;
                ZSetCapability(lContext);
            }

            public void SetIdleConfiguration(cIdleConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetIdleConfiguration), pConfiguration);
                mPipeline.SetIdleConfiguration(pConfiguration, pParentContext);
            }

            public void SetFetchPropertiesConfiguration(cFetchSizeConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetFetchPropertiesConfiguration), pConfiguration);
                if (pConfiguration == null) throw new ArgumentNullException(nameof(pConfiguration));
                mFetchPropertiesSizer = new cFetchSizer(pConfiguration);
            }

            public void SetFetchBodyReadConfiguration(cFetchSizeConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetFetchBodyReadConfiguration), pConfiguration);
                if (pConfiguration == null) throw new ArgumentNullException(nameof(pConfiguration));
                mFetchBodyReadSizer = new cFetchSizer(pConfiguration);
            }

            public void SetEncoding(Encoding pEncoding, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetEncoding), pEncoding.WebName);
                mEncoding = pEncoding;
            }

            public eState State => _State;
            public bool IsUnconnected => _State == eState.notconnected || _State == eState.disconnected;
            public bool IsConnected => _State == eState.authenticated || _State == eState.selected;

            private void ZSetState(eState pState, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetState), pState);

                if (pState == _State) return;

                _State = pState;
                mPipeline.SetState(pState, lContext);

                mEventSynchroniser.PropertyChanged(nameof(cIMAPClient.State), lContext);
            }

            private void ZSetCapabilities(cCapabilities pCapabilities, cCapabilities pAuthenticationMechanisms, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetCapabilities), pCapabilities, pAuthenticationMechanisms);
                _Capabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                _AuthenticationMechanisms = pAuthenticationMechanisms ?? throw new ArgumentNullException(nameof(pAuthenticationMechanisms));
                ZSetCapability(lContext);
            }

            public cCapability Capability => _Capability;

            private void ZSetCapability(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetCapability));

                if (_Capabilities == null) return;

                _Capability = new cCapability(_Capabilities, _AuthenticationMechanisms, _IgnoreCapabilities);

                mConnection.SetCapability(_Capability, lContext);
                mPipeline.SetCapability(_Capability, lContext);
                mResponseTextProcessor.MailboxReferrals = _Capability.MailboxReferrals;
                _SelectedMailbox?.SetCapability(_Capability, lContext);

                mEventSynchroniser.PropertyChanged(nameof(cIMAPClient.Capability), lContext);
            }

            public cURL HomeServerReferral => _HomeServerReferral;

            private void ZSetHomeServerReferral(cURL pReferral, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetHomeServerReferral), pReferral);
                _HomeServerReferral = pReferral ?? throw new ArgumentNullException(nameof(pReferral));
            }

            public cAccountId ConnectedAccountId => _ConnectedAccountId;

            private void ZSetConnectedAccountId(cAccountId pAccountId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetConnectedAccountId), pAccountId);
                if (_ConnectedAccountId != null) throw new InvalidOperationException(); // can only be set once
                _ConnectedAccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
                ZSetState(eState.authenticated, lContext);
            }

            public bool SecurityInstalled => mConnection?.SecurityInstalled ?? false;

            private void ZSetSelectedMailbox(cSelectedMailbox pSelectedMailbox, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetSelectedMailbox));

                if (_SelectedMailbox == pSelectedMailbox) return; // should only happen when both are null

                var lOldSelectedMailbox = _SelectedMailbox;
                _SelectedMailbox = pSelectedMailbox;

                if (_SelectedMailbox != null) _SelectedMailbox.SetAsSelected(lContext);

                // the selected mailbox processes messages and response text codes
                mResponseTextProcessor.SelectedMailbox = _SelectedMailbox;
                mPipeline.SelectedMailbox = _SelectedMailbox;

                // state change
                if (_SelectedMailbox == null) ZSetState(eState.authenticated, lContext);
                else ZSetState(eState.selected, lContext);

                // mailbox events
                if (lOldSelectedMailbox != null) mEventSynchroniser.MailboxPropertyChanged(lOldSelectedMailbox.MailboxId, nameof(cMailbox.Selected), lContext);
                if (_SelectedMailbox != null) mEventSynchroniser.MailboxPropertyChanged(_SelectedMailbox.MailboxId, nameof(cMailbox.Selected), lContext);
            }

            public ReadOnlyCollection<cNamespaceName> PersonalNamespaces { get; private set; } = null;
            public ReadOnlyCollection<cNamespaceName> OtherUsersNamespaces { get; private set; } = null;
            public ReadOnlyCollection<cNamespaceName> SharedNamespaces { get; private set; } = null;

            public void SetNamespaces(cNamespaceList pPersonal, cNamespaceList pOtherUsers, cNamespaceList pShared, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetNamespaces), pPersonal, pOtherUsers, pShared);

                // this code to make sure that the INBOX will always be found by examining the personal namespaces

                bool lAddInbox = true;
                cNamespaceList lPersonal;

                if (pPersonal == null) lPersonal = new cNamespaceList();
                else
                {
                    lPersonal = pPersonal;

                    foreach (var lNamespace in pPersonal)
                    {
                        cMailboxNamePattern lPattern = new cMailboxNamePattern(lNamespace.Prefix, "%", lNamespace.Delimiter);

                        if (lPattern.Matches(cMailboxName.InboxString))
                        {
                            lAddInbox = false;
                            break;
                        }
                    }
                }

                if (lAddInbox) lPersonal.Add(new cNamespaceName(cMailboxName.InboxString, null));

                // now I know that there is at least one personal namespace that matches the INBOX

                PersonalNamespaces = lPersonal.AsReadOnly();
                OtherUsersNamespaces = pOtherUsers == null ? null : pOtherUsers.AsReadOnly();
                SharedNamespaces = pShared == null ? null : pShared.AsReadOnly();

                mEventSynchroniser.PropertyChanged(nameof(cIMAPClient.Namespaces), lContext);
            }

            public cMailbox Inbox { get; set; } = null;
            public cMailboxId SelectedMailboxId => _SelectedMailbox?.MailboxId;

            public iMailboxProperties GetMailboxProperties(cMailboxId pMailboxId)
            {
                if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
                cSelectedMailbox lSelectedMailbox = _SelectedMailbox;
                if (lSelectedMailbox != null && lSelectedMailbox.MailboxId == pMailboxId) return lSelectedMailbox;
                // this is where I would check the mailboxes being monitored and return them if there was a match
                return null;
            }

            public void Disconnect(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(Disconnect));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State >= eState.disconnecting) return;

                ZSetState(eState.disconnecting, lContext);

                if (mPipeline != null)
                {
                    try { mPipeline.Stop(lContext); }
                    catch { }
                }

                if (mConnection != null)
                {
                    try { mConnection.Disconnect(lContext); }
                    catch { }
                }

                ZSetState(eState.disconnected, lContext);
            }

            public void Dispose()
            {
                if (mDisposed) return;

                if (mSelectExclusiveAccess != null)
                {
                    try { mSelectExclusiveAccess.Dispose(); }
                    catch { }
                }

                if (mSetUnseenExclusiveAccess != null)
                {
                    try { mSetUnseenExclusiveAccess.Dispose(); }
                    catch { }
                }

                if (mSearchExclusiveAccess != null)
                {
                    try { mSearchExclusiveAccess.Dispose(); }
                    catch { }
                }

                if (mSortExclusiveAccess != null)
                {
                    try { mSortExclusiveAccess.Dispose(); }
                    catch { }
                }

                if (mMSNUnsafeBlock != null)
                {
                    try { mMSNUnsafeBlock.Dispose(); }
                    catch { }
                }

                if (mPipeline != null)
                {
                    try { mPipeline.Dispose(); }
                    catch { }
                }

                if (mConnection != null)
                {
                    try { mConnection.Dispose(); }
                    catch { }
                }

                mDisposed = true;
            }
        }
    }
}