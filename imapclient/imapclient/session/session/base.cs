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

            private eConnectionState _ConnectionState = eConnectionState.notconnected;

            private readonly cConnection mConnection = new cConnection();

            private readonly cEventSynchroniser mEventSynchroniser;
            private readonly fMailboxCacheData mMailboxCacheData;
            private readonly fKnownCapabilities mIgnoreCapabilities;
            private readonly cResponseTextProcessor mResponseTextProcessor;
            private readonly cCommandPipeline mPipeline;

            private cFetchSizer mFetchAttributesSizer;
            private cFetchSizer mFetchBodyReadSizer;

            private cCommandPartFactory mCommandPartFactory;
            private cCommandPartFactory mEncodingPartFactory;

            // properties
            private cCapabilities mCapabilities = null;
            private cURL _HomeServerReferral = null;
            private cAccountId _ConnectedAccountId = null;

            // set once enabled
            private fMailboxCacheData mStatusAttributes = 0;
            private cMailboxCache mMailboxCache = null;

            // locks
            private readonly cExclusiveAccess mSelectExclusiveAccess = new cExclusiveAccess("select", 1);
            private readonly cExclusiveAccess mSearchExclusiveAccess = new cExclusiveAccess("search", 50);
            private readonly cExclusiveAccess mSortExclusiveAccess = new cExclusiveAccess("sort", 50);
            // note that 100 is the idle block in the command pipeline
            private readonly cExclusiveAccess mMSNUnsafeBlock = new cExclusiveAccess("msnunsafeblock", 200);
            // (note for when adding more: they need to be disposed)

            public cSession(cEventSynchroniser pEventSynchroniser, fKnownCapabilities pIgnoreCapabilities, fMailboxCacheData pMailboxCacheData, cIdleConfiguration pIdleConfiguration, cFetchSizeConfiguration pFetchAttributesConfiguration, cFetchSizeConfiguration pFetchBodyReadConfiguration, Encoding pEncoding, cTrace.cContext pParentContext)
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

            public void SetEnabled(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetEnabled));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.authenticated) throw new InvalidOperationException("must be authenticated");

                bool lUTF8Enabled = (EnabledExtensions & fEnableableExtensions.utf8) != 0;

                if (lUTF8Enabled)
                {
                    mCommandPartFactory = new cCommandPartFactory(true, null);
                    mEncodingPartFactory = mCommandPartFactory;
                }

                mStatusAttributes = mMailboxCacheData & fMailboxCacheData.allstatus;
                if (!mCapabilities.CondStore) mStatusAttributes &= ~fMailboxCacheData.highestmodseq;

                mMailboxCache = new cMailboxCache(mEventSynchroniser, mMailboxCacheData, _ConnectedAccountId, mCommandPartFactory, mCapabilities, ZSetState);

                mResponseTextProcessor.Enable(mMailboxCache, lContext);

                mPipeline.Install(new cResponseDataParserSelect());
                mPipeline.Install(new cResponseDataParserFetch());
                mPipeline.Install(new cResponseDataParserList(lUTF8Enabled));
                mPipeline.Install(new cResponseDataParserLSub(lUTF8Enabled));
                if (mCapabilities.ESearch || mCapabilities.ESort) mPipeline.Install(new cResponseDataParserESearch());

                mPipeline.Enable(mMailboxCache, mCapabilities, lContext);

                ZSetState(eConnectionState.enabled, lContext);
            }

            public void SetInitialised(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetInitialised));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.enabled) throw new InvalidOperationException("must be enabled");

                ZSetState(eConnectionState.notselected, lContext);
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

            public eConnectionState ConnectionState => _ConnectionState;
            public bool IsUnconnected => _ConnectionState == eConnectionState.notconnected || _ConnectionState == eConnectionState.disconnected;
            public bool IsConnected => _ConnectionState == eConnectionState.notselected || _ConnectionState == eConnectionState.selected;

            private void ZSetState(eConnectionState pConnectionState, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetState), pConnectionState);
                if (pConnectionState == _ConnectionState) return;
                _ConnectionState = pConnectionState;
                mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.ConnectionState), lContext);
            }

            public cCapabilities Capabilities => mCapabilities;

            public cURL HomeServerReferral => _HomeServerReferral;

            private bool ZSetHomeServerReferral(cResponseText pResponseText)
            {
                if (pResponseText.Code != eResponseTextCode.referral || pResponseText.Strings == null || pResponseText.Strings.Count != 1) return false;
                string lReferral = pResponseText.Strings[0];
                return cURL.TryParse(lReferral, out _HomeServerReferral);
            }

            public cAccountId ConnectedAccountId => _ConnectedAccountId;

            private void ZSetConnectedAccountId(cAccountId pAccountId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetConnectedAccountId), pAccountId);
                if (_ConnectedAccountId != null) throw new InvalidOperationException(); // can only be set once
                _ConnectedAccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
                ZSetState(eConnectionState.authenticated, lContext);
            }

            public bool SASLSecurityInstalled => mConnection?.SASLSecurityInstalled ?? false;

            public ReadOnlyCollection<cNamespaceName> PersonalNamespaces => mNamespaceDataProcessor.Personal;
            public ReadOnlyCollection<cNamespaceName> OtherUsersNamespaces => mNamespaceDataProcessor.OtherUsers;
            public ReadOnlyCollection<cNamespaceName> SharedNamespaces => mNamespaceDataProcessor.Shared;

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
                if (_ConnectionState >= eConnectionState.disconnecting) return;

                ZSetState(eConnectionState.disconnecting, lContext);

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

                ZSetState(eConnectionState.disconnected, lContext);
            }

            public void Dispose()
            {
                if (mDisposed) return;

                if (mSelectExclusiveAccess != null)
                {
                    try { mSelectExclusiveAccess.Dispose(); }
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