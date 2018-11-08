using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private interface iSelectedMailboxCache
        {
            uint UIDValidity { get; }
            bool NoModSeq { get; }
            iMailboxHandle MailboxHandle { get; }
            void SetSynchronised(cTrace.cContext pParentContext);
        }
    }
}