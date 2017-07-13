using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private fMailboxTypes mDefaultMailboxTypes = 0;
        private fMailboxProperties mDefaultMailboxProperties = fMailboxProperties.canhavechildren | fMailboxProperties.canselect | fMailboxProperties.ismarked | fMailboxProperties.islocal; // these are the ones that are rfc3501 list command properties

        public fMailboxTypes DefaultMailboxTypes
        {
            get => mDefaultMailboxTypes;

            set
            {
                if ((value & fMailboxTypes.clientdefault) != 0) throw new ArgumentOutOfRangeException();
                mDefaultMailboxTypes = value;
            }
        }

        public fMailboxProperties DefaultMailboxProperties
        {
            get => mDefaultMailboxProperties;

            set
            {
                if ((value & fMailboxProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException();
                mDefaultMailboxProperties = value;
            }
        }

        // manual list

        public List<cMailbox> Mailboxes(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxProperties pProperties = fMailboxProperties.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Mailboxes));
            var lTask = ZMailboxesAsync(pListMailbox, pDelimiter, pTypes, pProperties, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMailbox>> MailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxProperties pProperties = fMailboxProperties.clientdefault)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MailboxesAsync));
            return ZMailboxesAsync(pListMailbox, pDelimiter, pTypes, pProperties, lContext);
        }

        private Task<List<cMailbox>> ZMailboxesAsync(string pListMailbox, char? pDelimiter, fMailboxTypes pTypes, fMailboxProperties pProperties, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMailboxesAsync), pListMailbox, pDelimiter, pTypes, pProperties);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));

            cMailboxNamePattern lPattern = new cMailboxNamePattern(string.Empty, pListMailbox, pDelimiter);

            return ZZMailboxesAsync(lSession, pListMailbox, pDelimiter, lPattern, pTypes, pProperties, lContext);
        }

        // mailbox sub-mailbox list

            ;?;

        public List<cMailbox> Mailboxes(cMailboxId pMailboxId, bool pStatus)
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

        private async Task<List<cMailbox>> ZZMailboxesAsync(cSession pSession, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, fMailboxTypes pTypes, fMailboxProperties pProperties, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMailboxesAsync), pListMailbox, pDelimiter, pPattern, pTypes, pProperties);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lCapability = pSession.Capability;

            fMailboxTypes lTypes = pTypes;
            if ((pTypes & fMailboxTypes.clientdefault) != 0) lTypes |= mDefaultMailboxTypes;

            if (!lCapability.MailboxReferrals) lTypes = lTypes & ~fMailboxTypes.remote;

            fMailboxProperties lProperties = pProperties;
            if ((pProperties & fMailboxProperties.clientdefault) != 0) lProperties |= mDefaultMailboxProperties;
            lProperties = SupportedMailboxProperties(lCapability, lProperties);

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