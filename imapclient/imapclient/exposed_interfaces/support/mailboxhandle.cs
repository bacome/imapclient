using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents an IMAP mailbox uniquely within a mailbox cache.
    /// </summary>
    /// <seealso cref="cMailbox.Handle"/>
    /// <seealso cref="iSelectedMailboxDetails"/>
    /// <seealso cref="iMessageCache"/>
    /// <seealso cref="cMailboxPropertyChangedEventArgs"/>"/>
    /// <seealso cref="cMailboxMessageDeliveryEventArgs"/>"/>
    public interface iMailboxHandle
    {
        /**<summary>Gets an object that represents the mailbox cache that this instance belongs to.</summary>*/
        object Cache { get; }
        /**<summary>Gets the name of the mailbox.</summary>*/
        cMailboxName MailboxName { get; }
        /**<summary>Indicates whether the mailbox exists on the server.</summary>*/
        bool? Exists { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox, may be <see langword="null"/>.</summary>*/
        cListFlags ListFlags { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox, may be <see langword="null"/>.</summary>*/
        cLSubFlags LSubFlags { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox, may be <see langword="null"/>.</summary>*/
        cMailboxStatus MailboxStatus { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox.</summary>*/
        cMailboxSelectedProperties SelectedProperties { get; } // not null
    }
}