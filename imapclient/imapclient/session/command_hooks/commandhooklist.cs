using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookListMailboxes : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly cMailboxPathPattern mPattern;
                private readonly List<cMailboxName> mMailboxes = new List<cMailboxName>();
                private int mSequence;

                public cCommandHookListMailboxes(cMailboxCache pCache, cMailboxPathPattern pPattern)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                }

                public List<iMailboxHandle> MailboxHandles { get; private set; } = null;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookListMailboxes), nameof(CommandStarted));
                    mSequence = mCache.Sequence;
                }

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookListMailboxes), nameof(ProcessData));

                    if (!(pData is cResponseDataListMailbox lListMailbox)) return eProcessDataResult.notprocessed;
                    if (!mPattern.Matches(lListMailbox.MailboxName.Path)) return eProcessDataResult.notprocessed;

                    mMailboxes.Add(lListMailbox.MailboxName);
                    return eProcessDataResult.observed;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookListMailboxes), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType != eCommandResultType.ok) return;
                    
                    mCache.ResetExists(mPattern, mSequence, lContext);
                    MailboxHandles = mCache.GetHandles(mMailboxes);
                }
            }
        }
    }
}