using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A set of optionally cached mailbox attributes
    /// </summary>
    [Flags]
    public enum fMailboxCacheData
    {
        /// <summary>
        /// the backing data for the cMailbox.IsSubscribed property
        /// </summary>
        /// <seealso cref="cMailbox.IsSubscribed"/>
        subscribed = 1 << 0,

        /// <summary>
        /// the backing data for the cMailbox.HasChildren property
        /// </summary>
        /// <seealso cref="cMailbox.HasChildren"/>
        children = 1 << 1,

        /// <summary>
        /// the backing data for the cMailbox Contains* properties and the IsArchive property
        /// </summary>
        /// <seealso cref="cMailbox.ContainsAll"/>
        /// <seealso cref="cMailbox.IsArchive"/>
        /// <seealso cref="cMailbox.ContainsDrafts"/>
        /// <seealso cref="cMailbox.ContainsFlagged"/>
        /// <seealso cref="cMailbox.ContainsJunk"/>
        /// <seealso cref="cMailbox.ContainsSent"/>
        /// <seealso cref="cMailbox.ContainsTrash"/>
        specialuse = 1 << 2,

        /// <summary>
        /// the backing data for the cMailbox.MessageCount property
        /// </summary>
        /// <seealso cref="cMailbox.MessageCount"/>
        messagecount = 1 << 3,

        /// <summary>
        /// the backing data for the cMailbox.RecentCount property
        /// </summary>
        /// <seealso cref="cMailbox.RecentCount"/>
        recentcount = 1 << 4,

        /// <summary>
        /// the backing data for the cMailbox.UIDNext property
        /// </summary>
        /// <seealso cref="cMailbox.UIDNext"/>
        uidnext = 1 << 5,

        /// <summary>
        /// the backing data for the cMailbox.UIDValidity property
        /// </summary>
        /// <seealso cref="cMailbox.UIDValidity"/>
        uidvalidity = 1 << 6,

        /// <summary>
        /// the backing data for the cMailbox.UnseenCount property
        /// </summary>
        /// <seealso cref="cMailbox.UnseenCount"/>
        unseencount = 1 << 7,

        /// <summary>
        /// the backing data for the cMailbox.HighestModSeq property
        /// </summary>
        /// <remarks>
        /// Note that if the mailbox does not support CONDSTORE (RFC 7162) then the value will be null
        /// </remarks>
        /// <seealso cref="cMailbox.HighestModSeq"/>
        highestmodseq = 1 << 8,

        allstatus = messagecount | recentcount | uidnext | uidvalidity | unseencount | highestmodseq
    }
}