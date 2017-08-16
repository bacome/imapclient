using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public interface iMessageCache : IReadOnlyList<iMessageHandle>
    {
        iMailboxHandle MailboxHandle { get; }
        bool NoModSeq { get; }
        int RecentCount { get; }
        uint UIDNext { get; }
        int UIDNextUnknownCount { get; }
        uint UIDValidity { get; }
        int UnseenCount { get; }
        int UnseenUnknownCount { get; }
        ulong HighestModSeq { get; }
    }
}
