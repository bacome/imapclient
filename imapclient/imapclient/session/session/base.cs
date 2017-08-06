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
            private bool mDisposed = false;

            private eState _State = eState.notconnected;

            private readonly cConnection mConnection = new cConnection();

            private readonly cEventSynchroniser mEventSynchroniser;
            private readonly fMailboxCacheData mMailboxCacheData;
            private readonly fCapabilities mIgnoreCapabilities;
            private readonly cResponseTextProcessor mResponseTextProcessor;
            private readonly cCommandPipeline mPipeline;

            private cFetchSizer mFetchAttributesSizer;
            private cFetchSizer mFetchBodyReadSizer;

            private cCommandPartFactory mCommandPartFactory;
            private cCommandPartFactory mEncodingPartFactory;

            // properties
            private cCapability mCapability = null;
            private cURL mHomeServerReferral = null;
            private cAccountId _ConnectedAccountId = null;

            // set once initialised
            private fMailboxCacheData mStatusAttributes = 0;
            private cMailboxCache mMailboxCache = null;

            // locks
            private readonly cExclusiveAccess mSelectExclusiveAccess = new cExclusiveAccess("select", 1);
            private readonly cExclusiveAccess mSetUnseenExclusiveAccess = new cExclusiveAccess("setunseen", 40);
            private readonly cExclusiveAccess mSearchExclusiveAccess = new cExclusiveAccess("search", 50);
            private readonly cExclusiveAccess mSortExclusiveAccess = new cExclusiveAccess("sort", 50);
            // note that 100 is the idle block in the command pipeline
            private readonly cExclusiveAccess mMSNUnsafeBlock = new cExclusiveAccess("msnunsafeblock", 200);
            // (note for when adding more: they need to be disposed)

            public cSession(cEventSynchroniser pEventSynchroniser, fCapabilities pIgnoreCapabilities, fMailboxCacheData pMailboxCacheData, cIdleConfiguration pIdleConfiguration, cFetchSizeConfiguration pFetchAttributesConfiguration, cFetchSizeConfiguration pFetchBodyReadConfiguration, Encoding pEncoding, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cSession), pIgnoreCapabilities, pIdleConfiguration, pFetchAttributesConfiguration, pFetchBodyReadConfiguration);

                mEventSynchroniser = pEventSynchroniser;
                mIgnoreCapabilities = pIgnoreCapabilities;
                mMailboxCacheData = pMailboxCacheData;
                mResponseTextProcessor = new cResponseTextProcessor(mEventSynchroniser);

                mPipeline = new cCommandPipeline(pEventSynchroniser, mConnection, mResponseTextProcessor, pIdleConfiguration, Disconnect, lContext);

                mFetchAttributesSizer = new cFetchSizer(pFetchAttributesConfiguration);
                mFetchBodyReadSizer = new cFetchSizer(pFetchBodyReadConfiguration);

                mCommandPartFactory = new cCommandPartFactory(false, null);

                if (pEncoding == null) mEncodingPartFactory = mCommandPartFactory;
                else mEncodingPartFactory = new cCommandPartFactory(false, pEncoding);
            }

            public bool TLSInstalled => mConnection.TLSInstalled;

            public void Enabled(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(Go));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.authenticated) throw new InvalidOperationException("must be authenticated");

                bool lUTF8Enabled = (EnabledExtensions & fEnableableExtensions.utf8) != 0;

                if (lUTF8Enabled)
                {
                    mCommandPartFactory = new cCommandPartFactory(true, null);
                    mEncodingPartFactory = mCommandPartFactory;
                }

                mStatusAttributes = mMailboxCacheData & fMailboxCacheData.allstatus;
                if (!mCapability.CondStore) mStatusAttributes &= ~fMailboxCacheData.highestmodseq;

                mMailboxCache = new cMailboxCache(mEventSynchroniser, mMailboxCacheData, _ConnectedAccountId, mCommandPartFactory, mCapability, ZSetState);
                mResponseTextProcessor.Go(mMailboxCache, lContext);

                mPipeline.Install(new cResponseDataParserSelect());
                mPipeline.Install(new cResponseDataParserFetch());
                mPipeline.Install(new cResponseDataParserList(lUTF8Enabled));
                mPipeline.Install(new cResponseDataParserLSub(lUTF8Enabled));
                if (mCapability.ESearch || mCapability.ESort) mPipeline.Install(new cResponseDataParserESearch());

                mPipeline.Go(mMailboxCache, mCapability, lContext);

                ZSetState(eState.enabled, lContext);
            }

            public void Go()
            {
                ;?; // may pass in the delimiter for use in namespace ...

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
                if ((EnabledExtensions & fEnableableExtensions.utf8) != 0 || pEncoding == null) mEncodingPartFactory = mCommandPartFactory;
                else mEncodingPartFactory = new cCommandPartFactory(false, pEncoding);
            }

            public eState State => _State;
            public bool IsUnconnected => _State == eState.notconnected || _State == eState.disconnected;
            public bool IsConnected => _State == eState.notselected || _State == eState.selected;

            private void ZSetState(eState pState, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetState), pState);
                if (pState == _State) return;
                _State = pState;
                mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.State), lContext);
            }

            public cCapability Capability => mCapability;

            public cURL HomeServerReferral => mHomeServerReferral;

            public cAccountId ConnectedAccountId => _ConnectedAccountId;

            private void ZSetConnectedAccountId(cAccountId pAccountId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetConnectedAccountId), pAccountId);
                if (_ConnectedAccountId != null) throw new InvalidOperationException(); // can only be set once
                _ConnectedAccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
                ZSetState(eState.authenticated, lContext);
            }

            public bool SASLSecurityInstalled => mConnection?.SASLSecurityInstalled ?? false;

            public ReadOnlyCollection<cNamespaceName> PersonalNamespaces => mNamespaceDataProcessor?.Personal;
            public ReadOnlyCollection<cNamespaceName> OtherUsersNamespaces => mNamespaceDataProcessor?.OtherUsers;
            public ReadOnlyCollection<cNamespaceName> SharedNamespaces => mNamespaceDataProcessor?.Shared;

            ;?; // this
            public void SetNamespaces(cNamespaceList pPersonal, cNamespaceList pOtherUsers, cNamespaceList pShared, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetNamespaces), pPersonal, pOtherUsers, pShared);

                ;?;

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

            public iSelectedMailboxDetails SelectedMailboxDetails => mMailboxCache?.SelectedMailboxDetails;

            public iMailboxHandle GetMailboxHandle(cMailboxName pMailboxName)
            {
                if (mMailboxCache == null) throw new InvalidOperationException();
                return mMailboxCache.GetHandle(pMailboxName);
            }

            public bool? HasCachedChildren(iMailboxHandle pHandle) => mMailboxCache?.HasChildren(pHandle);

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