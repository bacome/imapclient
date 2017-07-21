﻿using System;
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
            private readonly cCommandPipeline mPipeline;
            private readonly cCapabilityDataProcessor mCapabilityDataProcessor;

            private cFetchSizer mFetchAttributesSizer;
            private cFetchSizer mFetchBodyReadSizer;
            private Encoding mEncoding; // can be null

            // properties
            private cCapabilities _Capabilities = null;
            private cCapabilities _AuthenticationMechanisms = null;
            private cCapability _Capability = null;
            private cURL _HomeServerReferral = null;
            private cAccountId _ConnectedAccountId = null;

            // post enable processing sets these
            private bool mEnableDone = false;
            private bool mUTF8Enabled = false;
            private cCommandPart.cFactory mStringFactory = null;
            private cMailboxCache mMailboxCache = null;

            // selected mailbox
            //private cSelectedMailbox _SelectedMailbox = null;

            // locks
            private readonly cExclusiveAccess mSelectExclusiveAccess = new cExclusiveAccess("select", 1);
            private readonly cExclusiveAccess mSetUnseenExclusiveAccess = new cExclusiveAccess("setunseen", 40);
            private readonly cExclusiveAccess mSearchExclusiveAccess = new cExclusiveAccess("search", 50);
            private readonly cExclusiveAccess mSortExclusiveAccess = new cExclusiveAccess("sort", 50);
            // note that 100 is the idle block in the command pipeline
            private readonly cExclusiveAccess mMSNUnsafeBlock = new cExclusiveAccess("msnunsafeblock", 200);
            // (note for when adding more: they need to be disposed)

            public cSession(cEventSynchroniser pEventSynchroniser, fCapabilities pIgnoreCapabilities, cIdleConfiguration pIdleConfiguration, cFetchSizeConfiguration pFetchAttributesConfiguration, cFetchSizeConfiguration pFetchBodyReadConfiguration, Encoding pEncoding, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cSession), pIgnoreCapabilities, pIdleConfiguration, pFetchAttributesConfiguration, pFetchBodyReadConfiguration);

                mEventSynchroniser = pEventSynchroniser;

                _IgnoreCapabilities = pIgnoreCapabilities;

                mResponseTextProcessor = new cResponseTextProcessor(mEventSynchroniser.FireResponseText);
                mPipeline = new cCommandPipeline(mConnection, mResponseTextProcessor, pIdleConfiguration, Disconnect, lContext);

                mCapabilityDataProcessor = new cCapabilityDataProcessor(ZSetCapabilities);
                mPipeline.Install(mCapabilityDataProcessor);

                mFetchAttributesSizer = new cFetchSizer(pFetchAttributesConfiguration);
                mFetchBodyReadSizer = new cFetchSizer(pFetchBodyReadConfiguration);
                mEncoding = pEncoding;
            }

            public void EnableDone(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(EnableDone));

                if (mEnableDone) throw new InvalidOperationException();
                mEnableDone = true;

                mUTF8Enabled = (EnabledExtensions & fEnableableExtensions.utf8) != 0;
                mStringFactory = new cCommandPart.cFactory(mUTF8Enabled);
                mMailboxCache = new cMailboxCache(mEventSynchroniser, _ConnectedAccountId, mUTF8Enabled, ZSetState, _Capability);

                mResponseTextProcessor.SetMailboxCache(mMailboxCache, lContext);
                mPipeline.SetMailboxCache(mMailboxCache, lContext);


                mStatusDataProcessor = new cStatusDataProcessor(mMailboxCache);
                mPipeline.Install(mStatusDataProcessor);

                mListDataProcessor = new cListDataProcessor(EnabledExtensions, ZGetCapability, mMailboxCache);
                mPipeline.Install(mListDataProcessor);

                mLSubDataProcessor = new cLSubDataProcessor(EnabledExtensions, mMailboxCache);
                mPipeline.Install(mLSubDataProcessor);
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
                mPipeline.SetIdleConfiguration(pConfiguration, lContext);
            }

            public void SetFetchAttributesConfiguration(cFetchSizeConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetFetchAttributesConfiguration), pConfiguration);
                if (pConfiguration == null) throw new ArgumentNullException(nameof(pConfiguration));
                mFetchAttributesSizer = new cFetchSizer(pConfiguration);
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
                mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.State), lContext);
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
                if (mMailboxCache != null) mMailboxCache.SetCapability(_Capability, lContext);

                mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.Capability), lContext);
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

                mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.Namespaces), lContext);
            }

            public cMailbox Inbox { get; set; } = null;

            public iSelectedMailboxDetails SelectedMailboxDetails => mMailboxCache?.selectedmailbox;

            public iMailboxHandle GetMailboxHandle(cMailboxName pMailboxName) => mMailboxCache.GetHandle(pMailboxName);

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