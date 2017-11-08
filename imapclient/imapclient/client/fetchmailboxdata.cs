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
        internal void Fetch(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchAsync(pHandle, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task FetchAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            return ZFetchAsync(pHandle, pDataSets, lContext);
        }

        private async Task ZFetchAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchAsync), pHandle, pDataSets);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            if (pHandle.MailboxName == null) throw new ArgumentOutOfRangeException(nameof(pHandle));
            if (pDataSets == 0) return;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);

                if (pDataSets == fMailboxCacheDataSets.status)
                {
                    await lSession.StatusAsync(lMC, pHandle, lContext).ConfigureAwait(false);
                    return;
                }

                string lListMailbox = pHandle.MailboxName.Path.Replace('*', '%');
                cMailboxPathPattern lPattern = new cMailboxPathPattern(pHandle.MailboxName.Path, string.Empty, pHandle.MailboxName.Delimiter);

                var lCapabilities = lSession.Capabilities;
                bool lList = (pDataSets & fMailboxCacheDataSets.list) != 0;
                bool lLSub = (pDataSets & fMailboxCacheDataSets.lsub) != 0;
                bool lStatus = (pDataSets & fMailboxCacheDataSets.status) != 0;
                bool lListStatus = lStatus && lCapabilities.ListStatus;

                List<Task> lTasks = new List<Task>();

                if (lCapabilities.ListExtended && (lList || (lLSub && (mMailboxReferrals || lListStatus))))
                {
                    if (lList)
                    {
                        lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.exists, mMailboxReferrals, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lListStatus, lContext));
                        if (lLSub) lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, mMailboxReferrals, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));
                    }
                    else if (lLSub) lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, mMailboxReferrals, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lListStatus, lContext));

                    if (lStatus && !lListStatus) lTasks.Add(lSession.StatusAsync(lMC, pHandle, lContext));
                }
                else
                {
                    if (lList)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lTasks.Add(lSession.RListAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lContext));
                        else lTasks.Add(lSession.ListMailboxesAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lContext));
                    }

                    if (lLSub)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lTasks.Add(lSession.RLSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));
                        else lTasks.Add(lSession.LSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));
                    }

                    if (lStatus) lTasks.Add(lSession.StatusAsync(lMC, pHandle, lContext));
                }

                await Task.WhenAll(lTasks).ConfigureAwait(false);
            }
        }

        private Task ZFetchStatus(cMethodControl pMC, cSession pSession, List<iMailboxHandle> pHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchStatus), pMC);
            List<Task> lTasks = new List<Task>();
            foreach (var lHandle in pHandles) if (lHandle.ListFlags?.CanSelect == true) lTasks.Add(pSession.StatusAsync(pMC, lHandle, lContext));
            return Task.WhenAll(lTasks);
        }
    }
}