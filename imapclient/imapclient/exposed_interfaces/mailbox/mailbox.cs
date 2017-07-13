using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

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

        public bool? Exists => Client.MailboxCacheItem(MailboxId).Exists;

        public cMailboxFlags MailboxFlags => Client.MailboxCacheItem(MailboxId).MailboxFlags;
        public bool CanHaveChildren => Client.MailboxCacheItem(MailboxId).MailboxFlags.CanHaveChildren;
        public bool CanSelect => Client.MailboxCacheItem(MailboxId).MailboxFlags.CanSelect;
        public bool? IsMarked => Client.MailboxCacheItem(MailboxId).MailboxFlags.IsMarked;
        public bool NonExistent => Client.MailboxCacheItem(MailboxId).MailboxFlags.NonExistent;
        public bool IsSubscribed => Client.MailboxCacheItem(MailboxId).MailboxFlags.IsSubscribed;
        public bool IsRemote => Client.MailboxCacheItem(MailboxId).MailboxFlags.IsRemote;
        public bool? HasChildren => Client.MailboxCacheItem(MailboxId).MailboxFlags.HasChildren;
        public bool HasSubscribedChildren => Client.MailboxCacheItem(MailboxId).MailboxFlags.HasSubscribedChildren;
        public bool ContainsAll => Client.MailboxCacheItem(MailboxId).MailboxFlags.ContainsAll;
        public bool IsArchive => Client.MailboxCacheItem(MailboxId).MailboxFlags.IsArchive;
        public bool ContainsDrafts => Client.MailboxCacheItem(MailboxId).MailboxFlags.ContainsDrafts;
        public bool ContainsFlagged => Client.MailboxCacheItem(MailboxId).MailboxFlags.ContainsFlagged;
        public bool ContainsJunk => Client.MailboxCacheItem(MailboxId).MailboxFlags.ContainsJunk;
        public bool ContainsSent => Client.MailboxCacheItem(MailboxId).MailboxFlags.ContainsSent;
        public bool ContainsTrash => Client.MailboxCacheItem(MailboxId).MailboxFlags.ContainsTrash;

        public cMailboxStatus MailboxStatus => Client.Status(MailboxId);
        public int MessageCount => Client.Status(MailboxId).MessageCount;
        public int RecentCount => Client.Status(MailboxId).RecentCount;
        public uint UIDNext => Client.Status(MailboxId).UIDNext;
        public int NewUnknownUIDCount => Client.Status(MailboxId).NewUnknownUIDCount;
        public uint UIDValidity => Client.Status(MailboxId).UIDValidity;
        public int UnseenCount => Client.Status(MailboxId).UnseenCount;
        public int UnseenUnknownCount => Client.Status(MailboxId).UnseenUnknownCount;
        public ulong HighestModSeq => Client.Status(MailboxId).HighestModSeq;

        public cMailboxSelected MailboxSelected => Client.MailboxCacheItem(MailboxId).MailboxSelected;
        public bool IsSelected => Client.MailboxCacheItem(MailboxId).MailboxSelected.IsSelected;
        public bool IsSelectedForUpdate => Client.MailboxCacheItem(MailboxId).MailboxSelected.IsSelectedForUpdate;
        public bool IsAccessReadOnly => Client.MailboxCacheItem(MailboxId).MailboxSelected.IsAccessReadOnly;
        public bool HasBeenSelected => Client.MailboxCacheItem(MailboxId).MailboxSelected.HasBeenSelected;
        public bool HasBeenSelectedForUpdate => Client.MailboxCacheItem(MailboxId).MailboxSelected.HasBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => Client.MailboxCacheItem(MailboxId).MailboxSelected.HasBeenSelectedReadOnly;
        public cMessageFlags MessageFlags => Client.MailboxCacheItem(MailboxId).MailboxSelected.MessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => Client.MailboxCacheItem(MailboxId).MailboxSelected.ForUpdatePermanentFlags;
        public cMessageFlags ReadOnlyPermanentFlags => Client.MailboxCacheItem(MailboxId).MailboxSelected.ReadOnlyPermanentFlags;

        // talk to server

        public cMailboxFlags Flags() => Client.Flags(MailboxId);
        public Task<cMailboxFlags> FlagsAsync() => Client.FlagsAsync(MailboxId);

        public cMailboxStatus Status(int? pCacheAgeMax = null) => Client.Status(MailboxId, pCacheAgeMax);
        public Task<cMailboxStatus> StatusAsync(int? pCacheAgeMax = null) => Client.StatusAsync(MailboxId, pCacheAgeMax);

        public List<cMailbox> Mailboxes(bool pStatus = false) => Client.Mailboxes(MailboxId, pStatus);
        public Task<List<cMailbox>> MailboxesAsync(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxProperties pProperties = 0) => Client.MailboxesAsync(MailboxId, pTypes, pProperties);
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

        // mailbox actions

        public void Select(fSelectOptions pOptions) => Client.Select(MailboxId, pOptions);
        public Task SelectAsync(fSelectOptions pOptions) => Client.SelectAsync(MailboxId, pOptions);

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({MailboxId})";
    }
}