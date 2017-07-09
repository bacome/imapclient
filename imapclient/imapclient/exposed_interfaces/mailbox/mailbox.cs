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

        public bool CanHaveChildren => ZMailboxFlags(cMailboxFlags.CanHaveChildrenFlagSets).CanHaveChildren;
        public bool? HasChildren => ZMailboxFlags(cMailboxFlags.HasChildrenFlagSets).HasChildren;
        public bool CanSelect => ZMailboxFlags(cMailboxFlags.CanSelectFlagSets).CanSelect;
        public bool? IsMarked => ZMailboxFlags(cMailboxFlags.IsMarkedFlagSets).IsMarked;
        public bool IsSubscribed => ZMailboxFlags(cMailboxFlags.IsSubscribedFlagSets).IsSubscribed;
        public bool HasSubscribedChildren => ZMailboxFlags(cMailboxFlags.HasSubscribedChildrenFlagSets).HasSubscribedChildren;
        public bool IsLocal => ZMailboxFlags(cMailboxFlags.IsLocalFlagSets).IsLocal;
        public bool ContainsAll => ZMailboxFlags(cMailboxFlags.ContainsAllFlagSets).ContainsAll;
        public bool IsArchive => ZMailboxFlags(cMailboxFlags.IsArchiveFlagSets).IsArchive;
        public bool ContainsDrafts => ZMailboxFlags(cMailboxFlags.ContainsDraftsFlagSets).ContainsDrafts;
        public bool ContainsFlagged => ZMailboxFlags(cMailboxFlags.ContainsFlaggedFlagSets).ContainsFlagged;
        public bool ContainsJunk => ZMailboxFlags(cMailboxFlags.ContainsJunkFlagSets).ContainsJunk;
        public bool ContainsSent => ZMailboxFlags(cMailboxFlags.ContainsSentFlagSets).ContainsSent;
        public bool ContainsTrash => ZMailboxFlags(cMailboxFlags.ContainsTrashFlagSets).ContainsTrash;

        private cMailboxFlags ZMailboxFlags(fMailboxFlagSets pRequiredFlagSets)
        {
            var lMailboxFlags = Client.MailboxCacheItem(MailboxId)?.MailboxFlags;

            if (lMailboxFlags == null || (lMailboxFlags.FlagSets & pRequiredFlagSets) != pRequiredFlagSets)
            {
                Client.UpdateMailboxCache(MailboxId, pRequiredFlagSets, false);
                lMailboxFlags = Client.MailboxCacheItem(MailboxId).MailboxFlags;
            }

            return lMailboxFlags;
        }

        public int MessageCount => Client.Status(MailboxId, null).MessageCount;
        public int RecentCount => Client.Status(MailboxId, null).RecentCount;
        public uint UIDNext => Client.Status(MailboxId, null).UIDNext;
        public int NewUnknownUIDCount => Client.Status(MailboxId, null).NewUnknownUIDCount;
        public uint UIDValidity => Client.Status(MailboxId, null).UIDValidity;
        public int UnseenCount => Client.Status(MailboxId, null).UnseenCount;
        public int UnseenUnknownCount => Client.Status(MailboxId, null).UnseenUnknownCount;
        public ulong HighestModSeq => Client.Status(MailboxId, null).HighestModSeq;

        public bool IsSelected => Client.MailboxCacheItem(MailboxId)?.IsSelected ?? false;
        public bool IsSelectedForUpdate => Client.MailboxCacheItem(MailboxId)?.IsSelectedForUpdate ?? false;
        public bool IsAccessReadOnly => Client.MailboxCacheItem(MailboxId)?.IsAccessReadOnly ?? false;

        public cMessageFlags MessageFlags => Client.MailboxCacheItem(MailboxId)?.MessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => Client.MailboxCacheItem(MailboxId)?.ForUpdatePermanentFlags;
        public cMessageFlags ReadOnlyPermanentFlags => Client.MailboxCacheItem(MailboxId)?.ReadOnlyPermanentFlags;

        // talk to server

        public void UpdateCache(fMailboxProperties pProperties) => Client.UpdateMailboxCache(MailboxId, pProperties);
        public Task UpdateCacheAsync(fMailboxProperties pProperties) => Client.UpdateMailboxCacheAsync(MailboxId, pProperties);

        public cMailboxStatus Status(int? pCacheAgeMax = null) => Client.Status(MailboxId, pCacheAgeMax);
        public Task<cMailboxStatus> StatusAsync(int? pCacheAgeMax = null) => Client.StatusAsync(MailboxId, pCacheAgeMax);

        public List<cMailbox> Mailboxes(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxProperties pProperties = 0) => Client.Mailboxes(MailboxId, pTypes, pProperties);
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