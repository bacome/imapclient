using System;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private sealed partial class cSession : IDisposable
        {
            private bool mDisposed = false;

            private object mConnectionStateLock = new object();
            private eConnectionState _ConnectionState = eConnectionState.notconnected;

            private readonly cCallbackSynchroniser mSynchroniser;
            private readonly fCapabilities mIgnoreCapabilities;
            private readonly fMailboxCacheDataItems mMailboxCacheDataItems;
            private readonly cCommandPipeline mPipeline;

            private cBatchSizer mFetchCacheItemsSizer;
            private cBatchSizer mFetchBodyReadSizer;
            private cBatchSizer mAppendBatchSizer;

            private cStorableFlags mAppendDefaultFlags;
            private int mAppendTargetBufferSize;
            private cBatchSizerConfiguration mAppendStreamReadConfiguration;

            private cCommandPartFactory mCommandPartFactory;
            private cCommandPartFactory mEncodingPartFactory;

            // properties
            private cCapabilities _Capabilities = null;
            private cURL _HomeServerReferral = null;
            private cAccountId _ConnectedAccountId = null;

            // set once enabled
            private fMailboxCacheDataItems mStatusAttributes = 0;
            private cMailboxCache mMailboxCache = null;

            // locks
            private readonly cExclusiveAccess mSelectExclusiveAccess = new cExclusiveAccess("select", 1);
            private readonly cExclusiveAccess mSearchExclusiveAccess = new cExclusiveAccess("search", 50);
            private readonly cExclusiveAccess mSortExclusiveAccess = new cExclusiveAccess("sort", 50);
            // note that 100 is the idle block in the command pipeline
            private readonly cExclusiveAccess mMSNUnsafeBlock = new cExclusiveAccess("msnunsafeblock", 200);
            // (note for when adding more: they need to be disposed)

            public cSession(
                cCallbackSynchroniser pSynchroniser, fCapabilities pIgnoreCapabilities, fMailboxCacheDataItems pMailboxCacheDataItems, cBatchSizerConfiguration pNetworkWriteConfiguration,
                cIdleConfiguration pIdleConfiguration, 
                cBatchSizerConfiguration pFetchCacheItemsConfiguration, cBatchSizerConfiguration pFetchBodyReadConfiguration, cBatchSizerConfiguration pAppendBatchConfiguration,
                cStorableFlags pAppendDefaultFlags, int pAppendTargetBufferSize, cBatchSizerConfiguration pAppendStreamReadConfiguration,
                Encoding pEncoding,
                cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cSession), pIgnoreCapabilities, pMailboxCacheDataItems, pNetworkWriteConfiguration, pIdleConfiguration, pFetchCacheItemsConfiguration, pFetchBodyReadConfiguration, pAppendBatchConfiguration, pAppendTargetBufferSize, pAppendStreamReadConfiguration);

                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mIgnoreCapabilities = pIgnoreCapabilities;
                mMailboxCacheDataItems = pMailboxCacheDataItems;

                mPipeline = new cCommandPipeline(pSynchroniser, ZDisconnected, pNetworkWriteConfiguration, pIdleConfiguration, lContext);

                mFetchCacheItemsSizer = new cBatchSizer(pFetchCacheItemsConfiguration);
                mFetchBodyReadSizer = new cBatchSizer(pFetchBodyReadConfiguration);
                mAppendBatchSizer = new cBatchSizer(pAppendBatchConfiguration);

                mAppendDefaultFlags = pAppendDefaultFlags;
                mAppendTargetBufferSize = pAppendTargetBufferSize;
                mAppendStreamReadConfiguration = pAppendStreamReadConfiguration;

                mCommandPartFactory = new cCommandPartFactory(false, null);

                if (pEncoding == null) mEncodingPartFactory = mCommandPartFactory;
                else mEncodingPartFactory = new cCommandPartFactory(false, pEncoding);
            }

            public bool TLSInstalled => mPipeline.TLSInstalled;

            public void SetEnabled(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetEnabled));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.authenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotAuthenticated);

                bool lUTF8Enabled = (EnabledExtensions & fEnableableExtensions.utf8) != 0;

                if (lUTF8Enabled)
                {
                    mCommandPartFactory = new cCommandPartFactory(true, null);
                    mEncodingPartFactory = mCommandPartFactory;
                }

                mStatusAttributes = mMailboxCacheDataItems & fMailboxCacheDataItems.allstatus;
                if (!_Capabilities.CondStore) mStatusAttributes &= ~fMailboxCacheDataItems.highestmodseq;

                mMailboxCache = new cMailboxCache(mSynchroniser, mMailboxCacheDataItems, mCommandPartFactory, _Capabilities, ZSetState);

                mPipeline.Install(new cResponseTextCodeParserSelect(_Capabilities));
                mPipeline.Install(new cResponseDataParserSelect());
                mPipeline.Install(new cResponseDataParserFetch());
                mPipeline.Install(new cResponseDataParserList(lUTF8Enabled));
                mPipeline.Install(new cResponseDataParserLSub(lUTF8Enabled));
                if (_Capabilities.ESearch || _Capabilities.ESort) mPipeline.Install(new cResponseDataParserESearch());

                mPipeline.Enable(mMailboxCache, _Capabilities, lContext);

                ZSetState(eConnectionState.enabled, lContext);
            }

            public void SetInitialised(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetInitialised));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.enabled) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotEnabled);

                ZSetState(eConnectionState.notselected, lContext);
            }

            public void SetIdleConfiguration(cIdleConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetIdleConfiguration), pConfiguration);
                mPipeline.SetIdleConfiguration(pConfiguration, lContext);
            }

            public void SetFetchCacheItemsConfiguration(cBatchSizerConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetFetchCacheItemsConfiguration), pConfiguration);
                if (pConfiguration == null) throw new ArgumentNullException(nameof(pConfiguration));
                mFetchCacheItemsSizer = new cBatchSizer(pConfiguration);
            }

            public void SetFetchBodyReadConfiguration(cBatchSizerConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetFetchBodyReadConfiguration), pConfiguration);
                if (pConfiguration == null) throw new ArgumentNullException(nameof(pConfiguration));
                mFetchBodyReadSizer = new cBatchSizer(pConfiguration);
            }

            public void SetAppendBatchConfiguration(cBatchSizerConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetAppendBatchConfiguration), pConfiguration);
                if (pConfiguration == null) throw new ArgumentNullException(nameof(pConfiguration));
                mAppendBatchSizer = new cBatchSizer(pConfiguration);
            }

            public void SetAppendDefaultFlags(cStorableFlags pFlags, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetAppendDefaultFlags), pFlags);
                mAppendDefaultFlags = pFlags;
            }

            public void SetAppendTargetBufferSize(int pSize, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetAppendTargetBufferSize), pSize);
                if (pSize < 1) throw new ArgumentOutOfRangeException(nameof(pSize));
                mAppendTargetBufferSize = pSize;
            }

            public void SetAppendStreamReadConfiguration(cBatchSizerConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetAppendStreamReadConfiguration), pConfiguration);
                mAppendStreamReadConfiguration = pConfiguration ?? throw new ArgumentNullException(nameof(pConfiguration));
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

                bool lIsUnconnected;
                bool lIsConnected;

                // the lock is required because state can be changed by;
                //  the user's thread (via calls to Connect or Disconnect), and by
                //  the command pipeline's thread (via a response from the server)
                //  (note that a unilateral bye which can arrive at any time (e.g. during connect) will disconnect)
                //  (note that the user may call Disconnect at any time (e.g. during connectasync))
                //
                lock (mConnectionStateLock)
                {
                    if (pConnectionState == _ConnectionState) return;
                    if (_ConnectionState == eConnectionState.disconnected) return;

                    lIsUnconnected = IsUnconnected;
                    lIsConnected = IsConnected;

                    _ConnectionState = pConnectionState;
                }

                mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.ConnectionState), lContext);
                if (IsConnected != lIsConnected) mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.IsConnected), lContext);
                if (IsUnconnected != lIsUnconnected) mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.IsUnconnected), lContext);
            }

            public cCapabilities Capabilities => _Capabilities;

            private void ZSetCapabilities(cStrings pCapabilities, cStrings pAuthenticationMechanisms, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetCapabilities), pCapabilities, pAuthenticationMechanisms);

                _Capabilities = new cCapabilities(pCapabilities, pAuthenticationMechanisms, mIgnoreCapabilities);
                mPipeline.SetCapabilities(_Capabilities, lContext);
                mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
            }

            public cURL HomeServerReferral => _HomeServerReferral;

            private bool ZSetHomeServerReferral(cResponseText pResponseText, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetHomeServerReferral), pResponseText);

                if (pResponseText.Code != eResponseTextCode.referral || pResponseText.Arguments == null || pResponseText.Arguments.Count != 1) return false;

                if (cURL.TryParse(pResponseText.Arguments[0], out var lReferral) && lReferral.IsHomeServerReferral)
                {
                    _HomeServerReferral = lReferral;
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.HomeServerReferral), lContext);
                    return true;
                }

                return false;
            }

            public cAccountId ConnectedAccountId => _ConnectedAccountId;

            private void ZSetConnectedAccountId(cAccountId pAccountId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetConnectedAccountId), pAccountId);
                if (_ConnectedAccountId != null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyConnected); // can only be set once
                _ConnectedAccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
                ZSetState(eConnectionState.authenticated, lContext);
                mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.ConnectedAccountId), lContext);
            }

            public bool SASLSecurityInstalled => mPipeline.SASLSecurityInstalled;

            public cNamespaceNames NamespaceNames => mNamespaceDataProcessor?.NamespaceNames;

            public object MailboxCache => mMailboxCache;
            public iSelectedMailboxDetails SelectedMailboxDetails => mMailboxCache?.SelectedMailboxDetails;

            public iMailboxHandle GetMailboxHandle(cMailboxName pMailboxName)
            {
                if (mMailboxCache == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotEnabled);
                return mMailboxCache.GetHandle(pMailboxName);
            }

            public bool? HasCachedChildren(iMailboxHandle pMailboxHandle) => mMailboxCache?.HasChildren(pMailboxHandle);

            public void Disconnect(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(Disconnect));
                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                mPipeline.RequestStop(lContext);
                ZSetState(eConnectionState.disconnected, lContext);
            }

            private void ZDisconnected(cTrace.cContext pParentContext)
            {
                // called by the command pipeline when the background task exits
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZDisconnected));
                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
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

                mDisposed = true;
            }
        }
    }
}