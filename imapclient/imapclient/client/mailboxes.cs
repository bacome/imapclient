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
        /// Gets a list of mailboxes using an IMAP wildcard search.
        /// </summary>
        /// <param name="pListMailbox">The search string possibly including IMAP wildcards.</param>
        /// <param name="pDelimiter">The hierarchy delimiter used in <paramref name="pListMailbox"/>.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// The IMAP wildcards are;
        /// <list type="bullet">
        /// <item><token>*</token><description>Matches zero or more characters.</description></item>
        /// <item><token>%</token><description>Matches zero or more characters but not the hierarchy delimiter.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <paramref name="pDelimiter"/> is used in preparing <paramref name="pListMailbox"/> for sending to the server.
        /// It should be correctly specified.
        /// The value specified does not affect what character is not matched by the % wildcard.
        /// </para>
        /// </remarks>
        public List<cMailbox> Mailboxes(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pListMailbox, pDelimiter, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets a list of mailboxes using an IMAP wildcard search.
        /// </summary>
        /// <param name="pListMailbox">The search string possibly including IMAP wildcards.</param>
        /// <param name="pDelimiter">The hierarchy delimiter used in <paramref name="pListMailbox"/>.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <inheritdoc cref="Mailboxes(string, char?, fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<List<cMailbox>> MailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pListMailbox, pDelimiter, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pListMailbox, pDelimiter, pDataSets);
            if (string.IsNullOrEmpty(pListMailbox)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));
            cMailboxPathPattern lPattern = new cMailboxPathPattern(string.Empty, pListMailbox, pDelimiter);
            return ZZMailboxesAsync(pListMailbox, pDelimiter, lPattern, pDataSets, lContext);
        }

        internal List<cMailbox> Mailboxes(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pHandle, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMailbox>> MailboxesAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pHandle, pDataSets, lContext);
        }

        private async Task<List<cMailbox>> ZMailboxesAsync(iMailboxHandle pHandle, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pHandle, pDataSets);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pHandle.MailboxName.Delimiter == null) return new List<cMailbox>();

            string lListMailbox = pHandle.MailboxName.Path.Replace('*', '%') + pHandle.MailboxName.Delimiter + "%";
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pHandle.MailboxName.Path + pHandle.MailboxName.Delimiter, "%", pHandle.MailboxName.Delimiter);

            return await ZZMailboxesAsync(lListMailbox, pHandle.MailboxName.Delimiter, lPattern, pDataSets, lContext).ConfigureAwait(false);
        }

        internal List<cMailbox> Mailboxes(cNamespaceName pNamespaceName, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pNamespaceName, pDataSets, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMailbox>> MailboxesAsync(cNamespaceName pNamespaceName, fMailboxCacheDataSets pDataSets)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pNamespaceName, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(cNamespaceName pNamespaceName, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pNamespaceName, pDataSets);

            if (pNamespaceName == null) throw new ArgumentNullException(nameof(pNamespaceName));

            string lListMailbox = pNamespaceName.Prefix.Replace('*', '%') + "%";
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pNamespaceName.Prefix, "%", pNamespaceName.Delimiter);

            return ZZMailboxesAsync(lListMailbox, pNamespaceName.Delimiter, lPattern, pDataSets, lContext);
        }

        // common processing

        private async Task<List<cMailbox>> ZZMailboxesAsync(string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesAsync), pListMailbox, pDelimiter, pPattern, pDataSets);

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
                bool lLSub = (pDataSets & fMailboxCacheDataSets.lsub) != 0;
                bool lStatus = (pDataSets & fMailboxCacheDataSets.status) != 0;

                Task<List<iMailboxHandle>> lListTask;
                Task lLSubTask;

                if (lCapabilities.ListExtended)
                {
                    bool lListStatus = lStatus && lCapabilities.ListStatus;

                    lListTask = lSession.ListExtendedAsync(lMC, eListExtendedSelect.exists, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lListStatus, lContext);

                    if (lLSub && (mMailboxCacheDataItems & fMailboxCacheDataItems.subscribed) == 0)
                    {
                        if (mMailboxReferrals) lLSubTask = lSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, true, pListMailbox, pDelimiter, pPattern, false, lContext);
                        else lLSubTask = lSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                    }
                    else lLSubTask = null;

                    lHandles = await lListTask.ConfigureAwait(false);

                    if (lStatus && !lListStatus) await ZFetchStatus(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
                }
                else
                {
                    if (mMailboxReferrals && lCapabilities.MailboxReferrals) lListTask = lSession.RListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    else lListTask = lSession.ListMailboxesAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);

                    if (lLSub)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lLSubTask = lSession.RLSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                        else lLSubTask = lSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                    }
                    else lLSubTask = null;

                    lHandles = await lListTask.ConfigureAwait(false);

                    if (lStatus) await ZFetchStatus(lMC, lSession, lHandles, lContext).ConfigureAwait(false);
                }

                if (lLSubTask != null) await lLSubTask.ConfigureAwait(false);
            }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lHandle in lHandles) lMailboxes.Add(new cMailbox(this, lHandle));
            return lMailboxes;
        }
    }
}