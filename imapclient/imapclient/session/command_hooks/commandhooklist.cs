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
            private class cCommandHookList : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly cMailboxNamePattern mPattern;
                private readonly List<cMailboxName> mMailboxes = new List<cMailboxName>();
                private readonly int mSequence;

                public cCommandHookList(cMailboxCache pCache, cMailboxNamePattern pPattern)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                    mSequence = pCache.Sequence;
                }

                public List<iMailboxHandle> Handles { get; private set; } = null;

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookList), nameof(ProcessData));

                    if (!(pData is cResponseDataList lList)) return eProcessDataResult.notprocessed;
                    if (!mPattern.Matches(lList.MailboxName.Name)) return eProcessDataResult.notprocessed;

                    mMailboxes.Add(lList.MailboxName);
                    return eProcessDataResult.observed;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookList), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType != eCommandResultType.ok) return;
                    
                    mCache.ResetListFlags(mPattern, mSequence, lContext);
                    Handles = mCache.GetHandles(mMailboxes);
                }
            }
        }
    }
}