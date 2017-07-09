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

        ;?; // zero is not valid
        public fMailboxProperties DefaultMailboxProperties { get; set; }

        // manual list

        public List<cMailbox> Mailboxes(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxProperties )
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pListMailbox, pDelimiter, pTypes, pFlagSets, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, bool? pStatus = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pListMailbox, pDelimiter, pTypes, pFlagSets, pStatus, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pListMailbox, pDelimiter, pTypes, pFlagSets, pStatus);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));

            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);

            return ZZMailboxesAsync(lSession, pListMailbox, pDelimiter, lPattern, pTypes, pFlagSets, pStatus, lContext);
        }

        // mailbox sub-mailbox list

        public List<cMailbox> Mailboxes(cMailboxId pMailboxId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pMailboxId, pTypes, pFlagSets, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(cMailboxId pMailboxId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pMailboxId, pTypes, pFlagSets, pStatus, lContext);
        }

        private async Task<List<cMailbox>> ZMailboxesAsync(cMailboxId pMailboxId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pMailboxId);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxId.MailboxName.Delimiter == null) return new List<cMailbox>();

            if (lSession.ConnectedAccountId != pMailboxId.AccountId) throw new cAccountNotConnectedException(lContext);

            string lListMailbox = pMailboxId.MailboxName.Name.Replace('*', '%') + pMailboxId.MailboxName.Delimiter + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pMailboxId.MailboxName.Name + pMailboxId.MailboxName.Delimiter, "%", pMailboxId.MailboxName.Delimiter);

            return await ZZMailboxesAsync(lSession, lListMailbox, pMailboxId.MailboxName.Delimiter, lPattern, pTypes, pFlagSets, pStatus, lContext).ConfigureAwait(false);
        }

        // namespace sub-mailbox list

        public List<cMailbox> Mailboxes(cNamespaceId pNamespaceId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pNamespaceId, pTypes, pFlagSets, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(cNamespaceId pNamespaceId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pNamespaceId, pTypes, pFlagSets, pStatus, lContext);
        }

        private async Task<List<cMailbox>> ZMailboxesAsync(cNamespaceId pNamespaceId, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pNamespaceId);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pNamespaceId == null) throw new ArgumentNullException(nameof(pNamespaceId));

            if (lSession.ConnectedAccountId != pNamespaceId.AccountId) throw new cAccountNotConnectedException(lContext);

            string lListMailbox = pNamespaceId.NamespaceName.Prefix.Replace('*', '%') + "%";
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pNamespaceId.NamespaceName.Prefix, "%", pNamespaceId.NamespaceName.Delimiter);

            return await ZZMailboxesAsync(lSession, lListMailbox, pNamespaceId.NamespaceName.Delimiter, lPattern, pTypes, pFlagSets, pStatus, lContext).ConfigureAwait(false);
        }

        // common processing

        private async Task<List<cMailbox>> ZZMailboxesAsync(cSession pSession, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, fMailboxTypes pTypes, fMailboxFlagSets pFlagSets, bool? pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesAsync), pListMailbox, pDelimiter, pPattern, pTypes, pFlagSets, pStatus);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            fMailboxTypes lTypes = pTypes & fMailboxTypes.all;
            if ((pTypes & fMailboxTypes.clientdefault) != 0) lTypes |= mDefaultMailboxTypes;
            if (lTypes == 0) throw new ArgumentOutOfRangeException(nameof(pTypes));

            fMailboxFlagSets lFlagSets = pFlagSets & fMailboxFlagSets.all;
            if ((pFlagSets & fMailboxFlagSets.clientdefault) != 0) lFlagSets |= mDefaultMailboxFlagSets;
            // zero is valid

            bool lStatus = pStatus ?? DefaultMailboxStatus;

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                // ;?; // note this is where we do the stuff to honour the flags
                // including choosing list/rlist/lsub etc
                // TODO!

                // if using list-extended, always ask for specialuse attributes if specialuse is advertised 

                // also note that while we may have to do a status command for each mailbox; non-selectable mailboxes do not have a status
                //  list-extended won't return status for non-selectable mailboxes and may not return it for any mailbox

                // must check the list-extended result to see if status was returned for each item: we may need to patch up the status
                //  NO: not now :- now if the status wasn't returned accessing it through the mailbox will get it

                if (lTypes == (fMailboxTypes.normal | fMailboxTypes.subscribed) && (lFlagSets & ~(fMailboxFlagSets.rfc3501)) == 0)
                {
                    var lList = await pSession.ListAsync(lMC, new cListPattern(pListMailbox, pDelimiter, pPattern), lContext).ConfigureAwait(false);

                    List<cMailbox> lResult = new List<cMailbox>();

                    foreach (var lItem in lList)
                    {
                        lResult.Add(new cMailbox(this, new cMailboxId(pSession.ConnectedAccountId, lItem.MailboxName)));

                        ;?; // cache the result

                        ;?; // note that multiple lists at one time may be running. which one ends up in the cache?
                    }

                    return lResult;
                }

                throw new NotImplementedException();
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}