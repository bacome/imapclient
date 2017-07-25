using System;
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

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) mCache.ResetLSubFlags(mPattern, mSequence, lContext);
                }
            }
        }
    }
}