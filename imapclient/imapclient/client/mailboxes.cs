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
        // single mailbox

        public cMailbox Mailbox(cMailboxName pMailboxName, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailbox));
            var lTask = ZMailboxAsync(pMailboxName, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cMailbox> MailboxAsync(cMailboxName pMailboxName, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxAsync));
            return ZMailboxAsync(pMailboxName, pStatus, lContext);
        }

        private async Task<cMailbox> ZMailboxAsync(cMailboxName pMailboxName, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxAsync), pMailboxName, pStatus);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            var lHandle = mSession.GetMailboxHandle(pMailboxName);

            string lListMailbox = pMailboxName.Name.Replace('*', '%');
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pMailboxName.Name, string.Empty, pMailboxName.Delimiter);

            await ZZMailboxesAsync(lListMailbox, pMailboxName.Delimiter, lPattern, pStatus, lContext).ConfigureAwait(false);

            return new cMailbox(this, lHandle);
        }

        // manual list

        public List<cMailbox> Mailboxes(string pListMailbox, char? pDelimiter, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pListMailbox, pDelimiter, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(string pListMailbox, char? pDelimiter, bool pStatus = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pListMailbox, pDelimiter, pStatus, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(string pListMailbox, char? pDelimiter, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pListMailbox, pDelimiter, pStatus);
            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);
            return ZZMailboxesAsync(pListMailbox, pDelimiter, lPattern, pStatus, lContext);
        }

        // mailbox sub-mailbox list

        public List<cMailbox> Mailboxes(iMailboxHandle pHandle, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pHandle, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(iMailboxHandle pHandle, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pHandle, pStatus, lContext);
        }

        private async Task<List<cMailbox>> ZMailboxesAsync(iMailboxHandle pHandle, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pHandle, pStatus);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pHandle.MailboxName.Delimiter == null) return new List<cMailbox>();

            string lListMailbox = pHandle.MailboxName.Name.Replace('*', '%') + pHandle.MailboxName.Delimiter + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pHandle.MailboxName.Name + pHandle.MailboxName.Delimiter, "%", pHandle.MailboxName.Delimiter);

            return await ZZMailboxesAsync(lListMailbox, pHandle.MailboxName.Delimiter, lPattern, pStatus, lContext).ConfigureAwait(false);
        }

        // namespace sub-mailbox list

        public List<cMailbox> Mailboxes(cNamespaceName pNamespaceName, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pNamespaceName, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(cNamespaceName pNamespaceName, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pNamespaceName, pStatus, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(cNamespaceName pNamespaceName, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pNamespaceName, pStatus);

            if (pNamespaceName == null) throw new ArgumentNullException(nameof(pNamespaceName));

            string lListMailbox = pNamespaceName.Prefix.Replace('*', '%') + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pNamespaceName.Prefix, "%", pNamespaceName.Delimiter);

            return ZZMailboxesAsync(lListMailbox, pNamespaceName.Delimiter, lPattern, pStatus, lContext);
        }

        // common processing

        private async Task<List<cMailbox>> ZZMailboxesAsync(string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesAsync), pListMailbox, pDelimiter, pPattern, pStatus);

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

                if (lCapability.ListExtended)
                {
                    bool lListStatus = pStatus && lCapability.ListStatus;
                    lHandles = await lSession.ListExtendedAsync(lMC, false, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lListStatus, lContext).ConfigureAwait(false);
                    if (pStatus && !lListStatus) await ZZMailboxesStatusAsync(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
                }
                else
                {
                    Task<List<iMailboxHandle>> lListTask;
                    if (mMailboxReferrals && lCapability.MailboxReferrals) lListTask = lSession.RListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    else lListTask = lSession.ListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);

                    Task lLSubTask;
                    if ((mMailboxFlagSets & fMailboxFlagSets.subscribed) == 0) lLSubTask = null;
                    else if (mMailboxReferrals && lCapability.MailboxReferrals) lLSubTask = lSession.RLSubAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    else lLSubTask = lSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);

                    lHandles = await lListTask.ConfigureAwait(false);

                    if (pStatus) await ZZMailboxesStatusAsync(lMC, lSession, lHandles, lContext).ConfigureAwait(false);

                    if (lLSubTask != null) await lLSubTask.ConfigureAwait(false);
                }
            }
            finally { mAsyncCounter.Decrement(lContext); }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lHandle in lHandles) lMailboxes.Add(new cMailbox(this, lHandle));
            return lMailboxes;
        }

        private Task ZZMailboxesStatusAsync(cMethodControl pMC, cSession pSession, List<iMailboxHandle> pHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesStatusAsync), pMC);
            List<Task> lStatuses = new List<Task>();
            foreach (var lHandle in pHandles) if (lHandle.ListFlags?.CanSelect == true) lStatuses.Add(pSession.StatusAsync(pMC, lHandle, lContext));
            return cTerminator.AwaitAll(pMC, lStatuses);
        }
    }
}