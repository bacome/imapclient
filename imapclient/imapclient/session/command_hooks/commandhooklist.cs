﻿using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookList : cCommandHook
            {
                private cMailboxCache mCache;
                private cMailboxNamePattern mPattern;
                private int mSequence;

                public cCommandHookList(cMailboxCache pCache, cMailboxNamePattern pPattern, int pSequence)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                    mSequence = pSequence;
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookList), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) mCache.ResetExists(mPattern, mSequence, lContext);
                }
            }
        }
    }
}