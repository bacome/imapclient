using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Request(iMailboxHandle pMailboxHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZRequestAsync(pMailboxHandle, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task RequestAsync(iMailboxHandle pMailboxHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            return ZRequestAsync(pMailboxHandle, pDataSets, lContext);
        }

        private async Task ZRequestAsync(iMailboxHandle pMailboxHandle, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZRequestAsync), pMailboxHandle, pDataSets);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            if (pMailboxHandle.MailboxName == null) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
            if (pDataSets == 0) return;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);

                if (pDataSets == fMailboxCacheDataSets.status)
                {
                    await lSession.StatusAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                    return;
                }

                string lListMailbox = pMailboxHandle.MailboxName.Path.Replace('*', '%');
                cMailboxPathPattern lPattern = new cMailboxPathPattern(pMailboxHandle.MailboxName.Path, string.Empty, pMailboxHandle.MailboxName.Delimiter);

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
                        lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.exists, mMailboxReferrals, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, lListStatus, lContext));
                        if (lLSub) lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, mMailboxReferrals, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, false, lContext));
                    }
                    else if (lLSub) lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, mMailboxReferrals, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, lListStatus, lContext));

                    if (lStatus && !lListStatus) lTasks.Add(lSession.StatusAsync(lMC, pMailboxHandle, lContext));
                }
                else
                {
                    if (lList)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lTasks.Add(lSession.RListAsync(lMC, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, lContext));
                        else lTasks.Add(lSession.ListMailboxesAsync(lMC, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, lContext));
                    }

                    if (lLSub)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lTasks.Add(lSession.RLSubAsync(lMC, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, false, lContext));
                        else lTasks.Add(lSession.LSubAsync(lMC, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, false, lContext));
                    }

                    if (lStatus) lTasks.Add(lSession.StatusAsync(lMC, pMailboxHandle, lContext));
                }

                await Task.WhenAll(lTasks).ConfigureAwait(false);
            }
        }

        private Task ZRequestStatus(cMethodControl pMC, cSession pSession, List<iMailboxHandle> pMailboxHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZRequestStatus), pMC);
            List<Task> lTasks = new List<Task>();
            foreach (var lMailboxHandle in pMailboxHandles) if (lMailboxHandle.ListFlags?.CanSelect == true) lTasks.Add(pSession.StatusAsync(pMC, lMailboxHandle, lContext));
            return Task.WhenAll(lTasks);
        }
    }
}