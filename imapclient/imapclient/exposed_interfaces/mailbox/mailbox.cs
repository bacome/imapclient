using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailbox : iChildMailboxes
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
                return Handle.Exists == true;
            }
        }

        public bool CanHaveChildren
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.CanHaveChildren;
            }
        }

        public bool CanSelect
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.CanSelect;
            }
        }

        public bool? IsMarked
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.IsMarked;
            }
        }

        public bool IsRemote
        {
            get
            {
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.IsRemote;
            }
        }

        public bool? HasChildren
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.children) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
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
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.ContainsAll;
            }
        }

        public bool IsArchive
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.IsArchive;
            }
        }

        public bool ContainsDrafts
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.ContainsDrafts;
            }
        }

        public bool ContainsFlagged
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.ContainsFlagged;
            }
        }

        public bool ContainsJunk
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.ContainsJunk;
            }
        }

        public bool ContainsSent
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.ContainsSent;
            }
        }

        public bool ContainsTrash
        {
            get
            {
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.ListFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.ListFlags.ContainsTrash;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                if (Handle.LSubFlags == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.lsub);
                if (Handle.LSubFlags == null) throw new cInternalErrorException();
                return Handle.LSubFlags.Subscribed;
            }
        }

        public int MessageCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.MessageCount;
                if ((Client.MailboxCacheData & fMailboxCacheData.messagecount) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.MailboxStatus == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.status);
                if (Handle.MailboxStatus == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.MailboxStatus.MessageCount;
            }
        }

        public int RecentCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.RecentCount;
                if ((Client.MailboxCacheData & fMailboxCacheData.recentcount) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.MailboxStatus == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.status);
                if (Handle.MailboxStatus == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.MailboxStatus.RecentCount;
            }
        }

        public uint UIDNext
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDNext;
                if ((Client.MailboxCacheData & fMailboxCacheData.uidnext) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.MailboxStatus == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.status);
                if (Handle.MailboxStatus == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.MailboxStatus.UIDNext;
            }
        }

        public int UIDNextUnknownCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDNextUnknownCount;
                return Handle.MailboxStatus?.UIDNextUnknownCount ?? 0;
            }
        }

        public uint UIDValidity
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDValidity;
                if ((Client.MailboxCacheData & fMailboxCacheData.uidvalidity) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.MailboxStatus == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.status);
                if (Handle.MailboxStatus == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.MailboxStatus.UIDValidity;
            }
        }

        public int UnseenCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UnseenCount;
                if ((Client.MailboxCacheData & fMailboxCacheData.unseencount) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.MailboxStatus == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.status);
                if (Handle.MailboxStatus == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.MailboxStatus.UnseenCount;
            }
        }

        public int UnseenUnknownCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UnseenUnknownCount;
                return Handle.MailboxStatus?.UnseenUnknownCount ?? 0;
            }
        }

        public ulong HighestModSeq
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.HighestModSeq;
                if ((Client.MailboxCacheData & fMailboxCacheData.highestmodseq) == 0) throw new InvalidOperationException("mailbox not caching this data");
                if (Handle.MailboxStatus == null) Client.GetMailboxData(Handle, fMailboxCacheDataSets.status);
                if (Handle.MailboxStatus == null) throw new InvalidOperationException("mailbox doesn't exist");
                return Handle.MailboxStatus.HighestModSeq;
            }
        }

        public bool HasBeenSelected => Handle.SelectedProperties.HasBeenSelected;
        public bool HasBeenSelectedForUpdate => Handle.SelectedProperties.HasBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => Handle.SelectedProperties.HasBeenSelectedReadOnly;

        public cMessageFlags MessageFlags
        {
            get
            {
                var lSelectedProperties = Handle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelected) throw new InvalidOperationException("must have been selected");
                return lSelectedProperties.MessageFlags;
            }
        }

        public cMessageFlags ForUpdatePermanentFlags
        {
            get
            {
                var lSelectedProperties = Handle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelectedForUpdate) throw new InvalidOperationException("must have been selected for update");
                return lSelectedProperties.ForUpdatePermanentFlags;
            }
        }

        public cMessageFlags ReadOnlyPermanentFlags
        {
            get
            {
                var lSelectedProperties = Handle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelectedReadOnly) throw new InvalidOperationException("must have been selected read only");
                return lSelectedProperties.ReadOnlyPermanentFlags;
            }
        }

        public bool IsSelected => ReferenceEquals(Client.SelectedMailboxDetails?.Handle, Handle);

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

        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(Handle, pDataSets);
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(Handle, pDataSets);

        public List<cMailbox> Subscribed(fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(Handle, false, pDataSets);
        public Task<List<cMailbox>> SubscribedAsync(fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(Handle, false, pDataSets);

        public void Select(bool pForUpdate = false) => Client.Select(Handle, pForUpdate);
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(Handle, pForUpdate);

        public List<cMessage> Messages(cFilter pFilter = null, cSort pSort = null, fMessageProperties pProperties = fMessageProperties.clientdefault) => Client.Messages(Handle, pFilter, pSort, pProperties);
        public Task<List<cMessage>> MessagesAsync(cFilter pFilter = null, cSort pSort = null, fMessageProperties pProperties = fMessageProperties.clientdefault) => Client.MessagesAsync(Handle, pFilter, pSort, pProperties);

        ;?; // maybe ... any search where the filter is everything or just unseen should do .. the problem might be thread; but have a look => don't need a specail API
        public List<cMessage> UnseenMessages() => Client.setunseenmessages(Handle);
        ;?; // async

        public List<cMessage> Messages(IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchControl pFC = null)
        {
            Client.Fetch(pHandles, pProperties, pFC);
            return ZMessages(pHandles);
        }

        public async Task<List<cMessage>> MessagesAsync(IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchControl pFC = null)
        {
            await Client.FetchAsync(pHandles, pProperties, pFC).ConfigureAwait(false);
            return ZMessages(pHandles);
        }

        private List<cMessage> ZMessages(IList<iMessageHandle> pHandles)
        {
            List<cMessage> lMessages = new List<cMessage>();
            foreach (var lHandle in pHandles) lMessages.Add(new cMessage(Client, lHandle));
            return lMessages;
        }

        public cMessage Message(cUID pUID, fMessageProperties pProperties) => Client.Message(Handle, pUID, pProperties);
        public Task<cMessage> MessageAsync(cUID pUID, fMessageProperties pProperties) => Client.MessageAsync(Handle, pUID, pProperties);
        public List<cMessage> Messages(IList<cUID> pUIDs, fMessageProperties pProperties, cFetchControl pFC = null) => Client.Messages(Handle, pUIDs, pProperties, pFC);
        public Task<List<cMessage>> MessagesAsync(IList<cUID> pUIDs, fMessageProperties pProperties, cFetchControl pFC = null) => Client.MessagesAsync(Handle, pUIDs, pProperties, pFC);

        public void GetMailboxData(fMailboxCacheDataSets pDataSets) => Client.GetMailboxData(Handle, pDataSets);
        public Task GetMailboxDataAsync(fMailboxCacheDataSets pDataSets) => Client.GetMailboxDataAsync(Handle, pDataSets);

        public void Fetch(IList<cMessage> pMessages, fMessageProperties pProperties, cFetchControl pFC = null) => Client.Fetch(ZHandles(pMessages), pProperties, pFC);
        public Task FetchAsync(IList<cMessage> pMessages, fMessageProperties pProperties, cFetchControl pFC = null) => Client.FetchAsync(ZHandles(pMessages), pProperties, pFC);

        private List<iMessageHandle> ZHandles(IList<cMessage> pMessages)
        {
            List<iMessageHandle> lHandles = new List<iMessageHandle>();

            foreach (var lMessage in pMessages)
            {
                if (lMessage == null) throw new ArgumentOutOfRangeException(nameof(pMessages));
                lHandles.Add(lMessage.Handle);
            }

            return lHandles;
        }

        public void UIDFetch(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.UIDFetch(Handle, pUID, pSection, pDecoding, pStream, pFC);
        public Task UIDFetchAsync(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.UIDFetchAsync(Handle, pUID, pSection, pDecoding, pStream, pFC);

        // uid/store TODO

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({Handle})";
    }
}