using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents a message in the internal message cache.
    /// </summary>
    public interface iMessageHandle
    {
        /**<summary>Gets the cache that this message handle belongs to.</summary>*/
        iMessageCache Cache { get; }
        /**<summary>Gets the sequence of this message handle in the cache.</summary>*/
        int CacheSequence { get; }
        /**<summary>Indicates if the server has informed the client that the message has been expunged.</summary>*/
        bool Expunged { get; }
        /**<summary>Gets the message attributes that the cache currently contains for this message.</summary>*/
        fCacheAttributes Attributes { get; }
        /**<summary>Gets the IMAP BODY data - i.e. the <see cref="BodyStructure"/> data without the extension data (see <see cref="cBodyPartExtensionData"/>) - if it is cached.</summary>*/
        cBodyPart Body { get; }
        /**<summary>Gets the IMAP BODYSTRUCTURE data if it is cached.</summary>*/
        cBodyPart BodyStructure { get; }
        /**<summary>Gets the IMAP ENVELOPE data if it is cached.</summary>*/
        cEnvelope Envelope { get; }
        /**<summary>Gets the IMAP FLAGS data if it is cached.</summary>*/
        cFetchableFlags Flags { get; }
        /**<summary>Gets the RFC 7162 modification sequence if it is cached. This may be zero if the mailbox does not support CONDSTORE.</summary>*/
        ulong? ModSeq { get; }
        /**<summary>Gets the IMAP INTERNALDATE data if it is cached.</summary>*/
        DateTime? Received { get; }
        /**<summary>Gets the IMAP RFC822.SIZE data if it is cached.</summary>*/
        uint? Size { get; }
        /**<summary>Gets the UID of the message if it is cached.</summary>*/
        cUID UID { get; }
        /**<summary>Gets the set of header fields that are cached for the message, may be <see langword="null"/> if none have been cached.</summary>*/
        cHeaderFields HeaderFields { get; }
        /**<summary>Gets the binary part sizes that are cached for the message, may be <see langword="null"/> if none have been cached.</summary>*/
        cBinarySizes BinarySizes { get; }

        /// <summary>
        /// Determines if all the specified items are cached.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        bool Contains(cCacheItems pItems);

        /// <summary>
        /// Determines if none of the specified items are cached.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        bool ContainsNone(cCacheItems pItems);

        /// <summary>
        /// Returns those items from the specified items that are not cached.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        cCacheItems Missing(cCacheItems pItems);
    }
}
