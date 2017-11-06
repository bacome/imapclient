using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>Sets of data that can be fetched about a mailbox.</para>
    /// <para>See <see cref="cIMAPClient.MailboxCacheData"/>.</para>
    /// </summary>
    [Flags]
    public enum fMailboxCacheDataSets
    {
        ;?; // this not working
        /// <summary>
        /// <para>Data returned by the LIST command.</para>
        /// <para>Affects the following properties;
        /// <list type="table">
        /// <listheader><term>When</term><term>Properties</term></listheader>
        /// <item>Always</item><description><see cref="cMailbox.Exists"/><see cref="cMailbox.CanHaveChildren"/><see cref="cMailbox.CanSelect"/><see cref="cMailbox.IsMarked"/><see cref="cMailbox.IsRemote"/></description>
        /// <item>If caching <see cref="fMailboxCacheData.children"/></item><description><see cref="cMailbox.HasChildren"/></description>
        /// <item>If caching <see cref="fMailboxCacheData.specialuse"/></item><description><see cref="cMailbox.ContainsAll"/>, <see cref="cMailbox.IsArchive"/> etc</description>
        /// </list>
        /// </para>
        /// </summary>
        list = 1 << 0,

        /// <summary>
        /// <para>Data returned by the LSUB command.</para>
        /// <para>Affects <see cref="cMailbox.IsSubscribed"/>.</para>
        /// </summary>
        lsub = 1 << 1,

        /// <summary>
        /// <para>Data returned by the STATUS command.</para>
        /// <para>Affects the following properties;
        /// <list type="table">
        /// <listheader><term>When caching</term><term>Property</term></listheader>
        /// <item><see cref="fMailboxCacheData.messagecount"/></item><description><see cref="cMailbox.MessageCount"/></description>
        /// <item><see cref="fMailboxCacheData.recentcount"/></item><description><see cref="cMailbox.RecentCount"/></description>
        /// <item><see cref="fMailboxCacheData.uidnext"/></item><description><see cref="cMailbox.UIDNext"/></description>
        /// <item><see cref="fMailboxCacheData.uidvalidity"/></item><description><see cref="cMailbox.UIDValidity"/></description>
        /// <item><see cref="fMailboxCacheData.unseencount"/></item><description><see cref="cMailbox.UnseenCount"/></description>
        /// <item><see cref="fMailboxCacheData.highestmodseq"/></item><description><see cref="cMailbox.HighestModSeq"/></description>
        /// </list>
        /// </para>
        /// </summary>
        status = 1 << 2
    }
}