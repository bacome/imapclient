using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP namespace.
    /// </summary>
    /// <seealso cref="cIMAPClient.Namespaces"/>
    public class cNamespace : iMailboxContainer
    {
        /**<summary>The client that this instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The namespace's name.</summary>*/
        public readonly cNamespaceName NamespaceName;

        internal cNamespace(cIMAPClient pClient, cNamespaceName pNamespaceName)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            NamespaceName = pNamespaceName ?? throw new ArgumentNullException(nameof(pNamespaceName));
        }

        /// <summary>
        /// Gets the name prefix of the namespace. May be <see cref="String.Empty"/>.
        /// </summary>
        public string Prefix => NamespaceName.Prefix;

        /// <summary>
        /// Gets the hierarchy delimiter used in the namespace. May be <see langword="null"/>. 
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> if the server has no hierarchy in its names.
        /// </remarks>
        public char? Delimiter => NamespaceName.Delimiter;

        /// <summary>
        /// Gets the mailboxes at the top level of hierarchy in the namespace.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(NamespaceName, pDataSets);

        /// <summary>
        /// Asynchronously gets the mailboxes at the top level of hierarchy in the namespace.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(NamespaceName, pDataSets);

        /// <summary>
        /// Gets the subscribed mailboxes in the namespace. 
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all subscribed mailboxes in the namespace are returned, if <see langword="false"/> only mailboxes at the top level of hierarchy are returned.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <remarks>
        /// Mailboxes that do not exist may be returned.
        /// Subscribed mailboxes and levels in the mailbox hierarchy do not necessarily exist as mailboxes on the server.
        /// </remarks>
        public List<cMailbox> Subscribed(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(NamespaceName, pDescend, pDataSets);

        /// <summary>
        /// Asynchronously gets the subscribed mailboxes in the namespace.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all subscribed mailboxes in the namespace are returned, if <see langword="false"/> only mailboxes at the top level of hierarchy are returned.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Subscribed(bool, fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(NamespaceName, pDescend, pDataSets);

        /// <summary>
        /// Creates a mailbox at the top level of hierarchy in the namespace.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="cIMAPClient.Create(cMailboxName, bool)" select="remarks"/>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pName), pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a mailbox at the top level of hierarchy in the namespace.
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="CreateChild(string, bool)" select="remarks"/>
        public Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent = true) => Client.CreateAsync(ZCreateChild(pName), pAsFutureParent);

        private cMailboxName ZCreateChild(string pName)
        {
            if (NamespaceName.Delimiter == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NoMailboxHierarchy);
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (pName.IndexOf(NamespaceName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(NamespaceName.Prefix + pName, NamespaceName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cMailbox)}({NamespaceName})";
    }
}