using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task RequestMailboxDataAsync(iMailboxHandle pMailboxHandle, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(RequestMailboxDataAsync), pMailboxHandle, pDataSets);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            if (pMailboxHandle.MailboxName == null) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
            if (pDataSets == 0) return;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);

                if (pDataSets == fMailboxCacheDataSets.status)
                {
                    await lSession.StatusAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                    return;
                }

                string lListMailbox = pMailboxHandle.MailboxName.Path.Replace('*', '%');
                cMailboxPathPattern lPattern = new cMailboxPathPattern(pMailboxHandle.MailboxName.Path, cStrings.Empty, string.Empty, pMailboxHandle.MailboxName.Delimiter);

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

        private Task ZRequestMailboxStatusDataAsync(cMethodControl pMC, cSession pSession, IEnumerable<iMailboxHandle> pMailboxHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZRequestMailboxStatusDataAsync), pMC);
            return Task.WhenAll(from lMailboxHandle in pMailboxHandles where lMailboxHandle.ListFlags?.CanSelect == true select pSession.StatusAsync(pMC, lMailboxHandle, lContext));
        }
    }
}