using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of IMAP message attributes that can be cached.
    /// </summary>
    /// <seealso cref="cMessageCacheItems"/>
    /// <seealso cref="cSort.Attributes(out bool)"/>
    /// <seealso cref="cSortItem"/>
    /// <seealso cref="iMessageHandle.Attributes"/>
    [Flags]
    public enum fMessageCacheAttributes
    {
        /**<summary>The flags that are set for the message.</summary>*/
        flags = 1 << 0,
        /**<summary>The envelope structure of the message.</summary>*/
        envelope = 1 << 1,
        /**<summary>The internal date of the message.</summary>*/
        received = 1 << 2,
        /**<summary>The RFC 2822 size of the message.</summary>*/
        size = 1 << 3,
        /**<summary>The non-extensible form of <see cref="bodystructure"/>.</summary>*/
        body = 1 << 4,
        /**<summary>The MIME body structure of the message.</summary>*/
        bodystructure = 1 << 5,
        /**<summary>The unique identifier of the message.</summary>*/
        uid = 1 << 6,
        /**<summary>The mod-sequence of the message.</summary>*/
        modseq = 1 << 7,
        // macros from rfc3501
        /**<summary>The IMAP FAST macro (equivalent to: <see cref="flags"/>, <see cref="received"/> and <see cref="size"/>).</summary>*/
        macrofast = flags | received | size,
        /**<summary>The IMAP ALL macro (equivalent to: <see cref="flags"/>, <see cref="received"/>, <see cref="size"/> and <see cref="envelope"/>).</summary>*/
        macroall = flags | envelope | received | size,
        /**<summary>The IMAP FULL macro (equivalent to: <see cref="flags"/>, <see cref="received"/>, <see cref="size"/>, <see cref="envelope"/> and <see cref="body"/>).</summary>*/
        macrofull = flags | envelope | received | size | body
    }
}
