using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookSelect : cCommandHook
            {
                private static readonly cBytes kClosed = new cBytes("CLOSED");
                private static readonly cBytes kNoModSeq = new cBytes("NOMODSEQ");
                private static readonly cBytes kUIDNotSticky = new cBytes("UIDNOTSTICKY");

                private readonly cMailboxCache mMailboxCache;
                private readonly cIMAPCapabilities mCapabilities;
                private readonly iMailboxHandle mMailboxHandle;
                private readonly bool mForUpdate;
                private readonly bool mUsingQResync;

                private cFetchableFlags mFlags = null;
                private int mExists = 0;
                private int mRecent = 0;
                private cPermanentFlags mPermanentFlags = null;
                private uint mUIDNext = 0;
                private uint mUIDValidity = 0;
                private ulong mHighestModSeq = 0; 
                private bool mUIDNotSticky = false;
                private bool mAccessReadOnly = false;

                public cCommandHookSelect(cMailboxCache pMailboxCache, cIMAPCapabilities pCapabilities, iMailboxHandle pMailboxHandle, bool pForUpdate, bool pUsingQResync)
                {
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                    mMailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
                    mForUpdate = pForUpdate;
                    mUsingQResync = pUsingQResync;
                }

                public cSelectResult Result => new cSelectResult(mUIDValidity, mHighestModSeq, mUIDNotSticky);

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandStarted));
                    if (mMailboxCache.SelectedMailboxDetails != null && !mCapabilities.QResync) mMailboxCache.Unselect(lContext);
                }

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessData));

                    if (mMailboxCache.SelectedMailboxDetails != null) return eProcessDataResult.notprocessed;

                    // qresync: process vanished (accumulate UIDs) and fetches (accumulate MSN->data) here

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
                    }

                    return eProcessDataResult.notprocessed;
                }

                public override void ProcessTextCode(eIMAPResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessTextCode), pTextContext, pData);

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
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessTextCode), pTextContext, pCode, pArguments);

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
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eIMAPCommandResultType.ok) return;

                    ;?; // return the callback for turning on highetmodseq IF qresync was off AND 



                    // qresync: send the accumulated fetch data to the mailboxcache here and the accumulated expunged uids to the cache

                    mMailboxCache.Select(mMailboxHandle, mForUpdate, mAccessReadOnly, mUIDNotSticky, mFlags, mPermanentFlags, mExists, mRecent, mUIDNext, mUIDValidity, mHighestModSeq, lContext);
                }
            }
        }
    }
}