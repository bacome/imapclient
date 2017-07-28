using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailbox : iMailboxes
    {
        private PropertyChangedEventHandler mPropertyChanged;
        private object mPropertyChangedLock = new object();

        private EventHandler<cMessageDeliveryEventArgs> mMessageDelivery;
        private object mMessageDeliveryLock = new object();

        public readonly cIMAPClient Client;
        public readonly iMailboxHandle Handle;

        public cMailbox(cIMAPClient pClient, iMailboxHandle pHandle)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
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
            if (ReferenceEquals(pArgs.Handle, Handle)) mPropertyChanged?.Invoke(this, pArgs);
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
            if (ReferenceEquals(pArgs.Handle, Handle)) mMessageDelivery?.Invoke(this, pArgs);
        }

        // convenience method

        public string Name => Handle.MailboxName.Name;

        // properties

        public bool Exists
        {
            get
            {
                Client.MailboxHandleUpdate(MailboxId, Handle, fMailboxProperties.exists, false);
                return Handle.Exists.Value;
            }
        }

        ;?; // this isn't right - it should be like the message ones and do the fetch if required


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

        public int MessageCount => Client.Status(MailboxId).MessageCount;
        public int RecentCount => Client.Status(MailboxId).RecentCount;
        public uint UIDNext => Client.Status(MailboxId).UIDNext;
        public int UIDNextUnknownCount => Client.Status(MailboxId).NewUnknownUIDCount;
        public uint UIDValidity => Client.Status(MailboxId).UIDValidity;
        public int UnseenCount => Client.Status(MailboxId).UnseenCount;
        public int UnseenUnknownCount => Client.Status(MailboxId).UnseenUnknownCount;
        public ulong HighestModSeq => Client.Status(MailboxId).HighestModSeq;

        public Task<cMailboxStatus> GetStatusAsync() => Client.StatusAsync(MailboxId, pAgeMax); ;?;

        public cMailboxSelectedProperties SelectedProperties => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected;
        public bool HasBeenSelected => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.HasBeenSelected;
        public bool HasBeenSelectedForUpdate => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.HasBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.HasBeenSelectedReadOnly;
        public cMessageFlags MessageFlags => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.MessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.ForUpdatePermanentFlags;
        public cMessageFlags ReadOnlyPermanentFlags => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.ReadOnlyPermanentFlags;

        public cMailboxSelected Selected
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null | lDetails.MailboxId != MailboxId) return new cMailboxSelected();
                return new cMailboxSelected(lDetails.SelectedForUpdate, lDetails.AccessReadOnly);
            }
        }

        public bool IsSelected
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null | lDetails.MailboxId != MailboxId) return false;
                return true;
            }
        }

        public bool IsSelectedForUpdate
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null | lDetails.MailboxId != MailboxId) return false;
                return lDetails.SelectedForUpdate;
            }
        }

        public bool IsAccessReadOnly
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null | lDetails.MailboxId != MailboxId) return false;
                return lDetails.AccessReadOnly;
            }
        }

        // talk to server

        public cMailboxStatus Status() => Client.Status(MailboxId); // this should do a status command regardless
        public cMailboxStatus StatusAsync() => Client.Statusxxx(MailboxId); // this should do a status command regardless

        public void Update(bool pStatus) => Client.MailboxHandleUpdate(MailboxId, Handle, pProperties, true);
        public Task UpdateAsync(bool pStatus) => Client.MailboxHandleUpdateAsync(MailboxId, Handle, pProperties, true);

        public List<cMailbox> Mailboxes(bool pStatus) => Client.Mailboxes(MailboxId, pProperties);
        public Task<List<cMailbox>> MailboxesAsync(bool pStatus) => Client.MailboxesAsync(MailboxId, pProperties);

        public List<cMailbox> SubscribedMailboxes(fMailboxProperties pProperties = fMailboxProperties.clientdefault) => Client.Mailboxes(MailboxId, pProperties);
        public Task<List<cMailbox>> SubscribedMailboxesAsync(fMailboxProperties pProperties = fMailboxProperties.clientdefault) => Client.MailboxesAsync(MailboxId, pProperties);

        public List<cMessage> Messages(cFilter pFilter = null, cSort pSort = null, fMessageProperties pProperties = fMessageProperties.clientdefault) => Client.Messages(MailboxId, pFilter, pSort, pAttributes);
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

        ;?;


        public cMessage Message(iMessageHandle pHandle, fMessageProperties pProperties = fMessageProperties.clientdefault)
        {
            ;?;
        }

        public List<cMessage> Messages(IList<iMessageHandle> pHandles, fMessageProperties pProperties = fMessageProperties.clientdefault)
        {
            ;?; // nononono
            List<cMessage> lMessages = new List<cMessage>();
            foreach (var lHandle in pHandles) lMessages.Add(new cMessage(Client, MailboxId, lHandle));
            return lMessages;
        }

        public cMessage Message(cUID pUID, fFetchAttributes pAttributes) => Client.UIDFetch(MailboxId, pUID, pAttributes);
        public Task<cMessage> UIDFetchAsync(cUID pUID, fFetchAttributes pAttributes) => Client.UIDFetchAsync(MailboxId, pUID, pAttributes);
        public List<cMessage> UIDFetch(IList<cUID> pUIDs, fFetchAttributes pAttributes, cFetchControl pFC = null) => Client.UIDFetch(MailboxId, pUIDs, pAttributes, pFC);
        public Task<List<cMessage>> UIDFetchAsync(IList<cUID> pUIDs, fFetchAttributes pAttributes, cFetchControl pFC = null) => Client.UIDFetchAsync(MailboxId, pUIDs, pAttributes, pFC);
        public void UIDFetch(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.UIDFetch(MailboxId, pUID, pSection, pDecoding, pStream, pFC);
        public Task UIDFetchAsync(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.UIDFetchAsync(MailboxId, pUID, pSection, pDecoding, pStream, pFC);

        // uidstore TODO

        // mailbox actions

        public void Select(bool pForUpdate) => Client.Select(MailboxId, pOptions);
        public Task SelectAsync(bool pForUpdate) => Client.SelectAsync(MailboxId, pOptions);

        public void SetUnseen() => Client.setunseen();
        ;?; // and async

        // helpers

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({MailboxId})";
    }
}