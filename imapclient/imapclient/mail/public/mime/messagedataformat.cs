using System;

namespace work.bacome.mailclient
{
    [Flags]
    public enum fMessageDataFormat
    {
        sevenbit = 0,
        eightbit = 1 << 0, // 8bit MIME (RFC 2045, 6152)
        binary = 1 << 1, // binary MIME (RFC 2045, 3030, 3516) [requires eightbit]
        utf8headers = 1 << 2, // internationalised email headers (RFC 6531, 6532, 6855) [requires eightbit]
    }
}
