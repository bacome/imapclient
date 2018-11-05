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
        /**<summary>The envelope structure of the message.</summary>*/
        envelope = 1 << 2,
        /**<summary>The internal date of the message.</summary>*/
        received = 1 << 3,
        /**<summary>The RFC 2822 size of the message.</summary>*/
        size = 1 << 4,
        /**<summary>The MIME body structure of the message.</summary>*/
        bodystructure = 1 << 5
    }
}
