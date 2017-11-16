using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Uniquely identifies a named mailbox in an internal mailbox cache.
    /// </summary>
    /// <seealso cref="cMailbox.Handle"/>
    /// <seealso cref="iSelectedMailboxDetails"/>
    /// <seealso cref="iMessageCache"/>
    /// <seealso cref="cMailboxPropertyChangedEventArgs"/>"/>
    /// <seealso cref="cMailboxMessageDeliveryEventArgs"/>"/>
    public interface iMailboxHandle
    {
        /**<summary>Gets an object that represents the internal mailbox cache that this handle belongs to.</summary>*/
        object Cache { get; }
        /**<summary>Gets the mailbox name associated with this handle.</summary>*/
        cMailboxName MailboxName { get; }
        /**<summary>Indicates if the mailbox referred to by the handle exists on the server.</summary>*/
        bool? Exists { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox, may be <see langword="null"/>.</summary>*/
        cListFlags ListFlags { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox, may be <see langword="null"/>.</summary>*/
        cLSubFlags LSubFlags { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox, may be <see langword="null"/>.</summary>*/
        cMailboxStatus MailboxStatus { get; }
        /**<summary>Gets an object that contains a subset of the data held about the mailbox, may be <see langword="null"/>.</summary>*/
        cMailboxSelectedProperties SelectedProperties { get; } // not null
    }
}