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

        private fListTypes mListDefaultTypes = fListTypes.normal | fListTypes.subscribed;
        private fListFlags mListDefaultListFlags = fListFlags.rfc3501;
        private fStatusAttributes mListDefaultStatusAttributes = fStatusAttributes.none;

        public fListTypes ListDefaultTypes
        {
            get => mListDefaultTypes;

            set
            {
                if ((value & fListTypes.all) == 0) throw new ArgumentOutOfRangeException(); // must have something returned
                if ((value & fListTypes.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mListDefaultTypes = value;
            }
        }

        public fListFlags ListDefaultListFlags
        {
            get => mListDefaultListFlags;

            set
            {
                // having nothing set is a valid option - this allows LSUB to be used by itself
                if ((value & fListFlags.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mListDefaultListFlags = value;
            }
        }

        public fStatusAttributes ListDefaultStatusAttributes
        {
            get => mListDefaultStatusAttributes;

            set
            {
                // having nothing set is a valid option - this means status information is not returned
                if ((value & fStatusAttributes.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mListDefaultStatusAttributes = value;
            }
        }

        // manual list

        public List<cMailboxListItem> List(string pListMailbox, char? pDelimiter, fListTypes pTypes = fListTypes.clientdefault, fListFlags pListFlags = fListFlags.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(List));
            var lTask = ZListAsync(pListMailbox, pDelimiter, pTypes, pListFlags, pStatusAttributes, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailboxListItem>> ListAsync(string pListMailbox, char? pDelimiter, fListTypes pTypes = fListTypes.clientdefault, fListFlags pListFlags = fListFlags.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ListAsync));
            return ZListAsync(pListMailbox, pDelimiter, pTypes, pListFlags, pStatusAttributes, lContext);
        }

        private Task<List<cMailboxListItem>> ZListAsync(string pListMailbox, char? pDelimiter, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZListAsync), pListMailbox, pDelimiter, pTypes, pListFlags, pStatusAttributes);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));

            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);

            return ZZListAsync(lSession, pListMailbox, pDelimiter, lPattern, pTypes, pListFlags, pStatusAttributes, lContext);
        }

        // mailbox sub-mailbox list

        public List<cMailboxListItem> List(cMailboxId pMailboxId, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(List));
            var lTask = ZListAsync(pMailboxId, pTypes, pListFlags, pStatusAttributes, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailboxListItem>> ListAsync(cMailboxId pMailboxId, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ListAsync));
            return ZListAsync(pMailboxId, pTypes, pListFlags, pStatusAttributes, lContext);
        }

        private async Task<List<cMailboxListItem>> ZListAsync(cMailboxId pMailboxId, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZListAsync), pMailboxId);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxId.MailboxName.Delimiter == null) return new List<cMailboxListItem>();

            if (lSession.ConnectedAccountId != pMailboxId.AccountId) throw new cAccountNotConnectedException(lContext);

            string lListMailbox = pMailboxId.MailboxName.Name.Replace('*', '%') + pMailboxId.MailboxName.Delimiter + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pMailboxId.MailboxName.Name + pMailboxId.MailboxName.Delimiter, "%", pMailboxId.MailboxName.Delimiter);

            return await ZZListAsync(lSession, lListMailbox, pMailboxId.MailboxName.Delimiter, lPattern, pTypes, pListFlags, pStatusAttributes, lContext).ConfigureAwait(false);
        }

        // namespace sub-mailbox list

        public List<cMailboxListItem> List(cNamespaceId pNamespaceId, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(List));
            var lTask = ZListAsync(pNamespaceId, pTypes, pListFlags, pStatusAttributes, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailboxListItem>> ListAsync(cNamespaceId pNamespaceId, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ListAsync));
            return ZListAsync(pNamespaceId, pTypes, pListFlags, pStatusAttributes, lContext);
        }

        private async Task<List<cMailboxListItem>> ZListAsync(cNamespaceId pNamespaceId, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZListAsync), pNamespaceId);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pNamespaceId == null) throw new ArgumentNullException(nameof(pNamespaceId));

            if (lSession.ConnectedAccountId != pNamespaceId.AccountId) throw new cAccountNotConnectedException(lContext);

            string lListMailbox = pNamespaceId.NamespaceName.Prefix.Replace('*', '%') + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pNamespaceId.NamespaceName.Prefix, "%", pNamespaceId.NamespaceName.Delimiter);

            return await ZZListAsync(lSession, lListMailbox, pNamespaceId.NamespaceName.Delimiter, lPattern, pTypes, pListFlags, pStatusAttributes, lContext).ConfigureAwait(false);
        }

        // common processing

        private async Task<List<cMailboxListItem>> ZZListAsync(cSession pSession, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, fListTypes pTypes, fListFlags pListFlags, fStatusAttributes pStatusAttributes, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZListAsync), pListMailbox, pDelimiter, pPattern, pTypes, pListFlags, pStatusAttributes);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            fListTypes lTypes = pTypes & fListTypes.all;
            if ((pTypes & fListTypes.clientdefault) != 0) lTypes |= mListDefaultTypes;
            if (lTypes == 0) throw new ArgumentOutOfRangeException(nameof(pTypes));

            fListFlags lListFlags = pListFlags & fListFlags.all;
            if ((pListFlags & fListFlags.clientdefault) != 0) lListFlags |= mListDefaultListFlags;
            // zero is valid

            fStatusAttributes lStatusAttributes = pStatusAttributes & fStatusAttributes.all;
            if ((pStatusAttributes & fStatusAttributes.clientdefault) != 0) lStatusAttributes |= mListDefaultStatusAttributes;
            // zero is valid

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);

                // ;?; // note this is where we do the stuff to honour the flags
                // including choosing list/rlist/lsub etc
                // TODO!

                // if using list-extended, always ask for specialuse attributes if specialuse is advertised 


                // also note that while we may have to do a status command for each mailbox; non-selectable mailboxes do not have a status
                //  list-extended won't return status for non-selectable mailboxes and may not return it for any mailbox (so I might have to do it manually)


                // must check the list-extended result to see if status was returned for each item: we may need to patch up the status

                if (lTypes == (fListTypes.normal | fListTypes.subscribed) && (lListFlags & ~(fListFlags.rfc3501)) == 0)
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