using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents an IMAP mailbox cache.
    /// </summary>
    public interface iMailboxCache
    {
        cAccountId AccountId { get; }
    }
}