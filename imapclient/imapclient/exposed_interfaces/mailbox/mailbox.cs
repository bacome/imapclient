using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

        // convenience method
        public string Name => MailboxId.MailboxName.Name;

        // properties

        public cMailboxFlags MailboxFlags(fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault)
        {
            var lItem = Client.MailboxCacheItem(MailboxId);
            // if the flagsets are all there, return the cached flags
            // else do a list
            // return the cached flags
        }

        public bool CanHaveChildren => MailboxFlags(fMailboxFlagSets.rfc3501).CanHaveChildren;
        public bool? HasChildren => MailboxFlags(fMailboxFlagSets.children).HasChildren;
        public bool CanSelect => Flags.CanSelect;
        public bool? IsMarked => Flags.IsMarked;
        public bool? IsSubscribed => Flags.IsSubscribed;
        public bool? HasSubscribedChildren => Flags.HasSubscribedChildren;
        public bool? IsRemote => Flags.IsRemote;
        public bool ContainsAll => Flags.ContainsAll;
        public bool ContainsArchived => Flags.ContainsArchived;
        public bool ContainsDrafts => Flags.ContainsDrafts;
        public bool ContainsFlagged => Flags.ContainsFlagged;
        public bool ContainsJunk => Flags.ContainsJunk;
        public bool ContainsSent => Flags.ContainsSent;
        public bool ContainsTrash => Flags.ContainsTrash;

        private cMailboxFlags ZMailboxFlags()




        public bool Selected => Client.MailboxCacheItem(MailboxId)?.Selected ?? false;
        public bool SelectedForUpdate => Client.MailboxCacheItem(MailboxId)?.SelectedForUpdate ?? false;
        public bool AccessReadOnly => Client.MailboxCacheItem(MailboxId)?.AccessReadOnly ?? false;

        public cMessageFlags MessageFlags => Client.MailboxCacheItem(MailboxId)?.MessageFlags;
        public cMessageFlags PermanentFlags => Client.MailboxCacheItem(MailboxId)?.PermanentFlags;










        // get data
        public cMailboxStatus Status(int? pCacheAgeMax = null) => Client.Status(MailboxId, pCacheAgeMax);
        public Task<cMailboxStatus> StatusAsync(int? pCacheAgeMax = null) => Client.StatusAsync(MailboxId, pCacheAgeMax);

        public int MessageCount => Client.Status(MailboxId, null).Messages;
        public int Recent => Client.Status(MailboxId, true).Recent;
        public uint UIDNext => Client.Status(MailboxId, true).UIDNext;
        public uint NewUnknownUID => Client.Status(MailboxId, true).NewUnknownUID;
        public uint UIDValidity => Client.Status(MailboxId, true).UIDValidity;
        public int Unseen => Client.Status(MailboxId, true).Unseen;
        public int UnseenUnknown => Client.Status(MailboxId, true).UnseenUnknown;
        public ulong HighestModSeq => Client.Status(MailboxId, true).HighestModSeq;

        public List<cMailbox> Mailboxes(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault) => Client.Mailboxes(MailboxId, pTypes, pListProperties);
        public Task<List<cMailbox>> MailboxesAsync(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fListMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.MailboxesAsync(MailboxId, pTypes, pFlagSets, pStatusAttributes);
        public List<cMessage> Messages(cFilter pFilter = null, cSort pSort = null, fFetchAttributes pAttributes = fFetchAttributes.clientdefault) => Client.Messages(MailboxId, pFilter, pSort, pAttributes);
        public Task<List<cMessage>> MessagesAsync(cFilter pFilter = null, cSort pSort = null, fFetchAttributes pAttributes = fFetchAttributes.clientdefault) => Client.MessagesAsync(MailboxId, pFilter, pSort, pAttributes);

        public List<cMessage> Fetch(IList<iMessageHandle> pHandles, fFetchAttributes pAttributes, cFetchControl pFC = null)
        {
            Client.Fetch(MailboxId, pHandles, pAttributes, pFC);
            return Messages(pHandles);
        }

        public async Task<List<cMessage>> FetchAsync(IList<iMessageHandle> pHandles, fFetchAttributes pAttributes, cFetchControl pFC = null)
        {
            await Client.FetchAsync(MailboxId, pHandles, pAttributes, pFC).ConfigureAwait(false);
            return Messages(pHandles);
        }

        public List<cMessage> Messages(IList<iMessageHandle> pHandles)
        {
            List<cMessage> lMessages = new List<cMessage>();
            foreach (var lHandle in pHandles) lMessages.Add(new cMessage(Client, MailboxId, lHandle));
            return lMessages;
        }

        public void Fetch(IList<cMessage> pMessages, fFetchAttributes pAttributes, cFetchControl pFC = null) => Client.Fetch(MailboxId, ZHandles(pMessages), pAttributes, pFC);
        public Task FetchAsync(IList<cMessage> pMessages, fFetchAttributes pAttributes, cFetchControl pFC = null) => Client.FetchAsync(MailboxId, ZHandles(pMessages), pAttributes, pFC);

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

        public cMessage UIDFetch(cUID pUID, fFetchAttributes pAttributes) => Client.UIDFetch(MailboxId, pUID, pAttributes);
        public Task<cMessage> UIDFetchAsync(cUID pUID, fFetchAttributes pAttributes) => Client.UIDFetchAsync(MailboxId, pUID, pAttributes);
        public List<cMessage> UIDFetch(IList<cUID> pUIDs, fFetchAttributes pAttributes, cFetchControl pFC = null) => Client.UIDFetch(MailboxId, pUIDs, pAttributes, pFC);
        public Task<List<cMessage>> UIDFetchAsync(IList<cUID> pUIDs, fFetchAttributes pAttributes, cFetchControl pFC = null) => Client.UIDFetchAsync(MailboxId, pUIDs, pAttributes, pFC);
        public void UIDFetch(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.UIDFetch(MailboxId, pUID, pSection, pDecoding, pStream, pFC);
        public Task UIDFetchAsync(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.UIDFetchAsync(MailboxId, pUID, pSection, pDecoding, pStream, pFC);

        // uidstore TODO

        // cached data
        public iMailboxProperties Properties => Client.GetMailboxProperties(MailboxId); // only works if the mailbox is selected (and in the future notified)

        // mailbox actions
        public void Select(bool pForUpdate = false) => Client.Select(MailboxId, pForUpdate);
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(MailboxId, pForUpdate);

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({MailboxId})";
    }
}