using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP namespace.
    /// </summary>
    /// <seealso cref="cIMAPClient.Namespaces"/>
    public class cNamespace : iMailboxContainer, IEquatable<cNamespace>
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
        public List<cMailbox> GetMailboxes(fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cNamespace), nameof(GetMailboxes), pDataSets);
            var lTask = Client.GetMailboxesAsync(NamespaceName, pDataSets, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets the mailboxes at the top level of hierarchy in the namespace.
        /// </summary>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        public Task<List<cMailbox>> GetMailboxesAsync(fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cNamespace), nameof(GetMailboxesAsync), pDataSets);
            return Client.GetMailboxesAsync(NamespaceName, pDataSets, lContext);
        }

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
        public List<cMailbox> GetSubscribed(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cNamespace), nameof(GetSubscribed), pDescend, pDataSets);
            var lTask = Client.GetSubscribedAsync(NamespaceName, pDescend, pDataSets, lContext);
            Client.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Asynchronously gets the subscribed mailboxes in the namespace.
        /// </summary>
        /// <param name="pDescend">If <see langword="true"/> all subscribed mailboxes in the namespace are returned, if <see langword="false"/> only mailboxes at the top level of hierarchy are returned.</param>
        /// <param name="pDataSets">The sets of data to fetch into cache for the returned mailboxes.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Subscribed(bool, fMailboxCacheDataSets)" select="returns|remarks"/>
        public Task<List<cMailbox>> GetSubscribedAsync(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cNamespace), nameof(GetSubscribedAsync), pDescend, pDataSets);
            return Client.GetSubscribedAsync(NamespaceName, pDescend, pDataSets, lContext);
        }

        /// <inheritdoc cref="iMailboxContainer.GetMailboxName(string)"/>
        public cMailboxName GetMailboxName(string pName)
        {
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (NamespaceName.Delimiter != null && pName.IndexOf(NamespaceName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(NamespaceName.Prefix + pName, NamespaceName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        /// <summary>
        /// Creates a mailbox at the top level of hierarchy in the namespace.
        /// </summary>
        /// <param name="pName"></param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="cIMAPClient.Create(cMailboxName, bool)" select="remarks"/>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = false) => Client.Create(GetMailboxName(pName), pAsFutureParent);

        /// <summary>
        /// Asynchronously creates a mailbox at the top level of hierarchy in the namespace.
        /// </summary>
        /// <param name="pName"></param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="CreateChild(string, bool)" select="remarks"/>
        public Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent = false) => Client.CreateAsync(GetMailboxName(pName), pAsFutureParent);

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cNamespace pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(iMailboxContainer pObject) => this == pObject as cNamespace;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cNamespace;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Client.GetHashCode();
                lHash = lHash * 23 + NamespaceName.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cMailbox)}({NamespaceName})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cNamespace pA, cNamespace pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Client.Equals(pB.Client) && pA.NamespaceName.Equals(pB.NamespaceName);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cNamespace pA, cNamespace pB) => !(pA == pB);
    }
}