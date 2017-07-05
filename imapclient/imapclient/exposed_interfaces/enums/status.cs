using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fStatusAttributes
    {
        // this lists the attributes that the status must have filled in accurately
        none = 0,
        clientdefault = 1,
        messages = 1 << 1,
        recent = 1 << 2,
        uidnext = 1 << 3,
        uidvalidity = 1 << 4,
        unseen = 1 << 5,
        highestmodseq = 1 << 6,
        all = 0b1111110
    }
}