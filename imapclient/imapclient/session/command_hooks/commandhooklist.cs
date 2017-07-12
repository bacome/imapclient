using System;
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
                private int mListFlagsLastSequence;

                public cCommandHookList(cMailboxCache pCache, cMailboxNamePattern pPattern, int pListFlagsLastSequence)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                    mListFlagsLastSequence = pListFlagsLastSequence;
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) mCache.SetNonExistent(mPattern, mListFlagsLastSequence);
                }
            }
        }
    }
}