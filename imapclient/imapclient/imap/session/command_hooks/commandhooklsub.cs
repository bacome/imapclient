using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookLSub : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly cMailboxPathPattern mPattern;
                private readonly bool mHasSubscribedChildren;
                private readonly List<cMailboxName> mMailboxes = new List<cMailboxName>();
                private int mSequence;

                public cCommandHookLSub(cMailboxCache pCache, cMailboxPathPattern pPattern, bool pHasSubscribedChildren)
                {
                    mCache = pCache;
                    mPattern = pPattern;
                    mHasSubscribedChildren = pHasSubscribedChildren;
                }

                public IEnumerable<iMailboxHandle> MailboxHandles { get; private set; } = null;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(CommandStarted));
                    mSequence = mCache.Sequence;
                }

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(ProcessData));

                    if (!(pData is cResponseDataLSub lLSub)) return eProcessDataResult.notprocessed;
                    if (!mPattern.Matches(lLSub.MailboxName.Path)) return eProcessDataResult.notprocessed;

                    if (lLSub.Subscribed) mMailboxes.Add(lLSub.MailboxName);
                    else if (mHasSubscribedChildren) mMailboxes.Add(lLSub.MailboxName);

                    return eProcessDataResult.observed;
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookLSub), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType != eIMAPCommandResultType.ok) return;

                    mCache.ResetLSubFlags(mPattern, mSequence, lContext);
                    MailboxHandles = mCache.GetHandles(mMailboxes);
                }
            }
        }
    }
}