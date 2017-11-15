using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of optionally requested mailbox data items.
    /// </summary>
    /// <seealso cref="cIMAPClient.MailboxCacheDataItems"/>
    [Flags]
    public enum fMailboxCacheDataItems
    {
        /// <summary>
        /// The backing data for <see cref="cMailbox.IsSubscribed"/>.
        /// </summary>
        subscribed = 1 << 0,

        /// <summary>
        /// The backing data for <see cref="cMailbox.HasChildren"/>.
        /// </summary>
        children = 1 << 1,

        /// <summary>
        /// The backing data for;
        /// <list type="bullet">
        /// <item><see cref="cMailbox.ContainsAll"/></item>
        /// <item><see cref="cMailbox.IsArchive"/></item>
        /// <item><see cref="cMailbox.ContainsDrafts"/></item>
        /// <item><see cref="cMailbox.ContainsFlagged"/></item>
        /// <item><see cref="cMailbox.ContainsJunk"/></item>
        /// <item><see cref="cMailbox.ContainsSent"/></item>
        /// <item><see cref="cMailbox.ContainsTrash"/></item>
        /// </list>
        /// </summary>
        specialuse = 1 << 2,

        /// <summary>
        /// The backing data for <see cref="cMailbox.MessageCount"/>.
        /// </summary>
        messagecount = 1 << 3,

        /// <summary>
        /// The backing data for <see cref="cMailbox.RecentCount"/>.
        /// </summary>
        recentcount = 1 << 4,

        /// <summary>
        /// The backing data for <see cref="cMailbox.UIDNext"/>.
        /// </summary>
        uidnext = 1 << 5,

        /// <summary>
        /// The backing data for <see cref="cMailbox.UIDValidity"/>.
        /// </summary>
        uidvalidity = 1 << 6,

        /// <summary>
        /// The backing data for <see cref="cMailbox.UnseenCount"/>.
        /// </summary>
        unseencount = 1 << 7,

        /// <summary>
        /// The backing data for <see cref="cMailbox.HighestModSeq"/>. The value is only requested if <see cref="cCapabilities.CondStore"/> is in use and the mailbox supports the persistent storage of mod-sequences.
        /// </summary>
        highestmodseq = 1 << 8,

        /// <summary>
        /// The backing data retrieved by the IMAP STATUS command.
        /// </summary>
        allstatus = messagecount | recentcount | uidnext | uidvalidity | unseencount | highestmodseq
    }
}