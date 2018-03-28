using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace work.bacome.mailclient
{
    internal class cMailMessageList : List<MailMessage>
    {
        public cMailMessageList() { }
        public cMailMessageList(IEnumerable<MailMessage> pMessages) : base(pMessages) { }

        public override string ToString() => $"{nameof(cMailMessageList)}({Count})";

        public static cMailMessageList FromMessage(MailMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            var lResult = new cMailMessageList();
            lResult.Add(pMessage);
            return lResult;
        }

        public static cMailMessageList FromMessages(IEnumerable<MailMessage> pMessages)
        {
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            var lResult = new cMailMessageList();

            foreach (var lMessage in pMessages)
            {
                if (lMessage == null) throw new ArgumentOutOfRangeException(nameof(pMessages), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lResult.Add(lMessage);
            }

            return lResult;
        }
    }
}