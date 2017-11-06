using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>Provides an API that allows interaction with an IMAP mailbox.</para>
    /// </summary>
    public class cMailbox : iMailboxParent
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

        /// <summary>
        /// <para>Fired when new messages arrive in the mailbox.</para>
        /// </summary>
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

        /// <summary>
        /// <para>The fully qualified name of the mailbox.</para>
        /// </summary>
        public string Path => Handle.MailboxName.Path;

        /// <summary>
        /// <para>The hierarchy delimiter used in the mailbox path.</para>
        /// </summary>
        public char? Delimiter => Handle.MailboxName.Delimiter;

        /// <summary>
        /// <para>The path of the parent mailbox.</para>
        /// <para>Will be null if there is no parent mailbox.</para>
        /// </summary>
        public string ParentPath => Handle.MailboxName.ParentPath;

        /// <summary>
        /// <para>The name of the mailbox.</para>
        /// <para>As compared to the <see cref="Path"/> this does not include the hierarchy.</para>
        /// </summary>
        /// 
        public string Name => Handle.MailboxName.Name;

        /// <summary>
        /// <para>True if this instance represents the inbox.</para>
        /// </summary>
        public bool IsInbox => Handle.MailboxName.IsInbox;

        /// <summary>
        /// <para>True if the mailbox exists on the server.</para>
        /// <para>Subscribed mailboxes and levels in the mailbox hierarchy do not need to exist.</para>
        /// </summary>
        public bool Exists
        {
            get
            {
                if (Handle.Exists == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                return Handle.Exists == true;
            }
        }

        /// <summary>
        /// <para>False if the mailbox can definitely not contain child mailboxes.</para>
        /// <para>See the IMAP \Noinferiors flag.</para>
        /// </summary>
        public bool? CanHaveChildren
        {
            get
            {
                if (Handle.Exists == false) return null; // don't know until the mailbox is created
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return null; // don't know
                return Handle.ListFlags.CanHaveChildren;
            }
        }

        /// <summary>
        /// <para>True if the mailbox can be selected</para>
        /// <para>See the IMAP \Noselect flag.</para>
        /// </summary>
        public bool CanSelect
        {
            get
            {
                if (Handle.Exists == false) return false;
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                return Handle.ListFlags.CanSelect;
            }
        }

        /// <summary>
        /// <para>Indicates if the mailbox has been marked "interesting" by the server.</para>
        /// <para>Null indicates that the server didn't say either way.</para>
        /// <para>See the IMAP \Marked and \Unmarked flags.</para>
        /// </summary>
        public bool? IsMarked
        {
            get
            {
                if (Handle.Exists == false) return false;
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                return Handle.ListFlags.IsMarked;
            }
        }

        /// <summary>
        /// <para>If true the mailbox is definitely a remote mailbox.</para>
        /// <para>If the connected server supports rfc 5258, if this flag is false the mailbox is definitely not a remote mailbox, otherwise it still may be one.</para>
        /// <para>Remote mailboxes will never be returned by the library if the <see cref="cIMAPClient.MailboxReferrals"/> is set to false.</para>
        /// </summary>
        public bool IsRemote
        {
            get
            {
                if (Handle.Exists == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                return Handle.ListFlags.IsRemote;
            }
        }

        /// <summary>
        /// <para>Indicates if the mailbox had children when the property was refreshed.</para>
        /// <para>Null indicates that the server didn't say either way.</para>
        /// <para>See the IMAP \HasChildren and \HasNoChildren flags.</para>
        /// </summary>
        public bool? HasChildren
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                bool? lHasChildren = Handle.ListFlags?.HasChildren;
                if (lHasChildren == true) return true;
                if (Client.HasCachedChildren(Handle) == true) return true;
                return lHasChildren;
            }
        }

        /// <summary>
        /// <para>If true the mailbox was marked with the \All flag indicating that the mailbox contains all messages.</para>
        /// <para>Null indicates that the specialuse flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </summary>
        public bool? ContainsAll
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsAll) return true;
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// <para>If true the mailbox was marked with the \Archive flag indicating that the mailbox contains the message archive.</para>
        /// <para>Null indicates that the specialuse flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </summary>
        public bool? IsArchive
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.IsArchive) return true;
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// <para>If true the mailbox was marked with the \Drafts flag indicating that the mailbox contains draft messages.</para>
        /// <para>Null indicates that the specialuse flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </summary>
        public bool? ContainsDrafts
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsDrafts) return true;
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// <para>If true the mailbox was marked with the \Flagged flag indicating that the mailbox contains flagged messages.</para>
        /// <para>Null indicates that the specialuse flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </summary>
        public bool? ContainsFlagged
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsFlagged) return true;
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// <para>If true the mailbox was marked with the \Junk flag indicating that the mailbox contains junk mail.</para>
        /// <para>Null indicates that the specialuse flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </summary>
        public bool? ContainsJunk
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsJunk) return true;
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// <para>If true the mailbox was marked with the \Sent flag indicating that the mailbox contains copies of messages that have been sent.</para>
        /// <para>Null indicates that the specialuse flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </summary>
        public bool? ContainsSent
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsSent) return true;
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// <para>If true the mailbox was marked with the \Trash flag indicating that the mailbox contains copies of messages that are deleted.</para>
        /// <para>Null indicates that the specialuse flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </summary>
        public bool? ContainsTrash
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsTrash) return true;
                if ((Client.MailboxCacheData & fMailboxCacheData.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// <para>Indicates if this mailbox is subscribed.</para>
        /// </summary>
        public bool IsSubscribed
        {
            get
            {
                if (Handle.LSubFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.lsub);
                if (Handle.LSubFlags == null) throw new cInternalErrorException();
                return Handle.LSubFlags.Subscribed;
            }
        }

        /// <summary>
        /// <para>The number of messages in the mailbox.</para>
        /// <para>Null indicates that the messagecount is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or was not sent by the server when requested.</para>
        /// <para>This property always has an up-to-date value when the mailbox is selected.</para>
        /// </summary>
        public int? MessageCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.Count;
                if ((Client.MailboxCacheData & fMailboxCacheData.messagecount) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.MessageCount;
            }
        }

        /// <summary>
        /// <para>The number of recent messages in the mailbox.</para>
        /// <para>Null indicates that the recentcount is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or was not sent by the server when requested.</para>
        /// <para>This property always has an up-to-date value when the mailbox is selected.</para>
        /// </summary>
        public int? RecentCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.RecentCount;
                if ((Client.MailboxCacheData & fMailboxCacheData.recentcount) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.RecentCount;
            }
        }

        /// <summary>
        /// <para>The predicted UID that will be given to the next new message entering the mailbox.</para>
        /// <para>Null indicates that the uidnext is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or was not sent by the server when requested.</para>
        /// <para>When the mailbox is selected, zero indicates that the value is unknown.</para>
        /// <para>When the mailbox is selected this property may not be up-to-date: see the value of <see cref="UIDNextUnknownCount"/> for the potential inaccuracy in this property value.</para>
        /// </summary>
        public uint? UIDNext
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDNext;
                if ((Client.MailboxCacheData & fMailboxCacheData.uidnext) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.UIDNext;
            }
        }

        /// <summary>
        /// <para>When the mailbox is not selected this will be zero.</para>
        /// <para>Otherwise it is the number of messages that have arrived since the mailbox was opened for which the library has not seen the value of the UID.</para>
        /// </summary>
        public int UIDNextUnknownCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDNextUnknownCount;
                return Handle.MailboxStatus?.UIDNextUnknownCount ?? 0;
            }
        }

        /// <summary>
        /// <para>The UIDValidity of the mailbox.</para>
        /// <para>Null indicates that the mailbox does not support UIDs or that the UIDValidity is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>).</para>
        /// <para>This property always has a value when the mailbox is selected, however zero indicates that the server does not support UIDs. (Also see <see cref="UIDNotSticky"/>.)</para>
        /// </summary>
        public uint? UIDValidity
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDValidity;
                if ((Client.MailboxCacheData & fMailboxCacheData.uidvalidity) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.UIDValidity;
            }
        }

        /// <summary>
        /// <para>The number of unseen messages in the mailbox.</para>
        /// <para>Null indicates that the unseencount is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or was not sent by the server when requested.</para>
        /// <para>When the mailbox is selected this property will always have a value but it may not be up-to-date: see the value of <see cref="UnseenUnknownCount"/> for the potential inaccuracy of this property value.</para>
        /// <para>To initialise the value of this property value when the mailbox is selected use <see cref="SetUnseen"/>.</para>
        /// <para>To maintain the value of this property when the mailbox is selected use <see cref="Messages(IEnumerable{iMessageHandle}, cCacheItems, cPropertyFetchConfiguration)"/> on the new messages that arrive (see <see cref="MessageDelivery"/>).</para>
        /// </summary>
        public int? UnseenCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UnseenCount;
                if ((Client.MailboxCacheData & fMailboxCacheData.unseencount) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.UnseenCount;
            }
        }

        /// <summary>
        /// <para>When the mailbox is not selected this will be zero.</para>
        /// <para>Otherwise it is the number of messages for which the library is unsure of the value of the \Seen flag.</para>
        /// <para>To keep this value at zero see the technique outlined here: <see cref="UnseenCount"/>.</para>
        /// </summary>
        public int UnseenUnknownCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UnseenUnknownCount;
                return Handle.MailboxStatus?.UnseenUnknownCount ?? 0;
            }
        }

        /// <summary>
        /// <para>See RFC 7162.</para>
        /// <para>Null indicates that the highestmodseq is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or was not sent by the server when requested.</para>
        /// <para>When the mailbox is selected this property will always have a value but zero indicates that RFC 7162 is not supported on the mailbox.</para>
        /// </summary>
        public ulong? HighestModSeq
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.HighestModSeq;
                if ((Client.MailboxCacheData & fMailboxCacheData.highestmodseq) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.HighestModSeq;
            }
        }

        /// <summary>
        /// <para>Indicates if the mailbox has been selected once in this session.</para>
        /// </summary>
        public bool HasBeenSelected => Handle.SelectedProperties.HasBeenSelected;

        /// <summary>
        /// <para>Indicates if the mailbox has been selected for update once in this session.</para>
        /// </summary>
        public bool HasBeenSelectedForUpdate => Handle.SelectedProperties.HasBeenSelectedForUpdate;

        /// <summary>
        /// <para>Indicates if the mailbox has been selected readonly once in this session.</para>
        /// </summary>
        public bool HasBeenSelectedReadOnly => Handle.SelectedProperties.HasBeenSelectedReadOnly;

        /// <summary>
        /// <para>Indicates if the mailbox has sticky UIDs.</para>
        /// <para>Null if the mailbox has never been selected.</para>
        /// </summary>
        public bool? UIDNotSticky => Handle.SelectedProperties.UIDNotSticky;

        /// <summary>
        /// <para>The defined flags in the mailbox.</para>
        /// <para>Null if the mailbox has never been selected.</para>
        /// </summary>
        public cMessageFlags MessageFlags
        {
            get
            {
                var lSelectedProperties = Handle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelected) return null;
                return lSelectedProperties.MessageFlags;
            }
        }

        /// <summary>
        /// <para>The flags that the client can change permanently in this mailbox when it is selected for update.</para>
        /// <para>Null if the mailbox has never been selected for update.</para>
        /// </summary>
        public cMessageFlags ForUpdatePermanentFlags
        {
            get
            {
                var lSelectedProperties = Handle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelectedForUpdate) return null;
                return lSelectedProperties.ForUpdatePermanentFlags;
            }
        }

        /// <summary>
        /// <para>The flags that the client can change permanently in this mailbox when it is selected readonly.</para>
        /// <para>Null if the mailbox has never been selected readonly.</para>
        /// </summary>
        public cMessageFlags ReadOnlyPermanentFlags
        {
            get
            {
                var lSelectedProperties = Handle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelectedReadOnly) return null;
                return lSelectedProperties.ReadOnlyPermanentFlags;
            }
        }

        /// <summary>
        /// <para>Indicates if the mailbox is currently the selected mailbox.</para>
        /// </summary>
        public bool IsSelected => ReferenceEquals(Client.SelectedMailboxDetails?.Handle, Handle);

        /// <summary>
        /// <para>Indicates if the mailbox is currently selected for update.</para>
        /// </summary>
        public bool IsSelectedForUpdate
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null || !ReferenceEquals(lDetails.Handle, Handle)) return false;
                return lDetails.SelectedForUpdate;
            }
        }

        /// <summary>
        /// <para>Indicates if the mailbox is currently selected but the mailbox can't be modified.</para>
        /// </summary>
        public bool IsAccessReadOnly
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null || !ReferenceEquals(lDetails.Handle, Handle)) return false;
                return lDetails.AccessReadOnly;
            }
        }

        // talk to server

        /// <summary>
        /// <para>Gets the mailbox's child mailboxes.</para>
        /// </summary>
        /// <param name="pDataSets">
        /// <para>The sets of data to retrieve when getting the child mailboxes.</para>
        /// <para>See <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </param>
        /// <returns>A list of mailboxes</returns>
        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(Handle, pDataSets);

        /// <summary>
        /// The async version of <see cref="Mailboxes(fMailboxCacheDataSets)"/>
        /// </summary>
        /// <param name="pDataSets"></param>
        /// <returns></returns>
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(Handle, pDataSets);

        public List<cMailbox> Subscribed(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(Handle, pDescend, pDataSets);
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(Handle, pDescend, pDataSets);

        public cMailbox CreateChild(string pName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pName), pAsFutureParent);
        public Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent = true) => Client.CreateAsync(ZCreateChild(pName), pAsFutureParent);

        private cMailboxName ZCreateChild(string pName)
        {
            if (Handle.MailboxName.Delimiter == null) throw new InvalidOperationException();
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (pName.IndexOf(Handle.MailboxName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(Handle.MailboxName.Path + Handle.MailboxName.Delimiter.Value + pName, Handle.MailboxName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        public void Subscribe() => Client.Subscribe(Handle);
        public Task SubscribeAsync() => Client.SubscribeAsync(Handle);

        public void Unsubscribe() => Client.Unsubscribe(Handle);
        public Task UnsubscribeAsync() => Client.UnsubscribeAsync(Handle);
    
        public cMailbox Rename(string pName) => Client.Rename(Handle, ZRename(pName));
        public Task<cMailbox> RenameAsync(string pName) => Client.RenameAsync(Handle, ZRename(pName));

        public cMailboxName ZRename(string pName)
        {
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (Handle.MailboxName.Delimiter == null) return new cMailboxName(pName, null);
            if (pName.IndexOf(Handle.MailboxName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(Handle.MailboxName.ParentPath + Handle.MailboxName.Delimiter + pName, Handle.MailboxName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        /*
        public cMailbox Rename(cNamespace pNamespace, string pName = null)
        {
            ;?;
        }

        public cMailbox Rename(cMailbox pMailbox, string pName = null)
        {
            ;?;
        } */

        public void Delete() => Client.Delete(Handle);
        public Task DeleteAsync() => Client.DeleteAsync(Handle);

        public void Select(bool pForUpdate = false) => Client.Select(Handle, pForUpdate);
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(Handle, pForUpdate);

        public void Expunge(bool pAndUnselect = false) => Client.Expunge(Handle, pAndUnselect);
        public Task ExpungeAsync(bool pAndUnselect = false) => Client.ExpungeAsync(Handle, pAndUnselect);

        public List<cMessage> Messages(cFilter pFilter = null, cSort pSort = null, cCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultCacheItems, pConfiguration);
        public Task<List<cMessage>> MessagesAsync(cFilter pFilter = null, cSort pSort = null, cCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultCacheItems, pConfiguration);

        public List<cMessage> Messages(IEnumerable<iMessageHandle> pHandles, cCacheItems pItems = null, cPropertyFetchConfiguration pConfiguration = null)
        {
            Client.Fetch(pHandles, pItems ?? Client.DefaultCacheItems, pConfiguration);
            return ZMessages(pHandles);
        }

        public async Task<List<cMessage>> MessagesAsync(IEnumerable<iMessageHandle> pHandles, cCacheItems pItems = null, cPropertyFetchConfiguration pConfiguration = null)
        {
            await Client.FetchAsync(pHandles, pItems ?? Client.DefaultCacheItems, pConfiguration).ConfigureAwait(false);
            return ZMessages(pHandles);
        }

        private List<cMessage> ZMessages(IEnumerable<iMessageHandle> pHandles)
        {
            List<cMessage> lMessages = new List<cMessage>();
            foreach (var lHandle in pHandles) lMessages.Add(new cMessage(Client, lHandle));
            return lMessages;
        }

        public cMessageHandleList SetUnseen() => Client.SetUnseen(Handle);
        public Task<cMessageHandleList> SetUnseenAsync() => Client.SetUnseenAsync(Handle);

        public cMessage Message(cUID pUID, cCacheItems pItems) => Client.Message(Handle, pUID, pItems);
        public Task<cMessage> MessageAsync(cUID pUID, cCacheItems pItems) => Client.MessageAsync(Handle, pUID, pItems);
        public List<cMessage> Messages(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pUIDs, pItems, pConfiguration);
        public Task<List<cMessage>> MessagesAsync(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pUIDs, pItems, pConfiguration);

        public void Fetch(fMailboxCacheDataSets pDataSets) => Client.Fetch(Handle, pDataSets);
        public Task FetchAsync(fMailboxCacheDataSets pDataSets) => Client.FetchAsync(Handle, pDataSets);

        public cCopyFeedback Copy(IEnumerable<cMessage> pMessages) => Client.Copy(cMessageHandleList.FromMessages(pMessages), Handle);
        public Task<cCopyFeedback> CopyAsync(IEnumerable<cMessage> pMessages) => Client.CopyAsync(cMessageHandleList.FromMessages(pMessages), Handle);

        public void UIDFetch(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetch(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);
        public Task UIDFetchAsync(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetchAsync(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);

        public cUIDStoreFeedback UIDStore(cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);
        public Task<cUIDStoreFeedback> UIDStoreAsync(cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);
        public cUIDStoreFeedback UIDStore(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);
        public Task<cUIDStoreFeedback> UIDStoreAsync(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        public cCopyFeedback UIDCopy(cUID pUID, cMailbox pDestination) => Client.UIDCopy(Handle, pUID, pDestination.Handle);
        public Task<cCopyFeedback> UIDCopyAsync(cUID pUID, cMailbox pDestination) => Client.UIDCopyAsync(Handle, pUID, pDestination.Handle);
        public cCopyFeedback UIDCopy(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopy(Handle, pUIDs, pDestination.Handle);
        public Task<cCopyFeedback> UIDCopyAsync(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopyAsync(Handle, pUIDs, pDestination.Handle);

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({Handle})";
    }
}