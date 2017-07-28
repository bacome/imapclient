using System;

namespace work.bacome.imapclient.support
{
    public interface iMessageCache
    {
        iMailboxHandle MailboxHandle { get; }
        uint UIDValidity { get; }
    }
}
