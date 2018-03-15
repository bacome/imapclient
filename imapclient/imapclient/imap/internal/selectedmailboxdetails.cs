using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal interface iSelectedMailboxDetails
    {
        iMailboxHandle MailboxHandle { get; }
        bool SelectedForUpdate { get; }
        bool AccessReadOnly { get; }
        iMessageCache MessageCache { get; }
    }
}