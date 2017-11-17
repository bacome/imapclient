using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an mailbox container.
    /// </summary>
    /// <seealso cref="cNamespace"/>
    /// <seealso cref="cMailbox"/>
    public interface iMailboxContainer
    {
        /// <summary>
        /// Gets the hierarchy delimiter. May be <see langword="null"/> if there is no hierarchy.
        /// </summary>
        char? Delimiter { get; }

        /// <summary>
        /// Gets the contained mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Asynchronously gets the contained mailboxes.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Gets the contained subscribed mailboxes.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all .</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <remarks>
        /// Mailboxes that do not exist may be returned.
        /// Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist as mailboxes on the server.
        /// </remarks>
        List<cMailbox> Subscribed(bool pDescend, fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Asynchronously gets the contained subscribed mailboxes.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Subscribed(bool, fMailboxCacheDataSets)" select="returns|remarks"/>
        Task<List<cMailbox>> SubscribedAsync(bool pDescend, fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Creates a mailbox in the container.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        cMailbox CreateChild(string pName, bool pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a mailbox in the container.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent);
    }
}