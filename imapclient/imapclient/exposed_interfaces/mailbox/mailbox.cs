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
    /// <para>Instances are only valid whilst the containing <see cref="cIMAPClient"/> remains connected. Reconnecting the client will not bring mailbox instances back to life.</para>
    /// <para>
    /// To interact with messages in a mailbox, IMAP requires that the mailbox be selected - use the <see cref="Select(bool)"/> method of this class to select the mailbox associated with the instance.
    /// Each IMAP connection (and hence each <see cref="cIMAPClient"/> instance) can have at most one selected mailbox – selecting a mailbox automatically un-selects the previously selected mailbox.
    /// An instance of this class may be selected and un-selected many times in its lifetime.
    /// Each time an instance is selected a new ‘select session’ is started.
    /// <see cref="cMessage"/> instances are valid only within a 'select session'.
    /// </para>
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
        /// Fired when the server notifies the client of a property value change that affects the mailbox associated with this instance.
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
        /// Fired when the server notifies the client that messages have arrived in the mailbox associated with this instance.
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
        /// Indicates if the associated mailbox exists on the server. Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist. Mailboxes can also be deleted.
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
        /// Indicates if the associated mailbox can definitely not contain child mailboxes. See the IMAP \Noinferiors flag. May be null if the associated mailbox does not exist.
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
        /// Indicates if the associated mailbox can be selected. See the IMAP \Noselect flag.
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
        /// Indicates if the associated mailbox has been marked "interesting" by the server. Null indicates that the server didn't say either way. See the IMAP \Marked and \Unmarked flags.
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
        /// Indicates if the associated mailbox is a remote mailbox. May be null if this can't be determined.
        /// </summary>
        /// <remarks>
        /// Null will be returned under the following set of circumstances;
        /// <list type="bullet">
        /// <item>The instance has <see cref="cIMAPClient.MailboxReferrals"/> set to true.</item>
        /// <item>The connected server supports <see cref="cCapabilities.MailboxReferrals"/>.</item>
        /// <item>The connected server does not support <see cref="cCapabilities.ListExtended"/>.</item>
        /// </list>
        /// Under these circumstances it is not possible to reliably determine if the associated mailbox is remote or not.
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
        /// Indicates if the associated mailbox had children when the property value was last refreshed. Null indicates that the server didn't say either way. See the IMAP \HasChildren and \HasNoChildren flags.
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
        /// Indicates if the associated mailbox contains all messages (see the <see cref="cCapabilities.SpecialUse"/> \All flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the associated mailbox contains the message archive (see the <see cref="cCapabilities.SpecialUse"/> \Archive flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the associated mailbox contains message drafts (see the <see cref="cCapabilities.SpecialUse"/> \Drafts flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the associated mailbox contains flagged messages (see the <see cref="cCapabilities.SpecialUse"/> \Flagged flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the associated mailbox contains junk mail (see the <see cref="cCapabilities.SpecialUse"/> \Junk flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the associated mailbox contains sent messages (see the <see cref="cCapabilities.SpecialUse"/> \Sent flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if the associated mailbox contains deleted or to-be deleted messages (see the <see cref="cCapabilities.SpecialUse"/> \Trash flag). Null indicates that the <see cref="fMailboxCacheData.specialuse"/> flags are not being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.
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
        /// Indicates if this associated mailbox is subscribed to or not.
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
        /// Gets the number of messages in the associated mailbox.
        /// May be null (see <see cref="cIMAPClient.MailboxCacheData"/>).
        /// This property always has an up-to-date value when the associated mailbox is selected.
        /// </summary>
        /// <remarks>
        /// Null indicates that <see cref="fMailboxCacheData.messagecount"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or that the value was not sent when requested.
        /// </remarks>
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
        /// Gets the number of messages in the associated mailbox.
        /// May be null (see <see cref="cIMAPClient.MailboxCacheData"/>).
        /// This property always has an up-to-date value when the associated mailbox is selected.
        /// </summary>
        /// <remarks>
        /// Null indicates that <see cref="fMailboxCacheData.recentcount"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or that the value was not sent when requested.
        /// </remarks>
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
        /// Gets the predicted next UID for the associated mailbox. 
        /// May be null (see <see cref="cIMAPClient.MailboxCacheData"/>).
        /// This property always has a value when the associated mailbox is selected, but it may not be up-to-date.
        /// </summary>
        /// <remarks>
        /// <para>Null indicates that <see cref="fMailboxCacheData.uidnext"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or that the value was not sent when requested.</para>
        /// <para>When the associated mailbox is selected, 0 indicates that the value is unknown.</para>
        /// <para>While the associated mailbox is selected the value of this property may not be up-to-date: see the value of <see cref="UIDNextUnknownCount"/> for the potential inaccuracy in this property value.</para>
        /// <para>IMAP does not mandate that the server keep the client updated with this value while a mailbox is selected but it also disallows retrieving the value for a mailbox while the mailbox is selected.</para>
        /// <para>While the associated mailbox is selected the library internally maintains the value of this property by monitoring IMAP FETCH responses from the server, but these responses have to be explicitly requested.</para>
        /// <para>If it is important to you that the value of this property be accurate when the associated mailbox is selected then you must fetch the <see cref="fCacheAttributes.uid"/> for new messages as they arrive.</para>
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
        /// Gets the number of messages that have arrived since the associated mailbox was selected for which the library has not seen the value of the UID.
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
        /// Gets the UIDValidity of the associated mailbox.
        /// May be null (see <see cref="cIMAPClient.MailboxCacheData"/>).
        /// This property always has a value when the associated mailbox is selected; however a value of 0 indicates that the server does not support UIDs.
        /// </summary>
        /// <remarks>
        /// Null indicates that the associated mailbox does not support UIDs or that the <see cref="fMailboxCacheData.uidvalidity"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or that the value was not sent when requested.
        /// </remarks>
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
        /// May be null (see <see cref="cIMAPClient.MailboxCacheData"/>).
        /// This property always has a value when the mailbox is selected, but it may not be up-to-date.
        /// </summary>
        /// <remarks>
        /// <para>Null indicates that <see cref="fMailboxCacheData.unseencount"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or that the value was not sent when requested.</para>
        /// <para>While the associated mailbox is selected the value of this property may not be up-to-date: see the value of <see cref="UnseenUnknownCount"/> for the potential inaccuracy in this property value.</para>
        /// <para>IMAP does not provide a mechanism for getting this value while a mailbox is selected.</para>
        /// <para>While the associated mailbox is selected the library internally maintains the value of this property by monitoring IMAP FETCH responses from the server, but the property value has to be explicitly initialised and the FETCH responses have to be explicitly requested.</para>
        /// <para>If it is important to you that the value of this property be accurate when the associated mailbox is selected then you must initialise the value by using <see cref="SetUnseenCount"/> and also fetch the <see cref="fCacheAttributes.flags"/> for new messages as they arrive.</para>
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
        /// Gets the modification sequence number for the associated mailbox. 
        /// See RFC 7162.
        /// May be null (see <see cref="cIMAPClient.MailboxCacheData"/>).
        /// When the associated mailbox is selected this property will always have a value but it could be 0.
        /// </summary>
        /// <remarks>
        /// <para>Null indicates that <see cref="fMailboxCacheData.highestmodseq"/> is not being cached (see <see cref="cIMAPClient.MailboxCacheData"/>) or that the value was not sent when requested.</para>
        /// <para>0 indicates that <see cref="cCapabilities.CondStore"/> is not supported on the associated mailbox.</para>
        /// </remarks>
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
        /// Indicates if the associated mailbox has been selected once in this <see cref="cIMAPClient"/> session.
        /// </summary>
        public bool HasBeenSelected => Handle.SelectedProperties.HasBeenSelected;

        /// <summary>
        /// Indicates if the associated mailbox has been selected for update once in this <see cref="cIMAPClient"/> session.
        /// </summary>
        public bool HasBeenSelectedForUpdate => Handle.SelectedProperties.HasBeenSelectedForUpdate;

        /// <summary>
        /// Indicates if the associated mailbox has been selected readonly once in this <see cref="cIMAPClient"/> session.
        /// </summary>
        public bool HasBeenSelectedReadOnly => Handle.SelectedProperties.HasBeenSelectedReadOnly;

        /// <summary>
        /// Indicates if the associated mailbox does not have persistent UIDs (see RFC 4315). Null if the associated mailbox has never been selected in this <see cref="cIMAPClient"/> session.
        /// </summary>
        public bool? UIDNotSticky => Handle.SelectedProperties.UIDNotSticky;

        /// <summary>
        /// Gets the defined flags in the associated mailbox. Null if the associated mailbox has never been selected in this <see cref="cIMAPClient"/> session.
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
        /// Gets the flags that the client can change permanently in the associated mailbox when it is selected for update. Null if the associated mailbox has never been selected in this <see cref="cIMAPClient"/> session.
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
        /// Gets the flags that the client can change permanently in the associated mailbox when it is selected readonly. Null if the associated mailbox has never been selected in this <see cref="cIMAPClient"/> session.
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
        /// Indicates if the associated mailbox is currently the selected mailbox.
        /// </summary>
        public bool IsSelected => ReferenceEquals(Client.SelectedMailboxDetails?.Handle, Handle);

        /// <summary>
        /// Indicates if the associated mailbox is currently selected for update.
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
        /// Indicates if the associated mailbox is currently selected but can't be modified.
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
        /// Gets the associated mailbox's child mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(Handle, pDataSets);

        /// <summary>
        /// Asynchronously gets the associated mailbox's child mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(Handle, pDataSets);

        /// <summary>
        /// Gets the associated mailbox's subscribed child mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If true all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public List<cMailbox> Subscribed(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(Handle, pDescend, pDataSets);

        /// <summary>
        /// Asynchronously gets the associated mailbox's subscribed child mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If true all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to retrieve when getting the child mailboxes. See <see cref="cIMAPClient.MailboxCacheData"/>.</param>
        /// <returns></returns>
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(Handle, pDescend, pDataSets);

        /// <summary>
        /// Creates a child mailbox of the associated mailbox.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pName), pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a child mailbox of the associated mailbox.
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
        /// Subscribes to the associated mailbox.
        /// </summary>
        public void Subscribe() => Client.Subscribe(Handle);

        /// <summary>
        /// Asynchronously subscribes to the associated mailbox.
        /// </summary>
        /// <returns></returns>
        public Task SubscribeAsync() => Client.SubscribeAsync(Handle);

        /// <summary>
        /// Unsubscribes from the associated mailbox.
        /// </summary>
        public void Unsubscribe() => Client.Unsubscribe(Handle);

        /// <summary>
        /// Asynchronously unsubscribes from the associated mailbox.
        /// </summary>
        public Task UnsubscribeAsync() => Client.UnsubscribeAsync(Handle);

        /// <summary>
        /// Changes the name of the associated mailbox.
        /// Note that this method leaves the mailbox in its containing mailbox, just changing the last part of the path hierarchy.
        /// </summary>
        /// <param name="pName">The new mailbox name.</param>
        /// <returns></returns>
        public cMailbox Rename(string pName) => Client.Rename(Handle, ZRename(pName));

        /// <summary>
        /// Ansynchronously changes the name of the associated mailbox.
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
        /// Deletes the associated mailbox.
        /// </summary>
        public void Delete() => Client.Delete(Handle);

        /// <summary>
        /// Asynchronously deletes the associated mailbox.
        /// </summary>
        public Task DeleteAsync() => Client.DeleteAsync(Handle);

        /// <summary>
        /// Selects the associated mailbox.
        /// Selecting a mailbox un-selects the previously selected mailbox (if there was one).
        /// </summary>
        /// <param name="pForUpdate">Indicates if the associated mailbox should be selected for update or not</param>
        public void Select(bool pForUpdate = false) => Client.Select(Handle, pForUpdate);

        /// <summary>
        /// Asynchronously selects the associated mailbox.
        /// Selecting a mailbox un-selects the previously selected mailbox (if there was one).
        /// </summary>
        /// <param name="pForUpdate">Indicates if the associated mailbox should be selected for update or not</param>
        /// <returns></returns>
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(Handle, pForUpdate);

        /// <summary>
        /// Expunges messages marked with the <see cref="kMessageFlagName.Deleted"/> flag from the associated mailbox. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pAndUnselect">Indicates if the associated mailbox should also be un-selected.</param>
        /// <remarks>
        /// <para>Setting <paramref name="pAndUnselect"/> to true also un-selects the associated mailbox; this reduces the amount of network activity associated with the expunge.</para>
        /// </remarks>
        /// <seealso cref="cMessage.Deleted"/>
        /// <seealso cref="cMessage.Store(eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStore(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="cIMAPClient.Store(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
        public void Expunge(bool pAndUnselect = false) => Client.Expunge(Handle, pAndUnselect);

        /// <summary>
        /// Asynchronously expunges messages marked with the <see cref="kMessageFlagName.Deleted"/> flag from the associated mailbox. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pAndUnselect">Indicates if the associated mailbox should also be un-selected.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Setting <paramref name="pAndUnselect"/> to true also un-selects the associated mailbox; this reduces the amount of network activity associated with the expunge.</para>
        /// </remarks>
        /// <seealso cref="cMessage.Deleted"/>
        /// <seealso cref="cMessage.StoreAsync(eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStoreAsync(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStoreAsync(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="cIMAPClient.StoreAsync(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
        public Task ExpungeAsync(bool pAndUnselect = false) => Client.ExpungeAsync(Handle, pAndUnselect);

        /// <summary>
        /// Gets a list of messages contained in the associated mailbox from the server. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pFilter">The filter to use to restrict the set of messages returned.</param>
        /// <param name="pSort">The sort to use to order the set of messages returned. If not specified the default (<see cref="cIMAPClient.DefaultSort"/>) will be used.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public List<cMessage> Messages(cFilter pFilter = null, cSort pSort = null, cCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultCacheItems, pConfiguration);

        /// <summary>
        /// Asynchronously gets a list of messages contained in the associated mailbox from the server. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pFilter">The filter to use to restrict the set of messages returned.</param>
        /// <param name="pSort">The sort to use to order the set of messages returned. If not specified the default (<see cref="cIMAPClient.DefaultSort"/>) will be used.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public Task<List<cMessage>> MessagesAsync(cFilter pFilter = null, cSort pSort = null, cCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultCacheItems, pConfiguration);

        /// <summary>
        /// Creates message instances from internal message cache items. (Useful when handling the <see cref="MessageDelivery"/> event.)
        /// </summary>
        /// <param name="pHandles"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public List<cMessage> Messages(IEnumerable<iMessageHandle> pHandles, cCacheItems pItems = null, cPropertyFetchConfiguration pConfiguration = null)
        {
            Client.Fetch(pHandles, pItems ?? Client.DefaultCacheItems, pConfiguration);
            return ZMessages(pHandles);
        }

        /// <summary>
        /// Asynchronously creates message instances from internal message cache items. (Useful when handling the <see cref="MessageDelivery"/> event.)
        /// </summary>
        /// <param name="pHandles"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
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
        /// Initialises the value of <see cref="UnseenCount"/>. See <see cref="UnseenCount"/> to understand why and when you might want to do this. The associated mailbox must be selected.
        /// </summary>
        /// <returns>A list of internal message cache items for messages in the associated mailbox that do not have the <see cref="kMessageFlagName.Seen"/> flag.</returns>
        public cMessageHandleList SetUnseenCount() => Client.SetUnseenCount(Handle);

        /// <summary>
        /// Asynchronously initialises the value of <see cref="UnseenCount"/>. See <see cref="UnseenCount"/> to understand why and when you might want to do this. The associated mailbox must be selected.
        /// </summary>
        /// <returns>A list of internal message cache items for messages in the associated mailbox that do not have the <see cref="kMessageFlagName.Seen"/> flag.</returns>
        public Task<cMessageHandleList> SetUnseenCountAsync() => Client.SetUnseenCountAsync(Handle);

        /// <summary>
        /// Gets a <see cref="cMessage"/> instance. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned message.</param>
        /// <returns></returns>
        public cMessage Message(cUID pUID, cCacheItems pItems) => Client.Message(Handle, pUID, pItems);

        /// <summary>
        /// Asynchronously gets a <see cref="cMessage"/> instance. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned message.</param>
        /// <returns></returns>
        public Task<cMessage> MessageAsync(cUID pUID, cCacheItems pItems) => Client.MessageAsync(Handle, pUID, pItems);

        /// <summary>
        /// Gets a set of <see cref="cMessage"/> instances. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs">.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public List<cMessage> Messages(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pUIDs, pItems, pConfiguration);

        /// <summary>
        /// Asynchronously gets a set of <see cref="cMessage"/> instances. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public Task<List<cMessage>> MessagesAsync(IEnumerable<cUID> pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pUIDs, pItems, pConfiguration);

        /// <summary>
        /// Refreshes the mailbox cache data for this mailbox.
        /// </summary>
        /// <param name="pDataSets">The sets of data to refresh.</param>
        public void Fetch(fMailboxCacheDataSets pDataSets) => Client.Fetch(Handle, pDataSets);

        /// <summary>
        /// Asynchronously refreshes the mailbox cache data for this mailbox.
        /// </summary>
        /// <param name="pDataSets">The sets of data to refresh.</param>
        /// <returns></returns>
        public Task FetchAsync(fMailboxCacheDataSets pDataSets) => Client.FetchAsync(Handle, pDataSets);

        /// <summary>
        /// Copies a set of messages to the mailbox associated with this instance.
        /// The messages must be in the currently selected mailbox.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response the pairs of UIDs for the copied messages; otherwise null.</returns>
        public cCopyFeedback Copy(IEnumerable<cMessage> pMessages) => Client.Copy(cMessageHandleList.FromMessages(pMessages), Handle);

        /// <summary>
        /// Asynchronously copies a set of messages to the mailbox associated with this instance.
        /// The messages must be in the currently selected mailbox.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response the pairs of UIDs for the copied messages; otherwise null.</returns>
        public Task<cCopyFeedback> CopyAsync(IEnumerable<cMessage> pMessages) => Client.CopyAsync(cMessageHandleList.FromMessages(pMessages), Handle);

        /// <summary>
        /// Fetches a section of a message in the associated mailbox into a stream. 
        /// The associated mailbox must be selected.
        /// Will throw if the <paramref name="pUID"/> does not exist in the mailbox.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pSection"></param>
        /// <param name="pDecoding">
        /// Specifies the decoding should be applied to the fetched data.
        /// If the connected server supports <see cref="cCapabilities.Binary"/> and the entire part (<see cref="eSectionTextPart.all"/>) is being fetched then this may be <see cref="eDecodingRequired.unknown"/> to get the server to do the decoding.
        /// </param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        public void UIDFetch(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetch(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// Asynchronously fetches a section of a message in the associated mailbox into a stream. 
        /// The associated mailbox must be selected.
        /// Will throw if the <paramref name="pUID"/> does not exist in the mailbox.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pSection"></param>
        /// <param name="pDecoding">
        /// Specifies the decoding should be applied to the fetched data.
        /// If the connected server supports <see cref="cCapabilities.Binary"/> and the entire part (<see cref="eSectionTextPart.all"/>) is being fetched then this may be <see cref="eDecodingRequired.unknown"/> to get the server to do the decoding.
        /// </param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public Task UIDFetchAsync(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetchAsync(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// Stores flags for a message. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq">
        /// The modseq to use in the UNCHANGEDSINCE clause of a conditional store (see RFC 7162).
        /// Can only be specified if the associated mailbox supports CONDSTORE.
        /// If the message has been modified since the specified modseq the server should fail the update.
        /// </param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        public cUIDStoreFeedback UIDStore(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Asynchronously stores flags for a message. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq">
        /// The modseq to use in the UNCHANGEDSINCE clause of a conditional store (see RFC 7162).
        /// Can only be specified if the associated mailbox supports CONDSTORE.
        /// If the message has been modified since the specified modseq the server should fail the update.
        /// </param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        public Task<cUIDStoreFeedback> UIDStoreAsync(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Stores flags for a set of messages. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq">
        /// The modseq to use in the UNCHANGEDSINCE clause of a conditional store (see RFC 7162).
        /// Can only be specified if the associated mailbox supports CONDSTORE.
        /// If any of the messages have been modified since the specified modseq the server should fail the update to that message.
        /// </param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        public cUIDStoreFeedback UIDStore(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Asynchronously stores flags for a set of messages. The associated mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq">
        /// The modseq to use in the UNCHANGEDSINCE clause of a conditional store (see RFC 7162).
        /// Can only be specified if the associated mailbox supports CONDSTORE.
        /// If any of the messages have been modified since the specified modseq the server should fail the update to that message.
        /// </param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        public Task<cUIDStoreFeedback> UIDStoreAsync(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Copies a message in this mailbox to another mailbox. The mailbox associated with this instance must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response the pairs of UIDs for the copied messages; otherwise null.</returns>
        public cCopyFeedback UIDCopy(cUID pUID, cMailbox pDestination) => Client.UIDCopy(Handle, pUID, pDestination.Handle);

        /// <summary>
        /// Asynchronously copies a message in this mailbox to another mailbox. The mailbox associated with this instance must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response the pairs of UIDs for the copied messages; otherwise null.</returns>
        public Task<cCopyFeedback> UIDCopyAsync(cUID pUID, cMailbox pDestination) => Client.UIDCopyAsync(Handle, pUID, pDestination.Handle);

        /// <summary>
        /// Copies messages in this mailbox to another mailbox. The mailbox associated with this instance must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response the pairs of UIDs for the copied messages; otherwise null.</returns>
        public cCopyFeedback UIDCopy(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopy(Handle, pUIDs, pDestination.Handle);

        /// <summary>
        /// Asynchronously copies messages in this mailbox to another mailbox. The mailbox associated with this instance must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response the pairs of UIDs for the copied messages; otherwise null.</returns>
        public Task<cCopyFeedback> UIDCopyAsync(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopyAsync(Handle, pUIDs, pDestination.Handle);

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString() => $"{nameof(cMailbox)}({Handle})";
    }
}