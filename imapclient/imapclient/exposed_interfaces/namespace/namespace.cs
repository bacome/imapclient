using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>Provides an API that allows interaction with an IMAP namespace.</para>
    /// </summary>
    public class cNamespace : iMailboxParent
    {
        public readonly cIMAPClient Client;

        /// <summary>
        /// The namespace name.
        /// </summary>
        public readonly cNamespaceName NamespaceName;

        public cNamespace(cIMAPClient pClient, cNamespaceName pNamespaceName)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            NamespaceName = pNamespaceName ?? throw new ArgumentNullException(nameof(pNamespaceName));
        }

        /// <summary>
        /// The name prefix of the namespace. May be the empty string.
        /// </summary>
        public string Prefix => NamespaceName.Prefix;

        /// <summary>
        /// The namespace hierarchy delimiter. May be null if there is no hierarchy.
        /// </summary>
        public char? Delimiter => NamespaceName.Delimiter;

        /// <summary>
        /// <para>Gets the mailboxes at the top level of hierarchy in the namespace.</para>
        /// </summary>
        /// <param name="pDataSets">
        /// <para>The sets of data to retrieve when getting the mailboxes.</para>
        /// <para>See <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </param>
        /// <returns>A list of mailboxes.</returns>
        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(NamespaceName, pDataSets);
        /**<summary>The async version of <see cref="Mailboxes(fMailboxCacheDataSets)"/></summary>*/
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(NamespaceName, pDataSets);

        /// <summary>
        /// <para>Gets the subscribed mailboxes in the namespace.</para>
        /// <para>Note that mailboxes that do not currently exist may be returned.</para>
        /// </summary>
        /// <param name="pDescend">If true all subscribed mailboxes are returned, if false only mailboxes at the top level of hierarchy are returned.</param>
        /// <param name="pDataSets">
        /// <para>The sets of data to retrieve when getting the mailboxes.</para>
        /// <para>See <see cref="cIMAPClient.MailboxCacheData"/>.</para>
        /// </param>
        /// <returns>A list of mailboxes.</returns>
        public List<cMailbox> Subscribed(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(NamespaceName, pDescend, pDataSets);
        /**<summary>The async version of <see cref="Subscribed(bool, fMailboxCacheDataSets)"/>.</summary>*/
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(NamespaceName, pDescend, pDataSets);

        /// <summary>
        /// <para>Creates a mailbox at the top level of this namespace.</para>
        /// </summary>
        /// <param name="pName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicate to the IMAP server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns>An object representing the newly created mailbox.</returns>
        public cMailbox CreateChild(string pName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pName), pAsFutureParent);
        /**<summary>The async version of <see cref="CreateChild(string, bool)"/>.</summary>*/
        public Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent = true) => Client.CreateAsync(ZCreateChild(pName), pAsFutureParent);

        private cMailboxName ZCreateChild(string pName)
        {
            if (NamespaceName.Delimiter == null) throw new InvalidOperationException();
            if (string.IsNullOrEmpty(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (pName.IndexOf(NamespaceName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cMailboxName.TryConstruct(NamespaceName.Prefix + pName, NamespaceName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pName));
            return lMailboxName;
        }

        public override string ToString() => $"{nameof(cMailbox)}({NamespaceName})";
    }
}