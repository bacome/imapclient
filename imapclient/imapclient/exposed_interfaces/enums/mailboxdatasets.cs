using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Sets of data that can be requested about a mailbox. The exact data items requested depend on the value of <see cref="cIMAPClient.MailboxCacheData"/>. See <see cref="cMailbox.Fetch(fMailboxCacheDataSets)"/>.
    /// </summary>
    [Flags]
    public enum fMailboxCacheDataSets
    {
        /// <summary>
        /// Data returned by the IMAP LIST command.
        /// </summary>
        /// <remarks>
        /// <para>This data affects the following <see cref="cMailbox"/> properties;
        /// <list type="bullet">
        /// <item><term>Always:</term><description><see cref="cMailbox.Exists"/>, <see cref="cMailbox.CanHaveChildren"/>, <see cref="cMailbox.CanSelect"/>, <see cref="cMailbox.IsMarked"/>, <see cref="cMailbox.IsRemote"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.children"/>:</term><description><see cref="cMailbox.HasChildren"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.specialuse"/>:</term><description><see cref="cMailbox.ContainsAll"/>, <see cref="cMailbox.IsArchive"/>, <see cref="cMailbox.ContainsDrafts"/> etc</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        list = 1 << 0,

        /// <summary>
        /// Data returned by the IMAP LSUB command. This data affects the <see cref="cMailbox.IsSubscribed"/> property.
        /// </summary>
        lsub = 1 << 1,

        /// <summary>
        /// Data returned by the IMAP STATUS command.
        /// </summary>
        /// <remarks>
        /// <para>This data affects the following <see cref="cMailbox"/> properties;
        /// <list type="bullet">
        /// <item><term>If caching <see cref="fMailboxCacheData.messagecount"/>:</term><description><see cref="cMailbox.MessageCount"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.recentcount"/>:</term><description><see cref="cMailbox.RecentCount"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.uidnext"/>:</term><description><see cref="cMailbox.UIDNext"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.uidvalidity"/>:</term><description><see cref="cMailbox.UIDValidity"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.unseencount"/>:</term><description><see cref="cMailbox.UnseenCount"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.highestmodseq"/>:</term><description><see cref="cMailbox.HighestModSeq"/></description></item>
        /// </list>
        /// </para>
        /// </remarks>
        status = 1 << 2
    }
}