using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fFetchAttributes
    {
        none = 0,
        clientdefault = 1,
        flags = 1 << 1,
        envelope = 1 << 2,
        received = 1 << 3,
        size = 1 << 4,
        body = 1 << 5,
        bodystructure = 1 << 6,
        uid = 1 << 7,
        references = 1 << 8,
        allmask = 0b111111110,
        // macros from rfc3501
        macrofast = flags | received | size,
        macroall = flags | envelope | received | size,
        macrofull = flags | envelope | received | size | body
    }
}
