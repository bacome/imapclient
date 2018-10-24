using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents sets of data items that can be requested about a mailbox. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The exact data items requested depend on the value of <see cref="cIMAPClient.MailboxCacheDataItems"/>.
    /// </para>
    /// <para>
    /// The <see cref="list"/> set of data will request data items that back the following <see cref="cMailbox"/> properties;
    /// <list type="bullet">
    /// <item>
    /// Always;
    ///   <list type="bullet">
    ///   <item><see cref="cMailbox.Exists"/></item>
    ///   <item><see cref="cMailbox.CanHaveChildren"/></item>
    ///   <item><see cref="cMailbox.CanSelect"/></item>
    ///   <item><see cref="cMailbox.IsMarked"/></item>
    ///   <item><see cref="cMailbox.IsRemote"/></item>
    ///   </list>
    /// </item>
    /// <item>
    /// If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.children"/>;
    ///   <list type="bullet">
    ///   <item><see cref="cMailbox.HasChildren"/></item>
    ///   </list>
    /// </item>
    /// <item>
    /// If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.specialuse"/>;
    ///   <list type="bullet">
    ///   <item><see cref="cMailbox.ContainsAll"/></item>
    ///   <item><see cref="cMailbox.IsArchive"/></item>
    ///   <item><see cref="cMailbox.ContainsDrafts"/></item>
    ///   <item><see cref="cMailbox.ContainsFlagged"/></item>
    ///   <item><see cref="cMailbox.ContainsJunk"/></item>
    ///   <item><see cref="cMailbox.ContainsSent"/></item>
    ///   <item><see cref="cMailbox.ContainsTrash"/></item>
    ///   </list>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// The <see cref="lsub"/> set of data will request data items that back <see cref="cMailbox.IsSubscribed"/>. 
    /// </para>
    /// <para>
    /// The <see cref="status"/> set of data will request data items that back the following <see cref="cMailbox"/> properties;
    /// <list type="bullet">
    /// <item>If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.messagecount"/>: <see cref="cMailbox.MessageCount"/></item>
    /// <item>If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.recentcount"/>: <see cref="cMailbox.RecentCount"/></item>
    /// <item>If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.uidvalidity"/>: <see cref="cMailbox.UIDValidity"/></item>
    /// <item>If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.uidnext"/>: <see cref="cMailbox.UIDNext"/></item>
    /// <item>If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.unseencount"/>: <see cref="cMailbox.UnseenCount"/></item>
    /// <item>If <see cref="cIMAPClient.MailboxCacheDataItems"/> includes <see cref="fMailboxCacheDataItems.highestmodseq"/>: <see cref="cMailbox.HighestModSeq"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="cNamespace.Mailboxes(fMailboxCacheDataSets)"/>
    /// <seealso cref="cNamespace.Subscribed(bool, fMailboxCacheDataSets)"/>
    /// <seealso cref="cMailbox.Mailboxes(fMailboxCacheDataSets)"/>
    /// <seealso cref="cMailbox.Subscribed(bool, fMailboxCacheDataSets)"/>
    /// <seealso cref="cMailbox.Refresh(fMailboxCacheDataSets)"/>
    /// <seealso cref="iMailboxContainer.Mailboxes(fMailboxCacheDataSets)"/>
    /// <seealso cref="iMailboxContainer.Subscribed(bool, fMailboxCacheDataSets)"/>
    /// <seealso cref="cIMAPClient.Mailboxes(string, char?, fMailboxCacheDataSets)"/>
    /// <seealso cref="cIMAPClient.Subscribed(string, char?, bool, fMailboxCacheDataSets)"/>
    [Flags]
    public enum fMailboxCacheDataSets
    {
        /**<summary>The data returned by the IMAP LIST command.</summary>*/
        list = 1 << 0,
        /**<summary>The data returned by the IMAP LSUB command.</summary>*/
        lsub = 1 << 1,
        /**<summary>The data returned by the IMAP STATUS command.</summary>*/
        status = 1 << 2,
        /**<summary>The data returned by the IMAP LIST, LSUB and STATUS commands.</summary>*/
        all = list | lsub | status
    }
}