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

        private Task<cMailbox> ZMailboxAsync(cMailboxName pMailboxName, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxAsync), pMailboxName, pProperties);

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            string lListMailbox = pMailboxName.Name.Replace('*', '%');
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pMailboxName.Name, string.Empty, pMailboxName.Delimiter);

            ;?; // has to get the result then check it and return null if 0, a value if 1 and throw if multiple
            return ZZMailboxesAsync(lListMailbox, pMailboxName.Delimiter, lPattern, pStatus, lContext);
        }




        // manual list

        public List<cMailbox> Mailboxes(string pListMailbox, char? pDelimiter, fMailboxProperties pProperties = fMailboxProperties.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pListMailbox, pDelimiter, pProperties, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxProperties pProperties = fMailboxProperties.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pListMailbox, pDelimiter, pProperties, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxProperties pProperties, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pListMailbox, pDelimiter, pProperties);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));

            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);

            return ZZMailboxesAsync(lSession, pListMailbox, pDelimiter, lPattern, pProperties, lContext);
        }

        // mailbox 

        ;?; // (refresh/ inquire for the first time)


        // mailbox sub-mailbox list

            ;?;

        public List<cMailbox> Mailboxes(cMailboxId pMailboxId, fmail pStatus)
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

        private async Task<List<cMailbox>> ZZMailboxesAsync(string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesAsync), pListMailbox, pDelimiter, pPattern, pStatus);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
            if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

            var lCapability = lSession.Capability;
            List<iMailboxHandle> lHandles;

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                if (lCapability.ListExtended)
                {
                    bool lStatus = pStatus && lCapability.ListStatus;

                    lHandles = await lSession.ListExtendedAsync(lMC, false, mMailboxReferrals, pListMailbox, pDelimiter, pPattern, lStatus, lContext).ConfigureAwait(false);

                    if (pStatus && !lStatus) lSession.StatusAsync()
    
                }
                else
                {
                    Task lListTask;
                    if (mMailboxReferrals) lListTask = lSession.RListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);
                    else lListTask = lSession.ListAsync(lMC, pListMailbox, pDelimiter, pPattern, lContext);

                    Task lLSubTask;

                    if ((pProperties & cLSubFlags.Mask) == 0) lLSubTask = null;
                    else if (mMailboxReferrals) lLSubTask = 
                    {
                        ;?; // do an r/lsub
                    }

                    //  if the lsub task is something, zero (not null) the non-refreshed ones
                }

                ;?; // now do status if required (common with lsub)
            }
            finally { mAsyncCounter.Decrement(lContext); }

            // now extract the matching entries from the mailbox cache
            ;?;












            if ()

            if 



            bool lLSubOnly;

            if ((lTypes & (fMailboxTypes.subscribedonly | fMailboxTypes.remote)) == fMailboxTypes.subscribedonly)
            {
                if ((lProperties & (fMailboxProperties.list | fMailboxProperties.lsub | fMailboxProperties.specialuse) & ~fMailboxProperties.lsub) == 0) lLSubOnly = true;
                else lLSubOnly = false;
            }
            else lLSubOnly = false;

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);

                if (!lLSubOnly && lCapability.ListExtended)
                {
                    // if using list-extended, always ask for specialuse attributes if specialuse is advertised 
                    ;?;

                    


                }
                else
                {
                    if ((lTypes & fMailboxTypes.remote) == 0)
                    {
                        if ((lTypes & fMailboxTypes.subscribedonly) == 0)
                        {
                            // start a list: ...
                            ;?;

                            if ((lProperties & (fMailboxProperties.issubscribed | fMailboxProperties.hassubscribedchildren)) != 0)
                            {
                                // start an lsub, this will set the above two attributes
                                ;?;
                            }
                        }
                        else
                        {
                            // start an lsub: this ...
                            ;?;

                            var lListProperties = lProperties & fMailboxProperties.list;

                            if (lListProperties != 0 && lListProperties != fMailboxProperties.islocal)
                            {
                                // start a list
                                ;?;
                            }
                        }
                    }
                    else
                    {
                        if ((lTypes & fMailboxTypes.subscribedonly) == 0)
                        {
                            // start an rlist: this returns ...
                            ;?;

                            if ((lProperties & (fMailboxProperties.issubscribed | fMailboxProperties.hassubscribedchildren)) != 0)
                            {
                                // start an rlsub, this will set the above two attributes
                                ;?;
                            }

                            if ((lProperties & fMailboxProperties.islocal) != 0)
                            {
                                // start a list
                                ;?;
                            }
                        }
                        else
                        {
                            // start rlsub: this returns the list of mailboxes that we are going to return
                            ;?;

                            var lListProperties = lProperties & fMailboxProperties.list;

                            if (lListProperties != 0)
                            {
                                if (lListProperties == fMailboxProperties.islocal)
                                {
                                    ;?; // do an lsub
                                }
                                else
                                {
                                    ;?; // do a list
                                }
                            }
                        }
                    }

                    ;?; // now do a status for each one if required (note that non-selectable mailboxes DONT do it)
                }

                ;?; // now return the list of amilboxes
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}