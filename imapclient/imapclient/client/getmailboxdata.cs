using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void GetMailboxData(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetMailboxData));
            var lTask = ZGetMailboxDataAsync(pHandle, pDataSets, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task GetMailboxDataAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetMailboxDataAsync));
            return ZGetMailboxDataAsync(pHandle, pDataSets, lContext);
        }

        private async Task ZGetMailboxDataAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetMailboxDataAsync), pHandle, pDataSets);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            if (pHandle.MailboxName == null) throw new ArgumentOutOfRangeException(nameof(pHandle));
            if (pDataSets == 0) return;

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                if (pDataSets == fMailboxCacheDataSets.status)
                {
                    await lSession.StatusAsync(lMC, pHandle, lContext).ConfigureAwait(false);
                    return;
                }

                string lListMailbox = pHandle.MailboxName.Name.Replace('*', '%');
                cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name, string.Empty, pHandle.MailboxName.Delimiter);

                var lCapability = lSession.Capability;
                bool lList = (pDataSets & fMailboxCacheDataSets.list) != 0;
                bool lLSub = (pDataSets & fMailboxCacheDataSets.lsub) != 0;
                bool lStatus = (pDataSets & fMailboxCacheDataSets.status) != 0;
                bool lListStatus = lStatus && lCapability.ListStatus;

                List<Task> lTasks = new List<Task>();

                if (lCapability.ListExtended && (mMailboxReferrals || lList || lListStatus))
                {
                    if (lList)
                    {
                        lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.exists, mMailboxReferrals, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lListStatus, lContext));
                        lListStatus = false;
                    }

                    if (lLSub) lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, mMailboxReferrals, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));

                    if (lStatus && !lListStatus) lTasks.Add(lSession.StatusAsync(lMC, pHandle, lContext));
                }
                else
                {
                    if (lList)
                    {
                        if (mMailboxReferrals && lCapability.MailboxReferrals) lTasks.Add(lSession.RListAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lContext));
                        else lTasks.Add(lSession.ListAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lContext));
                    }

                    if (lLSub)
                    {
                        if (mMailboxReferrals && lCapability.MailboxReferrals) lTasks.Add(lSession.RLSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));
                        else lTasks.Add(lSession.LSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));
                    }

                    if (lStatus) lTasks.Add(lSession.StatusAsync(lMC, pHandle, lContext));
                }

                await cTerminator.AwaitAll(lMC, lTasks).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

        private Task ZGetStatuses(cMethodControl pMC, cSession pSession, List<iMailboxHandle> pHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetStatuses), pMC);
            List<Task> lStatuses = new List<Task>();
            foreach (var lHandle in pHandles) if (lHandle.ListFlags?.CanSelect == true) lStatuses.Add(pSession.StatusAsync(pMC, lHandle, lContext));
            return cTerminator.AwaitAll(pMC, lStatuses);
        }
    }
}