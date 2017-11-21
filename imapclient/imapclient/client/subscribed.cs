﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

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
        public List<cMailbox> Subscribed(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets, lContext);
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
        public Task<List<cMailbox>> SubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribedAsync));
            return ZSubscribedAsync(pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZSubscribedAsync(string pListMailbox, char? pDelimiter, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribedAsync), pListMailbox, pDelimiter, pHasSubscribedChildren, pDataSets);
            if (string.IsNullOrEmpty(pListMailbox)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));
            cMailboxPathPattern lPattern = new cMailboxPathPattern(string.Empty, pListMailbox, pDelimiter);
            return ZZSubscribedAsync(pListMailbox, pDelimiter, lPattern, pHasSubscribedChildren, pDataSets, lContext);
        }

        internal List<cMailbox> Subscribed(iMailboxHandle pHandle, bool pDescend, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pHandle, pDescend, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMailbox>> SubscribedAsync(iMailboxHandle pHandle, bool pDescend, fMailboxCacheDataSets pDataSets)
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

            string lListMailbox = pHandle.MailboxName.Path.Replace('*', '%') + pHandle.MailboxName.Delimiter + lWildcard;
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pHandle.MailboxName.Path + pHandle.MailboxName.Delimiter, lWildcard, pHandle.MailboxName.Delimiter);

            return await ZZSubscribedAsync(lListMailbox, pHandle.MailboxName.Delimiter, lPattern, !pDescend, pDataSets, lContext).ConfigureAwait(false);
        }

        internal List<cMailbox> Subscribed(cNamespaceName pNamespaceName, bool pDescend, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribed));
            var lTask = ZSubscribedAsync(pNamespaceName, pDescend, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMailbox>> SubscribedAsync(cNamespaceName pNamespaceName, bool pDescend, fMailboxCacheDataSets pDataSets)
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
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pNamespaceName.Prefix, lWildcard, pNamespaceName.Delimiter);

            return ZZSubscribedAsync(lListMailbox, pNamespaceName.Delimiter, lPattern, !pDescend, pDataSets, lContext);
        }

        // common processing

        private async Task<List<cMailbox>> ZZSubscribedAsync(string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, bool pHasSubscribedChildren, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZSubscribedAsync), pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren, pDataSets);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

            List<iMailboxHandle> lHandles;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);

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

                    if (lStatus && !lListStatus) await ZFetchStatus(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
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
                        else lListTask = lSession.ListMailboxesAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    }
                    else lListTask = null;

                    lHandles = await lLSubTask.ConfigureAwait(false);

                    if (lStatus) await ZFetchStatus(lMC, lSession, lHandles, lContext).ConfigureAwait(false);

                    if (lListTask != null) await lListTask.ConfigureAwait(false);
                }
            }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lHandle in lHandles) lMailboxes.Add(new cMailbox(this, lHandle));
            return lMailboxes;
        }
    }
}