using System;

namespace work.bacome.mailclient
{
    internal enum eContentTransferEncoding
    {
        sevenbit,
        eightbit,
        binary,
        quotedprintable,
        base64,
        text // choose 7bit, 8bit, quotedprintable or base64 based on the content
    }
}