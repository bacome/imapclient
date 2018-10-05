using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of IMAP message attributes that can be cached.
    /// </summary>
    [Flags]
    public enum fMessageCacheAttributes
    {
        /**<summary>The unique identifier of the message.</summary>*/
        uid = 1 << 0,
        /**<summary>The mod-sequence and flags of the message.</summary>*/
        modseqflags = 1 << 1,
        /**<summary>The non-extensible form of <see cref="bodystructure"/>.</summary>*/
        body = 1 << 2,
        /**<summary>The envelope structure of the message.</summary>*/
        envelope = 1 << 3,
        /**<summary>The internal date of the message.</summary>*/
        received = 1 << 4,
        /**<summary>The RFC 2822 size of the message.</summary>*/
        size = 1 << 5,
        /**<summary>The MIME body structure of the message.</summary>*/
        bodystructure = 1 << 6,
        // macros from rfc3501
        /**<summary>The IMAP FAST macro (equivalent to: <see cref="flags"/>, <see cref="received"/> and <see cref="size"/>).</summary>*/
        macrofast = modseqflags | received | size,
        /**<summary>The IMAP ALL macro (equivalent to: <see cref="flags"/>, <see cref="received"/>, <see cref="size"/> and <see cref="envelope"/>).</summary>*/
        macroall = modseqflags | envelope | received | size,
        /**<summary>The IMAP FULL macro (equivalent to: <see cref="flags"/>, <see cref="received"/>, <see cref="size"/>, <see cref="envelope"/> and <see cref="body"/>).</summary>*/
        macrofull = modseqflags | envelope | received | size | body
    }
}
