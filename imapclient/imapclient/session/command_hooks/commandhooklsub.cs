using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookLSub : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly cMailboxNamePattern mPattern;
                private readonly int mSequence;

                public cCommandHookLSub(cMailboxCache pCache, cMailboxNamePattern pPattern)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                    mSequence = pCache.Sequence;
                }

                public List<iMailboxHandle> Handles { get; private set; } = null;


                ;?; // this will need more - must capture the list on the way through to get the subscribed children if one level descent 

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) Handles = mCache.LSub(mPattern, false, mSequence, lContext);
                }
            }
        }
    }
}