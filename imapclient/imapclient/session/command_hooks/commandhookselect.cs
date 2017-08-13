﻿using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookSelect : cCommandHook
            {
                private static readonly cBytes kClosedRBracketSpace = new cBytes("CLOSED] ");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kNoModSeqRBracketSpace = new cBytes("NOMODSEQ] ");

                private readonly cMailboxCache mMailboxCache;
                private readonly cCapabilities mCapabilities;
                private readonly iMailboxHandle mHandle;
                private readonly bool mForUpdate;

                private bool mDeselectDone = false;

                private cMessageFlags mFlags = null;
                private int mExists = 0;
                private int mRecent = 0;
                private cMessageFlags mPermanentFlags = null;
                private uint mUIDNext = 0;
                private uint mUIDValidity = 0;
                private uint mHighestModSeq = 0;
                private bool mAccessReadOnly = false;

                public cCommandHookSelect(cMailboxCache pMailboxCache, cCapabilities pCapabilities, iMailboxHandle pHandle, bool pForUpdate)
                {
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                    mHandle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
                    mForUpdate = pForUpdate;
                }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandStarted));

                    if (!mCapabilities.QResync)
                    {
                        mMailboxCache.Deselect(lContext);
                        mDeselectDone = true;
                    }
                }

                public override eProcessDataResult ProcessData(cResponseData pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessData));

                    if (!mDeselectDone) return eProcessDataResult.notprocessed;

                    switch (pCursor)
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

                public override bool ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessTextCode));

                    if (!mDeselectDone) return false;

                    switch (pData)
                    {
                        case cResponseDataPermanentFlags lFlags:

                            mPermanentFlags = lFlags.Flags;
                            return true;

                        case cResponseDataUIDNext lUIDNext:

                            mUIDNext = lUIDNext.UIDNext;
                            return true;

                        case cResponseDataUIDValidity lUIDValidity:

                            mUIDValidity = lUIDValidity.UIDValidity;
                            return true;

                        case cResponseDataHighestModSeq lHighestModSeq:

                            mHighestModSeq = lHighestModSeq.HighestModSeq;
                            return true;

                        case cResponseDataAccess lAccess:

                            mAccessReadOnly = lAccess.ReadOnly;
                            return true;

                        default:

                            return false;
                    }
                }

                public override bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessTextCode));

                    if (mDeselectDone)
                    {
                        if (pCursor.SkipBytes(kUnseenSpace))
                        {
                            if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace)) return true;
                            lContext.TraceWarning("likely malformed unseen response");
                            return false;
                        }

                        if (pCursor.SkipBytes(kNoModSeqRBracketSpace))
                        {
                            mHighestModSeq = 0;
                            return true;
                        }

                        return false;
                    }

                    if (pCursor.SkipBytes(kClosedRBracketSpace))
                    { 
                        mMailboxCache.Deselect(lContext);
                        mDeselectDone = true;
                        return true;
                    }

                    return false;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eCommandResultType.ok) return;
                    mMailboxCache.Select(mHandle, mForUpdate, mAccessReadOnly, mFlags, mPermanentFlags, mExists, mRecent, mUIDNext, mUIDValidity, mHighestModSeq, lContext);
                }
            }
        }
    }
}