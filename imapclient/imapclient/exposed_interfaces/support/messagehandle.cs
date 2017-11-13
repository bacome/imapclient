using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents an message in the internal message cache.
    /// </summary>
    public interface iMessageHandle
    {
        /**<summary>The cache that this message belongs to.</summary>*/
        iMessageCache Cache { get; }
        /**<summary>The sequence of this message in the cache.</summary>*/
        int CacheSequence { get; }
        /**<summary>True if the server has indicated that the message has been expunged.</summary>*/
        bool Expunged { get; }
        /**<summary>The message attributes that the cache currently contains for this message.</summary>*/
        fCacheAttributes Attributes { get; }
        /**<summary>The IMAP BODY data - i.e. the <see cref="BodyStructure"/> data without the extension data (see <see cref="cBodyPartExtensionData"/>).</summary>*/
        cBodyPart Body { get; }
        /**<summary>The IMAP BODYSTRUCTURE data.</summary>*/
        cBodyPart BodyStructure { get; }
        /**<summary>The IMAP ENVELOPE data.</summary>*/
        cEnvelope Envelope { get; }
        /**<summary>The IMAP FLAGS data.</summary>*/
        cFetchableFlags Flags { get; }
        /**<summary>The RFC 7162 modification sequence. This may be zero if the mailbox does not support CONDSTORE.</summary>*/
        ulong? ModSeq { get; }
        /**<summary>The IMAP INTERNALDATE data.</summary>*/
        DateTime? Received { get; }
        /**<summary>The IMAP RFC822.SIZE data.</summary>*/
        uint? Size { get; }
        /**<summary>The UID of the message.</summary>*/
        cUID UID { get; }
        /**<summary>A (possibly partial) set of header fields for the message.</summary>*/
        cHeaderFields HeaderFields { get; }
        /**<summary>A (possiblity partial) set of binary part sizes for the message.</summary>*/
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
