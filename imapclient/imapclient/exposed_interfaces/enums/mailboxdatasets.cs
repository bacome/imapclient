using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>Sets of data that can be fetched about a mailbox.</para>
    /// <para>The exact data items fetched depend on what is being cached, see <see cref="cIMAPClient.MailboxCacheData"/>.</para>
    /// </summary>
    [Flags]
    public enum fMailboxCacheDataSets
    {
        /// <summary>
        /// <para>Data returned by the IMAP LIST command.</para>
        /// <para>This data affects the following <see cref="cMailbox"/> properties;
        /// <list type="bullet">
        /// <item><term>Always:</term><description><see cref="cMailbox.Exists"/>, <see cref="cMailbox.CanHaveChildren"/>, <see cref="cMailbox.CanSelect"/>, <see cref="cMailbox.IsMarked"/>, <see cref="cMailbox.IsRemote"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.children"/>:</term><description><see cref="cMailbox.HasChildren"/></description></item>
        /// <item><term>If caching <see cref="fMailboxCacheData.specialuse"/>:</term><description><see cref="cMailbox.ContainsAll"/>, <see cref="cMailbox.IsArchive"/>, <see cref="cMailbox.ContainsDrafts"/> etc</description></item>
        /// </list>
        /// </para>
        /// </summary>
        list = 1 << 0,

        /// <summary>
        /// <para>Data returned by the IMAP LSUB command.</para>
        /// <para>This data affects the <see cref="cMailbox.IsSubscribed"/> property.</para>
        /// </summary>
        lsub = 1 << 1,

        /// <summary>
        /// <para>Data returned by the IMAP STATUS command.</para>
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
        /// </summary>
        status = 1 << 2
    }
}