using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a mailbox container.
    /// </summary>
    /// <seealso cref="cNamespace"/>
    /// <seealso cref="cMailbox"/>
    public interface iMailboxContainer
    {
        /// <summary>
        /// Gets the hierarchy delimiter used in the container. May be <see langword="null"/>. 
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> if the server has no hierarchy in its names.
        /// </remarks>
        char? Delimiter { get; }

        /// <summary>
        /// Gets the mailboxes at the top level of hierarchy in the container.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Asynchronously gets the mailboxes at the top level of hierarchy in the container.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Gets the subscribed mailboxes in the container.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all subscribed mailboxes in the container are returned, if <see langword="false"/> only mailboxes at the top level of hierarchy are returned.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <remarks>
        /// Mailboxes that do not exist may be returned.
        /// Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist as mailboxes on the server.
        /// </remarks>
        List<cMailbox> Subscribed(bool pDescend, fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Asynchronously gets the subscribed mailboxes in the container.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all subscribed mailboxes in the container are returned, if <see langword="false"/> only mailboxes at the top level of hierarchy are returned.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Subscribed(bool, fMailboxCacheDataSets)" select="returns|remarks"/>
        Task<List<cMailbox>> SubscribedAsync(bool pDescend, fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Creates a mailbox at the top level of hierarchy in the container.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="cIMAPClient.Create(cMailboxName, bool)" select="remarks"/>
        cMailbox CreateChild(string pName, bool pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a mailbox at the top level of hierarchy in the container.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="CreateChild(string, bool)" select="remarks"/>
        Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent);
    }
}