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
        encoded // choose quotedprintable or base64
    }
}