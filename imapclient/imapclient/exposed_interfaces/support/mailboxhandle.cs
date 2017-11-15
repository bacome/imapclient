using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents a mailbox in the internal mailbox cache.
    /// </summary>
    public interface iMailboxHandle
    {
        /**<summary>Gets the cache that this mailbox handle belongs to.</summary>*/
        object Cache { get; }
        /**<summary>Gets the mailbox name associated with the handle, may be <see langword="null"/>.</summary>*/
        cMailboxName MailboxName { get; }
        /**<summary>Indicates if the referenced mailbox on the server exists.</summary>*/
        bool? Exists { get; }
        /**<summary>Gets an object that contains the flags returned by the IMAP LIST command.</summary>*/
        cListFlags ListFlags { get; }
        /**<summary>Gets an object that contains the flags returned by the IMAP LSUB command.</summary>*/
        cLSubFlags LSubFlags { get; }
        /**<summary>Gets an object that contains the data returned by the IMAP STATUS command.</summary>*/
        cMailboxStatus MailboxStatus { get; }
        /**<summary>Gets an object that contains data that the server sends when a mailbox is in the process of being selected.</summary>*/
        cMailboxSelectedProperties SelectedProperties { get; } // not null
    }
}