using System;

namespace work.bacome.imapclient.support
{
    public interface iSelectedMailboxDetails
    {
        cMailboxId MailboxId { get; }
        bool SelectedForUpdate { get; }
        bool AccessReadOnly { get; }
    }
}