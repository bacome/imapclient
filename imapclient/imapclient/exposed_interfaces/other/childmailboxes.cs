using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an instance that has child mailboxes.
    /// </summary>
    /// <seealso cref="cNamespace"/>
    /// <seealso cref="cMailbox"/>
    public interface iMailboxParent
    {
        /// <summary>
        /// Gets the hierarchy delimiter. May be null if there is no hierarchy.
        /// </summary>
        char? Delimiter { get; }

        /// <summary>
        /// Gets the mailboxes at the top level of hierarchy.
        /// </summary>
        /// <param name="pDataSets">The sets of data to request when getting the mailboxes.</param>
        /// <returns></returns>
        List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Asynchronously gets the mailboxes at the top level of hierarchy.
        /// </summary>
        /// <param name="pDataSets">The sets of data to request when getting the mailboxes.</param>
        /// <returns></returns>
        Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Gets the subscribed mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If true all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to request when getting the mailboxes.</param>
        /// <returns></returns>
        List<cMailbox> Subscribed(bool pDescend, fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Asynchronously gets the subscribed mailboxes. Note that mailboxes that do not currently exist may be returned.
        /// </summary>
        /// <param name="pDescend">If true all descendants are returned (not just children, but also grandchildren ...).</param>
        /// <param name="pDataSets">The sets of data to request when getting the mailboxes.</param>
        /// <returns></returns>
        Task<List<cMailbox>> SubscribedAsync(bool pDescend, fMailboxCacheDataSets pDataSets = 0);

        /// <summary>
        /// Creates a child mailbox.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        cMailbox CreateChild(string pName, bool pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a child mailbox.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent);
    }
}