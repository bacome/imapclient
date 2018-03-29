using System;

namespace work.bacome.mailclient
{
    internal enum eContentTransferEncoding
    {
        notspecified,
        sevenbit,
        eightbit,
        binary,
        quotedprintable,
        base64,
        text // choose 7bit, quotedprintable or base64 based on the content
    }
}