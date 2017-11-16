using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    // TODO: improve the documentation here

    /// <summary>
    /// Represents an internal message cache.
    /// </summary>
    /// <seealso cref="iMessageHandle"/>
    /// <seealso cref="iSelectedMailboxDetails"/>
    public interface iMessageCache : IReadOnlyList<iMessageHandle>
    {
        /**<summary>Gets the internal mailbox handle of the mailbox that this message cache belongs to.</summary>*/
        iMailboxHandle MailboxHandle { get; }
        /**<summary>Indicates that the mailbox does not support the persistent storage of mod-sequences.</summary>*/
        bool NoModSeq { get; }
        /**<summary>Gets the number of recent messages in the mailbox.</summary>*/
        int RecentCount { get; }
        /**<summary>Gets the predicted next UID for the mailbox.</summary>*/
        uint UIDNext { get; }
        /**<summary>Indicates how inaccurate the <see cref="UIDNext"/> is.</summary>*/
        int UIDNextUnknownCount { get; }
        /**<summary>Gets the UIDValidity of the mailbox.</summary>*/
        uint UIDValidity { get; }
        /**<summary>Gets the number of unseen messages in the mailbox.</summary>*/
        int UnseenCount { get; }
        /**<summary>Indicates how inaccurate the <see cref="UnseenCount"/> is.</summary>*/
        int UnseenUnknownCount { get; }
        /**<summary>Gets the highest modification sequence number for the mailbox.</summary>*/
        ulong HighestModSeq { get; }
    }
}
