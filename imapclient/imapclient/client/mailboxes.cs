using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        // defaults

        private fMailboxTypes mDefaultMailboxTypes = fMailboxTypes.normal | fMailboxTypes.subscribed;
        private fMailboxFlagSets mDefaultMailboxFlagSets = fMailboxFlagSets.rfc3501;
        private fStatusAttributes mDefaultMailboxStatusAttributes = fStatusAttributes.none;

        public fMailboxTypes DefaultMailboxTypes
        {
            get => mDefaultMailboxTypes;

            set
            {
                if ((value & fMailboxTypes.all) == 0) throw new ArgumentOutOfRangeException(); // must have something returned
                if ((value & fMailboxTypes.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mDefaultMailboxTypes = value;
            }
        }

        public fMailboxFlagSets DefaultMailboxFlagSets
        {
            get => mDefaultMailboxFlagSets;

            set
            {
                // having nothing set is a valid option - this allows LSUB to be used by itself
                if ((value & fMailboxFlagSets.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mDefaultMailboxFlagSets = value;
            }
        }

        public fStatusAttributes DefaultMailboxStatusAttributes
        {
            get => mDefaultMailboxStatusAttributes;

            set
            {
                // having nothing set is a valid option - this means status information is not returned
                if ((value & fStatusAttributes.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mDefaultMailboxStatusAttributes = value;
            }
        }

        // manual list

        public List<cMailboxListItem> Mailboxes(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pListMailbox, pDelimiter, pTypes, pFlagSets, pStatusAttributes, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailboxListItem>> MailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pListMailbox, pDelimiter, pTypes, pFlagSets, pStatusAttributes, lContext);
        }

        private Task<List<cMailboxListItem>> ZMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pListMailbox, pDelimiter, pTypes, pFlagSets, pStatusAttributes);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));

            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);

            return ZZMailboxesAsync(lSession, pListMailbox, pDelimiter, lPattern, pTypes, pFlagSets, pStatusAttributes, lContext);
        }

        // mailbox sub-mailbox list

        public List<cMailboxListItem> Mailboxes(cMailboxId pMailboxId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pMailboxId, pTypes, pFlagSets, pStatusAttributes, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailboxListItem>> MailboxesAsync(cMailboxId pMailboxId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pMailboxId, pTypes, pFlagSets, pStatusAttributes, lContext);
        }

        private async Task<List<cMailboxListItem>> ZMailboxesAsync(cMailboxId pMailboxId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pMailboxId);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxId.MailboxName.Delimiter == null) return new List<cMailboxListItem>();

            if (lSession.ConnectedAccountId != pMailboxId.AccountId) throw new cAccountNotConnectedException(lContext);

            string lListMailbox = pMailboxId.MailboxName.Name.Replace('*', '%') + pMailboxId.MailboxName.Delimiter + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pMailboxId.MailboxName.Name + pMailboxId.MailboxName.Delimiter, "%", pMailboxId.MailboxName.Delimiter);

            return await ZZMailboxesAsync(lSession, lListMailbox, pMailboxId.MailboxName.Delimiter, lPattern, pTypes, pFlagSets, pStatusAttributes, lContext).ConfigureAwait(false);
        }

        // namespace sub-mailbox list

        public List<cMailboxListItem> Mailboxes(cNamespaceId pNamespaceId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pNamespaceId, pTypes, pFlagSets, pStatusAttributes, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailboxListItem>> MailboxesAsync(cNamespaceId pNamespaceId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pNamespaceId, pTypes, pFlagSets, pStatusAttributes, lContext);
        }

        private async Task<List<cMailboxListItem>> ZMailboxesAsync(cNamespaceId pNamespaceId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pNamespaceId);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pNamespaceId == null) throw new ArgumentNullException(nameof(pNamespaceId));

            if (lSession.ConnectedAccountId != pNamespaceId.AccountId) throw new cAccountNotConnectedException(lContext);

            string lListMailbox = pNamespaceId.NamespaceName.Prefix.Replace('*', '%') + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pNamespaceId.NamespaceName.Prefix, "%", pNamespaceId.NamespaceName.Delimiter);

            return await ZZMailboxesAsync(lSession, lListMailbox, pNamespaceId.NamespaceName.Delimiter, lPattern, pTypes, pFlagSets, pStatusAttributes, lContext).ConfigureAwait(false);
        }

        // common processing

        private async Task<List<cMailboxListItem>> ZZMailboxesAsync(cSession pSession, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesAsync), pListMailbox, pDelimiter, pPattern, pTypes, pFlagSets, pStatusAttributes);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            fMailboxTypes lTypes = pTypes & fMailboxTypes.all;
            if ((pTypes & fMailboxTypes.clientdefault) != 0) lTypes |= mDefaultMailboxTypes;
            if (lTypes == 0) throw new ArgumentOutOfRangeException(nameof(pTypes));

            fMailboxFlagSets lFlagSets = pFlagSets & fMailboxFlagSets.all;
            if ((pFlagSets & fMailboxFlagSets.clientdefault) != 0) lFlagSets |= mDefaultMailboxFlagSets;
            // zero is valid

            fStatusAttributes lStatusAttributes = pStatusAttributes & fStatusAttributes.all;
            if ((pStatusAttributes & fStatusAttributes.clientdefault) != 0) lStatusAttributes |= mDefaultMailboxStatusAttributes;
            // zero is valid

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                // ;?; // note this is where we do the stuff to honour the flags
                // including choosing list/rlist/lsub etc
                // TODO!

                // if using list-extended, always ask for specialuse attributes if specialuse is advertised 


                // also note that while we may have to do a status command for each mailbox; non-selectable mailboxes do not have a status
                //  list-extended won't return status for non-selectable mailboxes and may not return it for any mailbox (so I might have to do it manually)


                // must check the list-extended result to see if status was returned for each item: we may need to patch up the status

                if (lTypes == (fMailboxTypes.normal | fMailboxTypes.subscribed) && (lFlagSets & ~(fMailboxFlagSets.rfc3501)) == 0)
                {
                    var lList = await pSession.ListAsync(lMC, new cListPattern(pListMailbox, pDelimiter, pPattern), lContext).ConfigureAwait(false);
                    List<cMailboxListItem> lResult = new List<cMailboxListItem>();
                    foreach (var lItem in lList) lResult.Add(new cMailboxListItem(new cMailbox(this, new cMailboxId(pSession.ConnectedAccountId, lItem.MailboxName)), lItem.Flags, lItem.Status));
                    return lResult;
                }

                throw new NotImplementedException();
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}