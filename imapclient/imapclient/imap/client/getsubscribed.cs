using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Gets a list subscribed mailboxes using an IMAP wildcard search.
        /// </summary>
        /// <param name="pListMailbox">The search string possibly including IMAP wildcards.</param>
        /// <param name="pDelimiter">The hierarchy delimiter used in <paramref name="pListMailbox"/>.</param>
        /// <param name="pHasSubscribedChildren">Specifies if mailboxes that are not themselves subscribed, but that have subscribed children, are included in the returned list.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <inheritdoc cref="Mailboxes(string, char?, fMailboxCacheDataSets)" select="returns|remarks"/>
        public List<cMailbox> GetSubscribed(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets)
        {
            var lContext = RootContext.NewMethod(nameof(cIMAPClient), nameof(GetSubscribed));
            var lTask = ZGetSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets a list subscribed mailboxes using an IMAP wildcard search.
        /// </summary>
        /// <param name="pListMailbox">The search string possibly including IMAP wildcards.</param>
        /// <param name="pDelimiter">The hierarchy delimiter used in <paramref name="pListMailbox"/>.</param>
        /// <param name="pHasSubscribedChildren">Specifies if mailboxes that are not themselves subscribed, but that have subscribed children, are included in the returned list.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <inheritdoc cref="Mailboxes(string, char?, fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<List<cMailbox>> GetSubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets)
        {
            var lContext = RootContext.NewMethod(nameof(cIMAPClient), nameof(GetSubscribedAsync));
            return ZGetSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZGetSubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetSubscribedAsync), pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (string.IsNullOrEmpty(pListMailbox)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            cMailboxPathPattern lPattern = new cMailboxPathPattern(string.Empty, cStrings.Empty, pListMailbox, pDelimiter);

            return ZZGetSubscribedAsync(lSession, pListMailbox, pDelimiter, lPattern, pHasSubscribedChildren, pDataSets, lContext);
        }

        internal Task<List<cMailbox>> GetSubscribedAsync(iMailboxHandle pMailboxHandle, bool pDescend, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetSubscribedAsync), pMailboxHandle, pDescend, pDataSets);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMailboxHandle.MailboxName.Delimiter == null) return Task.FromResult(new List<cMailbox>());

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            string lWildcard;
            if (pDescend) lWildcard = "*";
            else lWildcard = "%";

            string lListMailbox = pMailboxHandle.MailboxName.Path.Replace('*', '%') + pMailboxHandle.MailboxName.Delimiter + lWildcard;
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pMailboxHandle.MailboxName.GetDescendantPathPrefix(), cStrings.Empty, lWildcard, pMailboxHandle.MailboxName.Delimiter);

            return ZZGetSubscribedAsync(lSession, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, !pDescend, pDataSets, lContext);
        }

        internal Task<List<cMailbox>> GetSubscribedAsync(cNamespaceName pNamespaceName, bool pDescend, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetSubscribedAsync), pNamespaceName, pDescend, pDataSets);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pNamespaceName == null) throw new ArgumentNullException(nameof(pNamespaceName));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            string lWildcard;
            if (pDescend) lWildcard = "*";
            else lWildcard = "%";

            string lListMailbox = pNamespaceName.Prefix.Replace('*', '%') + lWildcard;
            var lNotPrefixedWith = ZGetNotPrefixedWith(lSession, pNamespaceName.Prefix);
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pNamespaceName.Prefix, lNotPrefixedWith, lWildcard, pNamespaceName.Delimiter);

            return ZZGetSubscribedAsync(lSession, lListMailbox, pNamespaceName.Delimiter, lPattern, !pDescend, pDataSets, lContext);
        }

        // common processing

        private async Task<List<cMailbox>> ZZGetSubscribedAsync(cSession pSession, string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZGetSubscribedAsync), pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, pDataSets);

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

            List<iMailboxHandle> lMailboxHandles;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);

                var lCapabilities = pSession.Capabilities;
                bool lList = (pDataSets & fMailboxCacheDataSets.list) != 0;
                bool lStatus = (pDataSets & fMailboxCacheDataSets.status) != 0;
                bool lListStatus = lStatus && lCapabilities.ListStatus;

                if (lCapabilities.ListExtended && (lList || mMailboxReferrals || lListStatus))
                {
                    eListExtendedSelect lSelect;
                    if (pHasSubscribedChildren) lSelect = eListExtendedSelect.subscribedrecursive;
                    else lSelect = eListExtendedSelect.subscribed;

                    lMailboxHandles = await pSession.ListExtendedAsync(lMC, lSelect, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lListStatus, lContext).ConfigureAwait(false);

                    if (lStatus && !lListStatus) await ZRequestMailboxStatusDataAsync(lMC, pSession, lMailboxHandles, lContext).ConfigureAwait(false);
                }
                else
                {
                    Task<List<iMailboxHandle>> lLSubTask;
                    if (mMailboxReferrals && lCapabilities.MailboxReferrals) lLSubTask = pSession.RLSubAsync(lMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, lContext);
                    else lLSubTask = pSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, lContext);

                    Task lListTask;

                    if (lList)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lListTask = pSession.RListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                        else lListTask = pSession.ListMailboxesAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    }
                    else lListTask = null;

                    lMailboxHandles = await lLSubTask.ConfigureAwait(false);

                    if (lStatus) await ZRequestMailboxStatusDataAsync(lMC, pSession, lMailboxHandles, lContext).ConfigureAwait(false);

                    if (lListTask != null) await lListTask.ConfigureAwait(false);
                }
            }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lMailboxHandle in lMailboxHandles) lMailboxes.Add(new cMailbox(this, lMailboxHandle));
            return lMailboxes;
        }
    }
}