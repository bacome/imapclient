using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fEnableableExtensions
    {
        none = 0,
        utf8 = 1,
        // more here as required
        all = 0b1
    }
}