using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataListMailbox : cResponseData
            {
                public readonly cMailboxName MailboxName;
                public readonly fListFlags Flags;
                public readonly bool HasSubscribedChildren;

                public cResponseDataListMailbox(cMailboxName pMailboxName, fListFlags pFlags, bool pHasSubscribedChildren)
                {
                    MailboxName = pMailboxName;
                    Flags = pFlags;
                    HasSubscribedChildren = pHasSubscribedChildren;
                }

                public override string ToString() => $"{nameof(cResponseDataListMailbox)}({MailboxName},{Flags},{HasSubscribedChildren})";
            }
        }
    }
}
