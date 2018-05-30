using System;
using System.Text;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private sealed partial class cSession : IDisposable
        {
            private bool mDisposed = false;

            private object mConnectionStateLock = new object();
            private eIMAPConnectionState _ConnectionState = eIMAPConnectionState.notconnected;

            private readonly cIMAPCallbackSynchroniser mSynchroniser;
            private readonly Action<iMessageHandle, cTrace.cContext> mExpunged;
            private readonly Action<iMailboxHandle, cTrace.cContext> mUIDValidityDiscovered;
            private readonly fIMAPCapabilities mIgnoreCapabilities;
            private readonly fMailboxCacheDataItems mMailboxCacheDataItems;
            private readonly cBatchSizer mFetchCacheItemsSizer;
            private readonly cBatchSizer mFetchBodySizer;
            private readonly cStorableFlags mAppendDefaultFlags;
            private readonly cBatchSizer mAppendBatchSizer;
            private readonly cCommandPipeline mPipeline;

            private Encoding mEncoding;

            private cCommandPartFactory mCommandPartFactory;
            private cCommandPartFactory mEncodingPartFactory;

            // properties
            private cIMAPCapabilities _Capabilities = null;
            private cURL _HomeServerReferral = null;
            private cAccountId _ConnectedAccountId = null;
            private fMessageDataFormat _SupportedFormats = fMessageDataFormat.eightbit;

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
                cIMAPCallbackSynchroniser pSynchroniser, Action<iMessageHandle, cTrace.cContext> pExpunged, Action<iMailboxHandle, cTrace.cContext> pUIDValidityDiscovered,
                cBatchSizerConfiguration pNetworkWriteConfiguration,
                fIMAPCapabilities pIgnoreCapabilities, fMailboxCacheDataItems pMailboxCacheDataItems,
                cBatchSizerConfiguration pFetchCacheItemsConfiguration, cBatchSizerConfiguration pFetchBodyConfiguration,
                cStorableFlags pAppendDefaultFlags, cBatchSizerConfiguration pAppendBatchConfiguration, 
                cIdleConfiguration pIdleConfiguration, 
                Encoding pEncoding,
                cTrace.cContext pParentContext)
            {
                var lContext = 
                    pParentContext.NewObject(
                        nameof(cSession),
                        pNetworkWriteConfiguration, 
                        pIgnoreCapabilities, pMailboxCacheDataItems,
                        pFetchCacheItemsConfiguration, pFetchBodyConfiguration,
                        pAppendDefaultFlags, pAppendBatchConfiguration,
                        pIdleConfiguration);

                mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                mExpunged = pExpunged ?? throw new ArgumentNullException(nameof(pExpunged));
                mUIDValidityDiscovered = pUIDValidityDiscovered ?? throw new ArgumentNullException(nameof(pUIDValidityDiscovered));
                mIgnoreCapabilities = pIgnoreCapabilities;
                mMailboxCacheDataItems = pMailboxCacheDataItems;
                mFetchCacheItemsSizer = new cBatchSizer(pFetchCacheItemsConfiguration);
                mFetchBodySizer = new cBatchSizer(pFetchBodyConfiguration);
                mAppendDefaultFlags = pAppendDefaultFlags;
                mAppendBatchSizer = new cBatchSizer(pAppendBatchConfiguration);
                mPipeline = new cCommandPipeline(pSynchroniser, ZDisconnected, pNetworkWriteConfiguration, pIdleConfiguration, lContext);
                mEncoding = pEncoding ?? throw new ArgumentNullException(nameof(pEncoding));

                mCommandPartFactory = new cCommandPartFactory(false, null);
                mEncodingPartFactory = new cCommandPartFactory(false, pEncoding);
            }

            public bool TLSInstalled => mPipeline.TLSInstalled;

            public void SetEnabled(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetEnabled));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.authenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotAuthenticated);

                bool lUTF8Enabled = (EnabledExtensions & fEnableableExtensions.utf8) != 0;

                if (lUTF8Enabled)
                {
                    mCommandPartFactory = new cCommandPartFactory(true, null);
                    mEncodingPartFactory = mCommandPartFactory;
                }

                mStatusAttributes = mMailboxCacheDataItems & fMailboxCacheDataItems.allstatus;
                if (!_Capabilities.CondStore) mStatusAttributes &= ~fMailboxCacheDataItems.highestmodseq;

                mMailboxCache = new cMailboxCache(mSynchroniser, mExpunged, mUIDValidityDiscovered, mMailboxCacheDataItems, mCommandPartFactory, _Capabilities, _ConnectedAccountId, ZSetState);

                mPipeline.Install(new cResponseTextCodeParserSelect(_Capabilities));
                mPipeline.Install(new cResponseDataParserSelect());
                mPipeline.Install(new cResponseDataParserFetch(lUTF8Enabled));
                mPipeline.Install(new cResponseDataParserList(lUTF8Enabled));
                mPipeline.Install(new cResponseDataParserLSub(lUTF8Enabled));
                if (_Capabilities.ESearch || _Capabilities.ESort) mPipeline.Install(new cResponseDataParserESearch());

                mPipeline.Enable(mMailboxCache, _Capabilities, lContext);

                ZSetState(eIMAPConnectionState.enabled, lContext);
            }

            public void SetInitialised(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetInitialised));

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.enabled) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotEnabled);

                ZSetState(eIMAPConnectionState.notselected, lContext);
            }

            public void SetIdleConfiguration(cIdleConfiguration pConfiguration, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetIdleConfiguration), pConfiguration);
                mPipeline.SetIdleConfiguration(pConfiguration, lContext);
            }

            public void SetEncoding(Encoding pEncoding, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetEncoding), pEncoding);
                mEncoding = pEncoding ?? throw new ArgumentNullException(nameof(pEncoding));
                if ((EnabledExtensions & fEnableableExtensions.utf8) != 0) mEncodingPartFactory = mCommandPartFactory;
                else mEncodingPartFactory = new cCommandPartFactory(false, pEncoding);
            }

            public eIMAPConnectionState ConnectionState => _ConnectionState;
            public bool IsUnconnected => _ConnectionState == eIMAPConnectionState.notconnected || _ConnectionState == eIMAPConnectionState.disconnected;
            public bool IsConnected => _ConnectionState == eIMAPConnectionState.notselected || _ConnectionState == eIMAPConnectionState.selected;

            private void ZSetState(eIMAPConnectionState pConnectionState, cTrace.cContext pParentContext)
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
                    if (_ConnectionState == eIMAPConnectionState.disconnected) return;

                    lIsUnconnected = IsUnconnected;
                    lIsConnected = IsConnected;

                    _ConnectionState = pConnectionState;
                }

                mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.ConnectionState), lContext);
                if (IsConnected != lIsConnected) mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.IsConnected), lContext);
                if (IsUnconnected != lIsUnconnected) mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.IsUnconnected), lContext);
            }

            public cIMAPCapabilities Capabilities => _Capabilities;

            private void ZSetCapabilities(cStrings pCapabilities, cStrings pAuthenticationMechanisms, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetCapabilities), pCapabilities, pAuthenticationMechanisms);
                _Capabilities = new cIMAPCapabilities(pCapabilities, pAuthenticationMechanisms, mIgnoreCapabilities);
                if (_Capabilities.Binary) ZAddSupportedFormat(fMessageDataFormat.binary, lContext);
                mPipeline.SetCapabilities(_Capabilities, lContext);
                mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
            }

            public fMessageDataFormat SupportedFormats => _SupportedFormats;

            public void ZAddSupportedFormat(fMessageDataFormat pFormat, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZAddSupportedFormat), pFormat);
                if ((_SupportedFormats & pFormat) == pFormat) return;
                _SupportedFormats |= pFormat;
                mSynchroniser.InvokePropertyChanged(nameof(cMailClient.SupportedFormats), lContext);
            }

            public cURL HomeServerReferral => _HomeServerReferral;

            private bool ZSetHomeServerReferral(cIMAPResponseText pResponseText, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZSetHomeServerReferral), pResponseText);

                if (pResponseText.Code != eIMAPResponseTextCode.referral || pResponseText.Arguments == null || pResponseText.Arguments.Count != 1) return false;

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
                ZSetState(eIMAPConnectionState.authenticated, lContext);
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
                ZSetState(eIMAPConnectionState.disconnected, lContext);
            }

            private void ZDisconnected(cTrace.cContext pParentContext)
            {
                // called by the command pipeline when the background task exits
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZDisconnected));
                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                ZSetState(eIMAPConnectionState.disconnected, lContext);
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