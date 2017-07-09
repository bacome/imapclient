using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fMailboxCacheProperties
    {
        // NOTE: this enum includes all the values from fMailboxListProperties

            //allmailboxflags = 0b11111111111111,
        //allstatus = messagecount | recentcount | uidnext | newunknownuidcount | uidvalidity | unseencount | unseenunknowncount | highestmodseq

        isselected = 1 << 22,
        isselectedforupdate = 1 << 23,
        isaccessreadonly = 1 << 24,

        messageflags = 1 << 25,
        forupdatepermanentflags = 1 << 26,
        readonlypermanentflags = 1 << 27
    }
}