using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Provides an API that allows interaction with an IMAP mailbox.
    /// </summary>
    /// <remarks>
    /// Instances are only valid whilst the containing <see cref="cIMAPClient"/> remains connected. Reconnecting the client will not bring mailbox instances back to life.
    /// </remarks>
    /// <seealso cref="cIMAPClient.Inbox"/>
    /// <seealso cref="cIMAPClient.SelectedMailbox"/>
    /// <seealso cref="cIMAPClient.Mailbox(cMailboxName)"/>
    /// <seealso cref="cIMAPClient.Mailboxes(string, char?, fMailboxCacheDataSets)"/>
    /// <seealso cref="cIMAPClient.Subscribed(string, char?, bool, fMailboxCacheDataSets)"/>
    /// <seealso cref="cNamespace.Mailboxes(fMailboxCacheDataSets)"/>
    /// <seealso cref="cNamespace.Subscribed(bool, fMailboxCacheDataSets)"/>
    public class cMailbox : iMailboxParent
    {
        private PropertyChangedEventHandler mPropertyChanged;
        private object mPropertyChangedLock = new object();

        private EventHandler<cMessageDeliveryEventArgs> mMessageDelivery;
        private object mMessageDeliveryLock = new object();

        /**<summary>The client that this instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The internal mailbox cache item that this instance is attached to.</summary>*/
        public readonly iMailboxHandle Handle;

        internal cMailbox(cIMAPClient pClient, iMailboxHandle pHandle)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
        }

        /// <summary>
        /// Fired when the server notifies the client of a mailbox property value change.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="cIMAPClient.SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
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
        /// Fired when the server notifies the client that messages have arrived in the mailbox.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="cIMAPClient.SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
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
        /// <para>Gets the mailbox name including the full hierarchy.</para>
        /// </summary>
        public string Path => Handle.MailboxName.Path;

        /// <summary>
        /// <para>Gets the hierarchy delimiter used in <see cref="Path"/>.</para>
        /// </summary>
        public char? Delimiter => Handle.MailboxName.Delimiter;

        /// <summary>
        /// Gets the path of the parent mailbox. Will be null if there is no parent mailbox.
        /// </summary>
        public string ParentPath => Handle.MailboxName.ParentPath;

        /// <summary>
        /// Gets the name of the mailbox. As compared to <see cref="Path"/> this does not include the hierarchy.
        /// </summary>
        /// 
        public string Name => Handle.MailboxName.Name;

        /// <summary>
        /// Determines if this instance represents the INBOX.
        /// </summary>
        public bool IsInbox => Handle.MailboxName.IsInbox;

        /// <summary>
        /// Indicates if the mailbox exists on the server. Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist. Mailboxes can also be deleted.
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
        /// Indicates if the mailbox can definitely not contain child mailboxes. See the IMAP \Noinferiors flag. May be null if the mailbox does not exist.
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
        /// Indicates if the mailbox can be selected. See the IMAP \Noselect flag.
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
        /// Indicates if the mailbox has been marked "interesting" by the server. Null indicates that the server didn't say either way. See the IMAP \Marked and \Unmarked flags.
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
        /// Indicates if the mailbox is a remote mailbox. May be null if this can't be determined.
        /// </summary>
        /// <remarks>
        /// Null will be returned under the following set of circumstances;
        /// <list type="bullet">
        /// <item>The instance has <see cref="cIMAPClient.MailboxReferrals"/> set to true.</item>
        /// <item>The connected server supports <see cref="cCapabilities.MailboxReferrals"/>.</item>
        /// <item>The connected server does not support <see cref="cCapabilities.ListExtended"/>.</item>
        /// </list>
        /// Under these circumstances it is not possible to reliably determine if the mailbox is remote or not.
        /// </remarks>
        public bool? IsRemote
        {
            get
            {
                if (Handle.Exists == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.IsRemote) return true;
                if (Client.Capabilities.ListExtended) return false;
                if (Client.MailboxReferrals) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if the mailbox had children when the property was refreshed. Null indicates that the server didn't say either way. See the IMAP \HasChildren and \HasNoChildren flags.
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
        /// Indicates if the mailbox contains all messages (see the <see cref="cCapabilities.SpecialUse"/> \All flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the mailbox contains the message archive (see the <see cref="cCapabilities.SpecialUse"/> \Archive flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the mailbox contains message drafts (see the <see cref="cCapabilities.SpecialUse"/> \Drafts flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the mailbox contains flagged messages (see the <see cref="cCapabilities.SpecialUse"/> \Flagged flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the mailbox contains junk mail (see the <see cref="cCapabilities.SpecialUse"/> \Junk flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the mailbox contains sent messages (see the <see cref="cCapabilities.SpecialUse"/> \Sent flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the mailbox contains deleted or to-be deleted messages (see the <see cref="cCapabilities.SpecialUse"/> \Trash flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if this mailbox is subscribed to or not.
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
        /// Gets the number of messages in the mailbox. 
        /// Null indicates that <see cref="fMailboxCacheData.messagecount"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or the value was not sent.
        /// This property always has an up-to-date value when the mailbox is selected.
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
        /// Gets the number of recent messages in the mailbox. 
        /// Null indicates that <see cref="fMailboxCacheData.recentcount"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or the value was not sent.
        /// This property always has an up-to-date value when the mailbox is selected.
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
        /// Gets the predicted next UID for the mailbox. 
        /// Null indicates that <see cref="fMailboxCacheData.uidnext"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or the value was not sent.
        /// This property always has a value when the mailbox is selected, but it may not be up to date.
        /// </summary>
        /// <remarks>
        /// <para>When the mailbox is selected, zero indicates that the value is unknown.</para>
        /// <para>While the mailbox is selected the value of this property may not be up-to-date: see the value of <see cref="UIDNextUnknownCount"/> for the potential inaccuracy in this property value.</para>
        /// <para>IMAP does not mandate that the server keep the client updated with this value while the mailbox is selected but also disallows retrieving the value while the mailbox is selected.</para>
        /// <para>While the mailbox is selected the library internally maintains the value of this property by monitoring IMAP FETCH responses from the server, but these responses have to be explicitly requested.</para>
        /// <para>If it is important to you that the value of this property be accurate when the mailbox is selected then you must fetch the <see cref="fCacheAttributes.uid"/> for new messages as they arrive.</para>
        /// <para>See <see cref="MessageDelivery"/>, <see cref="Messages(IEnumerable{iMessageHandle}, cCacheItems, cPropertyFetchConfiguration)"/> and <see cref="cCacheItems"/>.</para>
        /// </remarks>
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
        /// Gets the number of messages that have arrived since the mailbox was selected for which the library has not seen the value of the UID.
        /// Indicates how inaccurate the <see cref="UIDNext"/> is.
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
        /// Gets the UIDValidity of the mailbox.
        /// Null indicates that the mailbox does not support UIDs or that the <see cref="fMailboxCacheData.uidvalidity"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or that the value was not sent.
        /// This property always has a value when the mailbox is selected; zero indicates that the server does not support UIDs.
        /// </summary>
        /// <seealso cref="UIDNotSticky"/>
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
        /// Gets the number of unseen messages in the mailbox.
        /// Null indicates that <see cref="fMailboxCacheData.unseencount"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or the value was not sent.
        /// This property always has a value when the mailbox is selected, but it may not be up to date.
        /// </summary>
        /// <remarks>
        /// <para>While the mailbox is selected the value of this property may not be up-to-date: see the value of <see cref="UnseenUnknownCount"/> for the potential inaccuracy in this property value.</para>
        /// <para>IMAP does not provide a mechanism for getting this value while the mailbox is selected.</para>
        /// <para>While the mailbox is selected the library internally maintains the value of this property by monitoring IMAP FETCH responses from the server, but the property value has to be explicitly initialised and the FETCH responses have to be explicitly requested.</para>
        /// <para>If it is important to you that the value of this property be accurate when the mailbox is selected then you must initialise the value by using <see cref="SetUnseenCount"/> and also fetch the <see cref="fCacheAttributes.flags"/> for new messages as they arrive.</para>
        /// <para>See <see cref="MessageDelivery"/>, <see cref="Messages(IEnumerable{iMessageHandle}, cCacheItems, cPropertyFetchConfiguration)"/> and <see cref="cCacheItems"/>.</para>
        /// </remarks>
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
        /// Gets the number of messages for which the library is unsure of the value of the <see cref="kMessageFlagName.Seen"/> flag.
        /// Indicates how inaccurate the <see cref="UnseenCount"/> is.
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
        /// Gets the modification sequence number for the mailbox. 
        /// See RFC 7162.
        /// Null indicates that <see cref="fMailboxCacheData.highestmodseq"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or the value was not sent.
        /// When the mailbox is selected this property will always have a value but zero indicates that <see cref="cCapabilities.CondStore"/> is not supported on the mailbox.
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
        /// Indicates if the mailbox has been selected once in this session.
        /// </summary>
        public bool HasBeenSelected => Handle.SelectedProperties.HasBeenSelected;

        /// <summary>
        /// Indicates if the mailbox has been selected for update once in this session.
        /// </summary>
        public bool HasBeenSelectedForUpdate => Handle.SelectedProperties.HasBeenSelectedForUpdate;

        /// <summary>
        /// Indicates if the mailbox has been selected readonly once in this session.
        /// </summary>
        public bool HasBeenSelectedReadOnly => Handle.SelectedProperties.HasBeenSelectedReadOnly;

        /// <summary>
        /// Indicates if the mailbox does not have persistent UIDs (see RFC 4315). Null if the mailbox has never been selected in this session.
        /// </summary>
        public bool? UIDNotSticky => Handle.SelectedProperties.UIDNotSticky;

        /// <summary>
        /// Gets the defined flags in the mailbox. Null if the mailbox has never been selected in this session.
        /// </summary>
        public cFetchableFlags MessageFlags
        {
            get
            {
                var lSelectedProperties = Handle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelected) return null;
                return lSelectedProperties.MessageFlags;
            }
        }

        /// <summary>
        /// Gets the flags that the client can change permanently in this mailbox when it is selected for update. Null if the mailbox has never been selected for update in this session.
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
        /// Gets the flags that the client can change permanently in this mailbox when it is selected readonly. Null if the mailbox has never been selected read-only in this session.
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
        /// Indicates if the mailbox is currently the selected mailbox.
        /// </summary>
        public bool IsSelected => ReferenceEquals(Client.SelectedMailboxDetails?.Handle, Handle);

        /// <summary>
        /// Indicates if the mailbox is currently selected for update.
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
        /// Indicates if the mailbox is currently selected but the mailbox can't be modified.
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
        /// Gets the mailbox's child mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(Handle, pDataSets);

        /// <summary>
        /// Asynchronously gets the mailbox's child mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(Handle, pDataSets);

        /// <summary>
        /// Gets the mailbox's subscribed child mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If true all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public List<cMailbox> Subscribed(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(Handle, pDescend, pDataSets);

        /// <summary>
        /// Asynchronously gets the mailbox's subscribed child mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If true all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(Handle, pDescend, pDataSets);

        /// <summary>
        /// Creates a child mailbox of this mailbox.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pName), pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a child mailbox of this mailbox.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
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
        /// Subscribes to this mailbox.
        /// </summary>
        public void Subscribe() => Client.Subscribe(Handle);

        /// <summary>
        /// Asynchronously subscribes to this mailbox.
        /// </summary>
        /// <returns></returns>
        public Task SubscribeAsync() => Client.SubscribeAsync(Handle);

        /// <summary>
        /// Unsubscribes from this mailbox.
        /// </summary>
        public void Unsubscribe() => Client.Unsubscribe(Handle);

        /// <summary>
        /// Asynchronously unsubscribes from this mailbox.
        /// </summary>
        public Task UnsubscribeAsync() => Client.UnsubscribeAsync(Handle);
    
        /// <summary>
        /// Changes the name of this mailbox.
        /// Note that this method leaves the mailbox in its containing mailbox, just changing the last part of the path hierarchy.
        /// </summary>
        /// <param name="pName">The new mailbox name.</param>
        /// <returns></returns>
        public cMailbox Rename(string pName) => Client.Rename(Handle, ZRename(pName));

        /// <summary>
        /// Ansynchronously changes the name of this mailbox.
        /// Note that this method leaves the mailbox in its containing mailbox, just changing the last part of the path hierarchy.
        /// </summary>
        /// <param name="pName">The new mailbox name.</param>
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
        /// Deletes this mailbox.
        /// </summary>
        public void Delete() => Client.Delete(Handle);

        /// <summary>
        /// Asynchronously deletes this mailbox.
        /// </summary>
        public Task DeleteAsync() => Client.DeleteAsync(Handle);

        ;?;

        /// <summary>
        /// <para>Select this mailbox.</para>
        /// <para>Selecting a mailbox un-selects the previously selected mailbox (if there was one).</para>
        /// </summary>
        /// <param name="pForUpdate">Indicates if the mailbox should be selected for update or not</param>
        public void Select(bool pForUpdate = false) => Client.Select(Handle, pForUpdate);
        /**<summary>The async version of <see cref="Select(bool)"/>.</summary>*/
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(Handle, pForUpdate);

        /// <summary>
        /// <para>Expunge messages marked with the deleted flag (see <see cref="cMessage.Deleted"/>) from the mailbox.</para>
        /// <para>Setting <paramref name="pAndUnselect"/> to true also un-selects the mailbox. This reduces the amount of network activity associated with the expunge.</para>
        /// </summary>
        /// <param name="pAndUnselect">Indicates if the mailbox should also be un-selected.</param>
        public void Expunge(bool pAndUnselect = false) => Client.Expunge(Handle, pAndUnselect);
        /**<summary>The async version of <see cref="Expunge(bool)"/>.</summary>*/
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
        /**<summary>The async version of <see cref="Messages(cFilter, cSort, cCacheItems, cMessageFetchConfiguration)"/>.</summary>*/
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

        /**<summary>The async version of <see cref="Messages(IEnumerable{iMessageHandle}, cCacheItems, cPropertyFetchConfiguration)"/>.</summary>*/
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
        /**<summary>The async version of <see cref="SetUnseenCount"/>.</summary>*/
        public Task<cMessageHandleList> SetUnseenCountAsync() => Client.SetUnseenCountAsync(Handle);

        /// <summary>
        /// <para>Resolve a UID to a message instance and ensure that the specified items are cached.</para>
        /// </summary>
        /// <param name="pUID">The UID to resolve.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <returns>An object representing the message.</returns>
        public cMessage Message(cUID pUID, cCacheItems pItems) => Client.Message(Handle, pUID, pItems);
        /**<summary>The async version of <see cref="Message(cUID, cCacheItems)"/>.</summary>*/
        public Task<cMessage> MessageAsync(cUID pUID, cCacheItems pItems) => Client.MessageAsync(Handle, pUID, pItems);

        /// <summary>
        /// <para>Resolve a set of UIDs to message instances and ensure that the specified items are cached.</para>
        /// </summary>
        /// <param name="pUIDs">The UIDs to resolve.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns>A list of messages.</returns>
        public List<cMessage> Messages(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pUIDs, pItems, pConfiguration);
        /**<summary>The async version of <see cref="MessagesAsync(IEnumerable{cUID}, cCacheItems, cPropertyFetchConfiguration)"/>.</summary>*/
        public Task<List<cMessage>> MessagesAsync(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pUIDs, pItems, pConfiguration);

        /// <summary>
        /// <para>Refresh the mailbox cache data for this mailbox.</para>
        /// </summary>
        /// <param name="pDataSets">The sets of data to refresh.</param>
        public void Fetch(fMailboxCacheDataSets pDataSets) => Client.Fetch(Handle, pDataSets);
        /**<summary>The async version of <see cref="Fetch(fMailboxCacheDataSets)"/>.</summary>*/
        public Task FetchAsync(fMailboxCacheDataSets pDataSets) => Client.FetchAsync(Handle, pDataSets);

        /// <summary>
        /// <para>Copy a set of messages to this mailbox.</para>
        /// <para>The source messages must be in the currently selected mailbox.</para>
        /// <para>If the server provides the UIDCOPY response code of RFC 4315 pairs of UIDs of the copied messages are returned.</para>
        /// </summary>
        /// <param name="pMessages">The set of messages to copy.</param>
        /// <returns>If the server provides a UIDCOPY response: the pairs of UIDs for the copied messages; otherwise null.</returns>
        public cCopyFeedback Copy(IEnumerable<cMessage> pMessages) => Client.Copy(cMessageHandleList.FromMessages(pMessages), Handle);
        /**<summary>The async version of <see cref="Copy(IEnumerable{cMessage})"/>.</summary>*/
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
        /**<summary>The async version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.</summary>*/
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
        public cUIDStoreFeedback UIDStore(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);
        /**<summary>The async version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.</summary>*/
        public Task<cUIDStoreFeedback> UIDStoreAsync(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// The multiple message version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public cUIDStoreFeedback UIDStore(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// The async multiple message version of <see cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public Task<cUIDStoreFeedback> UIDStoreAsync(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// <para>Copy a message to another mailbox.</para>
        /// <para>This mailbox must be selected.</para>
        /// <para>If the server provides the UIDCOPY response code of RFC 4315 pairs of UIDs of the copied messages are returned.</para>
        /// </summary>
        /// <param name="pUID">The UID of the message to copy.</param>
        /// <param name="pDestination">The destination mailbox.</param>
        /// <returns>If the server provides a UIDCOPY response: the pairs of UIDs for the copied messages; otherwise null.</returns>
        public cCopyFeedback UIDCopy(cUID pUID, cMailbox pDestination) => Client.UIDCopy(Handle, pUID, pDestination.Handle);
        /**<summary>The async version of <see cref="UIDCopy(cUID, cMailbox)"/>.</summary>*/
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