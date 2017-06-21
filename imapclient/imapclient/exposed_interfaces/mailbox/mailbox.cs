using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cMailbox
    {
        private PropertyChangedEventHandler mPropertyChanged;
        private object mPropertyChangedLock = new object();

        private EventHandler<cMessageDeliveryEventArgs> mMessageDelivery;
        private object mMessageDeliveryLock = new object();

        public readonly cIMAPClient Client;
        public readonly cMailboxId MailboxId;

        public cMailbox(cIMAPClient pClient, cMailboxId pMailboxId)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MailboxId = pMailboxId ?? throw new ArgumentNullException(nameof(pMailboxId));
        }

        // events

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                lock (mPropertyChangedLock)
                {
                    if (mPropertyChanged == null) Client.MailboxPropertyChanged += ZMailboxPropertyChanged;
                    mPropertyChanged += value;
                }
            }

            remove
            {
                lock (mPropertyChangedLock)
                {
                    mPropertyChanged -= value;
                    if (mPropertyChanged == null) Client.MailboxPropertyChanged -= ZMailboxPropertyChanged;
                }
            }
        }

        private void ZMailboxPropertyChanged(object pSender, cMailboxPropertyChangedEventArgs pArgs)
        {
            if (pArgs.MailboxId == MailboxId) mPropertyChanged?.Invoke(this, pArgs);
        }

        public event EventHandler<cMessageDeliveryEventArgs> MessageDelivery
        {
            add
            {
                lock (mMessageDeliveryLock)
                {
                    if (mMessageDelivery == null) Client.MailboxMessageDelivery += ZMailboxMessageDelivery;
                    mMessageDelivery += value;
                }
            }

            remove
            {
                lock (mMessageDeliveryLock)
                {
                    mMessageDelivery -= value;
                    if (mMessageDelivery == null) Client.MailboxMessageDelivery -= ZMailboxMessageDelivery;
                }
            }
        }

        private void ZMailboxMessageDelivery(object pSender, cMailboxMessageDeliveryEventArgs pArgs)
        {
            if (pArgs.MailboxId == MailboxId) mMessageDelivery?.Invoke(this, pArgs);
        }

        // selected
        public bool Selected => Client.GetMailboxProperties(MailboxId)?.Selected ?? false;

        // convenience methods
        public string Name => MailboxId.MailboxName.Name;

        // get data
        public cMailboxStatus Status(fStatusAttributes pAttributes = fStatusAttributes.clientdefault) => Client.Status(MailboxId, pAttributes);
        public Task<cMailboxStatus> StatusAsync(fStatusAttributes pAttributes = fStatusAttributes.clientdefault) => Client.StatusAsync(MailboxId, pAttributes);
        public List<cMailboxListItem> List(fListTypes pTypes = fListTypes.clientdefault, fListFlags pListFlags = fListFlags.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.List(MailboxId, pTypes, pListFlags, pStatusAttributes);
        public Task<List<cMailboxListItem>> ListAsync(fListTypes pTypes = fListTypes.clientdefault, fListFlags pListFlags = fListFlags.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.ListAsync(MailboxId, pTypes, pListFlags, pStatusAttributes);
        public List<cMessage> Search(cFilter pFilter = null, cSort pSort = null, fMessageProperties pProperties = fMessageProperties.clientdefault) => Client.Search(MailboxId, pFilter, pSort, pProperties);
        public Task<List<cMessage>> SearchAsync(cFilter pFilter = null, cSort pSort = null, fMessageProperties pProperties = fMessageProperties.clientdefault) => Client.SearchAsync(MailboxId, pFilter, pSort, pProperties);
        public void Fetch(IList<cMessage> pMessages, fMessageProperties pProperties) => Client.Fetch(MailboxId, ZHandles(pMessages), pProperties);
        public Task FetchAsync(IList<cMessage> pMessages, fMessageProperties pProperties) => Client.FetchAsync(MailboxId, ZHandles(pMessages), pProperties);
        public cMessage UIDFetch(cUID pUID, fMessageProperties pProperties) => Client.UIDFetch(MailboxId, pUID, pProperties);
        public Task<cMessage> UIDFetchAsync(cUID pUID, fMessageProperties pProperties) => Client.UIDFetchAsync(MailboxId, pUID, pProperties);
        public List<cMessage> UIDFetch(IList<cUID> pUIDs, fMessageProperties pProperties) => Client.UIDFetch(MailboxId, pUIDs, pProperties);
        public Task<List<cMessage>> UIDFetchAsync(IList<cUID> pUIDs, fMessageProperties pProperties) => Client.UIDFetchAsync(MailboxId, pUIDs, pProperties);

        private List<iMessageHandle> ZHandles(IList<cMessage> pMessages)
        {
            List<iMessageHandle> lHandles = new List<iMessageHandle>();

            foreach (var lMessage in pMessages)
            {
                if (!ReferenceEquals(lMessage.Client, Client) || lMessage.MailboxId != MailboxId) throw new ArgumentOutOfRangeException(nameof(pMessages));
                lHandles.Add(lMessage.Handle);
            }

            return lHandles;
        }

        // cached data
        public iMailboxProperties Properties => Client.GetMailboxProperties(MailboxId); // only works if the mailbox is selected (and in the future notified)

        // mailbox actions
        public void Select(bool pForUpdate = false) => Client.Select(MailboxId, pForUpdate);
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(MailboxId, pForUpdate);

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({MailboxId})";
    }
}