using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP mailbox.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of this class are only valid whilst the <see cref="Client"/> remains connected.
    /// Reconnecting the client will not bring instances back to life.
    /// </para>
    /// <para>
    /// To interact with messages in a mailbox, IMAP requires that the mailbox be selected - use <see cref="Select(bool)"/> to select the mailbox.
    /// Each IMAP connection (and hence each <see cref="cIMAPClient"/> instance) can have at most one mailbox selected – selecting a mailbox automatically un-selects the previously selected mailbox.
    /// An instance of this class may be selected and un-selected many times in its lifetime.
    /// <see cref="cIMAPMessage"/> instances are valid only whilst the mailbox they are in remains selected.
    /// </para>
    /// </remarks>
    public class cMailbox : iMailboxContainer, IEquatable<cMailbox>
    {
        private PropertyChangedEventHandler mPropertyChanged;
        private object mPropertyChangedLock = new object();

        private EventHandler<cMessageDeliveryEventArgs> mMessageDelivery;
        private object mMessageDeliveryLock = new object();

        /**<summary>The client that this instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The mailbox that this instance represents.</summary>*/
        public readonly iMailboxHandle MailboxHandle;

        internal cMailbox(cIMAPClient pClient, iMailboxHandle pMailboxHandle)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
        }

        /// <summary>
        /// Fired when the server notifies the client of a change that affects a property value of this instance.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
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
            if (ReferenceEquals(pArgs.MailboxHandle, MailboxHandle)) mPropertyChanged?.Invoke(this, pArgs);
        }

        /// <summary>
        /// Fired when the server notifies the client that messages have arrived in the mailbox.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
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
            if (ReferenceEquals(pArgs.MailboxHandle, MailboxHandle)) mMessageDelivery?.Invoke(this, pArgs);
        }

        /// <summary>
        /// Gets the mailbox name including the full hierarchy.
        /// </summary>
        public string Path => MailboxHandle.MailboxName.Path;

        /// <summary>
        /// Gets the hierarchy delimiter used in <see cref="Path"/>. May be <see langword="null"/>. 
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> if the server has no hierarchy in its names.
        /// </remarks>
        public char? Delimiter => MailboxHandle.MailboxName.Delimiter;

        /// <summary>
        /// Gets the path of the parent mailbox. Will be <see langword="null"/> if there is no parent mailbox.
        /// </summary>
        public string ParentPath => MailboxHandle.MailboxName.ParentPath;

        /// <summary>
        /// Gets the name of the mailbox. As compared to <see cref="Path"/> this does not include the hierarchy.
        /// </summary>
        /// 
        public string Name => MailboxHandle.MailboxName.Name;

        /// <summary>
        /// Indicates whether this instance represents the INBOX.
        /// </summary>
        public bool IsInbox => MailboxHandle.MailboxName.IsInbox;

        /// <summary>
        /// Indicates whether the mailbox exists on the server.
        /// </summary>
        /// <remarks>
        /// Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist as mailboxes on the server. Mailboxes can also be deleted.
        /// </remarks>
        public bool Exists
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(Exists));
                if (MailboxHandle.Exists == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                return MailboxHandle.Exists == true;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox can definitely not contain child mailboxes. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Reflects the IMAP \Noinferiors flag. Will be <see langword="null"/> if the mailbox does not exist on the server.
        /// </remarks>
        /// <seealso cref="Exists"/>
        public bool? CanHaveChildren
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(CanHaveChildren));
                if (MailboxHandle.Exists == false) return null; // don't know until the mailbox is created
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return null; // don't know
                return MailboxHandle.ListFlags.CanHaveChildren;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox can be selected.
        /// </summary>
        /// <remarks>
        /// Reflects the IMAP \Noselect flag.
        /// </remarks>
        public bool CanSelect
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(CanSelect));
                if (MailboxHandle.Exists == false) return false;
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                return MailboxHandle.ListFlags.CanSelect;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox has been marked "interesting" by the server. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the server didn't say either way. Reflects the IMAP \Marked and \Unmarked flags.
        /// </remarks>
        public bool? IsMarked
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(IsMarked));
                if (MailboxHandle.Exists == false) return false;
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                return MailboxHandle.ListFlags.IsMarked;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox is a remote mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> will only be returned under the following set of circumstances;
        /// <list type="bullet">
        /// <item><see cref="cIMAPClient.MailboxReferrals"/> is set to <see langword="true"/>, and</item>
        /// <item><see cref="cIMAPCapabilities.MailboxReferrals"/> is in use, and</item>
        /// <item><see cref="cIMAPCapabilities.ListExtended"/> is not in use.</item>
        /// </list>
        /// Under these circumstances it is not possible to reliably determine if the mailbox is remote or not.
        /// </remarks>
        public bool? IsRemote
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(IsRemote));
                if (MailboxHandle.Exists == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.IsRemote) return true;
                if (Client.Capabilities.ListExtended) return false;
                if (Client.MailboxReferrals) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox has child mailboxes. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the server didn't say either way, which may be because;
        /// <list type="bullet">
        /// <item><see cref="fMailboxCacheDataItems.children"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>), or</item>
        /// <item><see cref="cIMAPCapabilities.Children"/> is not in use.</item>
        /// </list>
        /// </remarks>
        public bool? HasChildren
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(IsRemote));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                bool? lHasChildren = MailboxHandle.ListFlags?.HasChildren;
                if (lHasChildren == true) return true;
                if (Client.HasCachedChildren(MailboxHandle) == true) return true;
                return lHasChildren;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox contains all messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Refects the RFC 6154 \All flag.
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.specialuse"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>).
        /// </remarks>
        /// <seealso cref="cIMAPCapabilities.SpecialUse"/>
        public bool? ContainsAll
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(ContainsAll));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.ContainsAll) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox contains the message archive. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Refects the RFC 6154 \Archive flag.
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.specialuse"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>).
        /// </remarks>
        /// <seealso cref="cIMAPCapabilities.SpecialUse"/>
        public bool? IsArchive
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(IsArchive));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.IsArchive) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox contains message drafts. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Refects the RFC 6154 \Drafts flag.
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.specialuse"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>).
        /// </remarks>
        /// <seealso cref="cIMAPCapabilities.SpecialUse"/>
        public bool? ContainsDrafts
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(ContainsDrafts));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.ContainsDrafts) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox contains flagged messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Refects the RFC 6154 \Flagged flag.
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.specialuse"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>).
        /// </remarks>
        /// <seealso cref="cIMAPCapabilities.SpecialUse"/>
        public bool? ContainsFlagged
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(ContainsFlagged));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.ContainsFlagged) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox contains junk mail. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Refects the RFC 6154 \Junk flag.
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.specialuse"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>).
        /// </remarks>
        /// <seealso cref="cIMAPCapabilities.SpecialUse"/>
        public bool? ContainsJunk
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(ContainsJunk));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.ContainsJunk) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox contains sent messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Refects the RFC 6154 \Sent flag.
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.specialuse"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>).
        /// </remarks>
        /// <seealso cref="cIMAPCapabilities.SpecialUse"/>
        public bool? ContainsSent
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(ContainsSent));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.ContainsSent) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox contains deleted or to-be deleted messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Refects the RFC 6154 \Trash flag.
        /// <see langword="null"/> indicates that <see cref="fMailboxCacheDataItems.specialuse"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>).
        /// </remarks>
        /// <seealso cref="cIMAPCapabilities.SpecialUse"/>
        public bool? ContainsTrash
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(ContainsTrash));
                if (MailboxHandle.ListFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.list, lContext);
                if (MailboxHandle.ListFlags == null) return false;
                if (MailboxHandle.ListFlags.ContainsTrash) return true;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.specialuse) == 0) return null;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox is subscribed-to or not.
        /// </summary>
        public bool IsSubscribed
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(IsSubscribed));
                if (MailboxHandle.LSubFlags == null) ZRequestMailboxData(fMailboxCacheDataSets.lsub, lContext);
                if (MailboxHandle.LSubFlags == null) throw new cInternalErrorException(nameof(cMailbox),nameof(IsSubscribed));
                return MailboxHandle.LSubFlags.Subscribed;
            }
        }

        /// <summary>
        /// Gets the number of messages in the mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property always has a value when the mailbox is selected. 
        /// </para>
        /// <para>
        /// <see langword="null"/> indicates that either;
        /// <list type="bullet">
        /// <item>The mailbox is not <see cref="CanSelect"/>, or</item>
        /// <item><see cref="fMailboxCacheDataItems.messagecount"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>), or</item>
        /// <item>The value was not sent when requested.</item>
        /// </list>
        /// </para>
        /// <para>
        /// When the mailbox is selected the library maintains the value of this property by monitoring responses from the server,
        /// but for the value to be up-to-date 
        /// the responses have to be solicited (use <see cref="cIMAPClient.Poll"/> or allow idling using <see cref="cIMAPClient.IdleConfiguration"/>).
        /// </para>
        /// </remarks>
        public int? MessageCount
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(MessageCount));
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.Count;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.messagecount) == 0 || MailboxHandle.ListFlags?.CanSelect == false) return null;
                if (MailboxHandle.MailboxStatus == null) ZRequestMailboxData(fMailboxCacheDataSets.status, lContext);
                return MailboxHandle.MailboxStatus?.MessageCount;
            }
        }

        /// <summary>
        /// Gets the number of recent messages in the mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// See RFC 3501 for a definition of recent.
        /// </para>
        /// <para>
        /// This property always has a value when the mailbox is selected.
        /// </para>
        /// <para>
        /// <see langword="null"/> indicates that either;
        /// <list type="bullet">
        /// <item>The mailbox is not <see cref="CanSelect"/>, or</item>
        /// <item><see cref="fMailboxCacheDataItems.recentcount"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>), or</item>
        /// <item>The value was not sent when requested.</item>
        /// </list>
        /// </para>
        /// <para>
        /// When the mailbox is selected the library maintains the value of this property by monitoring responses from the server,
        /// but for the value to be up-to-date 
        /// the responses have to be solicited (use <see cref="cIMAPClient.Poll"/> or allow idling using <see cref="cIMAPClient.IdleConfiguration"/>).
        /// </para>
        /// </remarks>
        public int? RecentCount
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(RecentCount));
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.RecentCount;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.recentcount) == 0 || MailboxHandle.ListFlags?.CanSelect == false) return null;
                if (MailboxHandle.MailboxStatus == null) ZRequestMailboxData(fMailboxCacheDataSets.status, lContext);
                return MailboxHandle.MailboxStatus?.RecentCount;
            }
        }

        /// <summary>
        /// Gets the predicted next UID for the mailbox. May be <see langword="null"/> or zero.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property always has a value when the mailbox is selected, but zero indicates that the value is not known.
        /// </para>
        /// <para>
        /// <see langword="null"/> indicates that either;
        /// <list type="bullet">
        /// <item>The mailbox is not <see cref="CanSelect"/>, or</item>
        /// <item><see cref="fMailboxCacheDataItems.uidnext"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>), or</item>
        /// <item>The value was not sent when requested.</item>
        /// </list>
        /// </para>
        /// <para>
        /// When the mailbox is selected the library maintains the value of this property by monitoring IMAP FETCH responses from the server,
        /// but for the value to be up-to-date 
        /// FETCH responses containing the UID have to be solicited for new messages  
        /// (to get notification of new messages via <see cref="MessageDelivery"/> use <see cref="cIMAPClient.Poll"/> or allow idling using <see cref="cIMAPClient.IdleConfiguration"/>; to solicit the FETCH responses required
        /// use <see cref="Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cFetchCacheItemConfiguration)"/> with <see cref="cMessageDeliveryEventArgs.MessageHandles"/> and <see cref="fMessageCacheAttributes.uid"/>
        /// ).
        /// </para>
        /// </remarks>
        public uint? UIDNext
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(UIDNext));
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.UIDNext;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.uidnext) == 0 || MailboxHandle.ListFlags?.CanSelect == false) return null;
                if (MailboxHandle.MailboxStatus == null) ZRequestMailboxData(fMailboxCacheDataSets.status, lContext);
                return MailboxHandle.MailboxStatus?.UIDNext;
            }
        }

        /// <summary>
        /// Gets the count of messages added to the message cache since the mailbox was selected for which the library has not seen the value of the UID.
        /// </summary>
        public int UIDNextUnknownCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.UIDNextUnknownCount;
                return MailboxHandle.MailboxStatus?.UIDNextUnknownCount ?? 0;
            }
        }

        /// <summary>
        /// Gets the UIDValidity of the mailbox. May be <see langword="null"/> or zero.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property always has a value when the mailbox is selected but zero indicates that the server does not support unique identifiers.
        /// </para>
        /// <para>
        /// <see langword="null"/> indicates that either;
        /// <list type="bullet">
        /// <item>The mailbox does not support unique identifiers, or</item>
        /// <item>The mailbox is not <see cref="CanSelect"/>, or</item>
        /// <item><see cref="fMailboxCacheDataItems.uidvalidity"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>), or</item>
        /// <item>The value was not sent when requested.</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <seealso cref="UIDNotSticky"/>
        public uint? UIDValidity
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(UIDValidity));
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.UIDValidity;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.uidvalidity) == 0 || MailboxHandle.ListFlags?.CanSelect == false) return null;
                if (MailboxHandle.MailboxStatus == null) ZRequestMailboxData(fMailboxCacheDataSets.status, lContext);
                return MailboxHandle.MailboxStatus?.UIDValidity;
            }
        }

        /// <summary>
        /// Gets the number of unseen messages in the mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property always has a value when the mailbox is selected.
        /// </para>
        /// <para>
        /// <see langword="null"/> indicates that either;
        /// <list type="bullet">
        /// <item>The mailbox is not <see cref="CanSelect"/>, or</item>
        /// <item><see cref="fMailboxCacheDataItems.unseencount"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>), or</item>
        /// <item>The value was not sent when requested.</item>
        /// </list>
        /// </para>
        /// <para>
        /// When the mailbox is selected the library maintains the value of this property by monitoring IMAP FETCH responses from the server, 
        /// but for the value to be accurate it has to be initialised after the mailbox is selected using <see cref="SetUnseenCount"/> and
        /// FETCH responses containing the message flags have to be solicited for new messages  
        /// (to get notification of new messages via <see cref="MessageDelivery"/> use <see cref="cIMAPClient.Poll"/> or allow idling using <see cref="cIMAPClient.IdleConfiguration"/>; to solicit the FETCH responses required
        /// use <see cref="Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cFetchCacheItemConfiguration)"/> with <see cref="cMessageDeliveryEventArgs.MessageHandles"/> and <see cref="fMessageCacheAttributes.flags"/>
        /// ).
        /// </para>
        /// </remarks>
        public int? UnseenCount
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(UnseenCount));
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.UnseenCount;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.unseencount) == 0 || MailboxHandle.ListFlags?.CanSelect == false) return null;
                if (MailboxHandle.MailboxStatus == null) ZRequestMailboxData(fMailboxCacheDataSets.status, lContext);
                return MailboxHandle.MailboxStatus?.UnseenCount;
            }
        }

        /// <summary>
        /// Gets the count of messages added to the message cache since the mailbox was selected for which the library is unsure of the value of the <see cref="kMessageFlag.Seen"/> flag.
        /// </summary>
        public int UnseenUnknownCount
        {
            get
            {
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.UnseenUnknownCount;
                return MailboxHandle.MailboxStatus?.UnseenUnknownCount ?? 0;
            }
        }

        /// <summary>
        /// Gets the highest mod-sequence (see RFC 7162) for the mailbox. May be <see langword="null"/> or zero.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the mailbox is selected this property will always have a value but zero indicates that <see cref="cIMAPCapabilities.CondStore"/> is not in use or that the mailbox does not support the persistent storage of mod-sequences.
        /// </para>
        /// <para>
        /// <see langword="null"/> indicates that either;
        /// <list type="bullet">
        /// <item>The mailbox is not <see cref="CanSelect"/>, or</item>
        /// <item><see cref="fMailboxCacheDataItems.highestmodseq"/> is not being requested (see <see cref="cIMAPClient.MailboxCacheDataItems"/>), or</item>
        /// <item>The value was not sent when requested.</item>
        /// </list>
        /// </para>
        /// <para>
        /// When the mailbox is selected the library maintains the value of this property by monitoring responses from the server,
        /// but for the value to be up-to-date 
        /// the responses have to be solicited - use <see cref="cIMAPClient.Poll"/> or allow idling using <see cref="cIMAPClient.IdleConfiguration"/>.
        /// </para>
        /// </remarks>
        public ulong? HighestModSeq
        {
            get
            {
                var lContext = Client.RootContext.NewGetProp(nameof(cMailbox), nameof(HighestModSeq));
                var lSelectedMailboxDetails = Client.SelectedMailboxDetails;
                if (ReferenceEquals(lSelectedMailboxDetails?.MailboxHandle, MailboxHandle)) return lSelectedMailboxDetails.MessageCache.HighestModSeq;
                if ((Client.MailboxCacheDataItems & fMailboxCacheDataItems.highestmodseq) == 0 || MailboxHandle.ListFlags?.CanSelect == false) return null;
                if (MailboxHandle.MailboxStatus == null) ZRequestMailboxData(fMailboxCacheDataSets.status, lContext);
                return MailboxHandle.MailboxStatus?.HighestModSeq;
            }
        }

        private void ZRequestMailboxData(fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailbox), nameof(ZRequestMailboxData), pDataSets);
            var lTask = Client.RequestMailboxDataAsync(MailboxHandle, pDataSets, lContext);
            Client.Wait(lTask, lContext);
        }

        /// <summary>
        /// Indicates whether the mailbox has been selected at least once on the current connection.
        /// </summary>
        public bool HasBeenSelected => MailboxHandle.SelectedProperties.HasBeenSelected;

        /// <summary>
        /// Indicates whether the mailbox has been selected for update at least once on the current connection.
        /// </summary>
        public bool HasBeenSelectedForUpdate => MailboxHandle.SelectedProperties.HasBeenSelectedForUpdate;

        /// <summary>
        /// Indicates whether the mailbox has been selected read-only at least once on the current connection.
        /// </summary>
        public bool HasBeenSelectedReadOnly => MailboxHandle.SelectedProperties.HasBeenSelectedReadOnly;

        /// <summary>
        /// Indicates whether the mailbox has persistent UIDs (see RFC 4315). May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox has never been selected on the current connection.
        /// </remarks>
        public bool? UIDNotSticky => MailboxHandle.SelectedProperties.UIDNotSticky;

        /// <summary>
        /// Gets the message flags defined in the mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox has never been selected on the current connection.
        /// </remarks>
        public cFetchableFlags MessageFlags
        {
            get
            {
                var lSelectedProperties = MailboxHandle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelected) return null;
                return lSelectedProperties.MessageFlags;
            }
        }

        /// <summary>
        /// Gets the flags that the client can change permanently on messages in the mailbox when it is selected for update. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox has never been selected for update on the current connection.
        /// </remarks>
        public cMessageFlags ForUpdatePermanentFlags
        {
            get
            {
                var lSelectedProperties = MailboxHandle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelectedForUpdate) return null;
                return lSelectedProperties.ForUpdatePermanentFlags;
            }
        }

        /// <summary>
        /// Gets the flags that the client can change permanently on messages in the mailbox when it is selected read-only. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that the mailbox has never been selected read-only on the current connection.
        /// </remarks>
        public cMessageFlags ReadOnlyPermanentFlags
        {
            get
            {
                var lSelectedProperties = MailboxHandle.SelectedProperties;
                if (!lSelectedProperties.HasBeenSelectedReadOnly) return null;
                return lSelectedProperties.ReadOnlyPermanentFlags;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox is the currently selected mailbox.
        /// </summary>
        public bool IsSelected => ReferenceEquals(Client.SelectedMailboxDetails?.MailboxHandle, MailboxHandle);

        /// <summary>
        /// Indicates whether the mailbox is currently selected for update.
        /// </summary>
        public bool IsSelectedForUpdate
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null || !ReferenceEquals(lDetails.MailboxHandle, MailboxHandle)) return false;
                return lDetails.SelectedForUpdate;
            }
        }

        /// <summary>
        /// Indicates whether the mailbox is currently selected, but can't be modified.
        /// </summary>
        public bool IsAccessReadOnly
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null || !ReferenceEquals(lDetails.MailboxHandle, MailboxHandle)) return false;
                return lDetails.AccessReadOnly;
            }
        }

        /// <summary>
        /// Gets the message cache that is associated with the mailbox. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> when the mailbox is not selected.
        /// </remarks>
        public iMessageCache MessageCache
        {
            get
            {
                var lDetails = Client.SelectedMailboxDetails;
                if (lDetails == null || !ReferenceEquals(lDetails.MailboxHandle, MailboxHandle)) return null;
                return lDetails.MessageCache;
            }
        }

        public bool IsValid => ReferenceEquals(Client.MailboxCache, MailboxHandle.MailboxCache);

        /// <summary>
        /// Gets the mailbox's child mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        public IEnumerable<cMailbox> GetMailboxes(fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMailboxes), pDataSets);
            var lTask = Client.GetMailboxesAsync(MailboxHandle, pDataSets, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets the mailbox's child mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <inheritdoc cref="Mailboxes(fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<IEnumerable<cMailbox>> GetMailboxesAsync(fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMailboxesAsync), pDataSets);
            return Client.GetMailboxesAsync(MailboxHandle, pDataSets, lContext);
        }

        /// <summary>
        /// Gets the mailbox's subscribed child mailboxes.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <remarks>
        /// Mailboxes that do not exist may be returned.
        /// Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist as mailboxes on the server.
        /// </remarks>
        public IEnumerable<cMailbox> GetSubscribed(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetSubscribed), pDescend, pDataSets);
            var lTask = Client.GetSubscribedAsync(MailboxHandle, pDescend, pDataSets, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets the mailbox's subscribed child mailboxes.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <inheritdoc cref="Subscribed(bool, fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<IEnumerable<cMailbox>> GetSubscribedAsync(bool pDescend = false, fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetSubscribedAsync), pDescend, pDataSets);
            return Client.GetSubscribedAsync(MailboxHandle, pDescend, pDataSets, lContext);
        }

        /// <inheritdoc cref="iMailboxContainer.GetMailboxName(string)"/>
        public cMailboxName GetMailboxName(string pName)
        {
            if (MailboxHandle.MailboxName.Delimiter == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NoMailboxHierarchy);
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (pName.IndexOf(MailboxHandle.MailboxName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(MailboxHandle.MailboxName.GetDescendantPathPrefix() + pName, MailboxHandle.MailboxName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        /// <summary>
        /// Creates a child mailbox of the mailbox.
        /// </summary>
        /// <param name="pName"></param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="cIMAPClient.Create(cMailboxName, bool)" select="remarks"/>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = false) => Client.Create(GetMailboxName(pName), pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a child mailbox of the mailbox.
        /// </summary>
        /// <param name="pName"></param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <inheritdoc cref="CreateChild(string, bool)" select="returns|remarks"/>
        public Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent = false) => Client.CreateAsync(GetMailboxName(pName), pAsFutureParent);

        /// <summary>
        /// Subscribes to the mailbox.
        /// </summary>
        public void Subscribe()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Subscribe));
            Client.Wait(Client.SubscribeAsync(MailboxHandle, lContext), lContext);
        }

        /// <summary>
        /// Asynchronously subscribes to the mailbox.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Subscribe" select="remarks"/>
        public Task SubscribeAsync()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(SubscribeAsync));
            return Client.SubscribeAsync(MailboxHandle, lContext);
        }

        /// <summary>
        /// Unsubscribes from the mailbox.
        /// </summary>
        public void Unsubscribe()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Subscribe));
            Client.Wait(Client.UnsubscribeAsync(MailboxHandle, lContext), lContext);
        }

        /// <summary>
        /// Asynchronously unsubscribes from the mailbox.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Unsubscribe" select="remarks"/>
        public Task UnsubscribeAsync()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(SubscribeAsync));
            return Client.UnsubscribeAsync(MailboxHandle, lContext);
        }

        /// <summary>
        /// Changes the <see cref="Name"/> of the mailbox.
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        /// <remarks>
        /// After renaming the current instance will continue to have the same <see cref="Path"/>, which means that it will no longer represent a mailbox that <see cref="Exists"/> on the server (unless the mailbox <see cref="IsInbox"/>).
        /// A new instance representing a mailbox with the new <see cref="Path"/> is returned.
        /// </remarks>
        public cMailbox Rename(string pName)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Rename), pName);
            var lTask = Client.RenameAsync(MailboxHandle, ZRename(pName), lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Ansynchronously changes the <see cref="Name"/> of the mailbox.
        /// </summary>
        /// <param name="pName"></param>
        /// <inheritdoc cref="Rename(string)" select="returns|remarks"/>
        public Task<cMailbox> RenameAsync(string pName)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(RenameAsync), pName);
            return Client.RenameAsync(MailboxHandle, ZRename(pName), lContext);
        }

        private cMailboxName ZRename(string pName)
        {
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (MailboxHandle.MailboxName.Delimiter == null) return new cMailboxName(pName, null);
            if (pName.IndexOf(MailboxHandle.MailboxName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(MailboxHandle.MailboxName.ParentPath + MailboxHandle.MailboxName.Delimiter + pName, MailboxHandle.MailboxName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        /// <summary>
        /// Changes the <see cref="Path"/> of the mailbox.
        /// </summary>
        /// <param name="pContainer">The mailbox container that provides first part of the new <see cref="Path"/>.</param>
        /// <param name="pName">The new mailbox name inside the <paramref name="pContainer"/>. If <see langword="null"/> the current <see cref="Name"/> is used.</param>
        /// <inheritdoc cref="Rename(string)" select="returns|remarks"/>
        public cMailbox Rename(iMailboxContainer pContainer, string pName = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Rename), pContainer, pName);
            var lTask = Client.RenameAsync(MailboxHandle, ZRename(pContainer, pName), lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Ansynchronously changes the <see cref="Path"/> of the mailbox.
        /// </summary>
        /// <param name="pContainer"></param>
        /// <param name="pName"></param>
        /// <inheritdoc cref="Rename(iMailboxContainer, string)" select="returns|remarks"/>
        public Task<cMailbox> RenameAsync(iMailboxContainer pContainer, string pName = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(RenameAsync), pContainer, pName);
            return Client.RenameAsync(MailboxHandle, ZRename(pContainer, pName), lContext);
        }

        private cMailboxName ZRename(iMailboxContainer pContainer, string pName)
        {
            if (pContainer == null) throw new ArgumentNullException(nameof(pContainer));

            string lName;
            if (pName == null) lName = MailboxHandle.MailboxName.Name;
            else lName = pName;

            return pContainer.GetMailboxName(lName);
        }

        /// <summary>
        /// Deletes the mailbox.
        /// </summary>
        public void Delete()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Delete));
            Client.Wait(Client.DeleteAsync(MailboxHandle, lContext), lContext);
        }

        /// <summary>
        /// Asynchronously deletes the mailbox.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Delete" select="remarks"/>
        public Task DeleteAsync()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(DeleteAsync));
            return Client.DeleteAsync(MailboxHandle, lContext);
        }

        /// <summary>
        /// Selects the mailbox.
        /// </summary>
        /// <param name="pForUpdate">Indicates whether the mailbox should be selected for update or not.</param>
        /// <remarks>
        /// Selecting a mailbox un-selects the previously selected mailbox (if there was one).
        /// </remarks>
        public void Select(bool pForUpdate = false)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Select), pForUpdate);
            Client.Wait(Client.SelectAsync(MailboxHandle, pForUpdate, lContext), lContext);
        }

        /// <summary>
        /// Asynchronously selects the mailbox.
        /// </summary>
        /// <param name="pForUpdate">Indicates whether the mailbox should be selected for update or not.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Select(bool)" select="remarks"/>
        public Task SelectAsync(bool pForUpdate = false)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(SelectAsync), pForUpdate);
            return Client.SelectAsync(MailboxHandle, pForUpdate, lContext);
        }

        /// <summary>
        /// Expunges messages marked with the <see cref="kMessageFlag.Deleted"/> flag from the mailbox. The mailbox must be selected.
        /// </summary>
        /// <param name="pAndUnselect">Indicates whether the mailbox should also be un-selected.</param>
        /// <remarks>
        /// Setting <paramref name="pAndUnselect"/> to <see langword="true"/> also un-selects the mailbox; this reduces the amount of network activity associated with the expunge.
        /// </remarks>
        /// <seealso cref="cIMAPMessage.Deleted"/>
        /// <seealso cref="kMessageFlag.Deleted"/>
        /// <seealso cref="cIMAPMessage.Store(eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStore(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="cIMAPClient.Store(IEnumerable{cIMAPMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
        public void Expunge(bool pAndUnselect = false)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Expunge), pAndUnselect);
            Client.Wait(Client.ExpungeAsync(MailboxHandle, pAndUnselect, lContext), lContext);
        }

        /// <summary>
        /// Asynchronously expunges messages marked with the <see cref="kMessageFlag.Deleted"/> flag from the mailbox. The mailbox must be selected.
        /// </summary>
        /// <param name="pAndUnselect">Indicates whether the mailbox should also be un-selected.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Expunge(bool)" select="remarks"/>
        /// <seealso cref="cIMAPMessage.Deleted"/>
        /// <seealso cref="kMessageFlag.Deleted"/>
        /// <seealso cref="cIMAPMessage.StoreAsync(eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStoreAsync(cUID, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="UIDStoreAsync(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>,
        /// <seealso cref="cIMAPClient.StoreAsync(IEnumerable{cIMAPMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
        public Task ExpungeAsync(bool pAndUnselect = false)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(ExpungeAsync), pAndUnselect);
            return Client.ExpungeAsync(MailboxHandle, pAndUnselect, lContext);
        }

        /// <summary>
        /// Gets a list of messages contained in the mailbox from the server. The mailbox must be selected.
        /// </summary>
        /// <param name="pFilter">The filter to use to restrict the set of messages returned.</param>
        /// <param name="pSort">The sort to use to order the set of messages returned. If not specified <see cref="cIMAPClient.DefaultSort"/> will be used.</param>
        /// <param name="pItems">The set of items to ensure are cached for the returned messages. If not specified <see cref="cIMAPClient.DefaultMessageCacheItems"/> will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        /// <remarks>
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fIMAPMessageProperties"/>. This means that you can use values of those types as arguments to this method.</note>
        /// </remarks>
        public IEnumerable<cIMAPMessage> GetMessages(cFilter pFilter = null, cSort pSort = null, cMessageCacheItems pItems = null, cSetMaximumConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessages), pFilter, pSort, pItems, pConfiguration);
            var lTask = Client.GetMessagesAsync(MailboxHandle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultMessageCacheItems, pConfiguration, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets a list of messages contained in the mailbox from the server. The mailbox must be selected.
        /// </summary>
        /// <param name="pFilter">The filter to use to restrict the set of messages returned.</param>
        /// <param name="pSort">The sort to use to order the set of messages returned. If not specified <see cref="cIMAPClient.DefaultSort"/> will be used.</param>
        /// <param name="pItems">The set of items to ensure are cached for the returned messages. If not specified <see cref="cIMAPClient.DefaultMessageCacheItems"/> will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchCacheItemConfiguration)" select="returns|remarks"/>
        public Task<IEnumerable<cIMAPMessage>> GetMessagesAsync(cFilter pFilter = null, cSort pSort = null, cMessageCacheItems pItems = null, cSetMaximumConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessagesAsync), pFilter, pSort, pItems, pConfiguration);
            return Client.GetMessagesAsync(MailboxHandle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pItems ?? Client.DefaultMessageCacheItems, pConfiguration, lContext);
        }

        /// <summary>
        /// Gets a list of messages. (Useful when handling <see cref="MessageDelivery"/>.)
        /// </summary>
        /// <param name="pMessageHandles"></param>
        /// <param name="pItems">The set of items to ensure are cached for the returned messages. If not specified <see cref="cIMAPClient.DefaultMessageCacheItems"/> will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        /// <remarks>
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fIMAPMessageProperties"/>. This means that you can use values of those types as arguments to this method.</note>
        /// </remarks>
        public IEnumerable<cIMAPMessage> GetMessages(IEnumerable<iMessageHandle> pMessageHandles, cMessageCacheItems pItems = null, cIncrementConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessages), pItems, pConfiguration);
            var lMessageHandles = cMessageHandleList.FromMessageHandles(pMessageHandles);
            var lTask = Client.FetchCacheItemsAsync(lMessageHandles, pItems ?? Client.DefaultMessageCacheItems, pConfiguration, lContext);
            Client.Wait(lTask, lContext);
            return from lMessageHandle in pMessageHandles select new cIMAPMessage(Client, lMessageHandle);
        }

        /// <summary>
        /// Asynchronously gets a list of messages. (Useful when handling <see cref="MessageDelivery"/>.)
        /// </summary>
        /// <param name="pMessageHandles"></param>
        /// <param name="pItems">The set of items to ensure are cached for the returned messages. If not specified <see cref="cIMAPClient.DefaultMessageCacheItems"/> will be used.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cFetchCacheItemConfiguration)" select="returns|remarks"/>
        public async Task<IEnumerable<cIMAPMessage>> GetMessagesAsync(IEnumerable<iMessageHandle> pMessageHandles, cMessageCacheItems pItems = null, cIncrementConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessagesAsync), pItems, pConfiguration);
            var lMessageHandles = cMessageHandleList.FromMessageHandles(pMessageHandles);
            await Client.FetchCacheItemsAsync(lMessageHandles, pItems ?? Client.DefaultMessageCacheItems, pConfiguration, lContext).ConfigureAwait(false);
            return from lMessageHandle in pMessageHandles select new cIMAPMessage(Client, lMessageHandle);
        }

        /// <summary>
        /// Initialises the value of <see cref="UnseenCount"/>. The mailbox must be selected.
        /// </summary>
        /// <returns>A list of the unseen messages.</returns>
        /// <remarks>
        /// <see cref="UnseenCount"/> must be initialised after the mailbox is selected if the value is to be accurate while the mailbox is selected. See <see cref="UnseenCount"/> for more detail.
        /// </remarks>
        public cMessageHandleList SetUnseenCount()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(SetUnseenCount));
            var lTask = Client.SetUnseenCountAsync(MailboxHandle, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously initialises the value of <see cref="UnseenCount"/>. The mailbox must be selected.
        /// </summary>
        /// <inheritdoc cref="SetUnseenCount" select="returns|remarks"/>
        public Task<cMessageHandleList> SetUnseenCountAsync()
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(SetUnseenCountAsync));
            return Client.SetUnseenCountAsync(MailboxHandle, lContext);
        }

        /// <summary>
        /// Gets a <see cref="cIMAPMessage"/> from a <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pItems">The set of items to ensure are cached for the returned message.</param>
        /// <returns></returns>
        /// <remarks>
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fIMAPMessageProperties"/>. This means that you can use values of those types as arguments to this method.</note>
        /// </remarks>
        public cIMAPMessage GetMessage(cUID pUID, cMessageCacheItems pItems)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessage), pUID, pItems);
            var lTask = Client.GetMessagesAsync(MailboxHandle, cUIDList.FromUID(pUID), pItems, null, lContext);
            Client.Wait(lTask, lContext);
            return ZGetMessage(lTask.Result, lContext);
        }

        /// <summary>
        /// Asynchronously gets a <see cref="cIMAPMessage"/> from a <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pItems">The set of items to ensure are cached for the returned message.</param>
        /// <inheritdoc cref="Message(cUID, cMessageCacheItems)" select="returns|remarks"/>
        public async Task<cIMAPMessage> GetMessageAsync(cUID pUID, cMessageCacheItems pItems)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessageAsync), pUID, pItems);
            var lMessages = await Client.GetMessagesAsync(MailboxHandle, cUIDList.FromUID(pUID), pItems, null, lContext).ConfigureAwait(false);
            return ZGetMessage(lMessages, lContext);
        }

        private cIMAPMessage ZGetMessage(IEnumerable<cIMAPMessage> pMessages, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailbox), nameof(ZGetMessage));

            cIMAPMessage lResult = null;

            foreach (var lMessage in pMessages)
                if (lResult != null) throw new cInternalErrorException(lContext);
                else lResult = lMessage;

            return lResult;
        }

        /// <summary>
        /// Gets a list of <see cref="cIMAPMessage"/> from a set of <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pItems">The set of items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        /// <remarks>
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fIMAPMessageProperties"/>. This means that you can use values of those types as arguments to this method.</note>
        /// </remarks>
        public IEnumerable<cIMAPMessage> GetMessages(IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cIncrementConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessages), pItems, pConfiguration);
            var lTask = Client.GetMessagesAsync(MailboxHandle, cUIDList.FromUIDs(pUIDs), pItems, pConfiguration, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets a list of <see cref="cIMAPMessage"/> from a set of <see cref="cUID"/>. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pItems">The set of items to ensure are cached for the returned messages.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Messages(IEnumerable{cUID}, cMessageCacheItems, cFetchCacheItemConfiguration)" select="returns|remarks"/>
        public Task<IEnumerable<cIMAPMessage>> GetMessagesAsync(IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cIncrementConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetMessagesAsync), pItems, pConfiguration);
            return Client.GetMessagesAsync(MailboxHandle, cUIDList.FromUIDs(pUIDs), pItems, pConfiguration, lContext);
        }

        public IEnumerable<cUID> GetUIDs(cFilter pFilter = null, cSort pSort = null, cSetMaximumConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetUIDs), pFilter, pSort, pConfiguration);
            var lTask = Client.GetUIDsAsync(MailboxHandle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pConfiguration, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<IEnumerable<cUID>> GetUIDsAsync(cFilter pFilter = null, cSort pSort = null, cSetMaximumConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(GetUIDsAsync), pFilter, pSort, pConfiguration);
            return Client.GetUIDsAsync(MailboxHandle, pFilter ?? cFilter.All, pSort ?? Client.DefaultSort, pConfiguration, lContext);
        }

        /// <summary>
        /// Refreshes the data that is cached for the mailbox.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache.</param>
        public void Refresh(fMailboxCacheDataSets pDataSets)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Refresh), pDataSets);
            Client.Wait(Client.RequestMailboxDataAsync(MailboxHandle, pDataSets, lContext), lContext);
        }

        /// <summary>
        /// Asynchronously refreshes the data that is cached for the mailbox.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Refresh(fMailboxCacheDataSets)" select="remarks"/>
        public Task RefreshAsync(fMailboxCacheDataSets pDataSets)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(RefreshAsync), pDataSets);
            return Client.RequestMailboxDataAsync(MailboxHandle, pDataSets, lContext);
        }

        /// <summary>
        /// Copies a set of messages to the mailbox represented by the instance.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response an object containing the pairs of UIDs involved in the copy, otherwise <see langword="null"/>.</returns>
        /// <remarks>
        /// The messages must be in the currently selected mailbox.
        /// </remarks>
        public cCopyFeedback Copy(IEnumerable<cIMAPMessage> pMessages)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(Copy));
            var lTask = Client.CopyAsync(cMessageHandleList.FromMessages(pMessages), MailboxHandle, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously copies a set of messages to the mailbox represented by the instance.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <inheritdoc cref="Copy(IEnumerable{cIMAPMessage})" select="returns|remarks"/>
        public Task<cCopyFeedback> CopyAsync(IEnumerable<cIMAPMessage> pMessages)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(CopyAsync));
            return Client.CopyAsync(cMessageHandleList.FromMessages(pMessages), MailboxHandle, lContext);
        }

        /* TEMP comment out for cachefile work
        public cUID Append(cAppendData pData, cAppendConfiguration pConfiguration = null) => ZAppendResult(Client.Append(MailboxHandle, cAppendDataList.FromData(pData), pConfiguration));
        public async Task<cUID> AppendAsync(cAppendData pData, cAppendConfiguration pConfiguration = null) => ZAppendResult(await Client.AppendAsync(MailboxHandle, cAppendDataList.FromData(pData), pConfiguration).ConfigureAwait(false));
        public cUID Append(MailMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, cAppendMailMessageConfiguration pConfiguration = null) 
            => ZAppendResult(Client.Append(MailboxHandle, cMailMessageList.FromMessage(pMessage), pFlags, pReceived, pConfiguration));
        public async Task<cUID> AppendAsync(MailMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, cAppendMailMessageConfiguration pConfiguration = null)
            => ZAppendResult(await Client.AppendAsync(MailboxHandle, cMailMessageList.FromMessage(pMessage), pFlags, pReceived, pConfiguration).ConfigureAwait(false));

        private cUID ZAppendResult(cAppendFeedback pFeedback)
        {
            if (pFeedback.Count != 1) throw new cInternalErrorException(nameof(cMailbox),nameof(ZAppendResult), 1);
            var lFeedbackItem = pFeedback[0];
            if (pFeedback.SucceededCount == 1) return lFeedbackItem.UID;
            if (lFeedbackItem.Exception != null) throw lFeedbackItem.Exception;
            //if (lFeedbackItem.Type == eAppendFeedbackType.notattempted) should never happen: if so this would result in the internal error exception below
            var lResult = lFeedbackItem.Result;
            if (lResult == null) throw new cInternalErrorException(nameof(cMailbox), nameof(ZAppendResult), 2);
            if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, lFeedbackItem.TryIgnoring);
            throw new cIMAPProtocolErrorException(lResult, lFeedbackItem.TryIgnoring);
        }

        public cAppendFeedback Append(IEnumerable<cAppendData> pData, cAppendConfiguration pConfiguration = null) => Client.Append(MailboxHandle, cAppendDataList.FromData(pData), pConfiguration);
        public Task<cAppendFeedback> AppendAsync(IEnumerable<cAppendData> pData, cAppendConfiguration pConfiguration = null) => Client.AppendAsync(MailboxHandle, cAppendDataList.FromData(pData), pConfiguration);
        public cAppendFeedback Append(IEnumerable<MailMessage> pMessages, cStorableFlags pFlags = null, DateTime? pReceived = null, cAppendMailMessageConfiguration pConfiguration = null) 
            => Client.Append(MailboxHandle, cMailMessageList.FromMessages(pMessages), pFlags, pReceived, pConfiguration);
        public Task<cAppendFeedback> AppendAsync(IEnumerable<MailMessage> pMessages, cStorableFlags pFlags = null, DateTime? pReceived = null, cAppendMailMessageConfiguration pConfiguration = null)
            => Client.AppendAsync(MailboxHandle, cMailMessageList.FromMessages(pMessages), pFlags, pReceived, pConfiguration);
            */
        
        public Stream GetMessageDataStream(cUID pUID, cSection pSection = null, eDecodingRequired pDecoding = eDecodingRequired.none)
        {
            if (!IsValid) throw new InvalidOperationException(kInvalidOperationExceptionMessage.IsInvalid);
            return new cIMAPMessageDataStream(Client, MailboxHandle, pUID, pSection ?? cSection.All, pDecoding);
        }

        /// <summary>
        /// Stores flags for a message. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        /// <remarks>
        /// <paramref name="pIfUnchangedSinceModSeq"/> may only be specified if <see cref="HighestModSeq"/> is not zero. 
        /// (i.e. <see cref="cIMAPCapabilities.CondStore"/> is in use and the mailbox supports the persistent storage of mod-sequences.)
        /// If the message has been modified since the specified value then the server will fail the store.
        /// </remarks>
        public cUIDStoreFeedback UIDStore(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDStore), pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);
            var lFeedback = new cUIDStoreFeedback(pUID, pOperation, pFlags);
            Client.Wait(Client.UIDStoreAsync(MailboxHandle, lFeedback, pIfUnchangedSinceModSeq, lContext), lContext);
            return lFeedback;
        }

        /// <summary>
        /// Asynchronously stores flags for a message. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public async Task<cUIDStoreFeedback> UIDStoreAsync(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDStoreAsync), pUID, pOperation, pFlags, pIfUnchangedSinceModSeq);
            var lFeedback = new cUIDStoreFeedback(pUID, pOperation, pFlags);
            await Client.UIDStoreAsync(MailboxHandle, lFeedback, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        /// <summary>
        /// Stores flags for a set of messages. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public cUIDStoreFeedback UIDStore(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDStore), pOperation, pFlags, pIfUnchangedSinceModSeq);
            var lFeedback = new cUIDStoreFeedback(pUIDs, pOperation, pFlags);
            Client.Wait(Client.UIDStoreAsync(MailboxHandle, lFeedback, pIfUnchangedSinceModSeq, lContext), lContext);
            return lFeedback;
        }

        /// <summary>
        /// Asynchronously stores flags for a set of messages. The mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public async Task<cUIDStoreFeedback> UIDStoreAsync(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDStoreAsync), pOperation, pFlags, pIfUnchangedSinceModSeq);
            var lFeedback = new cUIDStoreFeedback(pUIDs, pOperation, pFlags);
            await Client.UIDStoreAsync(MailboxHandle, lFeedback, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        /// <summary>
        /// Copies a message in this mailbox to another mailbox. The mailbox must be selected.
        /// </summary>
        /// <param name="pUID"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response, the UID of the message in the destination mailbox, otherwise <see langword="null"/>.</returns>
        public cUID UIDCopy(cUID pUID, cMailbox pDestination)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDCopy), pUID, pDestination);
            if (pDestination == null) throw new ArgumentNullException(nameof(pDestination));
            if (!ReferenceEquals(pDestination.Client, Client)) throw new ArgumentOutOfRangeException(nameof(pDestination));
            var lTask = Client.UIDCopyAsync(MailboxHandle, cUIDList.FromUID(pUID), pDestination.MailboxHandle, lContext);
            Client.Wait(lTask, lContext);
            var lFeedback = lTask.Result;
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedMessageUID;
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
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDCopyAsync), pUID, pDestination);
            if (pDestination == null) throw new ArgumentNullException(nameof(pDestination));
            if (!ReferenceEquals(pDestination.Client, Client)) throw new ArgumentOutOfRangeException(nameof(pDestination));
            var lFeedback = await Client.UIDCopyAsync(MailboxHandle, cUIDList.FromUID(pUID), pDestination.MailboxHandle, lContext).ConfigureAwait(false);
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedMessageUID;
            return null;
        }

        /// <summary>
        /// Copies messages in this mailbox to another mailbox. This mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response, an object containing the pairs of UIDs involved in the copy, otherwise <see langword="null"/>.</returns>
        public cCopyFeedback UIDCopy(IEnumerable<cUID> pUIDs, cMailbox pDestination)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDCopy), pDestination);
            if (pDestination == null) throw new ArgumentNullException(nameof(pDestination));
            if (!ReferenceEquals(pDestination.Client, Client)) throw new ArgumentOutOfRangeException(nameof(pDestination));
            var lTask = Client.UIDCopyAsync(MailboxHandle, cUIDList.FromUIDs(pUIDs), pDestination.MailboxHandle, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously copies messages in this mailbox to another mailbox. This mailbox must be selected.
        /// </summary>
        /// <param name="pUIDs"></param>
        /// <param name="pDestination"></param>
        /// <inheritdoc cref="UIDCopy(IEnumerable{cUID}, cMailbox)" select="returns|remarks"/>
        public Task<cCopyFeedback> UIDCopyAsync(IEnumerable<cUID> pUIDs, cMailbox pDestination)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cMailbox), nameof(UIDCopyAsync), pDestination);
            if (pDestination == null) throw new ArgumentNullException(nameof(pDestination));
            if (!ReferenceEquals(pDestination.Client, Client)) throw new ArgumentOutOfRangeException(nameof(pDestination));
            return Client.UIDCopyAsync(MailboxHandle, cUIDList.FromUIDs(pUIDs), pDestination.MailboxHandle, lContext);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMailbox pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(iMailboxContainer pObject) => this == pObject as cMailbox;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cMailbox;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode() => MailboxHandle.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailbox)}({MailboxHandle})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cMailbox pA, cMailbox pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MailboxHandle.Equals(pB.MailboxHandle);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cMailbox pA, cMailbox pB) => !(pA == pB);
    }
}
 