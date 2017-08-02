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
                private readonly bool mDescend;
                private readonly List<cMailboxName> mMailboxes = new List<cMailboxName>();
                private readonly int mSequence;

                public cCommandHookLSub(cMailboxCache pCache, cMailboxNamePattern pPattern, bool pDescend)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                    mDescend = pDescend;
                    mSequence = pCache.Sequence;
                }

                public List<iMailboxHandle> Handles { get; private set; } = null;

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(ProcessData));

                    var lLSub = pData as cResponseDataLSub;
                    if (lLSub == null) return eProcessDataResult.notprocessed;

                    if (!mPattern.Matches(lLSub.MailboxName.Name)) return eProcessDataResult.notprocessed;

                    if (lLSub.Subscribed) mMailboxes.Add(lLSub.MailboxName);
                    else if (!mDescend) mMailboxes.Add(lLSub.MailboxName);

                    return eProcessDataResult.observed;
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok)
                    {
                        mCache.ResetLSubFlags(mPattern, mSequence, lContext);
                        Handles = mCache.GetHandles(mMailboxes);
                    }
                }
            }
        }
    }
}