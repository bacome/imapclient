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
        // manual list

        public List<cMailbox> Mailboxes(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pListMailbox, pDelimiter, pDataSets, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pListMailbox, pDelimiter, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pListMailbox, pDelimiter, pDataSets);
            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);
            return ZZMailboxesAsync(pListMailbox, pDelimiter, lPattern, pDataSets, lContext);
        }

        // mailbox sub-mailbox list

        public List<cMailbox> Mailboxes(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pHandle, pDataSets, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pHandle, pDataSets, lContext);
        }

        private async Task<List<cMailbox>> ZMailboxesAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pHandle, pDataSets);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pHandle.MailboxName.Delimiter == null) return new List<cMailbox>();

            string lListMailbox = pHandle.MailboxName.Name.Replace('*', '%') + pHandle.MailboxName.Delimiter + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name + pHandle.MailboxName.Delimiter, "%", pHandle.MailboxName.Delimiter);

            return await ZZMailboxesAsync(lListMailbox, pHandle.MailboxName.Delimiter, lPattern, pDataSets, lContext).ConfigureAwait(false);
        }

        // namespace sub-mailbox list

        public List<cMailbox> Mailboxes(cNamespaceName pNamespaceName, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pNamespaceName, pDataSets, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(cNamespaceName pNamespaceName, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pNamespaceName, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(cNamespaceName pNamespaceName, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pNamespaceName, pDataSets);

            if (pNamespaceName == null) throw new ArgumentNullException(nameof(pNamespaceName));

            string lListMailbox = pNamespaceName.Prefix.Replace('*', '%') + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pNamespaceName.Prefix, "%", pNamespaceName.Delimiter);

            return ZZMailboxesAsync(lListMailbox, pNamespaceName.Delimiter, lPattern, pDataSets, lContext);
        }

        // common processing

        private async Task<List<cMailbox>> ZZMailboxesAsync(string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesAsync), pListMailbox, pDelimiter, pPattern, pDataSets);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

            List<iMailboxHandle> lHandles;

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                var lCapability = lSession.Capability;
                bool lLSub = (pDataSets & fMailboxCacheDataSets.lsub) != 0;
                bool lStatus = (pDataSets & fMailboxCacheDataSets.status) != 0;

                Task<List<iMailboxHandle>> lListTask;
                Task lLSubTask;

                if (lCapability.ListExtended)
                {
                    bool lListStatus = lStatus && lCapability.ListStatus;

                    lListTask = lSession.ListExtendedAsync(lMC, eListExtendedSelect.exists, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lListStatus, lContext);

                    if (lLSub)
                    {
                        if (mMailboxReferrals) lLSubTask = lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, true, pListMailbox, pDelimiter, pPattern, false, lContext);
                        else lLSubTask = lSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                    }
                    else lLSubTask = null;

                    lHandles = await lListTask.ConfigureAwait(false);

                    if (lStatus && !lListStatus) await ZGetStatuses(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
                }
                else
                {
                    if (mMailboxReferrals && lCapability.MailboxReferrals) lListTask = lSession.RListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    else lListTask = lSession.ListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);

                    if (lLSub)
                    {
                        if (mMailboxReferrals && lCapability.MailboxReferrals) lLSubTask = lSession.RLSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                        else lLSubTask = lSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                    }
                    else lLSubTask = null;

                    lHandles = await lListTask.ConfigureAwait(false);

                    if (lStatus) await ZGetStatuses(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
                }

                if (lLSubTask != null) await lLSubTask.ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lHandle in lHandles) lMailboxes.Add(new cMailbox(this, lHandle));
            return lMailboxes;
        }
    }
}