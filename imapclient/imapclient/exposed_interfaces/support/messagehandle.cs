using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents an IMAP message uniquely within a message cache.
    /// </summary>
    /// <seealso cref="cMessageHandleList"/>
    /// <seealso cref="cMessageDeliveryEventArgs"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.SetUnseenCount"/>
    /// <seealso cref="cAttachment.MessageHandle"/>
    /// <seealso cref="cMessage.MessageHandle"/>
    /// <seealso cref="cStoreFeedbackItem"/>
    /// <seealso cref="cUIDStoreFeedbackItem"/>
    /// <seealso cref="cSort"/>
    /// <seealso cref="iMessageCache"/>
    /// <seealso cref="cMessagePropertyChangedEventArgs"/>
    /// <seealso cref="cMessageExpungedException"/>
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
        /// Gets the IMAP BODY data, <see langword="null"/> if this data isn't cached.
        /// </summary>
        /// <remarks>
        /// The BODY data is the same as the BODYSTRUCTURE data but is missing the 'extension data'.
        /// In particular the following elements (and the properties that derive from them) will be <see langword="null"/>;
        /// <list type="bullet">
        /// <item><see cref="cMultiPartBody.ExtensionData"/></item>
        /// <item><see cref="cSinglePartBody.ExtensionData"/></item>
        /// </list>
        /// </remarks>
        cBodyPart Body { get; }

        /**<summary>Gets the IMAP BODYSTRUCTURE data, <see langword="null"/> if this data isn't cached.</summary>*/
        cBodyPart BodyStructure { get; }
        /**<summary>Gets the IMAP ENVELOPE data, <see langword="null"/> if this data isn't cached.</summary>*/
        cEnvelope Envelope { get; }
        /**<summary>Gets the current IMAP FLAGS data, <see langword="null"/> if this data isn't cached.</summary>*/
        cFetchableFlags Flags { get; }

        /// <summary>
        /// Gets the RFC 7162 mod-sequence data, <see langword="null"/> if this data isn't cached, may be zero.
        /// </summary>
        /// <remarks>
        /// Zero indicates that either <see cref="cCapabilities.CondStore"/> is not in use or that the mailbox does not support the persistent storage of mod-sequences.
        /// </remarks>
        ulong? ModSeq { get; }

        /**<summary>Gets the IMAP INTERNALDATE, <see langword="null"/> if this data isn't cached.</summary>*/
        DateTimeOffset? ReceivedDateTimeOffset { get; }
        /**<summary>Gets the IMAP INTERNALDATE (in local time if there is usable time zone information), <see langword="null"/> if this data isn't cached.</summary>*/
        DateTime? ReceivedDateTime { get; }

        /**<summary>Gets the IMAP RFC822.SIZE data, <see langword="null"/> if this data isn't cached.</summary>*/
        uint? Size { get; }

        /// <summary>
        /// Gets the UID of the message, may be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates that either the data isn't cached or that the mailbox does not support unique identifiers.
        /// </remarks>
        cUID UID { get; }

        /**<summary>Gets the set of header fields that are currently cached for the message, will be <see langword="null"/> if none have been cached.</summary>*/
        cHeaderFields HeaderFields { get; }
        /**<summary>Gets the binary body-part sizes that are currently cached for the message, will be <see langword="null"/> if none have been cached.</summary>*/
        cBinarySizes BinarySizes { get; }

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
    }
}
