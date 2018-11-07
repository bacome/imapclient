using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kSelectForUpdateCommandPart = new cTextCommandPart("SELECT ");
            private static readonly cCommandPart kSelectReadOnlyCommandPart = new cTextCommandPart("EXAMINE ");
            private static readonly cCommandPart kSelectCommandPartCondStore = new cTextCommandPart(" (CONDSTORE)");
            private static readonly cCommandPart kSelectCommandPartQResync = new cTextCommandPart(" (QRESYNC (");
            private static readonly cCommandPart kSelectCommandPartRParenRParen = new cTextCommandPart("))");

            public async Task<iSelectedMailboxCache> SelectExamineAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, bool pForUpdate, cQResyncParameters pQResyncParameters, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SelectAsync), pMC, pMailboxHandle, pForUpdate, pQResyncParameters);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pQResyncParameters != null && (EnabledExtensions & fEnableableExtensions.qresync) == 0) throw new ArgumentOutOfRangeException(nameof(pQResyncParameters));

                var lMailboxCacheItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // get exclusive access to the selected mailbox
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    PersistentCache.BeforeSelect(pMailboxHandle.MailboxId, lContext);

                    try
                    {
                        if (pForUpdate) lBuilder.Add(kSelectForUpdateCommandPart);
                        else lBuilder.Add(kSelectReadOnlyCommandPart);

                        lBuilder.Add(lMailboxCacheItem.MailboxNameCommandPart);

                        if (pQResyncParameters == null)
                        {
                            if (_Capabilities.CondStore) lBuilder.Add(kSelectCommandPartCondStore);
                        }
                        else
                        {
                            lBuilder.Add(kSelectCommandPartQResync);
                            lBuilder.Add(new cTextCommandPart(pQResyncParameters.CachedUIDValidity));
                            lBuilder.Add(cCommandPart.Space);
                            lBuilder.Add(new cTextCommandPart(pQResyncParameters.CachedHighestModSeq));
                            lBuilder.Add(cCommandPart.Space);
                            lBuilder.Add(new cTextCommandPart(pQResyncParameters.CachedUIDs));
                            lBuilder.Add(kSelectCommandPartRParenRParen);
                        }

                        var lHook = new cCommandHookSelectExamine(PersistentCache, mSynchroniser, UTF8Enabled, mMailboxCache, _Capabilities, lMailboxCacheItem, pForUpdate, pQResyncParameters != null);
                        lBuilder.Add(lHook);

                        var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                        if (lResult.ResultType == eIMAPCommandResultType.ok)
                        {
                            lContext.TraceInformation("select success");
                            return lHook.SelectedMailboxCache;
                        }

                        fIMAPCapabilities lTryIgnoring;
                        if (_Capabilities.CondStore) lTryIgnoring = fIMAPCapabilities.condstore;
                        if (_Capabilities.QResync) lTryIgnoring = fIMAPCapabilities.qresync;
                        else lTryIgnoring = 0;

                        if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, lTryIgnoring, lContext);
                        throw new cIMAPProtocolErrorException(lResult, lTryIgnoring, lContext);
                    }
                    catch
                    {
                        PersistentCache.AfterUnselect(pMailboxHandle.MailboxId, lContext);
                        throw;
                    }
                }
            }

            private class cCommandHookSelectExamine : cCommandHook
            {
                private static readonly cBytes kClosed = new cBytes("CLOSED");
                private static readonly cBytes kNoModSeq = new cBytes("NOMODSEQ");
                private static readonly cBytes kUIDNotSticky = new cBytes("UIDNOTSTICKY");

                private readonly cPersistentCache mPersistentCache;
                private readonly cIMAPCallbackSynchroniser mSynchroniser;
                private readonly bool mUTF8Enabled;
                private readonly cMailboxCache mMailboxCache;
                private readonly cIMAPCapabilities mCapabilities;
                private readonly cMailboxCacheItem mMailboxCacheItem;
                private readonly bool mForUpdate;
                private readonly bool mUsingQResync;

                private bool mClosed = false;
                private cFetchableFlags mFlags = null;
                private int mExists = 0;
                private int mRecent = 0;
                private cPermanentFlags mPermanentFlags = null;
                private uint mUIDValidity = 0;
                private uint mUIDNextComponent = 0;
                private ulong mHighestModSeq = 0;
                private bool mUIDNotSticky = false;
                private bool mAccessReadOnly = false;

                private cSelectedMailboxCache mSelectedMailboxCache = null;

                public cCommandHookSelectExamine(cPersistentCache pPersistentCache, cIMAPCallbackSynchroniser pSynchroniser, bool pUTF8Enabled, cMailboxCache pMailboxCache, cIMAPCapabilities pCapabilities, cMailboxCacheItem pMailboxCacheItem, bool pForUpdate, bool pUsingQResync)
                {
                    mPersistentCache = pPersistentCache ?? throw new ArgumentNullException(nameof(pPersistentCache));
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mUTF8Enabled = pUTF8Enabled;
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                    mMailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mForUpdate = pForUpdate;
                    mUsingQResync = pUsingQResync;
                }

                public cSelectedMailboxCache SelectedMailboxCache => mSelectedMailboxCache;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelectExamine), nameof(CommandStarted));
                    if (mMailboxCache.SelectedMailboxDetails != null && !mCapabilities.QResync) mMailboxCache.Unselect(lContext);
                }

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelectExamine), nameof(ProcessData));

                    if (mMailboxCache.SelectedMailboxDetails != null) return eProcessDataResult.notprocessed;

                    switch (pData)
                    {
                        case cResponseDataFlags lFlags:

                            if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                            mFlags = lFlags.Flags;
                            return eProcessDataResult.processed;

                        case cResponseDataExists lExists:

                            if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                            mExists = lExists.Exists;
                            return eProcessDataResult.processed;

                        case cResponseDataRecent lRecent:

                            if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                            mRecent = lRecent.Recent;
                            return eProcessDataResult.processed;

                        case cResponseDataVanished lVanished when mUsingQResync && lVanished.Earlier:

                            if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                            ZSetSelectedMailboxCache(lContext);
                            return mSelectedMailboxCache.ProcessData(lVanished, lContext);

                        case cResponseDataFetch lFetch when mUsingQResync:

                            ZSetSelectedMailboxCache(lContext);
                            return mSelectedMailboxCache.ProcessData(lFetch, lContext);
                    }

                    return eProcessDataResult.notprocessed;
                }

                public override void ProcessTextCode(eIMAPResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelectExamine), nameof(ProcessTextCode), pTextContext, pData);

                    if (mMailboxCache.SelectedMailboxDetails != null) return;

                    if (pTextContext == eIMAPResponseTextContext.information)
                    {
                        switch (pData)
                        {
                            case cResponseDataPermanentFlags lFlags:

                                if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                                mPermanentFlags = lFlags.Flags;
                                return;

                            case cResponseDataUIDValidity lUIDValidity:

                                if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                                mUIDValidity = lUIDValidity.UIDValidity;
                                return;

                            case cResponseDataUIDNext lUIDNext:

                                if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                                mUIDNextComponent = lUIDNext.UIDNext;
                                return;

                            case cResponseDataHighestModSeq lHighestModSeq:

                                if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                                mHighestModSeq = lHighestModSeq.HighestModSeq;
                                return;
                        }
                    }
                    else if (pTextContext == eIMAPResponseTextContext.success && pData is cResponseDataAccess lAccess) mAccessReadOnly = lAccess.ReadOnly;
                }

                public override void ProcessTextCode(eIMAPResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelectExamine), nameof(ProcessTextCode), pTextContext, pCode, pArguments);

                    if (mMailboxCache.SelectedMailboxDetails == null)
                    {
                        if (pTextContext == eIMAPResponseTextContext.information && pCode.Equals(kNoModSeq) && pArguments == null)
                        {
                            if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                            mHighestModSeq = 0;
                        }
                        else if (pTextContext == eIMAPResponseTextContext.warning && pCode.Equals(kUIDNotSticky) && pArguments == null)
                        {
                            if (mSelectedMailboxCache != null) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.SelectResponseOrderProblem, fIMAPCapabilities.qresync, lContext);
                            mUIDNotSticky = true;
                        }
                    }
                    else
                    {
                        // the spec (rfc 7162) doesn't specify where this comes - although the only example is of an untagged OK
                        if (pCode.Equals(kClosed) && pArguments == null) mMailboxCache.Unselect(lContext);
                    }
                }

                private void ZSetSelectedMailboxCache(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelectExamine), nameof(ZSetSelectedMailboxCache));
                    if (mSelectedMailboxCache == null) return;
                    mPersistentCache.CheckUIDValidity(mMailboxCacheItem.MailboxId, mUIDValidity, lContext);
                    mSelectedMailboxCache = new cSelectedMailboxCache(mPersistentCache, mSynchroniser, mUTF8Enabled, mMailboxCacheItem, mUIDValidity, mUIDNotSticky, mExists, mRecent, mUIDNextComponent, mHighestModSeq, lContext);
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelectExamine), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eIMAPCommandResultType.ok) return;
                    ZSetSelectedMailboxCache(lContext);
                    mSelectedMailboxCache.SetSelected(mFlags, mForUpdate, mPermanentFlags, lContext);
                    mMailboxCache.Select(mSelectedMailboxCache, mForUpdate, mAccessReadOnly, lContext);
                }
            }
        }
    }
}
