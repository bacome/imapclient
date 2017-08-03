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
        public void GetFlags(iMailboxHandle pHandle, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetFlags));
            var lTask = ZGetFlagsAsync(pHandle, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task GetFlagsAsync(iMailboxHandle pHandle, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetFlagsAsync));
            return ZGetFlagsAsync(pHandle, pStatus, lContext);
        }

        private async Task ZGetFlagsAsync(iMailboxHandle pHandle, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetFlagsAsync), pHandle, pStatus);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            if (pHandle.MailboxName == null) throw new ArgumentOutOfRangeException(nameof(pHandle));

            string lListMailbox = pHandle.MailboxName.Name.Replace('*', '%');
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name, string.Empty, pHandle.MailboxName.Delimiter);

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                List<Task> lTasks = new List<Task>();

                var lCapability = lSession.Capability;

                if (lCapability.ListExtended)
                {
                    bool lListStatus = pStatus && lCapability.ListStatus;
                    lTasks.Add(lSession.ListExtendedAsync(lMC, eListExtendedSelect.exists, mMailboxReferrals, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lListStatus, lContext));
                    if (pStatus && !lListStatus) lTasks.Add(ZGetStatus(lMC, lSession, pHandle, lContext));
                }
                else
                {
                    if (mMailboxReferrals && lCapability.MailboxReferrals) lTasks.Add(lSession.RListAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lContext));
                    else lTasks.Add(lSession.ListAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lContext));

                    if ((mMailboxFlagSets & fMailboxFlagSets.subscribed) != 0)
                    {
                        if (mMailboxReferrals && lCapability.MailboxReferrals) lTasks.Add(lSession.RLSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));
                        else lTasks.Add(lSession.LSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext));
                    }

                    if (pStatus) lTasks.Add(ZGetStatus(lMC, lSession, pHandle, lContext));
                }

                await cTerminator.AwaitAll(lMC, lTasks).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

        public void GetSubscribed(iMailboxHandle pHandle, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetSubscribed));
            var lTask = ZGetSubscribedAsync(pHandle, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task GetSubscribedAsync(iMailboxHandle pHandle, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetSubscribedAsync));
            return ZGetSubscribedAsync(pHandle, pStatus, lContext);
        }

        private async Task ZGetSubscribedAsync(iMailboxHandle pHandle, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetSubscribedAsync), pHandle, pStatus);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            if (pHandle.MailboxName == null) throw new ArgumentOutOfRangeException(nameof(pHandle));

            string lListMailbox = pHandle.MailboxName.Name.Replace('*', '%');
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name, string.Empty, pHandle.MailboxName.Delimiter);

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                var lCapability = lSession.Capability;
                bool lListStatus = pStatus && lCapability.ListStatus;

                Task lLSubTask;
                Task lStatusTask = null;

                if (lCapability.ListExtended && (mMailboxReferrals || lListStatus))
                {
                    lLSubTask = lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, mMailboxReferrals, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, lListStatus, lContext);
                    if (pStatus && !lListStatus) lStatusTask = ZGetStatus(lMC, lSession, pHandle, lContext);
                }
                else
                {
                    if (mMailboxReferrals && lCapability.MailboxReferrals) lLSubTask = lSession.RLSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext);
                    else lLSubTask = lSession.LSubAsync(lMC, lListMailbox, pHandle.MailboxName.Delimiter, lPattern, false, lContext);
                    if (pStatus) lStatusTask = ZGetStatus(lMC, lSession, pHandle, lContext);
                }

                await cTerminator.AwaitAll(lMC, lLSubTask, lStatusTask).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

        public void GetStatus(iMailboxHandle pHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetStatus));
            var lTask = ZGetStatusAsync(pHandle, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task GetStatusAsync(iMailboxHandle pHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(GetStatusAsync));
            return ZGetStatusAsync(pHandle, lContext);
        }

        private async Task ZGetStatusAsync(iMailboxHandle pHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetStatusAsync), pHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);
                await lSession.StatusAsync(lMC, pHandle, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

        private Task ZGetStatus(cMethodControl pMC, cSession pSession, iMailboxHandle pHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetStatus), pMC);
            if (pHandle.ListFlags?.CanSelect == true) return pSession.StatusAsync(pMC, pHandle, lContext);
            return null;
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