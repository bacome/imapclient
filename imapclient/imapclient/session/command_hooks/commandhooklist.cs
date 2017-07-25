using System;
using System.Collections.Generic;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookList : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly cMailboxNamePattern mPattern;
                private readonly int mSequence;

                public cCommandHookList(cMailboxCache pCache, cMailboxNamePattern pPattern)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                    mSequence = pCache.Sequence;
                }

                public List<cMailbox> Mailboxes { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookList), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok)
                    {
                        Mailboxes = mCache.ResetExists(mPattern, mSequence, lContext);
                    }
                }
            }
        }
    }
}