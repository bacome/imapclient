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
                if (Handle.Exists == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                return Handle.Exists.Value;
            }
        }

        public bool CanHaveChildren
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.CanHaveChildren;
            }
        }

        public bool CanSelect
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.CanSelect;
            }
        }

        public bool? IsMarked
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.IsMarked;
            }
        }

        public bool IsRemote
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.IsRemote;
            }
        }

        public bool? HasChildren
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.children) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                bool? lHasChildren = Handle.ListFlags.HasChildren;
                if (lHasChildren == true) return true;
                if (Client.HasCachedChildren(Handle) == true) return true;
                return lHasChildren;
            }
        }

        public bool ContainsAll
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.ContainsAll;
            }
        }

        public bool IsArchive
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.IsArchive;
            }
        }

        public bool ContainsDrafts
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.ContainsDrafts;
            }
        }

        public bool ContainsFlagged
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.ContainsFlagged;
            }
        }

        public bool ContainsJunk
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.ContainsJunk;
            }
        }

        public bool ContainsSent
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.ContainsSent;
            }
        }

        public bool ContainsTrash
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new cMailboxCacheDataException();
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.ListFlags.ContainsTrash;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                if (Handle.LSubFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.lsub);
                return Handle.LSubFlags.Subscribed;
            }
        }

        public int MessageCount
        {
            get
            {
                if (Handle.MailboxStatus == null) Client.GetStatus(Handle);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.MailboxStatus.MessageCount;
            }
        }

        public int MessageCount
        {
            get
            {
                if (Handle.MailboxStatus == null) Client.GetStatus(Handle);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.MailboxStatus.MessageCount;
            }
        }

        public int MessageCount
        {
            get
            {
                if (Handle.MailboxStatus == null) Client.GetStatus(Handle);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.MailboxStatus.MessageCount;
            }
        }

        public int MessageCount
        {
            get
            {
                if (Handle.MailboxStatus == null) Client.GetStatus(Handle);
                if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
                return Handle.MailboxStatus.MessageCount;
            }
        }


        ;?; // this isn't right - it should be like the message ones and do the fetch if required




        // status could be null (for unselectable mailbox) => these may throw
        public int MessageCount => Client.Status(MailboxId).MessageCount;
        public int RecentCount => Client.Status(MailboxId).RecentCount;
        public uint UIDNext => Client.Status(MailboxId).UIDNext;
        public int UIDNextUnknownCount => Client.Status(MailboxId).NewUnknownUIDCount;
        public uint UIDValidity => Client.Status(MailboxId).UIDValidity;
        public int UnseenCount => Client.Status(MailboxId).UnseenCount;
        public int UnseenUnknownCount => Client.Status(MailboxId).UnseenUnknownCount;
        public ulong HighestModSeq => Client.Status(MailboxId).HighestModSeq;

        public bool HasBeenSelected => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.HasBeenSelected;
        public bool HasBeenSelectedForUpdate => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.HasBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.HasBeenSelectedReadOnly;
        public cMessageFlags MessageFlags => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.MessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.ForUpdatePermanentFlags;
        public cMessageFlags ReadOnlyPermanentFlags => Client.MailboxCacheItem(MailboxId).MailboxBeenSelected.ReadOnlyPermanentFlags;

        public bool IsSelected
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null | lDetails.Handle != Handle) return false;
                return true;
            }
        }

        public bool IsSelectedForUpdate
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null | lDetails.Handle != Handle) return false;
                return lDetails.SelectedForUpdate;
            }
        }

        public bool IsAccessReadOnly
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null | lDetails.Handle != Handle) return false;
                return lDetails.AccessReadOnly;
            }
        }

        // talk to server

        ;?; // get flags, get sunscribed

        public void GetStatus()
        {
            Client.GetStatus(Handle);
            if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
        }

        public async Task GetStatusAsync()
        {
            await Client.GetStatusAsync(Handle).ConfigureAwait(false);
            if (Handle.Exists != true) throw new cMailboxDoesNotExistException();
        }

        public void Update(bool pStatus) => Client.MailboxHandleUpdate(MailboxId, Handle, pProperties, true);
        public Task UpdateAsync(bool pStatus) => Client.MailboxHandleUpdateAsync(MailboxId, Handle, pProperties, true);

        public List<cMailbox> Mailboxes(bool pStatus = false) => Client.Mailboxes(MailboxId, pProperties);
        public Task<List<cMailbox>> MailboxesAsync(bool pStatus = false) => Client.MailboxesAsync(MailboxId, pProperties);

        ;?; // note subscription parameter is descend and the client api takes hassubscribedchildren

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

        public List<cMessage> UnseenMessages Unseen() => Client.setunseen();
        ;?; // and async

        // helpers

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({MailboxId})";
    }
}