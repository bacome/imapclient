using System;

namespace work.bacome.mailclient
{
    [Flags]
    public enum fMessageDataFormat
    {
        eightbit = 0b001, // 8bit MIME (RFC 2045, 6152)
        binary = 0b011, // binary MIME (RFC 2045, 3030, 3516) [requires eightbit]
        utf8headers = 0b101 // internationalised email headers (RFC 6531, 6532, 6855) [requires eightbit]
    }
}
