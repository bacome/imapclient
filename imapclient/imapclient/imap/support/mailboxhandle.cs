using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents an IMAP mailbox uniquely within a mailbox cache.
    /// </summary>
    public interface iMailboxHandle
    {
        /**<summary>Gets the mailbox cache that the instance belongs to.</summary>*/
        iMailboxCache MailboxCache { get; }
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