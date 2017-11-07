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
        /// <para>The mailbox name including the full hierarchy.</para>
        /// </summary>
        public string Path => Handle.MailboxName.Path;

        /// <summary>
        /// <para>The hierarchy delimiter used in <see cref="Path"/>.</para>
        /// </summary>
        public char? Delimiter => Handle.MailboxName.Delimiter;

        /// <summary>
        /// <para>The path of the parent mailbox.</para>
        /// <para>Will be null if there is no parent mailbox.</para>
        /// </summary>
        public string ParentPath => Handle.MailboxName.ParentPath;

        /// <summary>
        /// <para>The name of the mailbox.</para>
        /// <para>As compared to <see cref="Path"/> this does not include the hierarchy.</para>
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
        /// <para>True if the mailbox can be selected.</para>
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
        /// <para>If true the mailbox was marked with the IMAP \All flag indicating that the mailbox contains all messages.</para>
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
        /// <para>If true the mailbox was marked with the IMAP \Archive flag indicating that the mailbox contains the message archive.</para>
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
        /// <para>If true the mailbox was marked with the IMAP \Drafts flag indicating that the mailbox contains draft messages.</para>
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
        /// <para>If true the mailbox was marked with the IMAP \Flagged flag indicating that the mailbox contains flagged messages.</para>
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
        /// <para>If true the mailbox was marked with the IMAP \Junk flag indicating that the mailbox contains junk mail.</para>
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
        /// <para>If true the mailbox was marked with the IMAP \Sent flag indicating that the mailbox contains copies of messages that have been sent.</para>
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
        /// <para>If true the mailbox was marked with the IMAP \Trash flag indicating that the mailbox contains copies of messages that are deleted.</para>
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
        /// <para>Indicates if this mailbox is subscribed to.</para>
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
        /// <para>This is the number of messages that arrived since the mailbox was opened for which the library has not seen the value of the UID.</para>
        /// <para>Indicates how inaccurate the <see cref="UIDNext"/> is.</para>
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
        /// <para>To initialise the value of this property value when the mailbox is selected use <see cref="SetUnseenCount"/>.</para>
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
        /// <para>This is the number of messages for which the library is unsure of the value of the IMAP \Seen flag.</para>
        /// <para>Indicates how inaccurate the <see cref="UnseenCount"/> is.</para>
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
        /// <para>Indicates if the mailbox does not have persistent UIDs.</para>
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

        /// <summary>
        /// <para>Gets the mailbox's subscribed child mailboxes.</para>
        /// <para>Note that mailboxes that do not currently exist may be returned.</para>
        /// </summary>
        /// <param name="pDescend">If true all descendants are returned (not just children, also grandchildren ...)</param>
        /// <param name="pDataSets">
        /// <para>The sets of data to retrieve when getting the child mailboxes.</para>
        /// <para>See <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </param>
        /// <returns>A list of mailboxes.</returns>
        public List<cMailbox> Subscribed(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(Handle, pDescend, pDataSets);

        /// <summary>
        /// The async version of <see cref="Subscribed(bool, fMailboxCacheDataSets)"/>.
        /// </summary>
        /// <param name="pDescend"></param>
        /// <param name="pDataSets"></param>
        /// <returns></returns>
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(Handle, pDescend, pDataSets);

        /// <summary>
        /// <para>Creates a child mailbox of this mailbox.</para>
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the IMAP server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns>The newly created mailbox</returns>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pName), pAsFutureParent);

        /// <summary>
        /// The async version of <see cref="CreateChild(string, bool)"/>.
        /// </summary>
        /// <param name="pName"></param>
        /// <param name="pAsFutureParent"></param>
        /// <returns></returns>
        public Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent = true) => Client.CreateAsync(ZCreateChild(pName), pAsFutureParent);

        private cMailboxName ZCreateChild(string pName)
        {
            if (Handle.MailboxName.Delimiter == null) throw new InvalidOperationException();
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (pName.IndexOf(Handle.MailboxName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(Handle.MailboxName.Path + Handle.MailboxName.Delimiter.Value + pName, Handle.MailboxName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        /// <summary>
        /// <para>Subscribe to this mailbox.</para>
        /// </summary>
        public void Subscribe() => Client.Subscribe(Handle);

        /// <summary>
        /// <para>Subscribe to this mailbox.</para>
        /// </summary>
        /// <returns></returns>
        public Task SubscribeAsync() => Client.SubscribeAsync(Handle);

        /// <summary>
        /// <para>Unsubscribe from this mailbox.</para>
        /// </summary>
        public void Unsubscribe() => Client.Unsubscribe(Handle);

        /// <summary>
        /// <para>Unsubscribe from this mailbox.</para>
        /// </summary>
        public Task UnsubscribeAsync() => Client.UnsubscribeAsync(Handle);
    
        /// <summary>
        /// <para>Change the name of this mailbox.</para>
        /// <para>Note that this leaves the mailbox in its containing mailbox, just changing the last part of the path hierarchy.</para>
        /// </summary>
        /// <param name="pName">The new mailbox name.</param>
        /// <returns>The newly created mailbox.</returns>
        public cMailbox Rename(string pName) => Client.Rename(Handle, ZRename(pName));

        /// <summary>
        /// The async version of <see cref="Rename(string)"/>.
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        public Task<cMailbox> RenameAsync(string pName) => Client.RenameAsync(Handle, ZRename(pName));

        private cMailboxName ZRename(string pName)
        {
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (Handle.MailboxName.Delimiter == null) return new cMailboxName(pName, null);
            if (pName.IndexOf(Handle.MailboxName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(Handle.MailboxName.ParentPath + Handle.MailboxName.Delimiter + pName, Handle.MailboxName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        /* TODO!
        public cMailbox Rename(cNamespace pNamespace, string pName = null)
        {
            ;?;
        }

        public cMailbox Rename(cMailbox pMailbox, string pName = null)
        {
            ;?;
        } */

        /// <summary>
        /// <para>Delete this mailbox.</para>
        /// </summary>
        public void Delete() => Client.Delete(Handle);

        /// <summary>
        /// <para>Delete this mailbox.</para>
        /// </summary>
        public Task DeleteAsync() => Client.DeleteAsync(Handle);

        /// <summary>
        /// <para>Select this mailbox.</para>
        /// <para>Selecting a mailbox un-selects the previously selected mailbox (if there was one).</para>
        /// </summary>
        /// <param name="pForUpdate">Indicates if the mailbox should be selected for update or not</param>
        public void Select(bool pForUpdate = false) => Client.Select(Handle, pForUpdate);

        /// <summary>
        /// The async version of <see cref="Select(bool)"/>.
        /// </summary>
        /// <param name="pForUpdate"></param>
        /// <returns></returns>
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(Handle, pForUpdate);

        /// <summary>
        /// <para>Expunge messages marked with the deleted flag (see <see cref="cMessage.Deleted"/>) from the mailbox.</para>
        /// <para>Setting <paramref name="pAndUnselect"/> to true also un-selects the mailbox. This reduces the amount of network activity associated with the expunge.</para>
        /// </summary>
        /// <param name="pAndUnselect">Indicates if the mailbox should also be un-selected.</param>
        public void Expunge(bool pAndUnselect = false) => Client.Expunge(Handle, pAndUnselect);

        /// <summary>
        /// The async version of <see cref="Expunge(bool)"/>.
        /// </summary>
        /// <param name="pAndUnselect"></param>
        /// <returns></returns>
        public Task ExpungeAsync(bool pAndUnselect = false) => Client.ExpungeAsync(Handle, pAndUnselect);

        /// <summary>
        /// <para>Get a list of messages contained in the mailbox from the server.</para>
        /// </summary>
        /// <param name="pFilter">
        /// <para>The filter to use to restrict the set of messages returned.</para>
        /// <para>Use the static members and operators of the <see cref="cFilter"/> class to create an optional message filter.</para>
        /// </param>
        /// <param name="pSort">
        /// <para>The sort to use to order the set of messages returned.</para>
        /// <para>Use the static members of the <see cref="cSortItem"/> class as parameters to a <see cref="cSort"/> constructor to create an optional sort specification.</para>
        /// <para>If not specified the default (<see cref="cIMAPClient.DefaultSort"/>) will be used.</para>
        /// </param>
        /// <param name="pItems">
        /// <para>The set of message cache items to ensure are cached for the returned messages.</para>
        /// <para>If not specified the default (<see cref="cIMAPClient.DefaultCacheItems"/>) will be used.</para>
        /// </param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns>A list of messages.</returns>
        public List<cMessage> Messages(cFilter pFilter = null, cSort pSort = null, cCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultCacheItems, pConfiguration);

        /// <summary>
        /// The async version of <see cref="Messages(cFilter, cSort, cCacheItems, cMessageFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pFilter"></param>
        /// <param name="pSort"></param>
        /// <param name="pItems"></param>
        /// <param name="pConfiguration"></param>
        /// <returns></returns>
        public Task<List<cMessage>> MessagesAsync(cFilter pFilter = null, cSort pSort = null, cCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultCacheItems, pConfiguration);

        /// <summary>
        /// <para>Get a list of messages from a set of handles.</para>
        /// <para>Useful when handling the <see cref="MessageDelivery"/> event.</para>
        /// </summary>
        /// <param name="pHandles">A set of message handles.</param>
        /// <param name="pItems">
        /// <para>The set of message cache items to ensure are cached for the returned messages.</para>
        /// <para>If not specified the default (<see cref="cIMAPClient.DefaultCacheItems"/>) will be used.</para>
        /// </param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns>A list of messages where the cache does NOT contain the requested items (i.e. where the fetch failed).</returns>
        public List<cMessage> Messages(IEnumerable<iMessageHandle> pHandles, cCacheItems pItems = null, cPropertyFetchConfiguration pConfiguration = null)
        {
            Client.Fetch(pHandles, pItems ?? Client.DefaultCacheItems, pConfiguration);
            return ZMessages(pHandles);
        }

        /// <summary>
        /// The async version of <see cref="Messages(IEnumerable{iMessageHandle}, cCacheItems, cPropertyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pHandles"></param>
        /// <param name="pItems"></param>
        /// <param name="pConfiguration"></param>
        /// <returns></returns>
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

        /// <summary>
        /// <para>When the mailbox is selected use this method to initialise the <see cref="UnseenCount"/>.</para>
        /// <para>IMAP does not have a mechanism for getting the unseencount when the mailbox is selected.</para>
        /// <para>Once the value is initialised it needs to be maintained by fetching the flags of newly arrived messages.</para>
        /// <para>You need to handle the <see cref="MessageDelivery"/> event and use the <see cref="Messages(IEnumerable{iMessageHandle}, cCacheItems, cPropertyFetchConfiguration)"/> method to achieve this.</para>
        /// </summary>
        /// <returns>A list of unseen message handles.</returns>
        public cMessageHandleList SetUnseenCount() => Client.SetUnseenCount(Handle);

        /// <summary>
        /// The async version of <see cref="SetUnseenCount"/>.
        /// </summary>
        /// <returns></returns>
        public Task<cMessageHandleList> SetUnseenCountAsync() => Client.SetUnseenCountAsync(Handle);

        /// <summary>
        /// <para>Resolve a UID to a message instance and ensure that the specified items are cached.</para>
        /// </summary>
        /// <param name="pUID">The UID to resolve.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <returns>A message</returns>
        public cMessage Message(cUID pUID, cCacheItems pItems) => Client.Message(Handle, pUID, pItems);

        /// <summary>
        /// The async version of <see cref="Message(cUID, cCacheItems)"/>.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pItems"></param>
        /// <returns></returns>
        public Task<cMessage> MessageAsync(cUID pUID, cCacheItems pItems) => Client.MessageAsync(Handle, pUID, pItems);

        /// <summary>
        /// <para>Resolve a set of UIDs to message instances and ensure that the specified items are cached.</para>
        /// </summary>
        /// <param name="pUIDs">The UIDs to resolve.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns>A list of messages</returns>
        public List<cMessage> Messages(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pUIDs, pItems, pConfiguration);

        /// <summary>
        /// The async version of <see cref="MessagesAsync(IEnumerable{cUID}, cCacheItems, cPropertyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pItems"></param>
        /// <param name="pConfiguration"></param>
        /// <returns></returns>
        public Task<List<cMessage>> MessagesAsync(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pUIDs, pItems, pConfiguration);

        /// <summary>
        /// <para>Refresh the mailbox cache data for this mailbox.</para>
        /// </summary>
        /// <param name="pDataSets">The sets of data to refresh.</param>
        public void Fetch(fMailboxCacheDataSets pDataSets) => Client.Fetch(Handle, pDataSets);

        /// <summary>
        /// The async version of <see cref="Fetch(fMailboxCacheDataSets)"/>.
        /// </summary>
        /// <param name="pDataSets"></param>
        /// <returns></returns>
        public Task FetchAsync(fMailboxCacheDataSets pDataSets) => Client.FetchAsync(Handle, pDataSets);

        /// <summary>
        /// <para>Copy a set of messages to this mailbox.</para>
        /// <para>The source messages must be in the currently selected mailbox.</para>
        /// <para>If the server provides the UIDCOPY response code of RFC 4315 pairs of UIDs of the copied messages are returned.</para>
        /// </summary>
        /// <param name="pMessages">The set of messages to copy.</param>
        /// <returns>If the server provides a UIDCOPY response: the pairs of UIDs for the copied messages; otherwise null.</returns>
        public cCopyFeedback Copy(IEnumerable<cMessage> pMessages) => Client.Copy(cMessageHandleList.FromMessages(pMessages), Handle);

        /// <summary>
        /// The async version of <see cref="Copy(IEnumerable{cMessage})"/>.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <returns></returns>
        public Task<cCopyFeedback> CopyAsync(IEnumerable<cMessage> pMessages) => Client.CopyAsync(cMessageHandleList.FromMessages(pMessages), Handle);

        /// <summary>
        /// <para>Fetch a section of a message into a stream.</para>
        /// <para>This mailbox must be selected.</para>
        /// <para>Will throw if the <paramref name="pUID"/> does not exist in the mailbox.</para>
        /// </summary>
        /// <param name="pUID">The UID of the message.</param>
        /// <param name="pSection">The section of the message to fetch.</param>
        /// <param name="pDecoding">
        /// <para>What decoding should be applied to the fetched data.</para>
        /// <para>If the connected server supports RFC 3516 and the entire part (<see cref="eSectionTextPart.all"/>) is being fetched then this may be <see cref="eDecodingRequired.unknown"/> to get the server to do the decoding.</para>
        /// </param>
        /// <param name="pStream">The stream to write the (decoded) data into.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        public void UIDFetch(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetch(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// The async version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pSection"></param>
        /// <param name="pDecoding"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration"></param>
        /// <returns></returns>
        public Task UIDFetchAsync(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetchAsync(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// <para>Store flags for a message.</para>
        /// <para>This mailbox must be selected.</para>
        /// </summary>
        /// <param name="pUID">The UID of the message.</param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags">The flags to store.</param>
        /// <param name="pIfUnchangedSinceModSeq">
        /// <para>The modseq to use in the unchangedsince clause of a conditional store (RFC 7162).</para>
        /// <para>Can only be specified if the mailbox supports RFC 7162.</para>
        /// <para>If the message has been modified since the specified modseq the server should fail the update.</para>
        /// </param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        public cUIDStoreFeedback UIDStore(cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// The async version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public Task<cUIDStoreFeedback> UIDStoreAsync(cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// The multiple message version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public cUIDStoreFeedback UIDStore(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// The async multiple message version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public Task<cUIDStoreFeedback> UIDStoreAsync(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// <para>Copy a message to another mailbox.</para>
        /// <para>This mailbox must be selected.</para>
        /// <para>If the server provides the UIDCOPY response code of RFC 4315 pairs of UIDs of the copied messages are returned.</para>
        /// </summary>
        /// <param name="pUID">The UID of the message to copy.</param>
        /// <param name="pDestination">The destination mailbox.</param>
        /// <returns>If the server provides a UIDCOPY response: the pairs of UIDs for the copied messages; otherwise null.</returns>
        public cCopyFeedback UIDCopy(cUID pUID, cMailbox pDestination) => Client.UIDCopy(Handle, pUID, pDestination.Handle);

        /// <summary>
        /// The async version of <see cref="UIDCopy(cUID, cMailbox)"/>.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pDestination"></param>
        /// <returns></returns>
        public Task<cCopyFeedback> UIDCopyAsync(cUID pUID, cMailbox pDestination) => Client.UIDCopyAsync(Handle, pUID, pDestination.Handle);

        /// <summary>
        /// The multiple message version of <see cref="UIDCopy(cUID, cMailbox)"/>.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <returns></returns>
        public cCopyFeedback UIDCopy(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopy(Handle, pUIDs, pDestination.Handle);

        /// <summary>
        /// The async multiple message version of <see cref="UIDCopy(cUID, cMailbox)"/>.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <returns></returns>
        public Task<cCopyFeedback> UIDCopyAsync(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopyAsync(Handle, pUIDs, pDestination.Handle);

        // blah
        public override string ToString() => $"{nameof(cMailbox)}({Handle})";
    }
}