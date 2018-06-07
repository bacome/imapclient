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
        public List<cMailbox> GetMailboxes(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets)
        {
            var lContext = RootContext.NewMethod(nameof(cIMAPClient), nameof(GetMailboxes));
            var lTask = ZGetMailboxesAsync(pListMailbox, pDelimiter, pDataSets, lContext);
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
        public Task<List<cMailbox>> GetMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets)
        {
            var lContext = RootContext.NewMethod(nameof(cIMAPClient), nameof(GetMailboxesAsync));
            return ZGetMailboxesAsync(pListMailbox, pDelimiter, pDataSets, lContext);
        }

        private Task<List<cMailbox>> ZGetMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetMailboxesAsync), pListMailbox, pDelimiter, pDataSets);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (string.IsNullOrEmpty(pListMailbox)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            cMailboxPathPattern lPattern = new cMailboxPathPattern(string.Empty, cStrings.Empty, pListMailbox, pDelimiter);

            return ZZGetMailboxesAsync(lSession, pListMailbox, pDelimiter, lPattern, pDataSets, lContext);
        }

        internal async Task<List<cMailbox>> GetMailboxesAsync(iMailboxHandle pMailboxHandle, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetMailboxesAsync), pMailboxHandle, pDataSets);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMailboxHandle.MailboxName.Delimiter == null) return new List<cMailbox>();

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            string lListMailbox = pMailboxHandle.MailboxName.Path.Replace('*', '%') + pMailboxHandle.MailboxName.Delimiter + "%";
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pMailboxHandle.MailboxName.GetDescendantPathPrefix(), cStrings.Empty, "%", pMailboxHandle.MailboxName.Delimiter);

            var lMailboxes = await ZZGetMailboxesAsync(lSession, lListMailbox, pMailboxHandle.MailboxName.Delimiter, lPattern, pDataSets, lContext);

            ZCacheIntegrationReconcile(pMailboxHandle.MailboxId, lMailboxes, lContext);

            return lMailboxes;
        }

        internal async Task<List<cMailbox>> GetMailboxesAsync(cNamespaceName pNamespaceName, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetMailboxesAsync), pNamespaceName, pDataSets);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pNamespaceName == null) throw new ArgumentNullException(nameof(pNamespaceName));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            string lListMailbox = pNamespaceName.Prefix.Replace('*', '%') + "%";
            var lNotPrefixedWith = ZGetNotPrefixedWith(lSession, pNamespaceName.Prefix);
            cMailboxPathPattern lPattern = new cMailboxPathPattern(pNamespaceName.Prefix, lNotPrefixedWith, "%", pNamespaceName.Delimiter);

            var lMailboxes = await ZZGetMailboxesAsync(lSession, lListMailbox, pNamespaceName.Delimiter, lPattern, pDataSets, lContext);

            ZCacheIntegrationReconcile(lSession.ConnectedAccountId, pNamespaceName.Prefix, lNotPrefixedWith, lMailboxes, lContext);

            return lMailboxes;
        }

        // common processing

        private async Task<List<cMailbox>> ZZGetMailboxesAsync(cSession pSession, string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZGetMailboxesAsync), pListMailbox, pDelimiter, pPattern, pDataSets);

            if (pSession == null) throw new ArgumentNullException(nameof(pSession));
            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

            List<iMailboxHandle> lMailboxHandles;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);

                var lCapabilities = pSession.Capabilities;
                bool lLSub = (pDataSets & fMailboxCacheDataSets.lsub) != 0;
                bool lStatus = (pDataSets & fMailboxCacheDataSets.status) != 0;

                Task<List<iMailboxHandle>> lListTask;
                Task lLSubTask;

                if (lCapabilities.ListExtended)
                {
                    bool lListStatus = lStatus && lCapabilities.ListStatus;

                    lListTask = pSession.ListExtendedAsync(lMC, eListExtendedSelect.exists, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lListStatus, lContext);

                    if (lLSub && (mMailboxCacheDataItems & fMailboxCacheDataItems.subscribed) == 0)
                    {
                        if (mMailboxReferrals) lLSubTask = pSession.ListExtendedAsync(lMC, eListExtendedSelect.subscribed, true, pListMailbox, pDelimiter, pPattern, false, lContext);
                        else lLSubTask = pSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                    }
                    else lLSubTask = null;

                    lMailboxHandles = await lListTask.ConfigureAwait(false);

                    if (lStatus && !lListStatus) await ZRequestMailboxStatusDataAsync(lMC, pSession, lMailboxHandles, lContext).ConfigureAwait(false);
                }
                else
                {
                    if (mMailboxReferrals && lCapabilities.MailboxReferrals) lListTask = pSession.RListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    else lListTask = pSession.ListMailboxesAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);

                    if (lLSub)
                    {
                        if (mMailboxReferrals && lCapabilities.MailboxReferrals) lLSubTask = pSession.RLSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                        else lLSubTask = pSession.LSubAsync(lMC, pListMailbox, pDelimiter, pPattern, false, lContext);
                    }
                    else lLSubTask = null;

                    lMailboxHandles = await lListTask.ConfigureAwait(false);

                    if (lStatus) await ZRequestMailboxStatusDataAsync(lMC, pSession, lMailboxHandles, lContext).ConfigureAwait(false);
                }

                if (lLSubTask != null) await lLSubTask.ConfigureAwait(false);
            }

            List<cMailbox> lMailboxes = new List<cMailbox>();
            foreach (var lMailboxHandle in lMailboxHandles) lMailboxes.Add(new cMailbox(this, lMailboxHandle));
            return lMailboxes;
        }
    }
}