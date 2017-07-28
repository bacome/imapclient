using System;

namespace work.bacome.imapclient.support
{
    public interface iSelectedMailboxDetails
    {
        iMailboxHandle Handle { get; }
        bool SelectedForUpdate { get; }
        bool AccessReadOnly { get; }
        bool NoModSeq { get; }
        iMessageCache Cache { get; }
    }
}