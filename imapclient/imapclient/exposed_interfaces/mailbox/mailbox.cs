using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using work.bacome.apidocumentation;
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
        /**<summary>The internal mailbox handle that this instance is attached to.</summary>*/
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
        /// If <see cref="cIMAPClient.SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.
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
        /// If <see cref="cIMAPClient.SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.
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
        /// Gets the mailbox name including the full hierarchy.
        /// </summary>
        public string Path => Handle.MailboxName.Path;

        /// <summary>
        /// Gets the hierarchy delimiter used in <see cref="Path"/>. May be <see langword="null"/> if there is no hierarchy.
        /// </summary>
        public char? Delimiter => Handle.MailboxName.Delimiter;

        /// <summary>
        /// Gets the path of the parent mailbox. Will be <see langword="null"/> if there is no parent mailbox.
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
        /// Indicates if the mailbox exists on the server.
        /// </summary>
        /// <remarks>
        /// Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist. Mailboxes can also be deleted.
        /// </remarks>
        public bool Exists
        {
            get
            {
                if (Handle.Exists == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                return Handle.Exists == true;
            }
        }

        /// <summary>
        /// Indicates if the mailbox can definitely not contain child mailboxes. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Reflects the IMAP \Noinferiors flag. May be <see langword="null"/> if the mailbox associated with the instance does not exist.
        /// </remarks>
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
        /// Indicates if the mailbox can be selected.
        /// </summary>
        /// <remarks>
        /// Reflects the IMAP \Noselect flag.
        /// </remarks>
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
        /// Indicates if the mailbox has been marked "interesting" by the server. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the server didn't say either way. Reflects the IMAP \Marked and \Unmarked flags.
        /// </remarks>
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
        /// Indicates if the mailbox is a remote mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> will be returned under the following set of circumstances;
        /// <list type="bullet">
        /// <item><see cref="cIMAPClient.MailboxReferrals"/> is set to <see langword="true"/>, and</item>
        /// <item><see cref="cCapabilities.MailboxReferrals"/> can be used, and</item>
        /// <item><see cref="cCapabilities.ListExtended"/> cannot be used.</item>
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
        /// Indicates if the mailbox has child mailboxes. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the server didn't say either way, 
        /// which may be because the <see cref="fMailboxCacheDataItems.children"/> flags are not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>)
        /// or maybe because the server doesn't support <see cref="cCapabilities.Children"/>
        /// .
        /// </remarks>
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
        /// Indicates if the mailbox contains all messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the <see cref="fMailboxCacheDataItems.specialuse"/> flags are not being requested; see <see cref="cIMAPClient.MailboxCacheDataItems"/>.
        /// Refects the RFC 6154 \All flag.
        /// </remarks>
        /// <seealso cref="cCapabilities.SpecialUse"/>
        public bool? ContainsAll
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsAll) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if the mailbox contains the message archive. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the <see cref="fMailboxCacheDataItems.specialuse"/> flags are not being requested; see <see cref="cIMAPClient.MailboxCacheDataItems"/>.
        /// Refects the RFC 6154 \Archive flag.
        /// </remarks>
        /// <seealso cref="cCapabilities.SpecialUse"/>
        public bool? IsArchive
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.IsArchive) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if the mailbox contains message drafts. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the <see cref="fMailboxCacheDataItems.specialuse"/> flags are not being requested; see <see cref="cIMAPClient.MailboxCacheDataItems"/>.
        /// Refects the RFC 6154 \Drafts flag.
        /// </remarks>
        /// <seealso cref="cCapabilities.SpecialUse"/>
        public bool? ContainsDrafts
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsDrafts) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if the mailbox contains flagged messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the <see cref="fMailboxCacheDataItems.specialuse"/> flags are not being requested; see <see cref="cIMAPClient.MailboxCacheDataItems"/>.
        /// Refects the RFC 6154 \Flagged flag.
        /// </remarks>
        /// <seealso cref="cCapabilities.SpecialUse"/>
        public bool? ContainsFlagged
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsFlagged) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if the mailbox contains junk mail. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the <see cref="fMailboxCacheDataItems.specialuse"/> flags are not being requested; see <see cref="cIMAPClient.MailboxCacheDataItems"/>.
        /// Refects the RFC 6154 \Junk flag.
        /// </remarks>
        /// <seealso cref="cCapabilities.SpecialUse"/>
        public bool? ContainsJunk
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsJunk) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if the mailbox contains sent messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the <see cref="fMailboxCacheDataItems.specialuse"/> flags are not being requested; see <see cref="cIMAPClient.MailboxCacheDataItems"/>.
        /// Refects the RFC 6154 \Sent flag.
        /// </remarks>
        /// <seealso cref="cCapabilities.SpecialUse"/>
        public bool? ContainsSent
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsSent) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if the mailbox contains deleted or to-be deleted messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the <see cref="fMailboxCacheDataItems.specialuse"/> flags are not being requested; see <see cref="cIMAPClient.MailboxCacheDataItems"/>.
        /// Refects the RFC 6154 \Trash flag.
        /// </remarks>
        /// <seealso cref="cCapabilities.SpecialUse"/>
        public bool? ContainsTrash
        {
            get
            {
                if (Handle.ListFlags == null) Client.Fetch(Handle, fMailboxCacheDataSets.list);
                if (Handle.ListFlags == null) return false;
                if (Handle.ListFlags.ContainsTrash) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if this mailbox is subscribed-to or not.
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
        /// May be <see langword="null"/>.
        /// This property always has an up-to-date value when the mailbox is selected.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.messagecount"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>) or that the value was not sent when requested.
        /// </remarks>
        public int? MessageCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.Count;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.messagecount) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.MessageCount;
            }
        }

        /// <summary>
        /// Gets the number of recent messages in the mailbox.
        /// May be <see langword="null"/>.
        /// This property always has an up-to-date value when the mailbox is selected.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.recentcount"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>) or that the value was not sent when requested.
        /// </remarks>
        public int? RecentCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.RecentCount;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.recentcount) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.RecentCount;
            }
        }

        /// <summary>
        /// Gets the predicted next UID for the mailbox. 
        /// May be <see langword="null"/> or zero.
        /// This property always has a value when the mailbox is selected, but it may not be up-to-date.
        /// </summary>
        /// <remarks>
        /// <para><see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.uidnext"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>) or that the value was not sent when requested.</para>
        /// <para>When the mailbox is selected, zero indicates that the value is unknown.</para>
        /// <para>While the mailbox is selected the value of this property may not be up-to-date: see the value of <see cref="UIDNextUnknownCount"/> for the potential inaccuracy in this property value.</para>
        /// <para>IMAP does not mandate that the server keep the client updated with this value while a mailbox is selected but it also disallows retrieving the value for a mailbox while the mailbox is selected.</para>
        /// <para>While the mailbox is selected the library internally maintains the value of this property by monitoring IMAP FETCH responses from the server, but these responses have to be explicitly requested.</para>
        /// <para>If it is important to you that the value of this property be accurate when the mailbox is selected then you must fetch the <see cref="fMessageCacheAttributes.uid"/> for new messages as they arrive.</para>
        /// </remarks>
        /// <seealso cref="MessageDelivery"/>
        /// <seealso cref="Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
        public uint? UIDNext
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDNext;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.uidnext) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.UIDNext;
            }
        }

        /// <summary>
        /// Indicates how inaccurate the <see cref="UIDNext"/> is.
        /// </summary>
        /// <remarks>
        /// This is the count of messages that have arrived since the mailbox was selected for which the library has not seen the value of the UID.
        /// </remarks>
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
        /// May be <see langword="null"/> or zero.
        /// This property always has a value when the mailbox is selected.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox does not support UIDs or that the <see cref="fMailboxCacheDataItems.uidvalidity"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>) or that the value was not sent when requested.
        /// Zero indicates that the server does not support UIDs.
        /// </remarks>
        /// <seealso cref="UIDNotSticky"/>
        public uint? UIDValidity
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UIDValidity;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.uidvalidity) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.UIDValidity;
            }
        }

        /// <summary>
        /// Gets the number of unseen messages in the mailbox.
        /// May be <see langword="null"/>.
        /// This property always has a value when the mailbox is selected, but it may not be up-to-date.
        /// </summary>
        /// <remarks>
        /// <para><see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.unseencount"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>) or that the value was not sent when requested.</para>
        /// <para>While the mailbox is selected the value of this property may not be up-to-date: see the value of <see cref="UnseenUnknownCount"/> for the potential inaccuracy in this property value.</para>
        /// <para>IMAP does not provide a mechanism for getting this value while a mailbox is selected.</para>
        /// <para>While the mailbox is selected the library internally maintains the value of this property by monitoring IMAP FETCH responses from the server, but the property value has to be explicitly initialised and the FETCH responses have to be explicitly requested.</para>
        /// <para>If it is important to you that the value of this property be accurate when the mailbox is selected then you must initialise the value by using <see cref="SetUnseenCount"/> and also fetch the <see cref="fMessageCacheAttributes.flags"/> for new messages as they arrive.</para>
        /// </remarks>
        /// <seealso cref="MessageDelivery"/>
        /// <seealso cref="Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
        /// <seealso cref="cMessageCacheItems"/>
        public int? UnseenCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.UnseenCount;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.unseencount) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.UnseenCount;
            }
        }

        /// <summary>
        /// Indicates how inaccurate the <see cref="UnseenCount"/> is.
        /// </summary>
        /// <remarks>
        /// This is the number of messages for which the library is unsure of the value of the <see cref="kMessageFlag.Seen"/> flag.
        /// </remarks>
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
        /// Gets the highest modification sequence number for the mailbox (see RFC 7162).
        /// May be <see langword="null"/> or zero.
        /// When the mailbox is selected this property will always have a value.
        /// </summary>
        /// <remarks>
        /// <para><see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.highestmodseq"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>) or that the value was not sent when requested.</para>
        /// <para>Zero indicates that <see cref="cCapabilities.CondStore"/> is not in use or that the mailbox does not support the persistent storage of mod-sequences.</para>
        /// </remarks>
        public ulong? HighestModSeq
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.Handle, Handle)) return lSelectedMailboxDetails.Cache.HighestModSeq;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.highestmodseq) == 0) return null;
                if (Handle.MailboxStatus == null) Client.Fetch(Handle, fMailboxCacheDataSets.status);
                return Handle.MailboxStatus?.HighestModSeq;
            }
        }

        /// <summary>
        /// Indicates if the mailbox associated with this instance has been selected at least once on the current connection.
        /// </summary>
        public bool HasBeenSelected => Handle.SelectedProperties.HasBeenSelected;

        /// <summary>
        /// Indicates if the mailbox associated with this instance has been selected for update at least once on the current connection.
        /// </summary>
        public bool HasBeenSelectedForUpdate => Handle.SelectedProperties.HasBeenSelectedForUpdate;

        /// <summary>
        /// Indicates if the mailbox associated with this instance has been selected read-only at least once on the current connection.
        /// </summary>
        public bool HasBeenSelectedReadOnly => Handle.SelectedProperties.HasBeenSelectedReadOnly;

        /// <summary>
        /// Indicates if the mailbox does not have persistent UIDs (see RFC 4315). May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox associated with this instance has never been selected on the current connection.
        /// </remarks>
        public bool? UIDNotSticky => Handle.SelectedProperties.UIDNotSticky;

        /// <summary>
        /// Gets the defined flags in the mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox associated with this instance has never been selected on the current connection.
        /// </remarks>
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
        /// Gets the flags that the client can change permanently on messages in the mailbox when it is selected for update. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox associated with this instance has never been selected for update on the current connection.
        /// </remarks>
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
        /// Gets the flags that the client can change permanently on messages in the mailbox when it is selected readonly. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox associated with this instance has never been selected read-only on the current connection.
        /// </remarks>
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
        /// Indicates if the mailbox associated with this instance is the currently selected mailbox.
        /// </summary>
        public bool IsSelected => ReferenceEquals(Client.SelectedMailboxDetails?.Handle, Handle);

        /// <summary>
        /// Indicates if the mailbox associated with this instance is currently selected for update.
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
        /// Indicates if the mailbox associated with this instance is currently selected, but can't be modified.
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
        /// <param name="pDataSets">The sets of data to request when getting the child mailboxes.</param>
        /// <returns></returns>
        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(Handle, pDataSets);

        /// <summary>
        /// Asynchronously gets the mailbox's child mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to request when getting the child mailboxes.</param>
        /// <inheritdoc cref="Mailboxes(fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(Handle, pDataSets);

        /// <summary>
        /// Gets the mailbox's subscribed child mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to request when getting the child mailboxes.</param>
        /// <returns></returns>
        public List<cMailbox> Subscribed(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(Handle, pDescend, pDataSets);

        /// <summary>
        /// Asynchronously gets the mailbox's subscribed child mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to request when getting the child mailboxes.</param>
        /// <inheritdoc cref="Subscribed(bool, fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(Handle, pDescend, pDataSets);

        /// <summary>
        /// Creates a child mailbox of the mailbox.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pName), pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a child mailbox of the mailbox.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <inheritdoc cref="CreateChild(string, bool)" select="returns|remarks"/>
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
        /// Subscribes to the mailbox.
        /// </summary>
        public void Subscribe() => Client.Subscribe(Handle);

        /// <summary>
        /// Asynchronously subscribes to the mailbox.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Subscribe" select="remarks"/>
        public Task SubscribeAsync() => Client.SubscribeAsync(Handle);

        /// <summary>
        /// Unsubscribes from the mailbox.
        /// </summary>
        public void Unsubscribe() => Client.Unsubscribe(Handle);

        /// <summary>
        /// Asynchronously unsubscribes from the mailbox.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Unsubscribe" select="remarks"/>
        public Task UnsubscribeAsync() => Client.UnsubscribeAsync(Handle);

        /// <summary>
        /// Changes the name of the mailbox.
        /// </summary>
        /// <param name="pName">The new mailbox name.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method renames the mailbox inside its containing mailbox - it just changes the last part of the path hierarchy.
        /// </remarks>
        public cMailbox Rename(string pName) => Client.Rename(Handle, ZRename(pName));

        /// <summary>
        /// Ansynchronously changes the name of the mailbox.
        /// </summary>
        /// <param name="pName">The new mailbox name.</param>
        /// <inheritdoc cref="Rename(string)" select="returns|remarks"/>
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
        /// Deletes the mailbox.
        /// </summary>
        public void Delete() => Client.Delete(Handle);

        /// <summary>
        /// Asynchronously deletes the mailbox.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Delete" select="remarks"/>
        public Task DeleteAsync() => Client.DeleteAsync(Handle);

        /// <summary>
        /// Selects the mailbox.
        /// </summary>
        /// <param name="pForUpdate">Indicates if the mailbox should be selected for update or not</param>
        /// <remarks>
        /// Selecting a mailbox un-selects the previously selected mailbox (if there was one).
        /// </remarks>
        public void Select(bool pForUpdate = false) => Client.Select(Handle, pForUpdate);

        /// <summary>
        /// Asynchronously selects the mailbox.
        /// </summary>
        /// <param name="pForUpdate">Indicates if the mailbox should be selected for update or not</param>
        /// <returns></returns>
        /// <inheritdoc cref="Select(bool)" select="remarks"/>
        public Task SelectAsync(bool pForUpdate = false) => Client.SelectAsync(Handle, pForUpdate);

        /// <summary>
        /// Expunges messages marked with the <see cref="kMessageFlag.Deleted"/> flag from the mailbox. The mailbox must be selected.
        /// </summary>
        /// <param name="pAndUnselect">Indicates if the mailbox should also be un-selected.</param>
        /// <remarks>
        /// Setting <paramref name="pAndUnselect"/> to <see langword="true"/> also un-selects the mailbox; this reduces the amount of network activity associated with the expunge.
        /// </remarks>
        /// <seealso cref="cMessage.Deleted"/>
        /// <seealso cref="kMessageFlag.Deleted"/>
        /// <seealso cref="cMessage.Store(eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStore(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="cIMAPClient.Store(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
        public void Expunge(bool pAndUnselect = false) => Client.Expunge(Handle, pAndUnselect);

        /// <summary>
        /// Asynchronously expunges messages marked with the <see cref="kMessageFlag.Deleted"/> flag from the mailbox. The mailbox must be selected.
        /// </summary>
        /// <param name="pAndUnselect">Indicates if the mailbox should also be un-selected.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Expunge(bool)" select="remarks"/>
        /// <seealso cref="cMessage.Deleted"/>
        /// <seealso cref="kMessageFlag.Deleted"/>
        /// <seealso cref="cMessage.StoreAsync(eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStoreAsync(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStoreAsync(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="cIMAPClient.StoreAsync(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
        public Task ExpungeAsync(bool pAndUnselect = false) => Client.ExpungeAsync(Handle, pAndUnselect);

        /// <summary>
        /// Gets a list of messages contained in the mailbox from the server. The mailbox must be selected.
        /// </summary>
        /// <param name="pFilter">The filter to use to restrict the set of messages returned.</param>
        /// <param name="pSort">The sort to use to order the set of messages returned. If not specified the default (<see cref="cIMAPClient.DefaultSort"/>) will be used.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultMessageCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public List<cMessage> Messages(cFilter pFilter = null, cSort pSort = null, cMessageCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultMessageCacheItems, pConfiguration);

        /// <summary>
        /// Asynchronously gets a list of messages contained in the mailbox from the server. The mailbox must be selected.
        /// </summary>
        /// <param name="pFilter">The filter to use to restrict the set of messages returned.</param>
        /// <param name="pSort">The sort to use to order the set of messages returned. If not specified the default (<see cref="cIMAPClient.DefaultSort"/>) will be used.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultMessageCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)" select="returns|remarks"/>
        public Task<List<cMessage>> MessagesAsync(cFilter pFilter = null, cSort pSort = null, cMessageCacheItems pItems = null, cMessageFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultMessageCacheItems, pConfiguration);

        /// <summary>
        /// Creates message instances from internal message cache items. (Useful when handling the <see cref="MessageDelivery"/> event.)
        /// </summary>
        /// <param name="pHandles"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultMessageCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public List<cMessage> Messages(IEnumerable<iMessageHandle> pHandles, cMessageCacheItems pItems = null, cCacheItemFetchConfiguration pConfiguration = null)
        {
            Client.Fetch(pHandles, pItems ?? Client.DefaultMessageCacheItems, pConfiguration);
            return ZMessages(pHandles);
        }

        /// <summary>
        /// Asynchronously creates message instances from internal message cache items. (Useful when handling the <see cref="MessageDelivery"/> event.)
        /// </summary>
        /// <param name="pHandles"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages. If not specified the default (<see cref="cIMAPClient.DefaultMessageCacheItems"/>) will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)" select="returns|remarks"/>
        public async Task<List<cMessage>> MessagesAsync(IEnumerable<iMessageHandle> pHandles, cMessageCacheItems pItems = null, cCacheItemFetchConfiguration pConfiguration = null)
        {
            await Client.FetchAsync(pHandles, pItems ?? Client.DefaultMessageCacheItems, pConfiguration).ConfigureAwait(false);
            return ZMessages(pHandles);
        }

        private List<cMessage> ZMessages(IEnumerable<iMessageHandle> pHandles)
        {
            List<cMessage> lMessages = new List<cMessage>();
            foreach (var lHandle in pHandles) lMessages.Add(new cMessage(Client, lHandle));
            return lMessages;
        }

        /// <summary>
        /// Initialises the value of <see cref="UnseenCount"/>. The mailbox must be selected.
        /// </summary>
        /// <returns>A list of internal message cache items for messages in the mailbox that do NOT have the <see cref="kMessageFlag.Seen"/> flag.</returns>
        /// <remarks>
        /// See <see cref="UnseenCount"/> to understand why and when you might want to do this. 
        /// </remarks>
        public cMessageHandleList SetUnseenCount() => Client.SetUnseenCount(Handle);

        /// <summary>
        /// Asynchronously initialises the value of <see cref="UnseenCount"/>. The mailbox must be selected.
        /// </summary>
        /// <inheritdoc cref="SetUnseenCount" select="returns|remarks"/>
        public Task<cMessageHandleList> SetUnseenCountAsync() => Client.SetUnseenCountAsync(Handle);

        /// <summary>
        /// Gets a <see cref="cMessage"/> instance from a <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned message.</param>
        /// <returns></returns>
        public cMessage Message(cUID pUID, cMessageCacheItems pItems) => Client.Message(Handle, pUID, pItems);

        /// <summary>
        /// Asynchronously gets a <see cref="cMessage"/> instance from a <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned message.</param>
        /// <inheritdoc cref="Message(cUID, cMessageCacheItems)" select="returns|remarks"/>
        public Task<cMessage> MessageAsync(cUID pUID, cMessageCacheItems pItems) => Client.MessageAsync(Handle, pUID, pItems);

        /// <summary>
        /// Gets a list of <see cref="cMessage"/> instances from a set of <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs">.</param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public List<cMessage> Messages(IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration = null) => Client.Messages(Handle, pUIDs, pItems, pConfiguration);

        /// <summary>
        /// Asynchronously gets a list of <see cref="cMessage"/> instances from a set of <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pItems">The set of message cache items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Messages(IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)" select="returns|remarks"/>
        public Task<List<cMessage>> MessagesAsync(IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration = null) => Client.MessagesAsync(Handle, pUIDs, pItems, pConfiguration);

        /// <summary>
        /// Refreshes the internal mailbox cache data for this mailbox.
        /// </summary>
        /// <param name="pDataSets">The sets of data to refresh.</param>
        public void Fetch(fMailboxCacheDataSets pDataSets) => Client.Fetch(Handle, pDataSets);

        /// <summary>
        /// Asynchronously refreshes the internal mailbox cache data for this mailbox.
        /// </summary>
        /// <param name="pDataSets">The sets of data to refresh.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Fetch(fMailboxCacheDataSets)" select="remarks"/>
        public Task FetchAsync(fMailboxCacheDataSets pDataSets) => Client.FetchAsync(Handle, pDataSets);

        /// <summary>
        /// Copies a set of messages to the mailbox associated with this instance.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response an object containing the pairs of UIDs involved in the copy, otherwise <see langword="null"/>.</returns>
        /// <remarks>
        /// The messages must be in the currently selected mailbox.
        /// </remarks>
        public cCopyFeedback Copy(IEnumerable<cMessage> pMessages) => Client.Copy(cMessageHandleList.FromMessages(pMessages), Handle);

        /// <summary>
        /// Asynchronously copies a set of messages to the mailbox associated with this instance.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <inheritdoc cref="Copy(IEnumerable{cMessage})" select="returns|remarks"/>
        public Task<cCopyFeedback> CopyAsync(IEnumerable<cMessage> pMessages) => Client.CopyAsync(cMessageHandleList.FromMessages(pMessages), Handle);
    
        /// <summary>
        /// Fetches a section of a message into a stream. 
        /// The mailbox must be selected.
        /// Will throw if the <paramref name="pUID"/> does not exist in the mailbox.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pSection"></param>
        /// <param name="pDecoding"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <remarks>
        /// If <see cref="cCapabilities.Binary"/> is in use and the entire part (<see cref="eSectionTextPart.all"/>) is being fetched then unless <paramref name="pDecoding"/> is <see cref="eDecodingRequired.none"/> the server will do the decoding that it determines is required
        /// (i.e. the decoding specified is ignored).
        /// </remarks>
        public void UIDFetch(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetch(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// Asynchronously fetches a section of a message into a stream. 
        /// The mailbox must be selected.
        /// Will throw if the <paramref name="pUID"/> does not exist in the mailbox.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pSection"></param>
        /// <param name="pDecoding"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        /// <inheritdoc cref="UIDFetch(cUID, cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)" select="returns|remarks"/>
        public Task UIDFetchAsync(cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.UIDFetchAsync(Handle, pUID, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// Stores flags for a message. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        /// <remarks>
        /// The <paramref name="pIfUnchangedSinceModSeq"/> can only be specified if the containing mailbox's <see cref="cMailbox.HighestModSeq"/> is not zero. 
        /// (i.e. <see cref="cCapabilities.CondStore"/> is in use and the mailbox supports the persistent storage of mod-sequences.)
        /// If the message has been modified since the specified value then the server will fail the store.
        /// </remarks>
        public cUIDStoreFeedback UIDStore(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Asynchronously stores flags for a message. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public Task<cUIDStoreFeedback> UIDStoreAsync(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Stores flags for a set of messages. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public cUIDStoreFeedback UIDStore(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStore(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Asynchronously stores flags for a set of messages. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public Task<cUIDStoreFeedback> UIDStoreAsync(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.UIDStoreAsync(Handle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

        /// <summary>
        /// Copies a message in this mailbox to another mailbox. This mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response the UID of the message in the destination mailbox, otherwise <see langword="null"/>.</returns>
        public cUID UIDCopy(cUID pUID, cMailbox pDestination)
        {
            var lFeedback = Client.UIDCopy(Handle, pUID, pDestination.Handle);
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedUID;
            return null;
        }

        /// <summary>
        /// Asynchronously copies a message in this mailbox to another mailbox. This mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pDestination"></param>
        /// <inheritdoc cref="UIDCopy(cUID, cMailbox)" select="returns|remarks"/>
        public async Task<cUID> UIDCopyAsync(cUID pUID, cMailbox pDestination)
        {
            var lFeedback = await Client.UIDCopyAsync(Handle, pUID, pDestination.Handle).ConfigureAwait(false);
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedUID;
            return null;
        }

        /// <summary>
        /// Copies messages in this mailbox to another mailbox. This mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response an object containing the pairs of UIDs involved in the copy, otherwise <see langword="null"/>.</returns>
        public cCopyFeedback UIDCopy(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopy(Handle, pUIDs, pDestination.Handle);

        /// <summary>
        /// Asynchronously copies messages in this mailbox to another mailbox. This mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <inheritdoc cref="UIDCopy(IEnumerable{cUID}, cMailbox)" select="returns|remarks"/>
        public Task<cCopyFeedback> UIDCopyAsync(IEnumerable<cUID> pUIDs, cMailbox pDestination) => Client.UIDCopyAsync(Handle, pUIDs, pDestination.Handle);

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMailbox)}({Handle})";
    }
}