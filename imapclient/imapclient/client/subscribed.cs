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

        public List<cMailbox> Subscribed(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> SubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribedAsync));
            return ZSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZSubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribedAsync), pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets);
            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);
            return ZZSubscribedAsync(pListMailbox, pDelimiter, lPattern, pHasSubscribedChildren, pDataSets, lContext);
        }

        // mailbox sub-mailbox list

        public List<cMailbox> Subscribed(iMailboxHandle pHandle, bool pDescend, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pHandle, pDescend, pDataSets, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> SubscribedAsync(iMailboxHandle pHandle, bool pDescend, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribedAsync));
            return ZSubscribedAsync(pHandle, pDescend, pDataSets, lContext);
        }

        private async Task<List<cMailbox>> ZSubscribedAsync(iMailboxHandle pHandle, bool pDescend, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribedAsync), pHandle, pDescend, pDataSets);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pHandle.MailboxName.Delimiter == null) return new List<cMailbox>();

            string lWildcard;
            if (pDescend) lWildcard = "*";
            else lWildcard = "%";

            string lListMailbox = pHandle.MailboxName.Name.Replace('*', '%') + pHandle.MailboxName.Delimiter + lWildcard;
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name + pHandle.MailboxName.Delimiter, lWildcard, pHandle.MailboxName.Delimiter);

            return await ZZSubscribedAsync(lListMailbox, pHandle.MailboxName.Delimiter, lPattern, !pDescend, pDataSets, lContext).ConfigureAwait(false);
        }

        // namespace sub-mailbox list

        public List<cMailbox> Subscribed(cNamespaceName pNamespaceName, bool pDescend, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pNamespaceName, pDescend, pDataSets, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> SubscribedAsync(cNamespaceName pNamespaceName, bool pDescend, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribedAsync));
            return ZSubscribedAsync(pNamespaceName, pDescend, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZSubscribedAsync(cNamespaceName pNamespaceName, bool pDescend, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribedAsync), pNamespaceName, pDescend, pDataSets);

            if (pNamespaceName == null) throw new ArgumentNullException(nameof(pNamespaceName));

            string lWildcard;
            if (pDescend) lWildcard = "*";
            else lWildcard = "%";

            string lListMailbox = pNamespaceName.Prefix.Replace('*', '%') + lWildcard;
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pNamespaceName.Prefix, lWildcard, pNamespaceName.Delimiter);

            return ZZSubscribedAsync(lListMailbox, pNamespaceName.Delimiter, lPattern, !pDescend, pDataSets, lContext);
        }

        // common processing

        private async Task<List<cMailbox>> ZZSubscribedAsync(string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZSubscribedAsync), pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, pDataSets);

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

                var lCapabilities = lSession.Capabilities;
                bool lList = (pDataSets & fMailboxCacheDataSets.list) != 0;
                bool lStatus = (pDataSets & fMailboxCacheDataSets.status) != 0;
                bool lListStatus = lStatus && lCapabilities.ListStatus;

                if (lCapabilities.ListExtended && (lList || mMailboxReferrals || lListStatus))
                {
                    eListExtendedSelect lSelect;
                    if (pHasSubscribedChildren) lSelect = eListExtendedSelect.subscribedrecursive;
                    else lSelect = eListExtendedSelect.subscribed;

                    lHandles = await lSession.ListExtendedAsync(lMC, lSelect, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lListStatus, lContext).ConfigureAwait(false);

                    if (lStatus && !lListStatus) await ZGetStatuses(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
                }
                else
                {
                    Task<List<iMailboxHandle>> lLSubTask;
                    if (mMailboxReferrals && lCapabilities.MailboxReferrals) lLSubTask = lSession.RLSubAsync(lMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, lContext);
                    else lLSubTask = lSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, lContext);

                    Task lListTask;

                    if (lList)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lListTask = lSession.RListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                        else lListTask = lSession.ListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    }
                    else lListTask = null;

                    lHandles = await lLSubTask.ConfigureAwait(false);

                    if (lStatus) await ZGetStatuses(lMC, lSession, lHandles, lContext).ConfigureAwait(false);

                    if (lListTask != null) await lListTask.ConfigureAwait(false);
                }
            }
            finally { mAsyncCounter.Decrement(lContext); }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lHandle in lHandles) lMailboxes.Add(new cMailbox(this, lHandle));
            return lMailboxes;
        }
    }
}