using System;

namespace work.bacome.imapclient.support
{
    public interface iMessageCache
    {
        iMailboxHandle MailboxHandle { get; }
        bool NoModSeq { get; }
        int MessageCount { get; }
        int RecentCount { get; }
        uint UIDNext { get; }
        int UIDNextUnknownCount { get; }
        uint UIDValidity { get; }
        int UnseenCount { get; }
        int UnseenUnknownCount { get; }
        ulong HighestModSeq { get; }
    }
}
