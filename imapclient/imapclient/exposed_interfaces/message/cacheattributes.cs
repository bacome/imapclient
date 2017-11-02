using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A set of IMAP message attributes
    /// </summary>
    [Flags]
    public enum fCacheAttributes
    {
        flags = 1 << 0,
        envelope = 1 << 1,
        received = 1 << 2,
        size = 1 << 3,
        body = 1 << 4,
        bodystructure = 1 << 5,
        uid = 1 << 6,
        modseq = 1 << 7,
        allmask = 0b11111111,
        // macros from rfc3501
        macrofast = flags | received | size,
        macroall = flags | envelope | received | size,
        macrofull = flags | envelope | received | size | body
    }
}
