using System;

namespace work.bacome.imapclient.support
{
    ;?; // see also
    /// <summary>
    /// Uniquely identifies a message in an internal message cache.
    /// </summary>
    public interface iMessageHandle
    {
        /**<summary>Gets an object that represents the internal message cache that this handle belongs to.</summary>*/
        iMessageCache Cache { get; }
        /**<summary>Gets the sequence in the cache of this handle.</summary>*/
        int CacheSequence { get; }
        /**<summary>Indicates if the message referred to by the handle exists on the server.</summary>*/
        bool Expunged { get; }
        /**<summary>Gets the message attributes that the cache currently contains for this handle.</summary>*/
        fMessageCacheAttributes Attributes { get; }

        /// <summary>
        /// Gets the IMAP BODY data or <see langword="null"/> if this data isn't cached.
        /// </summary>
        /// <remarks>
        /// The BODY data is the same as the BODYSTRUCTURE data but is missing the 'extension data'.
        /// In particular the following elements (and properties that derive from them) will be null;
        /// <list type="bullet">
        /// <item><see cref="cMultiPartBody.ExtensionData"/></item>
        /// <item><see cref="cSinglePartBody.ExtensionData"/></item>
        /// </list>
        /// </remarks>
        cBodyPart Body { get; }

        /**<summary>Gets the IMAP BODYSTRUCTURE data or <see langword="null"/> if this data isn't cached.</summary>*/
        cBodyPart BodyStructure { get; }
        /**<summary>Gets the IMAP ENVELOPE data or <see langword="null"/> if this data isn't cached.</summary>*/
        cEnvelope Envelope { get; }
        /**<summary>Gets the IMAP FLAGS data or <see langword="null"/> if this data isn't cached.</summary>*/
        cFetchableFlags Flags { get; }
        /**<summary>Gets the modseq or <see langword="null"/> if this data isn't cached. This will be zero if <see cref="cCapabilities.CondStore"/> is not in use or the mailbox does not support the persistent storage of mod-sequences</summary>*/
        ulong? ModSeq { get; }
        /**<summary>Gets the IMAP INTERNALDATE data or <see langword="null"/> if this data isn't cached.</summary>*/
        DateTime? Received { get; }
        /**<summary>Gets the IMAP RFC822.SIZE data or <see langword="null"/> if this data isn't cached.</summary>*/
        uint? Size { get; }
        /**<summary>Gets the UID of the message or <see langword="null"/> if this data isn't cached or the the mailbox does not support unique identifiers</summary>*/
        cUID UID { get; }
        /**<summary>Gets the set of header fields that are cached for the message, may be <see langword="null"/> if none have been cached.</summary>*/
        cHeaderFields HeaderFields { get; }
        /**<summary>Gets the binary part sizes that are cached for the message, may be <see langword="null"/> if none have been cached.</summary>*/
        cBinarySizes BinarySizes { get; }

        /// <summary>
        /// Determines if all the specified items are cached for this handle.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        bool Contains(cMessageCacheItems pItems);

        /// <summary>
        /// Determines if none of the specified items are cached for this handle.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        bool ContainsNone(cMessageCacheItems pItems);

        /// <summary>
        /// Returns those items from the specified items that are not cached for this handle.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns></returns>
        cMessageCacheItems Missing(cMessageCacheItems pItems);
    }
}
