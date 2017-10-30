using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataLSub : cResponseData
            {
                public readonly cMailboxName MailboxName;
                public readonly bool Subscribed;

                public cResponseDataLSub(cMailboxName pMailboxName, bool pSubscribed)
                {
                    MailboxName = pMailboxName;
                    Subscribed = pSubscribed;
                }

                public override string ToString() => $"{nameof(cResponseDataLSub)}({MailboxName},{Subscribed})";
            }
        }
    }
}