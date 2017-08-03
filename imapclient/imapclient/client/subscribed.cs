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

        public List<cMailbox> Subscribed(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren = true, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> SubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren = true, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribedAsync));
            return ZSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pStatus, lContext);
        }

        private Task<List<cMailbox>> ZSubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribedAsync), pListMailbox, pDelimiter, pHasSubscribedChildren, pStatus);
            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);
            return ZZSubscribedAsync(pListMailbox, pDelimiter, lPattern, pHasSubscribedChildren, pStatus, lContext);
        }

        // mailbox sub-mailbox list

        public List<cMailbox> Subscribed(iMailboxHandle pHandle, bool pDescend, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pHandle, pDescend, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> SubscribedAsync(iMailboxHandle pHandle, bool pDescend, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribedAsync));
            return ZSubscribedAsync(pHandle, pDescend, pStatus, lContext);
        }

        private async Task<List<cMailbox>> ZSubscribedAsync(iMailboxHandle pHandle, bool pDescend, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribedAsync), pHandle, pDescend, pStatus);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pHandle.MailboxName.Delimiter == null) return new List<cMailbox>();

            string lWildcard;
            if (pDescend) lWildcard = "*";
            else lWildcard = "%";

            string lListMailbox = pHandle.MailboxName.Name.Replace('*', '%') + pHandle.MailboxName.Delimiter + lWildcard;
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name + pHandle.MailboxName.Delimiter, lWildcard, pHandle.MailboxName.Delimiter);

            return await ZZSubscribedAsync(lListMailbox, pHandle.MailboxName.Delimiter, lPattern, !pDescend, pStatus, lContext).ConfigureAwait(false);
        }

        // namespace sub-mailbox list

        public List<cMailbox> Subscribed(cNamespaceName pNamespaceName, bool pDescend, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pNamespaceName, pDescend, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> SubscribedAsync(cNamespaceName pNamespaceName, bool pDescend, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribedAsync));
            return ZSubscribedAsync(pNamespaceName, pDescend, pStatus, lContext);
        }

        private Task<List<cMailbox>> ZSubscribedAsync(cNamespaceName pNamespaceName, bool pDescend, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribedAsync), pNamespaceName, pDescend, pStatus);

            if (pNamespaceName == null) throw new ArgumentNullException(nameof(pNamespaceName));

            string lWildcard;
            if (pDescend) lWildcard = "*";
            else lWildcard = "%";

            string lListMailbox = pNamespaceName.Prefix.Replace('*', '%') + lWildcard;
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pNamespaceName.Prefix, lWildcard, pNamespaceName.Delimiter);

            return ZZSubscribedAsync(lListMailbox, pNamespaceName.Delimiter, lPattern, !pDescend, pStatus, lContext);
        }

        // common processing

        private async Task<List<cMailbox>> ZZSubscribedAsync(string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pHasSubscribedChildren, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZSubscribedAsync), pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, pStatus);

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
                bool lListStatus = pStatus && lCapability.ListStatus;

                if (lCapability.ListExtended && (mMailboxReferrals || lListStatus))
                {
                    eListExtendedSelect lSelect;
                    if (pHasSubscribedChildren) lSelect = eListExtendedSelect.subscribedrecursive;
                    else lSelect = eListExtendedSelect.subscribed;

                    lHandles = await lSession.ListExtendedAsync(lMC, lSelect, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lListStatus, lContext).ConfigureAwait(false);

                    if (pStatus && !lListStatus) await ZGetStatuses(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
                }
                else
                {
                    if (mMailboxReferrals && lCapability.MailboxReferrals) lHandles = await lSession.RLSubAsync(lMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, lContext).ConfigureAwait(false);
                    else lHandles = await lSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, lContext).ConfigureAwait(false);

                    if (pStatus) await x;
                }
            }
            finally { mAsyncCounter.Decrement(lContext); }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lHandle in lHandles) lMailboxes.Add(new cMailbox(this, lHandle));
            return lMailboxes;
        }
    }
}