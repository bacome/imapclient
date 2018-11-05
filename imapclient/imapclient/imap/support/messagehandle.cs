using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents an IMAP message uniquely within a message cache.
    /// </summary>
    public interface iMessageHandle
    {
        /**<summary>Gets the message cache that the instance belongs to.</summary>*/
        iMessageCache MessageCache { get; }

        /**<summary>Gets the message's sequence in the <see cref="MessageCache"/>.</summary>*/
        int CacheSequence { get; }

        /**<summary>Indicates whether the message exists on the server.</summary>*/
        bool Expunged { get; }

        /**<summary>Gets the attributes that the message cache currently contains for the message.</summary>*/
        fMessageCacheAttributes Attributes { get; }

        /// <summary>
        /// Determines whether all the specified items are cached for the message.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        bool Contains(cMessageCacheItems pItems);

        /// <summary>
        /// Determines whether none of the specified items are cached for the message.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        bool ContainsNone(cMessageCacheItems pItems);

        /// <summary>
        /// Returns those items from the specified items that are not cached for the message.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        cMessageCacheItems Missing(cMessageCacheItems pItems);

        /// <summary>
        /// Gets the MessageUID of the message, may be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that either the data isn't cached or that the mailbox does not support unique identifiers.
        /// </remarks>
        cMessageUID MessageUID { get; }

        /// <summary>
        /// Gets the RFC 7162 mod-sequence data and the IMAP FLAGS data, <see langword="null"/> if this data isn't cached.
        /// </summary>
        cModSeqFlags ModSeqFlags { get; }

        /**<summary>Gets the IMAP ENVELOPE data, <see langword="null"/> if this data isn't cached.</summary>*/
        cEnvelope Envelope { get; }

        /**<summary>Gets the IMAP INTERNALDATE, <see langword="null"/> if this data isn't cached.</summary>*/
        cTimestamp Received { get; }

        /**<summary>Gets the IMAP RFC822.SIZE data, <see langword="null"/> if this data isn't cached.</summary>*/
        uint? Size { get; }

        /**<summary>Gets the IMAP BODYSTRUCTURE data, <see langword="null"/> if this data isn't cached.</summary>*/
        cBodyPart BodyStructure { get; }

        /**<summary>Gets the set of header fields that are currently cached for the message, will be <see langword="null"/> if none have been cached.</summary>*/
        cHeaderFields HeaderFields { get; }

        /**<summary>Gets the binary body-part sizes that are currently cached for the message, will be <see langword="null"/> if none have been cached.</summary>*/
        cBinarySizes BinarySizes { get; }
    }
}
