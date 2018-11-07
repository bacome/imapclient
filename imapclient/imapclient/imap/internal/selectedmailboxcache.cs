using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private interface iSelectedMailboxCache
        {
            uint UIDValidity { get; }
            bool NoModSeq { get; }
            void SetSynchronised(cTrace.cContext pParentContext);
        }
    }
}