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

            public async Task<cSelectResult> SelectExamineAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, bool pForUpdate, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SelectAsync), pMC, pMailboxHandle, pForUpdate);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

                var lItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // get exclusive access to the selected mailbox
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    PersistentCache.Open(pMailboxHandle.MailboxId, lContext);

                    try
                    {
                        if (pForUpdate) lBuilder.Add(kSelectForUpdateCommandPart);
                        else lBuilder.Add(kSelectReadOnlyCommandPart);

                        lBuilder.Add(lItem.MailboxNameCommandPart);

                        uint lUIDValidity = 0;
                        ulong lCachedHighestModSeq = 0;
                        HashSet<cUID> lUIDsToQResync = null; // null or empty == qresync not used
                        //
                        // the danger is that someone else adds things to the cache after I qresync
                        //  that means that for those items added I could be out of sync after selecting (because I didn't ask for those items to be synched)
                        //
                        // => after the select and before enabling setting the highestmodseq I should check the cache again for UIDs
                        //  if there are new ones I should manually sync flags for those ones 
                        //  [after selecting I don't need to worry about someone else adding things, as the server is obliged to keep me up to date after I select]

                        bool lQResyncEnabled = (EnabledExtensions & fEnableableExtensions.qresync) != 0;

                        if (lQResyncEnabled)
                        {
                            lUIDValidity = PersistentCache.GetUIDValidity(pMailboxHandle.MailboxId, lContext);

                            if (lUIDValidity != 0)
                            {
                                var lMailboxUID = new cMailboxUID(pMailboxHandle.MailboxId, lUIDValidity);
                                lCachedHighestModSeq = PersistentCache.GetHighestModSeq(lMailboxUID, lContext);
                                if (lCachedHighestModSeq != 0) lUIDsToQResync = PersistentCache.GetUIDs(lMailboxUID, lContext);
                            }
                        }

                        bool lUsingQResync;

                        if (lUIDsToQResync == null || lUIDsToQResync.Count == 0)
                        {
                            lUsingQResync = false;
                            if (_Capabilities.CondStore) lBuilder.Add(kSelectCommandPartCondStore);
                        }
                        else
                        {
                            lUsingQResync = true;
                            lBuilder.Add(kSelectCommandPartQResync);
                            lBuilder.Add(new cTextCommandPart(lUIDValidity));
                            lBuilder.Add(cCommandPart.Space);
                            lBuilder.Add(new cTextCommandPart(lCachedHighestModSeq));
                            lBuilder.Add(cCommandPart.Space);
                            lBuilder.Add(new cTextCommandPart(cSequenceSet.FromUInts(from lUID in lUIDsToQResync select lUID.UID, 50)));
                            lBuilder.Add(kSelectCommandPartRParenRParen);
                        }

                        var lHook = new cCommandHookSelectExamine(mMailboxCache, _Capabilities, lQResyncEnabled, pMailboxHandle, true, lUsingQResync);
                        lBuilder.Add(lHook);

                        var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                        if (lResult.ResultType == eIMAPCommandResultType.ok)
                        {
                            lContext.TraceInformation("select success");

                            if (lHook.UIDValidity != lUIDValidity || lHook.UIDNotSticky || lHook.HighestModSeq < lCachedHighestModSeq)
                            {
                                lCachedHighestModSeq = 0;
                                lUIDsToQResync = null;
                            }

                            return new cSelectResult(lHook.UIDValidity, lHook.UIDNotSticky, lCachedHighestModSeq, lUIDsToQResync, lHook.SetCallSetHighestModSeq);
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
                        PersistentCache.Close(pMailboxHandle.MailboxId, lContext);
                        throw;
                    }
                }
            }

            private class cCommandHookSelectExamine : cCommandHook
            {
                private static readonly cBytes kClosed = new cBytes("CLOSED");
                private static readonly cBytes kNoModSeq = new cBytes("NOMODSEQ");
                private static readonly cBytes kUIDNotSticky = new cBytes("UIDNOTSTICKY");

                private readonly cMailboxCache mMailboxCache;
                private readonly cIMAPCapabilities mCapabilities;
                private readonly bool mQResyncEnabled;
                private readonly iMailboxHandle mMailboxHandle;
                private readonly bool mForUpdate;
                private readonly bool mUsingQResync;

                private bool mClosed = false;
                private cFetchableFlags mFlags = null;
                private int mExists = 0;
                private int mRecent = 0;
                private cPermanentFlags mPermanentFlags = null;
                private uint mUIDNext = 0;
                private uint mUIDValidity = 0;
                private ulong mHighestModSeq = 0;
                private bool mUIDNotSticky = false;
                private bool mAccessReadOnly = false;

                private readonly List<cResponseDataVanished> mVanishedEalier = new List<cResponseDataVanished>();
                private readonly List<cResponseDataFetch> mFetch = new List<cResponseDataFetch>();

                private Action<cTrace.cContext> mSetCallSetHighestModSeq = null;

                public cCommandHookSelectExamine(cMailboxCache pMailboxCache, cIMAPCapabilities pCapabilities, bool pQResyncEnabled, iMailboxHandle pMailboxHandle, bool pForUpdate, bool pUsingQResync)
                {
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                    mQResyncEnabled = pQResyncEnabled;
                    mMailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
                    mForUpdate = pForUpdate;
                    mUsingQResync = pUsingQResync;
                }

                public uint UIDValidity => mUIDValidity;
                public ulong HighestModSeq => mHighestModSeq;
                public bool UIDNotSticky => mUIDNotSticky;
                public Action<cTrace.cContext> SetCallSetHighestModSeq => mSetCallSetHighestModSeq;

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

                            mFlags = lFlags.Flags;
                            return eProcessDataResult.processed;

                        case cResponseDataExists lExists:

                            mExists = lExists.Exists;
                            return eProcessDataResult.processed;

                        case cResponseDataRecent lRecent:

                            mRecent = lRecent.Recent;
                            return eProcessDataResult.processed;

                        case cResponseDataVanished lVanished:

                            if (mUsingQResync && lVanished.Earlier)
                            {
                                mVanishedEalier.Add(lVanished);
                                return eProcessDataResult.processed;
                            }

                            return eProcessDataResult.notprocessed;

                        case cResponseDataFetch lFetch:

                            if (mUsingQResync)
                            {
                                mFetch.Add(lFetch);
                                return eProcessDataResult.processed;
                            }

                            return eProcessDataResult.notprocessed;
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

                                mPermanentFlags = lFlags.Flags;
                                return;

                            case cResponseDataUIDNext lUIDNext:

                                mUIDNext = lUIDNext.UIDNext;
                                return;

                            case cResponseDataUIDValidity lUIDValidity:

                                mUIDValidity = lUIDValidity.UIDValidity;
                                return;

                            case cResponseDataHighestModSeq lHighestModSeq:

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
                        if (pTextContext == eIMAPResponseTextContext.information && pCode.Equals(kNoModSeq) && pArguments == null) mHighestModSeq = 0;
                        else if (pTextContext == eIMAPResponseTextContext.warning && pCode.Equals(kUIDNotSticky) && pArguments == null) mUIDNotSticky = true;
                    }
                    else
                    {
                        // the spec (rfc 7162) doesn't specify where this comes - although the only example is of an untagged OK
                        if (pCode.Equals(kClosed) && pArguments == null) mMailboxCache.Unselect(lContext);
                    }
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelectExamine), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eIMAPCommandResultType.ok) return;

                    // need UIDs to be able to process VANISHED
                    if (mQResyncEnabled && mHighestModSeq != 0 && mUIDValidity == 0) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.QResyncWithModSeqAndNoUIDValidity, fIMAPCapabilities.qresync, lContext);

                    // returns the callback for turning on sethighestmodseq which we must call after synchronising the persistent cache
                    mSetCallSetHighestModSeq = mMailboxCache.Select(mMailboxHandle, mForUpdate, mAccessReadOnly, mUIDNotSticky, mFlags, mPermanentFlags, mExists, mRecent, mUIDNext, mUIDValidity, mHighestModSeq, mVanishedEalier, mFetch, lContext);
                }
            }
        }
    }
}
