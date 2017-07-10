using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fFetchAttributes
    {
        flags = 1 << 0,
        envelope = 1 << 1,
        received = 1 << 2,
        size = 1 << 3,
        body = 1 << 4,
        bodystructure = 1 << 5,
        uid = 1 << 6,
        references = 1 << 7,
        modseq = 1 << 8,
        allmask = 0b111111111,
        // macros from rfc3501
        macrofast = flags | received | size,
        macroall = flags | envelope | received | size,
        macrofull = flags | envelope | received | size | body
    }
}
