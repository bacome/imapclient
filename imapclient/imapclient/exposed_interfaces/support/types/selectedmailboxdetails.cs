using System;

namespace work.bacome.imapclient.support
{
    public interface iSelectedMailboxDetails
    {
        iMailboxHandle Handle { get; }
        bool SelectedForUpdate { get; }
        bool AccessReadOnly { get; }
    }
}