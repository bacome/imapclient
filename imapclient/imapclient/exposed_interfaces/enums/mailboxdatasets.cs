using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Sets of data that can be fetched about a mailbox
    /// </summary>
    [Flags]
    public enum fMailboxCacheDataSets
    {
        /// <summary>
        /// Data returned by the LIST command
        /// </summary>
        /// <remarks>
        /// Affects the following properties of the mailbox class
        /// ALWAYS: Exists, CanHaveChildren, CanSelect, IsMarked, IsRemote
        /// IF the internal mailbox cache is caching the backing data: HasChildren (fMailboxCacheData.children), Contains* and IsArchive (fMailboxCacheData.specialuse)
        /// </remarks>
        /// <seealso cref="cIMAPClient.MailboxCacheData"/>
        /// <seealso cref="fMailboxCacheData.children"/>
        /// <seealso cref="fMailboxCacheData.specialuse"/>
        /// <seealso cref="cMailbox.Exists"/>
        /// <seealso cref="cMailbox.CanHaveChildren"/>
        /// <seealso cref="cMailbox.CanSelect"/>
        /// <seealso cref="cMailbox.IsMarked"/>
        /// <seealso cref="cMailbox.IsRemote"/>
        /// <seealso cref="cMailbox.HasChildren"/>
        /// <seealso cref="cMailbox.ContainsAll"/>
        /// <seealso cref="cMailbox.IsArchive"/>
        /// <seealso cref="cMailbox.ContainsDrafts"/>
        /// <seealso cref="cMailbox.ContainsFlagged"/>
        /// <seealso cref="cMailbox.ContainsJunk"/>
        /// <seealso cref="cMailbox.ContainsSent"/>
        /// <seealso cref="cMailbox.ContainsTrash"/>
        list = 1 << 0,

        /// <summary>
        /// Data returned by the LSUB command
        /// </summary>
        /// <remarks>
        /// Affects the IsSubscribed property of the mailbox class
        /// </remarks>
        /// <seealso cref="cIMAPClient.MailboxCacheData"/>
        /// <seealso cref="fMailboxCacheData.subscribed"/>
        /// <seealso cref="cMailbox.IsSubscribed"/>
        lsub = 1 << 1,

        /// <summary>
        /// Data returned by the STATUS command
        /// </summary>
        /// <remarks>
        /// Affects the following properties of the mailbox class IF the internal mailbox cache is caching the backing data: MessageCount, RecentCount, UIDNext, UIDValidity, UnseenCount, HighestModSeq
        /// </remarks>
        /// <seealso cref="cIMAPClient.MailboxCacheData"/>
        /// <seealso cref="fMailboxCacheData.messagecount"/>
        /// <seealso cref="fMailboxCacheData.recentcount"/>
        /// <seealso cref="fMailboxCacheData.uidnext"/>
        /// <seealso cref="fMailboxCacheData.uidvalidity"/>
        /// <seealso cref="fMailboxCacheData.unseencount"/>
        /// <seealso cref="fMailboxCacheData.highestmodseq"/>
        /// <seealso cref="cMailbox.MessageCount"/>
        /// <seealso cref="cMailbox.RecentCount"/>
        /// <seealso cref="cMailbox.UIDNext"/>
        /// <seealso cref="cMailbox.UIDValidity"/>
        /// <seealso cref="cMailbox.UnseenCount"/>
        /// <seealso cref="cMailbox.HighestModSeq"/>
        status = 1 << 2
    }
}