using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fSelectOptions
    {
        forupdate = 1,
        maintainunseencount = 1 << 1,
        maintainuidnext = 1 << 2,
    }
}